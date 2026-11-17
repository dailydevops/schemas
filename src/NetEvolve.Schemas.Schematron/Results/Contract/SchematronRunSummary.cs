#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the public result types (design #137 section 2)

namespace NetEvolve.Schemas.Schematron;

/// <summary>The end of a run.</summary>
public readonly ref struct SchematronRunSummary
{
    internal SchematronRunSummary(bool isTruncated, DiagnosticCode stopCode)
    {
        IsTruncated = isTruncated;
        StopCode = stopCode;
    }

    /// <summary>Gets a value indicating whether evaluation stopped before the end of the document.</summary>
    public bool IsTruncated { get; }

    /// <summary>
    /// Gets the code of the stop, <see cref="SchematronDiagnosticCodes.IssueLimitStop"/> for the issue limit and a core limit
    /// code for a limit breach; <see langword="default"/> when the run was not truncated.
    /// </summary>
    public DiagnosticCode StopCode { get; }
}
