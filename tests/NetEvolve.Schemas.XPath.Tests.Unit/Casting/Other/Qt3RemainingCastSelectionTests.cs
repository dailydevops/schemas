namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Other;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Casting.Other;
using NetEvolve.Schemas.XPath.Casting.Temporal;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Runs the QT3 cases of <see cref="Qt3RemainingCastSelection"/> through the compiled expressions and checks each against its assertion.</summary>
public class Qt3RemainingCastSelectionTests
{
    private static readonly ComparisonOperatorTable Operators = CreateOperators();

    public static IEnumerable<Func<(string Id, string Expression, string Expectation)>> Cases()
    {
        foreach (var entry in Qt3RemainingCastSelection.Cases)
        {
            var captured = entry;
            yield return () => captured;
        }
    }

    [Test]
    public async Task Selection_NamesEachCaseOnce_AndEachExclusionOnce()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, expression, expectation) in Qt3RemainingCastSelection.Cases)
        {
            await Assert.That(ids.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert
                .That(
                    id.StartsWith("prod-CastExpr/", StringComparison.Ordinal)
                        || id.StartsWith("prod-CastableExpr/", StringComparison.Ordinal)
                )
                .IsTrue();
            await Assert.That(expression).IsNotEmpty();
            await Assert.That(expectation).IsNotEmpty();
        }

        foreach (var (id, reason) in Qt3RemainingCastSelection.Excluded)
        {
            await Assert.That(ids.Add(id)).IsTrue().Because($"{id} is listed as a case and as an exclusion");
            await Assert.That(reason).IsNotEmpty();
        }

        await Assert.That(Qt3RemainingCastSelection.Cases.Length).IsGreaterThan(1500);
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Case_SatisfiesItsAssertion(string id, string expression, string expectation)
    {
        var (result, code) = Run(expression);
        var passed = false;
        foreach (var alternative in (expectation ?? string.Empty).Split('\t'))
        {
            passed |= Satisfies(alternative, result, code);
        }

        await Assert
            .That(passed)
            .IsTrue()
            .Because($"{id}: {expression} gave {(code.Length > 0 ? code : Describe(result))}, expected {expectation}");
    }

    private static ComparisonOperatorTable CreateOperators()
    {
        var table = new ComparisonOperatorTable();
        ComparisonOperators.Register(table);
        return table;
    }

    private static LoweringRegistry Registry()
    {
        var registry = CastFixture.Registry();
        ComparisonLowerings.Register(registry, Operators);
        return registry;
    }

    private static (XdmSequence Result, string Code) Run(string expression)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out var diagnostic))
        {
            return (default, diagnostic.Code);
        }

        if (
            !ExpressionCompiler.TryCompile(
                syntax!,
                expression,
                CastFixture.Statics,
                Registry(),
                out var compiled,
                out diagnostic
            )
        )
        {
            return (default, diagnostic.Code);
        }

        try
        {
            using var context = new XPathEvaluationContext();
            return (compiled!.Evaluate(context), string.Empty);
        }
        catch (XPathException failure)
        {
            return (default, failure.Diagnostic.Code);
        }
    }

    private static XdmSequence Evaluate(string expression)
    {
        var (result, code) = Run(expression);
        return code.Length == 0 ? result : throw new InvalidOperationException(expression + " raised " + code);
    }

    private static bool Satisfies(string alternative, XdmSequence result, string code)
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
            'R' => InRange(result, alternative.Substring(2)),
            _ => EqualsExpected(result, Evaluate(alternative.Substring(2))),
        };
    }

    // The string value of a sequence: the string values of the items, separated by a space.
    private static string StringValue(XdmSequence result)
    {
        var text = new StringBuilder();
        for (var i = 0; i < result.Count; i++)
        {
            _ = text.Append(i > 0 ? " " : string.Empty).Append(Format(result[i]));
        }

        return text.ToString();
    }

    private static string Format(in XdmItem item)
    {
        if (
            NumberText.TryFormat(item, out var text)
            || TemporalConversions.TryFormat(item, out text)
            || OtherConversions.TryFormat(item, out text)
        )
        {
            return text!;
        }

        return item.Kind.ToString();
    }

    private static string Normalize(string text) =>
        string.Join(" ", text.Split((char[])[' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    // The result is one numeric value strictly between the two bounds ("lower,upper"), compared as double.
    private static bool InRange(XdmSequence result, string bounds)
    {
        var parts = bounds.Split(',');
        if (result.Count != 1 || PrimitiveConversions.ToDouble(result[0], out var converted) != CastStatus.Ok)
        {
            return false;
        }

        return converted.TryGetDouble(out var value)
            && value > double.Parse(parts[0], CultureInfo.InvariantCulture)
            && value < double.Parse(parts[1], CultureInfo.InvariantCulture);
    }

    // assert-eq: the result is one value that is equal to the expected value, numeric values by value.
    private static bool EqualsExpected(XdmSequence result, XdmSequence expected)
    {
        if (result.Count != 1 || expected.Count != 1)
        {
            return false;
        }

        var left = result[0];
        var right = expected[0];
        var numeric = new[] { XdmItemKind.XsInteger, XdmItemKind.XsDecimal, XdmItemKind.XsFloat, XdmItemKind.XsDouble };
        if (Array.IndexOf(numeric, left.Kind) >= 0 && Array.IndexOf(numeric, right.Kind) >= 0)
        {
            var exact =
                left.Kind is XdmItemKind.XsInteger or XdmItemKind.XsDecimal
                && right.Kind is XdmItemKind.XsInteger or XdmItemKind.XsDecimal;
            CastConversion target = exact ? PrimitiveConversions.ToDecimal : PrimitiveConversions.ToDouble;
            _ = target(left, out var a);
            _ = target(right, out var b);
            return exact
                ? a.TryGetDecimal(out var x) && b.TryGetDecimal(out var y) && x == y
                : a.TryGetDouble(out var p) && b.TryGetDouble(out var q) && p == q;
        }

        return left == right;
    }

    private static string Describe(XdmSequence result)
    {
        var parts = new string[result.Count];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = result[i].Kind + ":" + Format(result[i]);
        }

        return "(" + string.Join(", ", parts) + ")";
    }
}
