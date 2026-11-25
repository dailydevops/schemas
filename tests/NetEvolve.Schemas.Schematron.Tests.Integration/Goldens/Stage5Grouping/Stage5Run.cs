namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage5Grouping;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Runs the stage 5 own schemas once per process; every test reads its results from this run.</summary>
internal static class Stage5Run
{
    /// <summary>The package name of the rate rows in the job summary; it differs from the row of the conformance suite on purpose.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (stage 5 goldens)";

    private const string RecordFile = "schematron-stage5.json";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IReadOnlyList<CaseResult>? s_results;

    /// <summary>Gets the directory of the suite manifests.</summary>
    public static string SuitesDirectory { get; } =
        Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "suites");

    /// <summary>Gets the directory of the own schemas and documents.</summary>
    public static string StageDirectory { get; } =
        Path.Combine(
            SchematronSuite.RepositoryRoot,
            "tests",
            "NetEvolve.Schemas.Schematron.Tests.Integration",
            "Goldens",
            "Stage5Grouping"
        );

    /// <summary>Reads the stage record.</summary>
    /// <returns>The record.</returns>
    public static Stage5Manifest LoadManifest() =>
        Stage5Manifest.Parse(File.ReadAllText(Path.Combine(SuitesDirectory, RecordFile)));

    /// <summary>Gets the results of the own documents.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per document.</returns>
    public static async Task<IReadOnlyList<CaseResult>> GetResultsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_results ??= await RunAsync(cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    private static Task<IReadOnlyList<CaseResult>> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            Stage5OwnSchemaRunner.Run(LoadManifest(), StageDirectory, SchematronSuite.RepositoryRoot, cancellationToken)
        );
    }
}
