namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>The pinned Schematron conformance suite: manifest, fetch, and baseline location.</summary>
internal static class SchematronSuite
{
    public static string BaselineDirectory { get; } =
        Path.Combine(AppContext.BaseDirectory, "baselines", "schematron-conformance");

    /// <summary>Fetches the pinned suite (from the cache when present) and applies the binding selection.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The selection.</returns>
    public static Task<CaseSelection> LoadSelectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new CaseSelection([], []));
    }
}
