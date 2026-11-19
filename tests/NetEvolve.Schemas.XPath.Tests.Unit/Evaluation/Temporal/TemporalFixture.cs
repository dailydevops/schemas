namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Temporal;

using System;
using System.Text;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Casting.Temporal;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The compiled expressions of the date, time, and duration tests: the numeric and the temporal entries in one arithmetic table and
/// one comparison table, as the composition root fills them, and the production lowerings of the arithmetic, the comparisons, the
/// casts, and the function calls over the default function library.
/// </summary>
internal static class TemporalFixture
{
    public static readonly ArithmeticOperatorTable Arithmetic = CreateArithmetic();

    public static readonly ComparisonOperatorTable Comparison = CreateComparison();

    /// <summary>The static context of the tests: the prefixes <c>xs</c> and <c>fn</c> are declared.</summary>
    public static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithNamespace("xs", "http://www.w3.org/2001/XMLSchema")
        .WithNamespace("fn", "http://www.w3.org/2005/xpath-functions")
        .Build();

    /// <summary>The registry of the tests: the literals, the logical and conditional expressions, the arithmetic, the comparisons, the casts, and the calls are production code.</summary>
    public static LoweringRegistry Registry()
    {
        var registry = new LoweringRegistry();
        FlowLowerings.Register(registry);
        SequenceTypeLowerings.Register(registry);
        registry.Register<FunctionCall>(static (n, c) => LowerCall(n, c, FunctionCatalog.Default.Registry));
        ArithmeticLowerings.Register(registry, Arithmetic);
        ComparisonLowerings.Register(registry, Comparison);
        CastingLowerings.Register(registry, CastingCatalog.Default.Table);
        return registry;
    }

    /// <summary>Evaluates an expression in a dynamic context with an implicit timezone.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="implicitTimezoneMinutes">The implicit timezone in minutes from UTC.</param>
    /// <returns>The result and the error code, which is empty when the evaluation succeeds.</returns>
    public static (XdmSequence Result, string Code) Run(string expression, int implicitTimezoneMinutes = 0)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out var diagnostic))
        {
            return (default, diagnostic.Code);
        }

        if (!ExpressionCompiler.TryCompile(syntax!, expression, Statics, Registry(), out var compiled, out diagnostic))
        {
            return (default, diagnostic.Code);
        }

        try
        {
            using var context = new XPathEvaluationContext { ImplicitTimezoneMinutes = implicitTimezoneMinutes };
            return (compiled!.Evaluate(context), string.Empty);
        }
        catch (XPathException failure)
        {
            return (default, failure.Diagnostic.Code);
        }
    }

    /// <summary>The error code that an expression raises, or <see cref="string.Empty"/> when it succeeds.</summary>
    public static string Code(string expression, int implicitTimezoneMinutes = 0) =>
        Run(expression, implicitTimezoneMinutes).Code;

    /// <summary>Evaluates an expression that must succeed and renders the result.</summary>
    public static string Value(string expression, int implicitTimezoneMinutes = 0)
    {
        var (result, code) = Run(expression, implicitTimezoneMinutes);
        return code.Length == 0 ? Describe(result) : "error:" + code;
    }

    /// <summary>Renders a sequence as the kind and the string value of each item, so that one string tells the type and the value apart.</summary>
    public static string Describe(in XdmSequence result)
    {
        var parts = new string[result.Count];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = result[i].Kind + ":" + Format(result[i]);
        }

        return string.Join(" ", parts);
    }

    /// <summary>The string value of an item: its canonical lexical form.</summary>
    public static string Format(in XdmItem item)
    {
        if (item.TryGetBoolean(out var boolean))
        {
            return boolean ? "true" : "false";
        }

        if (
            NumberText.TryFormat(item, out var text)
            || TemporalConversions.TryFormat(item, out text)
            || item.TryGetString(out text)
        )
        {
            return text!;
        }

        return item.Kind.ToString();
    }

    /// <summary>The string value of a sequence: the string values of the items, separated by a space.</summary>
    public static string StringValue(in XdmSequence result)
    {
        var text = new StringBuilder();
        for (var i = 0; i < result.Count; i++)
        {
            _ = text.Append(i > 0 ? " " : string.Empty).Append(Format(result[i]));
        }

        return text.ToString();
    }

    private static ArithmeticOperatorTable CreateArithmetic()
    {
        var table = new ArithmeticOperatorTable();
        NumericOperators.Register(table);
        TemporalOperators.Register(table);
        return table;
    }

    private static ComparisonOperatorTable CreateComparison()
    {
        var table = new ComparisonOperatorTable();
        ComparisonOperators.Register(table);
        TemporalOperators.Register(table);
        return table;
    }

    private static CallNode LowerCall(FunctionCall node, LoweringContext context, FunctionRegistry library)
    {
        var name = node.Name;
        var namespaceUri = context.StaticContext.DefaultFunctionNamespace;
        if (name.Prefix is not null && !context.StaticContext.TryResolveNamespace(name.Prefix, out namespaceUri))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0081, name.Location.Offset, name.Location.Length);
        }

        var qname = new XPathQName(namespaceUri ?? string.Empty, name.LocalName);
        var arity = node.Arguments.Count;
        if (!library.TryResolve(context.StaticContext, qname, arity, XPathHostRequirements.None, out var function))
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

    /// <summary>A constant operand.</summary>
    internal sealed class ConstantNode(XdmSequence value) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context) => value;
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
