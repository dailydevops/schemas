namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Paths;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles and runs path expressions over two trees: the tree of <see cref="AxisFixture"/> (keys such as <c>a</c>, <c>b1</c>,
/// <c>i</c>) and a nested tree <c>&lt;r&gt;&lt;s&gt;&lt;s&gt;&lt;t/&gt;&lt;/s&gt;&lt;t/&gt;&lt;/s&gt;&lt;t/&gt;&lt;/r&gt;</c> (keys
/// <c>N.doc</c>, <c>N.r</c>, <c>N.s1</c>, <c>N.s2</c>, <c>N.t1</c>, <c>N.t2</c>, <c>N.t3</c>). Nodes are rendered by key, so two nodes
/// of the same name stay apart. The literals, parentheses, variables, and arithmetic come from <see cref="ArithmeticFixture"/>;
/// <c>position()</c>, <c>last()</c>, integer comparison, and the comma operator are test-local stand-ins for the packages that own them.
/// </summary>
internal static class PathFixture
{
    public const string Xs = "http://www.w3.org/2001/XMLSchema";

    public static readonly XPathQName Atoms = new(string.Empty, "atoms");
    public static readonly XPathQName Mixed = new(string.Empty, "mixed");
    public static readonly XPathQName Nodes = new(string.Empty, "nodes");
    public static readonly XPathQName Untyped = new(string.Empty, "u");

    public static readonly XPathStaticContext Statics = AxisFixture
        .Statics.ToBuilder()
        .WithNamespace("xs", Xs)
        .WithVariable(Atoms)
        .WithVariable(Mixed)
        .WithVariable(Nodes)
        .WithVariable(Untyped)
        .Build();

    private static readonly Dictionary<string, XPathNode> Keys = BuildKeys();
    private static readonly Dictionary<XPathNode, string> Labels = Invert(Keys);

    public static XPathNode Node(string key) => Keys[key];

    /// <summary>Renders a sequence: nodes by key, atomic values by type and value, items separated by a space.</summary>
    public static string Render(in XdmSequence sequence)
    {
        if (sequence.IsEmpty)
        {
            return "()";
        }

        var text = new StringBuilder();
        for (var i = 0; i < sequence.Count; i++)
        {
            _ = text.Append(i == 0 ? string.Empty : " ");
            var item = sequence[i];
            _ = text.Append(item.TryGetNode(out var node) ? Labels[node] : ArithmeticFixture.Describe(item));
        }

        return text.ToString();
    }

    /// <summary>Gets a registry with every lowering the path tests need, the ones under test included.</summary>
    public static LoweringRegistry Registry()
    {
        var registry = ArithmeticFixture.Registry(ArithmeticFixture.Table());
        registry.Register<FunctionCall>(
            static (n, _) =>
                n.Name.LocalName switch
                {
                    "position" => new FocusNode(static c => c.Position),
                    "last" => new FocusNode(static c => c.Size),
                    _ => throw new InvalidOperationException("Unknown test function: " + n.Name.LocalName),
                }
        );
        registry.Register<ComparisonExpression>(
            static (n, c) => new ComparisonNode(n.Operator, c.Lower(n.Left), c.Lower(n.Right))
        );
        registry.Register<SequenceExpression>(
            static (n, c) =>
            {
                var items = new ExecutableNode[n.Items.Count];
                for (var i = 0; i < items.Length; i++)
                {
                    items[i] = c.Lower(n.Items[i]);
                }

                return new CommaNode(items);
            }
        );
        PathLowerings.Register(registry);
        return registry;
    }

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

    public static CompiledExpression CompileOrThrow(string expression) =>
        Compile(expression, out var diagnostic)
        ?? throw new InvalidOperationException($"'{expression}' does not compile: {diagnostic.Code}");

    /// <summary>Evaluates an expression with the node of the given key as the context item, or without a focus when the key is <see langword="null"/>.</summary>
    public static XdmSequence Evaluate(string expression, string? context = null)
    {
        using var evaluation = NewContext();
        if (context is not null)
        {
            evaluation.SetContextItem(XdmItem.FromNode(Node(context)));
        }

        return CompileOrThrow(expression).Evaluate(evaluation);
    }

    /// <summary>Evaluates an expression with an integer as the context item.</summary>
    public static XdmSequence EvaluateWithAtomicContext(string expression)
    {
        using var evaluation = NewContext();
        evaluation.SetContextItem(XdmItem.FromInteger(5));
        return CompileOrThrow(expression).Evaluate(evaluation);
    }

    public static string Run(string expression, string? context = null) => Render(Evaluate(expression, context));

    public static XPathException? Raised(string expression, string? context = null) =>
        ArithmeticFixture.Raised(() => Evaluate(expression, context));

    public static XPathEvaluationContext NewContext()
    {
        var context = new XPathEvaluationContext();
        context.Variables.Set(Atoms, XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromInteger(2)]));
        context.Variables.Set(Mixed, XdmSequence.Create([XdmItem.FromNode(Node("b1")), XdmItem.FromInteger(1)]));
        context.Variables.Set(
            Nodes,
            XdmSequence.Create([
                XdmItem.FromNode(Node("e")),
                XdmItem.FromNode(Node("b1")),
                XdmItem.FromNode(Node("b1")),
                XdmItem.FromNode(Node("b2")),
            ])
        );
        context.Variables.Set(Untyped, XdmItem.FromUntypedAtomic("1"));
        return context;
    }

    private static Dictionary<string, XPathNode> BuildKeys()
    {
        var keys = new Dictionary<string, XPathNode>(AxisFixture.Nodes, StringComparer.Ordinal);
        var tree = BuildNested();
        var doc = new XPathNode(tree.Document);
        var r = doc.FirstChild();
        var s1 = r.FirstChild();
        var s2 = s1.FirstChild();
        keys["N.doc"] = doc;
        keys["N.r"] = r;
        keys["N.s1"] = s1;
        keys["N.s2"] = s2;
        keys["N.t1"] = s2.FirstChild();
        keys["N.t2"] = s2.NextSibling();
        keys["N.t3"] = s1.NextSibling();
        return keys;
    }

    private static Dictionary<XPathNode, string> Invert(Dictionary<string, XPathNode> keys)
    {
        var labels = new Dictionary<XPathNode, string>();
        foreach (var pair in keys)
        {
            labels[pair.Value] = pair.Key;
        }

        return labels;
    }

    private static XmlDocumentTree BuildNested()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        Open(b, "r");
        Open(b, "s");
        Open(b, "s");
        Open(b, "t");
        Close(b, "t");
        Close(b, "s");
        Open(b, "t");
        Close(b, "t");
        Close(b, "s");
        Open(b, "t");
        Close(b, "t");
        Close(b, "r");
        b.EndDocument();
        return b.Complete();
    }

    private static void Open(XmlTreeBuilder b, string local)
    {
        b.StartElement(b.Names.Intern(string.Empty, local, string.Empty), default);
        b.EndAttributes();
    }

    private static void Close(XmlTreeBuilder b, string local) =>
        b.EndElement(b.Names.Intern(string.Empty, local, string.Empty));

    private sealed class FocusNode(Func<XPathEvaluationContext, int> read) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context) =>
            XdmSequence.Create(XdmItem.FromInteger(read(context)));
    }

    // The comparison of two single integers; the real operators belong to #262.
    private sealed class ComparisonNode(ComparisonOperator op, ExecutableNode left, ExecutableNode right)
        : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context)
        {
            var l = left.Evaluate(context);
            var r = right.Evaluate(context);
            if (l.IsEmpty || r.IsEmpty)
            {
                return XdmSequence.Empty;
            }

            _ = l[0].TryGetBigInteger(out var x);
            _ = r[0].TryGetBigInteger(out var y);
            var order = BigInteger.Compare(x, y);
            var result = op switch
            {
                ComparisonOperator.GeneralEqual or ComparisonOperator.ValueEqual => order == 0,
                ComparisonOperator.GeneralNotEqual or ComparisonOperator.ValueNotEqual => order != 0,
                ComparisonOperator.GeneralLessThan or ComparisonOperator.ValueLessThan => order < 0,
                ComparisonOperator.GeneralLessThanOrEqual or ComparisonOperator.ValueLessThanOrEqual => order <= 0,
                ComparisonOperator.GeneralGreaterThan or ComparisonOperator.ValueGreaterThan => order > 0,
                ComparisonOperator.GeneralGreaterThanOrEqual or ComparisonOperator.ValueGreaterThanOrEqual => order
                    >= 0,
                _ => throw new InvalidOperationException("Unsupported test comparison."),
            };
            return XdmSequence.Create(XdmItem.FromBoolean(result));
        }
    }

    // The comma operator; the real one belongs to the sequence expressions.
    private sealed class CommaNode(ExecutableNode[] items) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context)
        {
            var result = XdmSequence.Empty;
            foreach (var item in items)
            {
                result = XdmSequence.Concat(result, item.Evaluate(context));
            }

            return result;
        }
    }
}
