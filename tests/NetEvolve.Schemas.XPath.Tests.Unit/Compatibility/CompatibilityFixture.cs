namespace NetEvolve.Schemas.XPath.Tests.Unit.Compatibility;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles and runs expressions twice, with XPath 1.0 compatibility mode on and off: the production lowerings of the literals, the
/// variables, the paths, the arithmetic, the comparisons, and the calls over the default function library. The variables are declared
/// in the static context and bound by <see cref="NewContext"/>.
/// </summary>
internal static class CompatibilityFixture
{
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    // Two integers, the empty sequence, strings, booleans, untyped values, and the other numeric types.
    private static readonly string[] Names =
    [
        "two",
        "none",
        "text3",
        "yes",
        "no",
        "word",
        "u3",
        "dec",
        "flt",
        "strs",
        "a",
        "b",
    ];

    private static readonly ArithmeticOperatorTable Arithmetic = CreateArithmetic();
    private static readonly ComparisonOperatorTable Comparison = ComparisonFixture.Table();
    private static readonly XmlDocumentTree Tree = BuildTree();

    /// <summary>The static context with the variables declared, in the mode asked for.</summary>
    public static XPathStaticContext Statics(bool compatibility)
    {
        var builder = XPathStaticContext.Default.ToBuilder().WithXPath10Compatibility(compatibility);
        foreach (var name in Names)
        {
            builder.WithVariable(new XPathQName(string.Empty, name));
        }

        return builder.Build();
    }

    /// <summary>Gets a registry with the production lowerings and a call lowering over the default function library.</summary>
    public static LoweringRegistry Registry()
    {
        var registry = new LoweringRegistry();
        FlowLowerings.Register(registry);
        PathLowerings.Register(registry);
        ArithmeticLowerings.Register(registry, Arithmetic);
        ComparisonLowerings.Register(registry, Comparison);
        registry.Register<FunctionCall>(static (node, context) => LowerCall(node, context));
        return registry;
    }

    /// <summary>Compiles an expression; <see langword="null"/> and the diagnostic when it does not compile.</summary>
    public static CompiledExpression? TryCompile(string expression, bool compatibility, out XPathDiagnostic diagnostic)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out diagnostic))
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            Statics(compatibility),
            Registry(),
            out var compiled,
            out diagnostic
        );
        return compiled;
    }

    /// <summary>
    /// Runs an expression and describes the outcome: the items with their types (see <see cref="ArithmeticFixture.Describe(in XdmSequence)"/>),
    /// <see langword="true"/> or <see langword="false"/> for a boolean, or the code of the error that compiling or evaluating raises.
    /// </summary>
    public static string Run(string expression, bool compatibility)
    {
        var compiled = TryCompile(expression, compatibility, out var diagnostic);
        if (compiled is null)
        {
            return diagnostic.Code;
        }

        try
        {
            using var context = NewContext();
            return Show(compiled.Evaluate(context));
        }
        catch (XPathException exception)
        {
            return exception.Diagnostic.Code;
        }
    }

    /// <summary>Asserts the outcome in compatibility mode and, as a separate fact, the differing outcome of the same expression without it.</summary>
    public static async Task AssertModes(string expression, string compatibility, string standard)
    {
        await Assert.That(Run(expression, compatibility: false)).IsEqualTo(standard);
        await Assert.That(Run(expression, compatibility: true)).IsEqualTo(compatibility);
        await Assert.That(compatibility).IsNotEqualTo(standard);
    }

    public static string Show(in XdmSequence sequence) =>
        sequence.Count == 1 && sequence[0].TryGetBoolean(out var flag)
            ? (flag ? "true" : "false")
            : ArithmeticFixture.Describe(sequence);

    public static XPathEvaluationContext NewContext()
    {
        var context = new XPathEvaluationContext();
        context.Variables.Set(Name("two"), XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromInteger(2)]));
        context.Variables.Set(Name("none"), XdmSequence.Empty);
        context.Variables.Set(Name("text3"), XdmItem.FromString("3"));
        context.Variables.Set(Name("yes"), XdmItem.FromBoolean(true));
        context.Variables.Set(Name("no"), XdmItem.FromBoolean(false));
        context.Variables.Set(Name("word"), XdmItem.FromUntypedAtomic("x"));
        context.Variables.Set(Name("u3"), XdmItem.FromUntypedAtomic("3"));
        context.Variables.Set(Name("dec"), ArithmeticFixture.Item("d:2.5"));
        context.Variables.Set(Name("flt"), ArithmeticFixture.Item("f:1.5"));
        context.Variables.Set(Name("strs"), XdmSequence.Create([XdmItem.FromString("bb"), XdmItem.FromString("a")]));

        // The two a elements of <r><a>1</a><a>2</a><b/></r>, and the b element.
        var first = Tree.Document.FirstChild.FirstChild;
        var second = first.NextSibling;
        context.Variables.Set(
            Name("a"),
            XdmSequence.Create([XdmItem.FromNode(new XPathNode(first)), XdmItem.FromNode(new XPathNode(second))])
        );
        context.Variables.Set(Name("b"), XdmItem.FromNode(new XPathNode(second.NextSibling)));
        return context;
    }

    private static XPathQName Name(string local) => new(string.Empty, local);

    private static ArithmeticOperatorTable CreateArithmetic()
    {
        var table = new ArithmeticOperatorTable();
        NumericOperators.Register(table);
        return table;
    }

    private static XmlDocumentTree BuildTree()
    {
        using var builder = new XmlTreeBuilder();
        builder.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var root = builder.Names.Intern(string.Empty, "r", string.Empty);
        var a = builder.Names.Intern(string.Empty, "a", string.Empty);
        var b = builder.Names.Intern(string.Empty, "b", string.Empty);
        builder.StartElement(root, default);
        builder.EndAttributes();
        foreach (var text in (string[])["1", "2"])
        {
            builder.StartElement(a, default);
            builder.EndAttributes();
            builder.Text(text, XmlTextKind.Text);
            builder.EndElement(a);
        }

        builder.StartElement(b, default);
        builder.EndAttributes();
        builder.EndElement(b);
        builder.EndElement(root);
        builder.EndDocument();
        return builder.Complete();
    }

    private static CallNode LowerCall(FunctionCall node, LoweringContext context)
    {
        var arity = node.Arguments.Count;
        var qname = new XPathQName(FunctionNamespace, node.Name.LocalName);
        if (
            !FunctionCatalog.Default.Registry.TryResolve(
                context.StaticContext,
                qname,
                arity,
                XPathHostRequirements.None,
                out var function
            )
        )
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0017, node.Location.Offset, node.Location.Length);
        }

        var arguments = new ExecutableNode[arity];
        for (var i = 0; i < arity; i++)
        {
            arguments[i] = context.Lower(node.Arguments[i]);
        }

        return new CallNode(function!, arguments);
    }

    private sealed class CallNode(XPathFunction function, ExecutableNode[] arguments) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context)
        {
            var values = new XdmSequence[arguments.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = arguments[i].Evaluate(context);
            }

            return function.Call(context, values);
        }
    }
}
