namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>
/// The accepted differences between this library and the baseline. The text form starts with the header line
/// <c>baseline-runtime: &lt;major.minor&gt;</c>; every other non-blank line is a comment (<c>#</c>) or one entry with five
/// fields separated by <c> | </c>: case id, difference kind, observed difference, specification reference, reason.
/// </summary>
internal sealed class DeviationList
{
    public DeviationList(string baselineRuntime, IReadOnlyList<DeviationEntry> entries)
    {
        var keys = new HashSet<(string, DifferenceKind)>();
        foreach (var entry in entries)
        {
            if (!keys.Add((entry.CaseId, entry.Kind)))
            {
                throw new DeviationFormatException($"Two entries for case '{entry.CaseId}' and kind {entry.Kind}.");
            }
        }

        BaselineRuntime = baselineRuntime;
        Entries = entries;
    }

    /// <summary>The major and minor version of the runtime that produced the baseline verdicts.</summary>
    public string BaselineRuntime { get; }

    public IReadOnlyList<DeviationEntry> Entries { get; }

    public static DeviationList Parse(string text) => throw new NotSupportedException("Not implemented yet.");

    public static DeviationList Load(string path) => throw new NotSupportedException("Not implemented yet.");

    /// <summary>Finds the entry for a case and kind; the observed text is not compared here.</summary>
    public DeviationEntry? Find(string caseId, DifferenceKind kind) =>
        throw new NotSupportedException("Not implemented yet.");
}
