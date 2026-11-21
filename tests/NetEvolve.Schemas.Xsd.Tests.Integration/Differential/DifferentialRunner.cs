namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>Runs cases through this library and the baseline and checks every difference against the deviation list.</summary>
internal static class DifferentialRunner
{
    public static Task<DifferentialReport> RunAsync(
        IReadOnlyList<DifferentialCase> cases,
        IDifferentialEngine ours,
        IDifferentialEngine baseline,
        DeviationList deviations,
        DifferentialEnvironment environment,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            new DifferentialReport(
                false,
                [
                    $"Not implemented: {cases.Count} {ours.GetType().Name} {baseline.GetType().Name} {deviations.BaselineRuntime} {environment.TargetFramework} {cancellationToken.IsCancellationRequested}",
                ]
            )
        );
}
