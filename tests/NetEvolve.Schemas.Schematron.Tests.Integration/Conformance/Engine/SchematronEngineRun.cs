namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Runs the selected cases of the pinned suite on the engine once per process; every test set reads its results from this run.</summary>
internal static class SchematronEngineRun
{
    /// <summary>The test sets of the pinned suite, one baseline file each when it has known failures.</summary>
    public static IReadOnlyList<string> TestSets { get; } =
    [
        "iso-instance-document-selection",
        "iso-pattern-templating",
        "iso-reporting",
        "iso-rule-based-validation",
        "iso-schema-composition",
    ];

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IReadOnlyList<CaseResult>? s_results;

    /// <summary>Gets the results of all selected and excluded cases.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per case.</returns>
    public static async Task<IReadOnlyList<CaseResult>> GetResultsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_results ??= await SchematronConformanceRunner.RunAsync(
                await SchematronSuite.LoadSelectionAsync(cancellationToken),
                SchematronEngine.Instance,
                cancellationToken
            );
        }
        finally
        {
            _ = Gate.Release();
        }
    }
}
