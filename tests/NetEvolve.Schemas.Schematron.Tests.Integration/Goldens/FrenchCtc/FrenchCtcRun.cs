namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.FrenchCtc;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

/// <summary>Validates the sample documents of the French CTC, CDAR and Flux10 rule sets once per process.</summary>
/// <remarks>The record reuses the pinned archive and the record format of stage 3; it is a separate record because these rule sets do not call <c>document()</c>.</remarks>
internal static class FrenchCtcRun
{
    /// <summary>The package name of the rate rows in the job summary; it differs from the conformance suite row on purpose.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (french ctc goldens)";

    private const string RecordFile = "schematron-french-ctc.json";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IReadOnlyList<CaseResult>? s_results;

    /// <summary>Reads the record.</summary>
    /// <returns>The record.</returns>
    public static Stage3Manifest LoadManifest() =>
        Stage3Manifest.Parse(File.ReadAllText(Path.Combine(Stage3Run.SuitesDirectory, RecordFile)));

    /// <summary>Gets the results of the sample documents and of the build-only cases.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per document or per rule set without documents.</returns>
    public static async Task<IReadOnlyList<CaseResult>> GetResultsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage3Run.GetRootAsync(cancellationToken);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (s_results is null)
            {
                var manifest = LoadManifest();
                s_results =
                [
                    .. Stage3Runner.Run(
                        manifest,
                        root,
                        directory => new SandboxedFileSystemResourceResolver(directory),
                        cancellationToken
                    ),
                    .. FrenchCtcRunner.RunBuildOnly(manifest, root, cancellationToken),
                ];
            }

            return s_results;
        }
        finally
        {
            _ = Gate.Release();
        }
    }
}
