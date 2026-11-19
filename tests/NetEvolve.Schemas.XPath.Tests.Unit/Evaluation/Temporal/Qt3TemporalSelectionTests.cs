namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Temporal;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static TemporalFixture;

/// <summary>Runs the QT3 cases of <see cref="Qt3TemporalSelection"/> through the compiled expressions and checks each against its assertion.</summary>
public class Qt3TemporalSelectionTests
{
    public static IEnumerable<Func<(string Id, string Expression, string Expectation)>> Cases()
    {
        foreach (var entry in Qt3TemporalSelection.Cases)
        {
            var captured = entry;
            yield return () => captured;
        }
    }

    [Test]
    public async Task Selection_NamesEachCaseOnce_AndAccountsForEveryCaseOfTheSets()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, expression, expectation) in Qt3TemporalSelection.Cases)
        {
            await Assert.That(ids.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert.That(expression).IsNotEmpty();
            await Assert.That(expectation).IsNotEmpty();
        }

        foreach (var (id, reason) in Qt3TemporalSelection.OutOfShape)
        {
            await Assert.That(ids.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert.That(reason).IsNotEmpty();
        }

        foreach (var id in Qt3TemporalSelection.NotXPath20)
        {
            await Assert.That(ids.Add(id)).IsTrue().Because($"{id} is listed twice");
        }

        // Every case of the sets op-add-*, op-subtract-*, op-multiply-*, op-divide-*, the equality, less-than, and greater-than sets of the date, time,
        // and duration types, and the Gregorian equality sets is in one of the three lists.
        await Assert.That(ids.Count).IsEqualTo(Qt3TemporalSelection.Total);
        await Assert.That(Qt3TemporalSelection.Cases.Length).IsGreaterThan(1000);
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
            'Z' => result.Count == 0,
            'T' => result.Count == 1 && result[0].TryGetBoolean(out var yes) && yes,
            'F' => result.Count == 1 && result[0].TryGetBoolean(out var no) && !no,
            'S' => string.Equals(StringValue(result), alternative.Substring(2), StringComparison.Ordinal),
            'N' => string.Equals(
                Normalize(StringValue(result)),
                Normalize(alternative.Substring(2)),
                StringComparison.Ordinal
            ),
            _ => EqualsExpected(result, alternative.Substring(2)),
        };
    }

    private static string Normalize(string text) =>
        string.Join(" ", text.Split((char[])[' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    // assert-eq: the result is one value that is equal to the expected value by the value comparison eq.
    private static bool EqualsExpected(XdmSequence result, string expectedExpression)
    {
        var (expected, code) = Run(expectedExpression);
        if (code.Length > 0 || result.Count != 1 || expected.Count != 1)
        {
            return false;
        }

        var left = result[0];
        var right = expected[0];
        if (!Comparison.TryGetContextual(ComparisonRelation.Equal, left.TypeId, right.TypeId, out var equal))
        {
            return false;
        }

        using var context = new XPathEvaluationContext();
        return equal!(ComparisonRelation.Equal, left, right, context);
    }
}
