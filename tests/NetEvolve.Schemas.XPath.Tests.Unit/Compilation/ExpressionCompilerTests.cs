namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class ExpressionCompilerTests
{
    private static readonly XPathQName X = new(string.Empty, "x");

    [Test]
    public async Task Compile_HandBuiltTree_LowersToExecutableTreeThroughFakeLowerings()
    {
        var tree = new SequenceExpression(
            new SourceSpan(0, 5),
            [new IntegerLiteral(new SourceSpan(0, 1), "1"), new IntegerLiteral(new SourceSpan(4, 1), "2")]
        );

        var ok = ExpressionCompiler.TryCompile(
            tree,
            "1 , 2",
            XPathStaticContext.Default,
            FakeLowerings.Registry(),
            out var compiled,
            out _
        );

        await Assert.That(ok).IsTrue();
        var root = compiled!.Root as FakeLowerings.SequenceNode;
        await Assert.That(root).IsNotNull();
        await Assert.That(root!.Items.Count).IsEqualTo(2);
        await Assert.That(root.Items[0]).IsTypeOf<FakeLowerings.LiteralNode>();
        await Assert.That(compiled.Source).IsEqualTo("1 , 2");
    }

    [Test]
    public async Task Compile_UndefinedVariable_RaisesXpst0008WithLocation()
    {
        var tree = new VariableReference(new SourceSpan(3, 2), new QualifiedName(new SourceSpan(4, 1), null, "x"));

        var ok = ExpressionCompiler.TryCompile(
            tree,
            "1 + $x",
            XPathStaticContext.Default,
            FakeLowerings.Registry(),
            out var compiled,
            out var diagnostic
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0008");
        await Assert.That(diagnostic.Offset).IsEqualTo(3);
        await Assert.That(diagnostic.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Compile_UnknownFunction_RaisesXpst0017WithLocation()
    {
        var tree = new FunctionCall(new SourceSpan(0, 6), new QualifiedName(new SourceSpan(0, 3), null, "f"), []);

        var ok = ExpressionCompiler.TryCompile(
            tree,
            "f()",
            XPathStaticContext.Default,
            FakeLowerings.Registry(),
            out _,
            out var diagnostic
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0017");
        await Assert.That(diagnostic.Offset).IsEqualTo(0);
        await Assert.That(diagnostic.Length).IsEqualTo(6);
    }

    [Test]
    public async Task Compile_WrongArity_RaisesXpst0017()
    {
        var context = XPathStaticContext
            .Default.ToBuilder()
            .WithNamespace("f", FakeFunction.Namespace)
            .WithFunction(new FakeFunction("one", 1, 1, "r"))
            .Build();
        var tree = new FunctionCall(new SourceSpan(0, 5), new QualifiedName(new SourceSpan(0, 5), "f", "one"), []);

        var ok = ExpressionCompiler.TryCompile(tree, "f:one()", context, FakeLowerings.Registry(), out _, out var d);

        await Assert.That(ok).IsFalse();
        await Assert.That(d.Code).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task Compile_KnownFunction_Binds()
    {
        var context = XPathStaticContext
            .Default.ToBuilder()
            .WithNamespace("f", FakeFunction.Namespace)
            .WithFunction(new FakeFunction("one", 1, 1, "r"))
            .Build();
        var tree = new FunctionCall(
            new SourceSpan(0, 8),
            new QualifiedName(new SourceSpan(0, 5), "f", "one"),
            [new IntegerLiteral(new SourceSpan(6, 1), "1")]
        );

        var ok = ExpressionCompiler.TryCompile(tree, "f:one(1)", context, FakeLowerings.Registry(), out var c, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(c!.Root).IsTypeOf<FakeLowerings.CallNode>();
    }

    [Test]
    public async Task Compile_UndeclaredPrefix_RaisesXpst0081WithLocation()
    {
        var tree = new FunctionCall(new SourceSpan(2, 7), new QualifiedName(new SourceSpan(2, 4), "q", "f"), []);

        var ok = ExpressionCompiler.TryCompile(
            tree,
            "1 q:f()",
            XPathStaticContext.Default,
            FakeLowerings.Registry(),
            out _,
            out var diagnostic
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0081");
        await Assert.That(diagnostic.Offset).IsEqualTo(2);
        await Assert.That(diagnostic.Length).IsEqualTo(7);
    }

    [Test]
    public async Task Compile_UndeclaredVariablePrefix_RaisesXpst0081()
    {
        var tree = new VariableReference(new SourceSpan(0, 4), new QualifiedName(new SourceSpan(1, 3), "q", "x"));

        var ok = ExpressionCompiler.TryCompile(
            tree,
            "$q:x",
            XPathStaticContext.Default,
            FakeLowerings.Registry(),
            out _,
            out var diagnostic
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0081");
    }

    [Test]
    public async Task Compile_NodeKindWithoutLowering_FailsNamingTheKind()
    {
        var tree = new StringLiteral(new SourceSpan(1, 3), "a");

        var ok = ExpressionCompiler.TryCompile(
            tree,
            " 'a'",
            XPathStaticContext.Default,
            new LoweringRegistry(),
            out _,
            out var diagnostic
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(diagnostic.Message).Contains("StringLiteral");
        await Assert.That(diagnostic.Offset).IsEqualTo(1);
        await Assert.That(diagnostic.Length).IsEqualTo(3);
    }

    [Test]
    public async Task Register_SameNodeKindTwice_Throws()
    {
        var registry = FakeLowerings.Registry();

        await Assert
            .That(() => registry.Register<IntegerLiteral>(static (n, _) => new FakeLowerings.LiteralNode(n.Lexeme)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Compile_BoundVariable_IsInScopeOnlyInsideTheScope()
    {
        var registry = FakeLowerings.Registry();
        registry.Register<ParenthesizedExpression>(
            static (n, c) =>
            {
                using (c.DeclareVariable(new QualifiedName(n.Location, null, "i")))
                {
                    return c.Lower(n.Inner!);
                }
            }
        );
        var reference = new VariableReference(new SourceSpan(0, 2), new QualifiedName(new SourceSpan(0, 2), null, "i"));
        var scoped = new ParenthesizedExpression(new SourceSpan(0, 4), reference);

        var inside = ExpressionCompiler.TryCompile(scoped, "($i)", XPathStaticContext.Default, registry, out _, out _);
        var outside = ExpressionCompiler.TryCompile(
            reference,
            "$i",
            XPathStaticContext.Default,
            registry,
            out _,
            out var d
        );

        await Assert.That(inside).IsTrue();
        await Assert.That(outside).IsFalse();
        await Assert.That(d.Code).IsEqualTo("XPST0008");
    }

    [Test]
    public async Task CompiledTypes_HaveNoMutableInstanceField()
    {
        var types = new List<Type> { typeof(CompiledExpression), typeof(ExecutableNode) };
        foreach (var type in typeof(FakeLowerings).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (typeof(ExecutableNode).IsAssignableFrom(type))
            {
                types.Add(type);
            }
        }

        var mutable = new List<string>();
        foreach (var type in types)
        {
            foreach (
                var field in type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                )
            )
            {
                if (!field.IsInitOnly)
                {
                    mutable.Add($"{type.Name}.{field.Name}");
                }
            }
        }

        await Assert.That(types.Count).IsGreaterThan(2);
        await Assert.That(mutable).IsEmpty();
    }

    [Test]
    public async Task Evaluate_OneCompiledExpressionFromSixteenThreads_ReturnsEachContextsResult()
    {
        var context = XPathStaticContext.Default.ToBuilder().WithVariable(X).Build();
        var tree = new VariableReference(new SourceSpan(0, 2), new QualifiedName(new SourceSpan(1, 1), null, "x"));
        ExpressionCompiler.TryCompile(tree, "$x", context, FakeLowerings.Registry(), out var compiled, out _);

        var tasks = new Task<long>[16];
        for (var t = 0; t < tasks.Length; t++)
        {
            var value = t;
            tasks[t] = Task.Run(() =>
            {
                using var evaluation = new XPathEvaluationContext();
                long last = -1;
                for (var i = 0; i < 1000; i++)
                {
                    evaluation.Variables.Set(X, XdmItem.FromInteger((value * 1000L) + i));
                    compiled!.Evaluate(evaluation)[0].TryGetInteger(out last);
                    if (last != (value * 1000L) + i)
                    {
                        return -1;
                    }
                }

                return last;
            });
        }

        var results = await Task.WhenAll(tasks);

        for (var t = 0; t < results.Length; t++)
        {
            await Assert.That(results[t]).IsEqualTo((t * 1000L) + 999);
        }
    }
}
