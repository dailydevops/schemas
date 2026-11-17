namespace NetEvolve.Schemas.Xml.Contracts;

using System;
using System.Globalization;
using System.Runtime.InteropServices;

/// <summary>A position in the decoded character stream, counted in UTF-16 code units.</summary>
/// <remarks>Line and column are one-based and zero means unknown; the offset is zero-based. A leading byte order mark is not counted.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XmlPosition : IEquatable<XmlPosition>
{
    /// <summary>Initializes a new instance of the <see cref="XmlPosition"/> struct.</summary>
    /// <param name="line">The one-based line, zero when unknown.</param>
    /// <param name="column">The one-based column, zero when unknown.</param>
    /// <param name="charOffset">The zero-based offset in the decoded character stream.</param>
    public XmlPosition(int line, int column, long charOffset)
    {
        Line = line;
        Column = column;
        CharOffset = charOffset;
    }

    /// <summary>Gets the one-based line, zero when unknown.</summary>
    public int Line { get; }

    /// <summary>Gets the one-based column, zero when unknown.</summary>
    public int Column { get; }

    /// <summary>Gets the zero-based offset in the decoded character stream.</summary>
    public long CharOffset { get; }

    /// <inheritdoc />
    public bool Equals(XmlPosition other) =>
        Line == other.Line && Column == other.Column && CharOffset == other.CharOffset;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XmlPosition other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((((Line * 31) + Column) * 31) + CharOffset.GetHashCode());

    /// <inheritdoc />
    public override string ToString() =>
        Line.ToString(CultureInfo.InvariantCulture)
        + ":"
        + Column.ToString(CultureInfo.InvariantCulture)
        + " (offset "
        + CharOffset.ToString(CultureInfo.InvariantCulture)
        + ")";

    /// <summary>Determines whether two positions are equal.</summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XmlPosition left, XmlPosition right) => left.Equals(right);

    /// <summary>Determines whether two positions differ.</summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XmlPosition left, XmlPosition right) => !left.Equals(right);
}
