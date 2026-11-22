namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

internal static class Qt3Xp3Selector
{
    public static Qt3Selection Select(IReadOnlyList<Qt3TestSet> testSets, Qt3SpecLevel level)
    {
        _ = testSets;
        _ = level;
        return new Qt3Selection([], []);
    }
}
