namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Derived;

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
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Runs the QT3 cases of <see cref="Qt3DerivedCastSelection"/> through the compiled expressions and checks each against its assertion.</summary>
public class Qt3DerivedCastSelectionTests
{
    private const int CasesOfTheSets = 159 + 7 + 7 + 10;

    private static readonly ComparisonOperatorTable Operators = CreateOperators();

    public static IEnumerable<Func<(string Id, string Expression, string Expectation)>> Cases()
    {
        foreach (var entry in Qt3DerivedCastSelection.Cases)
        {
            var captured = entry;
            yield return () => captured;
        }
    }

    [Test]
    public async Task Selection_NamesEachCaseOnce_AndEachExclusionOnce()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, expression, expectation) in Qt3DerivedCastSelection.Cases)
        {
            await Assert.That(ids.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert.That(expression).IsNotEmpty();
            await Assert.That(expectation).IsNotEmpty();
        }

        foreach (var (id, reason) in Qt3DerivedCastSelection.Excluded)
        {
            await Assert.That(ids.Add(id)).IsTrue().Because($"{id} is listed as a case and as an exclusion");
            await Assert.That(reason).IsNotEmpty();
        }

        // Every case of the three sets is accounted for: 159 of prod-CastExpr.derived, 7 each of xs-normalizedString and xs-token,
        // and the ten cases of prod-CastExpr that name a derived type.
        await Assert.That(ids.Count).IsEqualTo(CasesOfTheSets);
        await Assert.That(Qt3DerivedCastSelection.Cases.Length).IsGreaterThan(150);
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

    // The literals, the for expression, and the logical operators are the production lowerings of the flow folder, which read an integer
    // literal beyond 64 bits; the function calls are the test-local ones of the casting fixture.
    private static LoweringRegistry Registry()
    {
        var registry = new LoweringRegistry();
        FlowLowerings.Register(registry);
        ArithmeticLowerings.Register(registry, ArithmeticFixture.Table());
        ComparisonLowerings.Register(registry, Operators);
        SequenceTypeLowerings.Register(registry);
        CastingLowerings.Register(registry, CastingCatalog.Default.Table);
        registry.Register<FunctionCall>(static (n, c) => CastFixture.LowerCall(n, c, FunctionCatalog.Default.Registry));
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
            return code.Length > 0 && string.Equals(code, alternative.Substring(2), StringComparison.Ordinal);
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

        return string.Equals(Format(left), Format(right), StringComparison.Ordinal);
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
