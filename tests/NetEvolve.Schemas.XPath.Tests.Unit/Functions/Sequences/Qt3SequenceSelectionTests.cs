namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>Runs the QT3 cases of <see cref="Qt3SequenceSelection"/> through the compiled expressions and checks each against its assertion.</summary>
public class Qt3SequenceSelectionTests
{
    private static readonly string[] TestSets =
    [
        "fn-count",
        "fn-sum",
        "fn-avg",
        "fn-min",
        "fn-max",
        "fn-distinct-values",
        "fn-deep-equal",
        "fn-index-of",
        "fn-subsequence",
        "fn-exists",
        "fn-empty",
    ];

    public static IEnumerable<Func<(string Id, string Expression, string Expectation)>> Cases()
    {
        foreach (var entry in Qt3SequenceSelection.Cases)
        {
            var captured = entry;
            yield return () => captured;
        }
    }

    [Test]
    public async Task Selection_NamesEachCaseOnce_FromTheElevenTestSets()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var sets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, expression, expectation) in Qt3SequenceSelection.Cases)
        {
            await Assert.That(ids.Add(id)).IsTrue().Because($"{id} is listed twice");
            _ = sets.Add(id.Substring(0, id.IndexOf('/', StringComparison.Ordinal)));
            await Assert.That(expression).IsNotEmpty();
            await Assert.That(expectation).IsNotEmpty();
        }

        await Assert.That(sets.Count).IsEqualTo(TestSets.Length);
        foreach (var set in TestSets)
        {
            await Assert.That(sets.Contains(set)).IsTrue().Because($"{set} has no case in the selection");
        }

        await Assert.That(ids.Count).IsGreaterThan(600);
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Case_SatisfiesItsAssertion(string id, string expression, string expectation)
    {
        var (result, code) = Qt3SequenceFixture.Run(expression);

        await Assert
            .That(Qt3SequenceFixture.Satisfies(expectation, result, code))
            .IsTrue()
            .Because($"{id}: {expression} gave {Qt3SequenceFixture.Describe(result, code)}, expected {expectation}");
    }
}
