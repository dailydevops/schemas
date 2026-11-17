#pragma warning disable CA1815 // Approved shape (#123): the struct defines no value equality.

namespace NetEvolve.Schemas.XPath.Api;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The outcome of an evaluation: status, value, and diagnostic.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathResult
{
    /// <summary>Initializes a new instance of the <see cref="XPathResult"/> struct.</summary>
    /// <param name="status">The status.</param>
    /// <param name="value">The value.</param>
    /// <param name="diagnostic">The diagnostic, <see langword="default"/> on success.</param>
    public XPathResult(XPathEvaluationStatus status, XdmSequence value, XPathDiagnostic diagnostic)
    {
        Status = status;
        Value = value;
        Diagnostic = diagnostic;
    }

    /// <summary>Gets the status.</summary>
    public XPathEvaluationStatus Status { get; }

    /// <summary>Gets the value; valid until the next evaluation on, or disposal of, the context.</summary>
    public XdmSequence Value { get; }

    /// <summary>Gets the diagnostic.</summary>
    public XPathDiagnostic Diagnostic { get; }
}
