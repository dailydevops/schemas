namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

internal static class Qt3Xp3Exclusions
{
    public static IReadOnlyList<Qt3ExclusionEntry> All { get; } = [new(Qt3ExclusionKind.TestSet, "stub", "stub")];

    public static bool Matches(Qt3ExclusionEntry entry, Qt3Dependency dependency) =>
        entry is null || dependency is null;
}
