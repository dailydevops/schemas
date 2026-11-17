namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>
/// Maps a query binding to the suite file that lists its cases and says whether the binding is selected.
/// A binding with an exclusion reason is not selected; its cases do not count toward the rate.
/// </summary>
/// <param name="binding">The binding id.</param>
/// <param name="suiteFile">The suite file of the binding, relative to the suite root.</param>
/// <param name="exclusionReason">Why the binding is not selected, or <see langword="null"/> when it is.</param>
internal sealed class BindingRule(string binding, string suiteFile, string? exclusionReason = null)
{
    public string Binding { get; } = binding;

    public string SuiteFile { get; } = suiteFile;

    public string? ExclusionReason { get; } = Validate(exclusionReason);

    public bool IsSelected => ExclusionReason is null;

    private static string? Validate(string? reason)
    {
        if (reason is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        }

        return reason;
    }
}
