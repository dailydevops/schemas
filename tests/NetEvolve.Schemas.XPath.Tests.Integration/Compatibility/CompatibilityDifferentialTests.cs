namespace NetEvolve.Schemas.XPath.Tests.Integration.Compatibility;

using System.Collections.Generic;

/// <summary>
/// The corpus of XPath 1.0 expressions evaluated by the library in compatibility mode and asserted against the recorded results of
/// System.Xml.XPath (<see cref="CompatibilityExpectations"/>); a case in <see cref="ListedDifferences"/> is asserted against the listed
/// result instead. These tests call no System.Xml.XPath, so they run on every leg; <see cref="CompatibilityBaselineTests"/> compares
/// with the live baseline on the designated leg.
/// </summary>
public class CompatibilityDifferentialTests
{
    public static IEnumerable<Func<(string Document, string Expression)>> Cases()
    {
        foreach (var testCase in CompatibilityCorpus.Cases)
        {
            var current = testCase;
            yield return () => (current.Document, current.Expression);
        }
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Expression_GivesTheRecordedResultOrTheListedDifference(string document, string expression)
    {
        var key = new CorpusCase(document, expression).Key;
        var expected = ListedDifferences.All.TryGetValue(key, out var listed)
            ? listed.Ours
            : CompatibilityExpectations.All[key];

        await Assert.That(CompatibilityEngine.Evaluate(document, expression, compatibility: true)).IsEqualTo(expected);
    }

    [Test]
    public async Task Expectations_CoverExactlyTheCasesOfTheCorpus()
    {
        var keys = new List<string>();
        foreach (var testCase in CompatibilityCorpus.Cases)
        {
            keys.Add(testCase.Key);
        }

        await Assert.That(keys).HasDistinctItems();
        await Assert.That(CompatibilityExpectations.All.Keys).IsEquivalentTo(keys);
    }

    [Test]
    public async Task ListedDifferences_AreCasesOfTheCorpusWithAReasonAndARealDifference()
    {
        foreach (var (key, difference) in ListedDifferences.All)
        {
            await Assert.That(CompatibilityExpectations.All.ContainsKey(key)).IsTrue().Because(key);
            await Assert.That(difference.Reason).IsNotEmpty().Because(key);
            await Assert.That(difference.Ours).IsNotEqualTo(CompatibilityExpectations.All[key]).Because(key);
        }
    }
}
