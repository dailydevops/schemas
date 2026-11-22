namespace NetEvolve.Schemas.Xml.Contracts;

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>An integer handle for one string interned in an <see cref="XmlNameTable"/>: a prefix, a local name, or a namespace URI.</summary>
/// <remarks><see langword="default"/> is the empty string in every table. A handle is meaningless without the table that issued it.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XmlSymbol : IEquatable<XmlSymbol>
{
    /// <summary>Initializes a new instance of the <see cref="XmlSymbol"/> struct.</summary>
    /// <param name="value">The handle value, zero or greater.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public XmlSymbol(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
        Value = value;
    }

    /// <summary>Gets the handle value; zero is the empty string.</summary>
    public int Value { get; }

    /// <summary>Gets a value indicating whether the handle is the empty string.</summary>
    public bool IsEmpty => Value == 0;

    /// <inheritdoc />
    public bool Equals(XmlSymbol other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XmlSymbol other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Determines whether two handles are equal.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XmlSymbol left, XmlSymbol right) => left.Equals(right);

    /// <summary>Determines whether two handles differ.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XmlSymbol left, XmlSymbol right) => !left.Equals(right);
}
