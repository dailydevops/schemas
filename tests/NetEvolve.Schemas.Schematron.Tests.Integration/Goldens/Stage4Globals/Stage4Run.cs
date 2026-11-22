namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage4Globals;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Runs the stage 4 conformance cases and the own schemas once per process; every test reads its results from this run.</summary>
internal static class Stage4Run
{
    /// <summary>The test set of the conformance suite cases of the stage.</summary>
    public const string ConformanceTestSet = "stage4-conformance-suite";

    /// <summary>The package name of the rate rows in the job summary; it differs from the conformance suite row on purpose.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (stage 4 goldens)";

    private const string RecordFile = "schematron-stage4.json";

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
            "Stage4Globals"
        );

    /// <summary>Reads the stage record.</summary>
    /// <returns>The record.</returns>
    public static Stage4Manifest LoadManifest() =>
        Stage4Manifest.Parse(File.ReadAllText(Path.Combine(SuitesDirectory, RecordFile)));

    /// <summary>Gets the results of the conformance cases and of the own documents.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per case and per document.</returns>
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

    private static async Task<IReadOnlyList<CaseResult>> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await SchematronSuite.LoadSelectionAsync(cancellationToken);
        var manifest = LoadManifest();
        var named = new List<SchematronCase>(manifest.Cases.Count);
        foreach (var id in manifest.Cases)
        {
            named.Add(
                selection.Selected.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal))
                    ?? throw new InvalidDataException(
                        $"The stage 4 case '{id}' is not a selected case of the pinned suite."
                    )
            );
        }

        var results = new List<CaseResult>();
        foreach (
            var result in await SchematronConformanceRunner.RunAsync(
                new CaseSelection(named, []),
                SchematronEngine.Instance,
                cancellationToken
            )
        )
        {
            results.Add(result with { TestSet = ConformanceTestSet });
        }

        results.AddRange(
            Stage4OwnSchemaRunner.Run(manifest, StageDirectory, SchematronSuite.RepositoryRoot, cancellationToken)
        );
        return results;
    }
}
