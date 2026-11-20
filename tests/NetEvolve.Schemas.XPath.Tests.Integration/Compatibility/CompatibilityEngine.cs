namespace NetEvolve.Schemas.XPath.Tests.Integration.Compatibility;

using System.Collections.Generic;
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
/// Evaluates an XPath 1.0 expression over one of the fixed documents with the library: the production lowerings of the literals, the
/// paths, the arithmetic, the comparisons, and the calls over the default function library, in the mode asked for. The composition
/// root (XP-19) does not exist yet, so the engine is assembled here. The result is rendered by <see cref="ResultFormat"/>, the same
/// way as the baseline, and a static or dynamic error is rendered as <c>error:CODE</c>.
/// </summary>
internal static class CompatibilityEngine
{
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    private static readonly ArithmeticOperatorTable Arithmetic = CreateArithmetic();
    private static readonly ComparisonOperatorTable Comparison = CreateComparison();
    private static readonly Dictionary<string, XPathNode> Documents = new(StringComparer.Ordinal);

    /// <summary>Evaluates an expression with the document node as the context item.</summary>
    /// <param name="document">The document key.</param>
    /// <param name="expression">The expression.</param>
    /// <param name="compatibility">Whether XPath 1.0 compatibility mode is on.</param>
    /// <returns>The rendered result, or <c>error:CODE</c>.</returns>
    public static string Evaluate(string document, string expression, bool compatibility)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out var diagnostic))
        {
            return "error:" + diagnostic.Code;
        }

        var statics = XPathStaticContext.Default.ToBuilder().WithXPath10Compatibility(compatibility).Build();
        if (!ExpressionCompiler.TryCompile(syntax!, expression, statics, Registry(), out var compiled, out diagnostic))
        {
            return "error:" + diagnostic.Code;
        }

        var root = Root(document);
        try
        {
            using var context = new XPathEvaluationContext();
            context.SetContextItem(XdmItem.FromNode(root));
            return ResultFormat.Render(compiled!.Evaluate(context), root);
        }
        catch (XPathException exception)
        {
            return "error:" + exception.Diagnostic.Code;
        }
    }

    private static XPathNode Root(string document)
    {
        lock (Documents)
        {
            if (!Documents.TryGetValue(document, out var root))
            {
                root = CompatibilityDocuments.Load(document);
                Documents.Add(document, root);
            }

            return root;
        }
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

        // The public API (XP-19) lowers a call the same way: the node of compatibility mode converts the arguments first.
        return context.StaticContext.XPath10Compatibility
            ? new CompatibilityCallNode(function!, arguments)
            : new CallNode(function!, arguments);
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
