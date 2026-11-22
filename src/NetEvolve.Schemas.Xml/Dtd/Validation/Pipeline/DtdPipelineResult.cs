namespace NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The outcome of one run of the validating reader mode: how the reader ended, the fatal diagnostic of the reader, and the
/// issues of the run. A fatal status of the reader is also the last issue, so <see cref="Issues"/> alone tells whether the
/// document is valid; the status and the diagnostic tell a fatal status apart from validity errors.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct DtdPipelineResult
{
    /// <summary>Initializes a new instance of the <see cref="DtdPipelineResult"/> struct.</summary>
    /// <param name="status">How the reader ended.</param>
    /// <param name="diagnostic">The fatal diagnostic of the reader, <see langword="null"/> when the document is well-formed.</param>
    /// <param name="issues">The issues of the run in the order of the events.</param>
    internal DtdPipelineResult(XmlReadStatus status, XmlDiagnostic? diagnostic, ValidationResult issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        Status = status;
        Diagnostic = diagnostic;
        Issues = issues;
    }

    /// <summary>Gets how the reader ended: <see cref="XmlReadStatus.Completed"/> for a well-formed document.</summary>
    internal XmlReadStatus Status { get; }

    /// <summary>Gets the fatal diagnostic of the reader, <see langword="null"/> when the document is well-formed.</summary>
    internal XmlDiagnostic? Diagnostic { get; }

    /// <summary>Gets the issues of the run: validity errors and warnings, and as the last one the fatal status of the reader or of a step.</summary>
    internal ValidationResult Issues { get; }

    /// <summary>Gets a value indicating whether the reader read the whole document without a fatal status.</summary>
    internal bool IsWellFormed => Status == XmlReadStatus.Completed;
}
