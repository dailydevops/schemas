namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>Compares what this library and the baseline observed for one case.</summary>
internal static class DifferentialComparer
{
    /// <summary>Lists the differences; issue positions are compared only when both schema and instance verdicts agree.</summary>
    public static IReadOnlyList<Difference> Compare(
        DifferentialCase differentialCase,
        EngineResult ours,
        EngineResult baseline
    ) => throw new NotSupportedException("Not implemented yet.");
}
