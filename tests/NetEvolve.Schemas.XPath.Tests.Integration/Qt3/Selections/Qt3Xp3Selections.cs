namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

internal static class Qt3Xp3Selections
{
    public static string BaselineDirectoryOf(Qt3SpecLevel level) => Qt3Suite.BaselineDirectory + level;

    public static string FormatCounts(Qt3Selection selection) => selection.SelectedTestSets.Count + "x";

    public static string CountsFileOf(Qt3SpecLevel level) => Path.Combine(Qt3Suite.RepositoryRoot, "missing" + level);

    public static async Task<Qt3Selection> GetSelectionAsync(Qt3SpecLevel level, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = level;
        return await Qt3Suite.GetSelectionAsync(cancellationToken);
    }

    public static async Task<Qt3Selection> GetNewCasesAsync(Qt3SpecLevel level, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = level;
        return await Qt3Suite.GetSelectionAsync(cancellationToken);
    }

    public static IReadOnlyDictionary<string, KnownFailureBaseline> LoadBaselines(Qt3SpecLevel level)
    {
        _ = level;
        return new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
    }

    public static void RegenerateBaselines(Qt3SpecLevel level, Qt3Selection newCases, IQt3Engine engine)
    {
        _ = level;
        _ = newCases;
        _ = engine;
    }
}
