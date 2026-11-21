namespace NetEvolve.Schemas.XPath.Tests.Unit.Collations;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs the QT3 cases of <see cref="Qt3CollationSelection"/> through the compiled expressions and checks each against its assertion, and
/// proves that the selection is exactly the applicable cases of the test sets <c>fn-compare</c> and <c>fn-codepoint-equal</c>: the cases
/// that the XPath 2.0 rule of the QT3 runner selects, which the baselines of <c>tests/conformance/baselines/qt3</c> list.
/// </summary>
public class Qt3CollationSelectionTests
{
    private static readonly string[] TestSets = ["fn-compare", "fn-codepoint-equal"];

    public static IEnumerable<Func<(string Id, string Expression, string Expectation)>> Cases()
    {
        foreach (var entry in Qt3CollationSelection.Cases)
        {
            var captured = entry;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Case_SatisfiesItsAssertion(string id, string expression, string expectation)
    {
        var (result, code) = Qt3CollationFixture.Run(expression);

        await Assert
            .That(Qt3SequenceFixture.Satisfies(expectation, result, code))
            .IsTrue()
            .Because($"{id}: {expression} gave {Qt3SequenceFixture.Describe(result, code)}, expected {expectation}");
    }

    [Test]
    public async Task Selection_ListsEachCaseOnce()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, expression, expectation) in Qt3CollationSelection.Cases)
        {
            await Assert.That(seen.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert.That(expression).IsNotEmpty();
            await Assert.That(expectation).IsNotEmpty();
        }
    }

    [Test]
    public async Task NotApplicable_AreNotSelected_AndHaveAReason()
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, _, _) in Qt3CollationSelection.Cases)
        {
            _ = selected.Add(id);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, reason) in Qt3CollationSelection.NotApplicable)
        {
            await Assert.That(seen.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert.That(selected.Contains(id)).IsFalse().Because($"{id} is selected");
            await Assert.That(reason).IsNotEmpty();
        }
    }

    [Test]
    public async Task Selection_HasACaseInEveryTestSet()
    {
        var sets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, _, _) in Qt3CollationSelection.Cases)
        {
            _ = sets.Add(id.Substring(0, id.IndexOf('/', StringComparison.Ordinal)));
        }

        foreach (var set in TestSets)
        {
            await Assert.That(sets.Contains(set)).IsTrue().Because($"{set} has no case in the selection");
        }
    }
}
