namespace NetEvolve.Schemas.Schematron.Tests.Unit.Bindings.Xslt1;

using System;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compatibility;
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
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles and runs an expression under a static context the binding handed out, over fixed variables: <c>$a</c> is the two element
/// nodes <c>a</c> of <c>&lt;r&gt;&lt;a&gt;1&lt;/a&gt;&lt;a&gt;2&lt;/a&gt;&lt;b/&gt;&lt;/r&gt;</c> and <c>$b</c> is the empty element <c>b</c>.
/// Limitation: the public compile and evaluate path of the XPath package (XP-19, #297) is not implemented, so the fixture assembles the
/// production lowerings itself, as the tests of the compatibility mode in the XPath package do.
/// </summary>
internal static class CompatibilityFixture
{
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    private static readonly ArithmeticOperatorTable Arithmetic = CreateArithmetic();
    private static readonly ComparisonOperatorTable Comparison = CreateComparison();
    private static readonly XmlDocumentTree Tree = BuildTree();

    /// <summary>The variable names of the scope the fixture compiles in; the slot is the index.</summary>
    internal static string[] VariableNames { get; } = ["a", "b"];

    /// <summary>Runs an expression and describes the outcome: the items with their types, or the code of the error.</summary>
    internal static string Run(string expression, XPathStaticContext statics)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out var diagnostic))
        {
            return diagnostic.Code;
        }

        if (!ExpressionCompiler.TryCompile(syntax!, expression, statics, Registry(), out var compiled, out diagnostic))
        {
            return diagnostic.Code;
        }

        try
        {
            using var context = NewContext();
            return Describe(compiled!.Evaluate(context));
        }
        catch (XPathException exception)
        {
            return exception.Diagnostic.Code;
        }
    }

    private static XPathEvaluationContext NewContext()
    {
        var context = new XPathEvaluationContext();
        var first = Tree.Document.FirstChild.FirstChild;
        var second = first.NextSibling;
        var a = XdmSequence.Create([XdmItem.FromNode(new XPathNode(first)), XdmItem.FromNode(new XPathNode(second))]);
        var b = XdmSequence.Create(XdmItem.FromNode(new XPathNode(second.NextSibling)));
        context.Variables.Set(new XPathQName(string.Empty, "a"), a);
        context.Variables.Set(new XPathQName(string.Empty, "b"), b);

        // The static context of the binding reads the variables by slot, in the order of VariableNames.
        context.Variables.AttachSlots([a, b], [true, true]);
        return context;
    }

    private static string Describe(in XdmSequence sequence)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < sequence.Count; i++)
        {
            var item = sequence[i];
            _ = builder.Append(i == 0 ? string.Empty : ", ");
            if (item.TryGetBoolean(out var flag))
            {
                _ = builder.Append("boolean:").Append(flag ? "true" : "false");
            }
            else if (item.TryGetInteger(out var integer))
            {
                _ = builder.Append("integer:").Append(integer);
            }
            else if (item.TryGetString(out var text))
            {
                _ = builder.Append("string:").Append(text);
            }
            else
            {
                _ = builder.Append("kind:").Append(item.Kind);
            }
        }

        return sequence.IsEmpty ? "()" : builder.ToString();
    }

    private static LoweringRegistry Registry()
    {
        var registry = new LoweringRegistry();
        FlowLowerings.Register(registry);
        PathLowerings.Register(registry);
        ArithmeticLowerings.Register(registry, Arithmetic);
        ComparisonLowerings.Register(registry, Comparison);
        registry.Register<FunctionCall>(static (node, context) => LowerCall(node, context));
        return registry;
    }

    private static ExecutableNode LowerCall(FunctionCall node, LoweringContext context)
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

        // The mode decides the node, as the public API will (XP-19): the node of compatibility mode converts the arguments first.
        return context.StaticContext.XPath10Compatibility
            ? new CompatibilityCallNode(function!, arguments)
            : new CallNode(function!, arguments);
    }

    private static ArithmeticOperatorTable CreateArithmetic()
    {
        var table = new ArithmeticOperatorTable();
        NumericOperators.Register(table);
        return table;
    }

    private static ComparisonOperatorTable CreateComparison()
    {
        var table = new ComparisonOperatorTable();
        ComparisonOperators.Register(table);
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
