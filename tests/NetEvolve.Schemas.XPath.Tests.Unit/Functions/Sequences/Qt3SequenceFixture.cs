namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Tests.Unit.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs a QT3 case through the compiled expression: the lowerings of the casting tests (literals and function calls are test-local;
/// arithmetic, cast, and castable are production) plus the production comparisons and a test-local range, the <c>fn</c> and
/// <c>xs</c> prefixes of the QT3 static context, and the check of an expectation. An expectation is a tab-separated list of
/// alternatives: <c>T</c> or <c>F</c> for a boolean, <c>S:</c> and <c>N:</c> for a string value (<c>N:</c> with normalized space),
/// <c>E:</c> for one value equal to an expression, <c>D:</c> for a sequence deep-equal to an expression, <c>C:</c> for an item count,
/// and <c>X:</c> for an error code.
/// </summary>
internal static class Qt3SequenceFixture
{
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    private static readonly XPathStaticContext Statics = TypeFixture
        .Statics.ToBuilder()
        .WithNamespace("fn", FunctionNamespace)
        .WithNamespace("err", "http://www.w3.org/2005/xqt-errors")
        .WithNamespace("local", "http://www.w3.org/2005/xquery-local-functions")
        .Build();

    private static readonly ComparisonOperatorTable Comparisons = ComparisonFixture.Table();

    /// <summary>Compiles and evaluates an expression; the error code when compiling or evaluating raises one.</summary>
    public static (XdmSequence Result, string Code) Run(string expression)
    {
        try
        {
            var parsed = XPathParser.TryParse(expression.AsSpan(), out var syntax, out var diagnostic);
            if (!parsed)
            {
                return (default, diagnostic.Code);
            }

            var registry = Registry();
            _ = ExpressionCompiler.TryCompile(syntax!, expression, Statics, registry, out var compiled, out diagnostic);
            if (compiled is null)
            {
                return (default, diagnostic.Code);
            }

            using var evaluation = new XPathEvaluationContext();
            return (compiled.Evaluate(evaluation), string.Empty);
        }
        catch (XPathException failure)
        {
            return (default, failure.Diagnostic.Code);
        }
    }

    // The lowerings of the casting tests, with a call lowering that resolves an unprefixed name in the function namespace.
    private static LoweringRegistry Registry()
    {
        var registry = TypeFixture.Registry();
        registry.Register<DecimalLiteral>(
            static (n, _) =>
                new CastFixture.ConstantNode(
                    XdmSequence.Create(XdmItem.FromDecimal(XsdDecimal.Parse(n.Lexeme.AsSpan()).Value))
                )
        );
        registry.Register<DoubleLiteral>(
            static (n, _) =>
                new CastFixture.ConstantNode(
                    XdmSequence.Create(XdmItem.FromDouble(double.Parse(n.Lexeme, CultureInfo.InvariantCulture)))
                )
        );
        registry.Register<FunctionCall>(LowerCall);
        ArithmeticLowerings.Register(registry, ArithmeticFixture.Table());
        CastingLowerings.Register(registry, CastingCatalog.Default.Table);
        ComparisonLowerings.Register(registry, Comparisons);
        registry.Register<RangeExpression>(
            (node, context) => new RangeNode(context.Lower(node.From), context.Lower(node.To))
        );
        return registry;
    }

    private static CallNode LowerCall(FunctionCall node, LoweringContext context)
    {
        var name = node.Name;
        var namespaceUri = FunctionNamespace;
        if (name.Prefix is not null && !context.StaticContext.TryResolveNamespace(name.Prefix, out namespaceUri))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0081, name.Location.Offset, name.Location.Length);
        }

        var arity = node.Arguments.Count;
        var qname = new XPathQName(namespaceUri!, name.LocalName);
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

    /// <summary>Checks a result against the alternatives of an expectation.</summary>
    public static bool Satisfies(string? expectation, XdmSequence result, string code)
    {
        foreach (var alternative in (expectation ?? string.Empty).Split('\t'))
        {
            if (Holds(alternative, result, code))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Shows the outcome of a run, for the message of a failed case.</summary>
    public static string Describe(XdmSequence result, string code)
    {
        if (code.Length > 0)
        {
            return code;
        }

        var parts = new string[result.Count];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = CastFixture.Render(result[i]);
        }

        return "(" + string.Join(", ", parts) + ")";
    }

    private static bool Holds(string alternative, XdmSequence result, string code)
    {
        if (alternative.StartsWith("X:", StringComparison.Ordinal))
        {
            var expected = alternative.Substring(2);
            return code.Length > 0
                && (
                    string.Equals(expected, "*", StringComparison.Ordinal)
                    || string.Equals(code, expected, StringComparison.Ordinal)
                );
        }

        if (code.Length > 0)
        {
            return false;
        }

        return alternative[0] switch
        {
            'T' => result.Count == 1 && result[0].TryGetBoolean(out var yes) && yes,
            'F' => result.Count == 1 && result[0].TryGetBoolean(out var no) && !no,
            'S' => string.Equals(StringValue(result), alternative.Substring(2), StringComparison.Ordinal),
            'N' => string.Equals(
                Normalize(StringValue(result)),
                Normalize(alternative.Substring(2)),
                StringComparison.Ordinal
            ),
            'C' => result.Count == int.Parse(alternative.AsSpan(2), NumberStyles.Integer, CultureInfo.InvariantCulture),
            'D' => SequencesEqual(result, Evaluate(alternative.Substring(2))),
            _ => result.Count == 1 && ItemsEqual(result[0], SingleExpected(alternative.Substring(2))),
        };
    }

    private static XdmSequence Evaluate(string expression)
    {
        var (result, code) = Run(expression);
        return code.Length > 0
            ? throw new InvalidOperationException("The expected value " + expression + " raised " + code)
            : result;
    }

    private static XdmItem SingleExpected(string expression)
    {
        var expected = Evaluate(expression);
        return expected.Count == 1 ? expected[0] : default;
    }

    // The string value of a sequence: the string values of the items, separated by a space.
    private static string StringValue(XdmSequence result)
    {
        var text = new StringBuilder();
        for (var i = 0; i < result.Count; i++)
        {
            _ = NumberText.TryFormat(result[i], out var item);
            _ = text.Append(i > 0 ? " " : string.Empty).Append(item);
        }

        return text.ToString();
    }

    private static string Normalize(string text) =>
        string.Join(" ", text.Split((char[])[' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    private static bool SequencesEqual(XdmSequence result, XdmSequence expected)
    {
        if (result.Count != expected.Count)
        {
            return false;
        }

        for (var i = 0; i < result.Count; i++)
        {
            if (!ItemsEqual(result[i], expected[i]))
            {
                return false;
            }
        }

        return true;
    }

    // assert-eq: numeric values by value, NaN equal to NaN, everything else by type and value.
    private static bool ItemsEqual(XdmItem left, XdmItem right)
    {
        XdmItemKind[] numeric =
        [
            XdmItemKind.XsInteger,
            XdmItemKind.XsDecimal,
            XdmItemKind.XsFloat,
            XdmItemKind.XsDouble,
        ];
        if (Array.IndexOf(numeric, left.Kind) < 0 || Array.IndexOf(numeric, right.Kind) < 0)
        {
            return string.Equals(CastFixture.Render(left), CastFixture.Render(right), StringComparison.Ordinal);
        }

        var exact =
            left.Kind is XdmItemKind.XsInteger or XdmItemKind.XsDecimal
            && right.Kind is XdmItemKind.XsInteger or XdmItemKind.XsDecimal;
        CastConversion target = exact ? PrimitiveConversions.ToDecimal : PrimitiveConversions.ToDouble;
        _ = target(left, out var a);
        _ = target(right, out var b);
        return exact
            ? a.TryGetDecimal(out var x) && b.TryGetDecimal(out var y) && x == y
            : a.TryGetDouble(out var p)
                && b.TryGetDouble(out var q)
                && (p == q || (double.IsNaN(p) && double.IsNaN(q)));
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

    /// <summary>The range expression, evaluated at once: the integers from the first operand up to the second.</summary>
    private sealed class RangeNode(ExecutableNode from, ExecutableNode to) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context)
        {
            var first = from.Evaluate(context);
            var last = to.Evaluate(context);
            if (first.IsEmpty || last.IsEmpty)
            {
                return XdmSequence.Empty;
            }

            if (
                first.Count != 1
                || last.Count != 1
                || !first[0].TryGetInteger(out var low)
                || !last[0].TryGetInteger(out var high)
            )
            {
                throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
            }

            // Limitation: the range is eager here, so a range that no test machine could hold is not evaluated; the lazy range belongs to the range lowering (XP-14).
            if (high - low > 1_000_000)
            {
                throw new NotSupportedException("range too large for the eager test lowering");
            }

            var items = new List<XdmItem>();
            for (var value = low; value <= high; value++)
            {
                items.Add(XdmItem.FromInteger(value));
            }

            return XdmSequence.Create(items.ToArray());
        }
    }
}
