namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.EformsSdk;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>Compares the SVRL report of the engine with the expected report of the eForms SDK for one notice.</summary>
/// <remarks>
/// <para>
/// The first verdict is the fired rule ids with their roles and locations, one case per validation stage that either report touches,
/// in the test set <c>stage-N</c>. A notice that fires nothing in both reports is one case of the test set <c>valid</c>. Findings are
/// compared as a multiset, so the order of the report does not matter.
/// </para>
/// <para>
/// The second verdict is the text, in the test set <c>text</c>: the findings that both reports share must carry the same text with
/// collapsed white space. It is separate, so a wording difference never hides or blames a rule id verdict.
/// </para>
/// </remarks>
internal static class EformsComparer
{
    /// <summary>The test set of the notices that fire nothing.</summary>
    public const string ValidTestSet = "valid";

    /// <summary>The test set of the text verdicts.</summary>
    public const string TextTestSet = "text";

    private const int MaxListed = 3;

    /// <summary>Compares the two reports of one notice.</summary>
    /// <param name="notice">The file name of the notice.</param>
    /// <param name="expected">The expected report of the SDK.</param>
    /// <param name="actual">The report of the engine.</param>
    /// <returns>The verdicts of the notice.</returns>
    public static IReadOnlyList<CaseResult> Compare(string notice, EformsReport expected, EformsReport actual)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(notice);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        var results = new List<CaseResult>();
        if (expected.Findings.Count == 0 && actual.Findings.Count == 0)
        {
            results.Add(new CaseResult($"{ValidTestSet}/{notice}", ValidTestSet, CaseOutcome.Pass));
            return results;
        }

        foreach (var stage in expected.Findings.Concat(actual.Findings).Select(f => f.Stage).Distinct().Order())
        {
            var testSet = $"stage-{stage.ToString(CultureInfo.InvariantCulture)}";
            var reason = Difference(
                expected.Findings.Where(f => f.Stage == stage).Select(f => f.Key),
                actual.Findings.Where(f => f.Stage == stage).Select(f => f.Key)
            );
            results.Add(Result(testSet, notice, reason));
        }

        results.Add(Result(TextTestSet, notice, TextDifference(expected.Findings, actual.Findings)));
        return results;
    }

    private static CaseResult Result(string testSet, string notice, string? reason) =>
        reason is null
            ? new CaseResult($"{testSet}/{notice}", testSet, CaseOutcome.Pass)
            : new CaseResult($"{testSet}/{notice}", testSet, CaseOutcome.Fail, reason);

    private static string? TextDifference(IReadOnlyList<EformsFinding> expected, IReadOnlyList<EformsFinding> actual)
    {
        var shared = expected
            .Select(f => f.Key)
            .Intersect(actual.Select(f => f.Key), StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        return Difference(
            expected.Where(f => shared.Contains(f.Key)).Select(f => f.Key + "\t" + f.Text),
            actual.Where(f => shared.Contains(f.Key)).Select(f => f.Key + "\t" + f.Text)
        );
    }

    private static string? Difference(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        var balance = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var key in expected)
        {
            balance[key] = balance.GetValueOrDefault(key) + 1;
        }

        foreach (var key in actual)
        {
            balance[key] = balance.GetValueOrDefault(key) - 1;
        }

        var missing = balance.Where(p => p.Value > 0).OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
        var unexpected = balance.Where(p => p.Value < 0).OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
        return missing.Count == 0 && unexpected.Count == 0
            ? null
            : string.Join(" ", Describe("Missing", missing), Describe("Unexpected", unexpected)).Trim();
    }

    private static string Describe(string label, List<KeyValuePair<string, int>> entries) =>
        entries.Count == 0
            ? string.Empty
            : $"{label} {entries.Sum(e => Math.Abs(e.Value)).ToString(CultureInfo.InvariantCulture)}: "
                + string.Join("; ", entries.Take(MaxListed).Select(e => e.Key.Replace('\t', ' ')))
                + (entries.Count > MaxListed ? "; ..." : string.Empty)
                + ".";
}
