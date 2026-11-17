namespace NetEvolve.Schemas.XPath.Api;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The outcome of an evaluation: status, value, and diagnostic.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathResult : IEquatable<XPathResult>
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

    /// <inheritdoc />
    public bool Equals(XPathResult other) =>
        Status == other.Status && Value.Equals(other.Value) && Diagnostic == other.Diagnostic;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XPathResult other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked((((((int)Status * 31) + Value.GetHashCode()) * 31)) + Diagnostic.GetHashCode());

    /// <summary>Determines whether two results are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XPathResult left, XPathResult right) => left.Equals(right);

    /// <summary>Determines whether two results differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XPathResult left, XPathResult right) => !left.Equals(right);
}
