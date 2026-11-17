namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.XPath.Syntax.Tree;

public class VisitorTests
{
    internal static List<Type> NodeKinds()
    {
        var kinds = new List<Type>();
        foreach (var type in typeof(SyntaxNode).Assembly.GetTypes())
        {
            if (
                type.IsClass
                && !type.IsAbstract
                && typeof(SyntaxNode).IsAssignableFrom(type)
                && string.Equals(type.Namespace, typeof(SyntaxNode).Namespace, StringComparison.Ordinal)
            )
            {
                kinds.Add(type);
            }
        }

        return kinds;
    }

    [Test]
    public async Task NodeKinds_ReflectionFindsAtLeastTheXPath20Forms() =>
        await Assert.That(NodeKinds().Count).IsGreaterThanOrEqualTo(40);

    [Test]
    public async Task CountingVisitor_OverAllKindsTree_VisitsEveryKindAndNothingTwice()
    {
        var factory = new TreeFactory();
        var root = factory.AllKinds();
        var visitor = new CountingVisitor();

        root.Accept(visitor);

        var expected = new Dictionary<Type, int>();
        foreach (var node in TreeWalker.Descendants(root))
        {
            expected[node.GetType()] = expected.TryGetValue(node.GetType(), out var count) ? count + 1 : 1;
        }

        var unvisitedKinds = new List<string>();
        foreach (var kind in NodeKinds())
        {
            if (!visitor.Counts.ContainsKey(kind))
            {
                unvisitedKinds.Add(kind.Name);
            }
        }

        await Assert.That(unvisitedKinds).IsEmpty();
        await Assert.That(visitor.Counts.Count).IsEqualTo(expected.Count);
        foreach (var pair in expected)
        {
            await Assert.That(visitor.Counts[pair.Key]).IsEqualTo(pair.Value);
        }
    }

    [Test]
    public async Task Visitors_HaveOneMethodPerNodeKind()
    {
        var voidMissing = new List<string>();
        var resultMissing = new List<string>();
        foreach (var kind in NodeKinds())
        {
            var name = "Visit" + kind.Name;
            if (typeof(SyntaxVisitor).GetMethod(name, [kind]) is not { } plain || plain.ReturnType != typeof(void))
            {
                voidMissing.Add(name);
            }

            var generic = typeof(SyntaxVisitor<>).GetMethod(name);
            if (
                generic is null
                || generic.GetParameters().Length != 1
                || generic.GetParameters()[0].ParameterType != kind
            )
            {
                resultMissing.Add(name);
            }
        }

        var visitMethods = 0;
        foreach (
            var method in typeof(SyntaxVisitor).GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
            )
        )
        {
            if (method.Name.StartsWith("Visit", StringComparison.Ordinal))
            {
                visitMethods++;
            }
        }

        await Assert.That(voidMissing).IsEmpty();
        await Assert.That(resultMissing).IsEmpty();
        await Assert.That(visitMethods).IsEqualTo(NodeKinds().Count);
    }

    [Test]
    public async Task ResultVisitor_ReturnsTheValueOfTheVisitedKind()
    {
        var factory = new TreeFactory();

        var integer = factory.Int("42").Accept(new LexemeVisitor());
        var other = factory.Dot().Accept(new LexemeVisitor());

        await Assert.That(integer).IsEqualTo("42");
        await Assert.That(other).IsNull();
    }

    [Test]
    public async Task ResultVisitor_ReturnsTheKindNameForEveryNodeOfTheAllKindsTree()
    {
        var root = new TreeFactory().AllKinds();
        var visitor = new KindNameVisitor();

        foreach (var node in TreeWalker.Descendants(root))
        {
            await Assert.That(node.Accept(visitor)).IsEqualTo(node.GetType().Name);
        }
    }

    [Test]
    public async Task DefaultVisitors_DoNothingAndReturnTheDefaultForEveryNodeOfTheAllKindsTree()
    {
        var root = new TreeFactory().AllKinds();
        var plain = new SilentVisitor();
        var withResult = new SilentResultVisitor();

        foreach (var node in TreeWalker.Descendants(root))
        {
            node.Accept(plain);
            await Assert.That(node.Accept(withResult)).IsEqualTo(0);
        }
    }

    private sealed class SilentVisitor : SyntaxVisitor;

    private sealed class SilentResultVisitor : SyntaxVisitor<int>;

    private sealed class LexemeVisitor : SyntaxVisitor<string>
    {
        public override string? VisitIntegerLiteral(IntegerLiteral node) => node.Lexeme;
    }
}
