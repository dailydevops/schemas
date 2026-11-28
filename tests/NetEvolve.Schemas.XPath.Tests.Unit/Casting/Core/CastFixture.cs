namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using System;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Helpers for the casting tests: a registry with test-local lowerings for the literals and function calls, the arithmetic lowerings of the
/// operator package,
/// the lowerings under test, and a text form of an item that carries its type.
/// </summary>
internal static class CastFixture
{
    public static readonly XPathStaticContext Statics = TypeFixture.Statics;

    private static readonly ArithmeticOperatorTable ArithmeticOperators = ArithmeticFixture.Table();

    /// <summary>The registry of the casting tests: literals and calls are test-local; arithmetic, cast, and castable are the production lowerings.</summary>
    public static LoweringRegistry Registry(CastingTable? table = null, FunctionRegistry? functions = null)
    {
        var resolved = table ?? CastingCatalog.Default.Table;
        var library = functions ?? FunctionCatalog.Default.Registry;
        var registry = TypeFixture.Registry();
        registry.Register<DecimalLiteral>(
            static (n, _) =>
                new ConstantNode(XdmSequence.Create(XdmItem.FromDecimal(XsdDecimal.Parse(n.Lexeme.AsSpan()).Value)))
        );
        registry.Register<DoubleLiteral>(
            static (n, _) =>
                new ConstantNode(
                    XdmSequence.Create(XdmItem.FromDouble(double.Parse(n.Lexeme, CultureInfo.InvariantCulture)))
                )
        );
        registry.Register<FunctionCall>((n, c) => LowerCall(n, c, library));
        ArithmeticLowerings.Register(registry, ArithmeticOperators);
        CastingLowerings.Register(registry, resolved);
        return registry;
    }

    /// <summary>Compiles an expression; <see langword="null"/> when a static error is found.</summary>
    public static CompiledExpression? Compile(
        string expression,
        out XPathDiagnostic diagnostic,
        CastingTable? table = null,
        FunctionRegistry? functions = null
    )
    {
        var parsed = XPathParser.TryParse(expression.AsSpan(), out var syntax, out diagnostic);
        if (!parsed)
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            Statics,
            Registry(table, functions),
            out var compiled,
            out diagnostic
        );
        return compiled;
    }

    public static XdmSequence Evaluate(
        string expression,
        CastingTable? table = null,
        FunctionRegistry? functions = null
    )
    {
        var compiled = Compile(expression, out var diagnostic, table, functions)
            ?? throw new InvalidOperationException("The test expression does not compile: " + diagnostic.Code);

        using var context = new XPathEvaluationContext();
        return compiled.Evaluate(context);
    }

    /// <summary>Evaluates an expression that yields one item and renders it.</summary>
    public static string EvaluateRendered(string expression)
    {
        var result = Evaluate(expression);
        return result.Count == 1 ? Render(result[0]) : "count:" + result.Count;
    }

    /// <summary>Turns the text of a cast into the text of the matching castable expression.</summary>
    public static string AsCastable(string? expression) =>
        new StringBuilder(expression).Replace(" cast as ", " castable as ").ToString();

    /// <summary>The error code that compiling or evaluating the expression raises, or <see cref="string.Empty"/> when it succeeds.</summary>
    public static string Code(string expression)
    {
        var compiled = Compile(expression, out var diagnostic);
        if (compiled is null)
        {
            return diagnostic.Code;
        }

        try
        {
            using var context = new XPathEvaluationContext();
            _ = compiled.Evaluate(context);
            return string.Empty;
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }

    /// <summary>Renders an item as its type and its value, so that one string tells the type and the value apart.</summary>
    public static string Render(in XdmItem item)
    {
        if (item.TryGetString(out var text))
        {
            return "string:" + text;
        }

        if (item.TryGetUntypedAtomic(out var untyped))
        {
            return "untyped:" + untyped;
        }

        if (item.TryGetBoolean(out var boolean))
        {
            return boolean ? "boolean:true" : "boolean:false";
        }

        if (item.TryGetInteger(out var integer))
        {
            return "integer:" + integer.ToString(CultureInfo.InvariantCulture);
        }

        if (item.TryGetDecimal(out var number))
        {
            return "decimal:" + number;
        }

        if (item.TryGetDouble(out var wide))
        {
            return "double:" + RenderDouble(wide);
        }

        return item.TryGetFloat(out var narrow) ? "float:" + RenderFloat(narrow) : "other:" + item.Kind;
    }

    public static string RenderDouble(double value) =>
        double.IsNaN(value) ? "NaN"
        : double.IsPositiveInfinity(value) ? "INF"
        : double.IsNegativeInfinity(value) ? "-INF"
        : value == 0 && BitConverter.DoubleToInt64Bits(value) < 0 ? "-0"
        : value == Math.Floor(value) && Math.Abs(value) < 1e18 ? ((long)value).ToString(CultureInfo.InvariantCulture)
        : value.ToString("R", CultureInfo.InvariantCulture);

    public static string RenderFloat(float value) =>
        float.IsNaN(value) ? "NaN"
        : float.IsPositiveInfinity(value) ? "INF"
        : float.IsNegativeInfinity(value) ? "-INF"
        : value == 0 && BitConverter.DoubleToInt64Bits(value) < 0 ? "-0"
        : value == Math.Floor(value) && Math.Abs(value) < 1e18 ? ((long)value).ToString(CultureInfo.InvariantCulture)
        : value.ToString("R", CultureInfo.InvariantCulture);

    internal static CallNode LowerCall(
        FunctionCall node,
        LoweringContext context,
        FunctionRegistry library,
        string defaultNamespace = ""
    )
    {
        var name = node.Name;
        var namespaceUri = defaultNamespace;
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

        // As the lowering of the engine does: a constructor function resolves a lexical QName against the static context of the call.
        return new CallNode(
            function is ConstructorFunction constructor && arity == 1
                ? constructor.BindTo(node.Arguments[0], context.StaticContext)
                : function!,
            arguments
        );
    }

    /// <summary>A constant operand.</summary>
    internal sealed class ConstantNode(XdmSequence value) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context) => value;
    }

    internal sealed class CallNode(XPathFunction function, ExecutableNode[] arguments) : ExecutableNode
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
