namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>Counts, per test set, the cases of the four structural assertion kinds that moved from skipped to passed or failed.</summary>
internal static class StructuralMovement
{
    /// <summary>Counts the structural cases by test set and outcome.</summary>
    /// <param name="cases">The cases that were run.</param>
    /// <param name="results">Their results, matched by case id.</param>
    /// <returns>One entry per test set that has a structural case, in ordinal order of the name.</returns>
    public static IReadOnlyList<SetMovement> Count(IEnumerable<Qt3Case> cases, IEnumerable<CaseResult> results) =>
        throw new NotSupportedException();

    /// <summary>Renders the counts as a Markdown table with a total row.</summary>
    /// <param name="movement">The counts.</param>
    /// <returns>The table; lines end with <c>\n</c>.</returns>
    public static string Render(IReadOnlyList<SetMovement> movement) => throw new NotSupportedException();
}
