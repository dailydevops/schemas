namespace NetEvolve.Schemas.XPath.Tests.Unit.Types;

using System;
using System.Globalization;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Helpers for the sequence type tests: a static context with the <c>xs</c> prefix, a parser shortcut, and a registry with the literals the tests need.</summary>
internal static class TypeFixture
{
    public const string Xs = "http://www.w3.org/2001/XMLSchema";

    // The static context predeclares only the xml prefix; xs and p are bound here.
    public static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithNamespace("xs", Xs)
        .WithNamespace("p", "urn:p")
        .Build();

    /// <summary>Parses the sequence type of <c>() instance of {type}</c>.</summary>
    public static SequenceType ParseType(string type)
    {
        var text = "() instance of " + type;
        var parsed = XPathParser.TryParse(text.AsSpan(), out var expression, out var diagnostic);
        if (!parsed)
        {
            throw new InvalidOperationException("The test type does not parse: " + diagnostic.Code);
        }

        return ((InstanceOfExpression)expression!).Type;
    }

    /// <summary>Binds a sequence type written as text; the offset reported for a static error is relative to <c>() instance of </c>.</summary>
    public static SequenceTypeMatcher Matcher(string type, XPathStaticContext? statics = null) =>
        SequenceTypeMatcher.Create(ParseType(type), statics ?? Statics);

    public static XdmSequence Seq(params XdmItem[] items) => XdmSequence.Create(items);

    public static XdmItem Node(string key) => XdmItem.FromNode(AxisFixture.Node(key));

    /// <summary>A registry with the literals the tests use plus the lowerings under test.</summary>
    public static LoweringRegistry Registry()
    {
        var registry = new LoweringRegistry();
        registry.Register<IntegerLiteral>(
            static (n, _) =>
                new ConstantNode(
                    XdmSequence.Create(XdmItem.FromInteger(long.Parse(n.Lexeme, CultureInfo.InvariantCulture)))
                )
        );
        registry.Register<StringLiteral>(
            static (n, _) => new ConstantNode(XdmSequence.Create(XdmItem.FromString(n.Value)))
        );
        registry.Register<ParenthesizedExpression>(
            static (n, c) => n.Inner is null ? new ConstantNode(XdmSequence.Empty) : c.Lower(n.Inner)
        );
        registry.Register<SequenceExpression>(
            static (n, c) =>
            {
                var parts = new ExecutableNode[n.Items.Count];
                for (var i = 0; i < parts.Length; i++)
                {
                    parts[i] = c.Lower(n.Items[i]);
                }

                return new ConcatNode(parts);
            }
        );
        SequenceTypeLowerings.Register(registry);
        return registry;
    }

    /// <summary>Compiles an expression; <see langword="null"/> when a static error is found.</summary>
    public static CompiledExpression? Compile(string expression, out XPathDiagnostic diagnostic)
    {
        var parsed = XPathParser.TryParse(expression.AsSpan(), out var syntax, out diagnostic);
        if (!parsed)
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        _ = ExpressionCompiler.TryCompile(syntax!, expression, Statics, Registry(), out var compiled, out diagnostic);
        return compiled;
    }

    public static XdmSequence Evaluate(string expression)
    {
        var compiled = Compile(expression, out var diagnostic)
            ?? throw new InvalidOperationException("The test expression does not compile: " + diagnostic.Code);

        using var context = new XPathEvaluationContext();
        return compiled.Evaluate(context);
    }

    private sealed class ConstantNode(XdmSequence value) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context) => value;
    }

    private sealed class ConcatNode(ExecutableNode[] parts) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context)
        {
            var result = XdmSequence.Empty;
            for (var i = 0; i < parts.Length; i++)
            {
                result = XdmSequence.Concat(result, parts[i].Evaluate(context));
            }

            return result;
        }
    }

    /// <summary>An annotation source that reports one node as annotated with a given type.</summary>
    public sealed class FakeAnnotations(XPathNode annotated, XdmTypeId type) : IXdmTypeAnnotationSource
    {
        public int Reads { get; private set; }

        public XdmTypeId GetTypeAnnotation(in XPathNode node)
        {
            Reads++;
            return node.IsSameNode(annotated) ? type : node.TypeAnnotation;
        }

        public bool TryGetTypedValue(in XPathNode node, out XdmItem typedValue)
        {
            typedValue = default;
            return false;
        }
    }
}
