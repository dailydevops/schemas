namespace NetEvolve.Schemas.XPath.Errors;

using System;
using System.Runtime.InteropServices;

/// <summary>An XPath error as a code with its kind, message, and expression location. Immutable.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathDiagnostic : IEquatable<XPathDiagnostic>
{
    private readonly string? _message;

    /// <summary>Initializes a new instance of the <see cref="XPathDiagnostic"/> struct.</summary>
    /// <param name="qname">The error code.</param>
    /// <param name="kind">The error kind.</param>
    /// <param name="message">The message.</param>
    /// <param name="offset">The offset in the expression, or -1 when unknown (always so at run time).</param>
    /// <param name="length">The length in the expression, zero when unknown.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is below -1 or <paramref name="length"/> is negative.</exception>
    public XPathDiagnostic(XPathErrorCode qname, XPathErrorKind kind, string message, int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, -1);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);
        ArgumentNullException.ThrowIfNull(message);

        QName = qname;
        Kind = kind;
        _message = message;
        Offset = offset;
        Length = length;
    }

    /// <summary>Gets the error code as a QName.</summary>
    public XPathErrorCode QName { get; }

    /// <summary>Gets the text form of the code, for example <c>XPST0003</c>.</summary>
    public string Code => QName.ToString();

    /// <summary>Gets the error kind.</summary>
    public XPathErrorKind Kind { get; }

    /// <summary>Gets the message.</summary>
    public string Message => _message ?? string.Empty;

    /// <summary>Gets the offset in the expression, -1 when unknown.</summary>
    public int Offset { get; }

    /// <summary>Gets the length in the expression, zero when unknown.</summary>
    public int Length { get; }

    /// <inheritdoc />
    public bool Equals(XPathDiagnostic other) =>
        QName.Equals(other.QName)
        && Kind == other.Kind
        && Offset == other.Offset
        && Length == other.Length
        && string.Equals(Message, other.Message, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XPathDiagnostic other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((QName.GetHashCode() * 31) + Offset);

    /// <summary>Determines whether two diagnostics are equal.</summary>
    /// <param name="left">The first diagnostic.</param>
    /// <param name="right">The second diagnostic.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XPathDiagnostic left, XPathDiagnostic right) => left.Equals(right);

    /// <summary>Determines whether two diagnostics differ.</summary>
    /// <param name="left">The first diagnostic.</param>
    /// <param name="right">The second diagnostic.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XPathDiagnostic left, XPathDiagnostic right) => !left.Equals(right);

    /// <summary>Maps a code to a diagnostic with its kind and default message.</summary>
    /// <param name="code">The error code; a code outside the table is an engine error whose message is the code text.</param>
    /// <param name="offset">The offset in the expression, or -1 when unknown.</param>
    /// <param name="length">The length in the expression.</param>
    /// <returns>The diagnostic.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is below -1 or <paramref name="length"/> is negative.</exception>
    public static XPathDiagnostic Create(XPathErrorCode code, int offset = -1, int length = 0) =>
        new(
            code,
            XPathErrorCodes.KindOf(code),
            XPathErrorCodes.TryGetMessage(code, out var message) ? message : code.ToString(),
            offset,
            length
        );
}
