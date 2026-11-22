namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>Counts, per test set, the cases of the four structural assertion kinds that moved from skipped to passed or failed.</summary>
internal static class StructuralMovement
{
    /// <summary>Counts the structural cases by test set and outcome.</summary>
    /// <param name="cases">The cases that were run.</param>
    /// <param name="results">Their results, matched by case id.</param>
    /// <returns>One entry per test set that has a structural case, in ordinal order of the name.</returns>
    public static IReadOnlyList<SetMovement> Count(IEnumerable<Qt3Case> cases, IEnumerable<CaseResult> results)
    {
        var outcomes = new Dictionary<string, CaseOutcome>(StringComparer.Ordinal);
        foreach (var result in results)
        {
            outcomes[result.CaseId] = result.Outcome;
        }

        var sets = new SortedDictionary<string, SetMovement>(StringComparer.Ordinal);
        foreach (var testCase in cases)
        {
            if (!testCase.Assertion.IsStructural)
            {
                continue;
            }

            var current = sets.TryGetValue(testCase.TestSet, out var found) ? found : new(testCase.TestSet, 0, 0, 0);
            sets[testCase.TestSet] = outcomes.GetValueOrDefault(testCase.Id, CaseOutcome.Skipped) switch
            {
                CaseOutcome.Pass => current with { Passed = current.Passed + 1 },
                CaseOutcome.Fail => current with { Failed = current.Failed + 1 },
                _ => current with { Skipped = current.Skipped + 1 },
            };
        }
        return [.. sets.Values];
    }

    /// <summary>Renders the counts as a Markdown table with a total row.</summary>
    /// <param name="movement">The counts.</param>
    /// <returns>The table; lines end with <c>\n</c>.</returns>
    public static string Render(IReadOnlyList<SetMovement> movement)
    {
        var table = new StringBuilder("| test set | passed | failed | moved |\n| --- | ---: | ---: | ---: |\n");
        var passed = 0;
        var failed = 0;
        foreach (var set in movement)
        {
            passed += set.Passed;
            failed += set.Failed;
            Row(table, set.TestSet, set.Passed, set.Failed);
        }
        Row(table, "total", passed, failed);
        return table.ToString();
    }

    private static void Row(StringBuilder table, string name, int passed, int failed) =>
        _ = table.Append(CultureInfo.InvariantCulture, $"| {name} | {passed} | {failed} | {passed + failed} |\n");
}
