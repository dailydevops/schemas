namespace NetEvolve.Schemas.Xml.Datatypes.Numeric;

using System;
using System.Globalization;
using System.Numerics;

/// <summary>A value of <c>xs:integer</c> or one of its derived types: an arbitrary-precision integer (XSD 1.1 Part 2, section 3.4.13).</summary>
public readonly struct XsdInteger : IEquatable<XsdInteger>, IComparable<XsdInteger>
{
    /// <summary>Initializes a new instance of the <see cref="XsdInteger"/> struct.</summary>
    /// <param name="value">The integer value.</param>
    public XsdInteger(BigInteger value) => Value = value;

    /// <summary>Gets the integer value.</summary>
    public BigInteger Value { get; }

    /// <summary>Compares this value with another by numeric order.</summary>
    /// <param name="other">The other value.</param>
    /// <returns>A negative number, zero or a positive number when this value is less than, equal to or greater than <paramref name="other"/>.</returns>
    public int CompareTo(XsdInteger other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    public bool Equals(XsdInteger other) => Value.Equals(other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XsdInteger other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>Gets the canonical form: no plus sign, no leading zeros, and <c>0</c> for zero.</summary>
    /// <returns>The canonical form.</returns>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Determines whether two values are equal.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the values are equal.</returns>
    public static bool operator ==(XsdInteger left, XsdInteger right) => left.Equals(right);

    /// <summary>Determines whether two values differ.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the values differ.</returns>
    public static bool operator !=(XsdInteger left, XsdInteger right) => !left.Equals(right);

    /// <summary>Determines whether the left value is less than the right value.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> is less.</returns>
    public static bool operator <(XsdInteger left, XsdInteger right) => left.CompareTo(right) < 0;

    /// <summary>Determines whether the left value is less than or equal to the right value.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> is less or equal.</returns>
    public static bool operator <=(XsdInteger left, XsdInteger right) => left.CompareTo(right) <= 0;

    /// <summary>Determines whether the left value is greater than the right value.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> is greater.</returns>
    public static bool operator >(XsdInteger left, XsdInteger right) => left.CompareTo(right) > 0;

    /// <summary>Determines whether the left value is greater than or equal to the right value.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> is greater or equal.</returns>
    public static bool operator >=(XsdInteger left, XsdInteger right) => left.CompareTo(right) >= 0;
}
