namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>An item: a node or an atomic value. A tagged struct; boolean, integer, double, and node need no allocation.</summary>
/// <remarks>
/// Limitation: the public factories and accessors cover boolean, integer, double, string, node, decimal, date and time, and
/// duration values; <c>xs:float</c>, <c>xs:untypedAtomic</c>, <c>xs:anyURI</c>, the Gregorian types, <c>xs:hexBinary</c>,
/// <c>xs:base64Binary</c>, and <c>xs:QName</c> are internal, and <c>xs:NOTATION</c> has no item because it is never the type of a
/// value in XPath 2.0 (it is abstract). Decimal, date and time, Gregorian, duration, binary, and QName values are boxed.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XdmItem : IEquatable<XdmItem>
{
    private readonly long _bits;
    private readonly object? _reference;

    private XdmItem(XdmItemKind kind, XdmTypeId typeId, long bits, object? reference)
    {
        Kind = kind;
        TypeId = typeId;
        _bits = bits;
        _reference = reference;
    }

    /// <summary>Gets the kind.</summary>
    public XdmItemKind Kind { get; }

    /// <summary>Gets the type annotation.</summary>
    public XdmTypeId TypeId { get; }

    /// <summary>Gets a value indicating whether the item is a node.</summary>
    public bool IsNode => Kind == XdmItemKind.Node;

    /// <summary>Creates a boolean item.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item.</returns>
    public static XdmItem FromBoolean(bool value) => new(XdmItemKind.Boolean, XdmTypeId.Boolean, value ? 1 : 0, null);

    /// <summary>Creates an integer item.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item.</returns>
    public static XdmItem FromInteger(long value) => new(XdmItemKind.XsInteger, XdmTypeId.XsInteger, value, null);

    /// <summary>Creates an <c>xs:integer</c> item of any width.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The inline integer when the value fits 64 bits; otherwise an item of the decimal family that carries the <c>xs:integer</c> type.</returns>
    /// <remarks>Limitation: a value beyond 64 bits is boxed, and <see cref="TryGetInteger"/> does not read it; use <see cref="TryGetBigInteger"/> (#123 AP2).</remarks>
    internal static XdmItem FromBigInteger(BigInteger value) =>
        value >= long.MinValue && value <= long.MaxValue
            ? FromInteger((long)value)
            : new(XdmItemKind.XsDecimal, XdmTypeId.XsInteger, 0, new XsdDecimal(value, 0));

    /// <summary>Creates a double item.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item.</returns>
    public static XdmItem FromDouble(double value) =>
        new(XdmItemKind.XsDouble, XdmTypeId.XsDouble, BitConverter.DoubleToInt64Bits(value), null);

    /// <summary>Creates a string item.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static XdmItem FromString(string value)
    {
        Argument.ThrowIfNull(value);

        return new(XdmItemKind.XsString, XdmTypeId.XsString, 0, value);
    }

    /// <summary>Creates an <c>xs:untypedAtomic</c> item.</summary>
    /// <param name="value">The lexical value.</param>
    /// <returns>The item.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    internal static XdmItem FromUntypedAtomic(string value)
    {
        Argument.ThrowIfNull(value);

        return new(XdmItemKind.UntypedAtomic, XdmTypeId.UntypedAtomic, 0, value);
    }

    /// <summary>Creates an <c>xs:anyURI</c> item.</summary>
    /// <param name="value">The lexical value.</param>
    /// <returns>The item.</returns>
    /// <remarks>Limitation: backed by the string like <c>xs:untypedAtomic</c> until the anyURI datatype value type (#545) replaces it.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    internal static XdmItem FromAnyUri(string value)
    {
        Argument.ThrowIfNull(value);

        return new(XdmItemKind.AnyUri, XdmTypeId.AnyUri, 0, value);
    }

    /// <summary>Creates an item of one of the eight date and time types: <c>xs:dateTime</c>, <c>xs:date</c>, <c>xs:time</c>, or a Gregorian type.</summary>
    /// <param name="value">The value; its kind selects the type of the item.</param>
    /// <returns>The item.</returns>
    /// <remarks>The public <see cref="FromDateTime"/> covers the first three kinds only.</remarks>
    internal static XdmItem FromTemporal(in XsdDateTime value) =>
        value.Kind switch
        {
            XsdDateTimeKind.DateTime => new(XdmItemKind.DateTime, XdmTypeId.DateTime, 0, value),
            XsdDateTimeKind.Date => new(XdmItemKind.Date, XdmTypeId.Date, 0, value),
            XsdDateTimeKind.Time => new(XdmItemKind.Time, XdmTypeId.Time, 0, value),
            XsdDateTimeKind.GYearMonth => new(XdmItemKind.GYearMonth, GregorianTypes.YearMonth, 0, value),
            XsdDateTimeKind.GYear => new(XdmItemKind.GYear, GregorianTypes.Year, 0, value),
            XsdDateTimeKind.GMonthDay => new(XdmItemKind.GMonthDay, GregorianTypes.MonthDay, 0, value),
            XsdDateTimeKind.GDay => new(XdmItemKind.GDay, GregorianTypes.Day, 0, value),
            _ => new(XdmItemKind.GMonth, GregorianTypes.Month, 0, value),
        };

    /// <summary>Creates an <c>xs:hexBinary</c> or <c>xs:base64Binary</c> item.</summary>
    /// <param name="value">The octets.</param>
    /// <param name="kind">The type of the item.</param>
    /// <returns>The item.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined kind.</exception>
    internal static XdmItem FromBinary(in XsdBinary value, BinaryKind kind)
    {
        Argument.ThrowIfLessThan((int)kind, (int)BinaryKind.HexBinary);
        Argument.ThrowIfGreaterThan((int)kind, (int)BinaryKind.Base64Binary);

        return kind == BinaryKind.HexBinary
            ? new(XdmItemKind.HexBinary, BinaryTypes.Hex, 0, value)
            : new(XdmItemKind.Base64Binary, BinaryTypes.Base64, 0, value);
    }

    /// <summary>Creates an <c>xs:QName</c> item.</summary>
    /// <param name="value">The expanded name with the prefix it was written with.</param>
    /// <returns>The item.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    internal static XdmItem FromQName(XdmQName value)
    {
        Argument.ThrowIfNull(value);

        return new(XdmItemKind.QName, BinaryTypes.QName, 0, value);
    }

    /// <summary>Creates an <c>xs:float</c> item.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item.</returns>
    internal static XdmItem FromFloat(float value) =>
        new(XdmItemKind.XsFloat, XdmTypeId.XsFloat, BitConverter.DoubleToInt64Bits(value), null);

    /// <summary>Creates a decimal item.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item.</returns>
    public static XdmItem FromDecimal(in XsdDecimal value) => new(XdmItemKind.XsDecimal, XdmTypeId.XsDecimal, 0, value);

    /// <summary>Creates an <c>xs:dateTime</c>, <c>xs:date</c>, or <c>xs:time</c> item, chosen by the kind of the value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item.</returns>
    public static XdmItem FromDateTime(in XsdDateTime value) =>
        value.Kind switch
        {
            XsdDateTimeKind.Date => new(XdmItemKind.Date, XdmTypeId.Date, 0, value),
            XsdDateTimeKind.Time => new(XdmItemKind.Time, XdmTypeId.Time, 0, value),
            _ => new(XdmItemKind.DateTime, XdmTypeId.DateTime, 0, value),
        };

    /// <summary>Creates an <c>xs:duration</c>, <c>xs:yearMonthDuration</c>, or <c>xs:dayTimeDuration</c> item.</summary>
    /// <param name="value">The value.</param>
    /// <param name="kind">The type of the item; the caller guarantees that the value fits it.</param>
    /// <returns>The item.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined kind.</exception>
    public static XdmItem FromDuration(in XsdDuration value, DurationKind kind = DurationKind.Duration)
    {
        Argument.ThrowIfLessThan((int)kind, (int)DurationKind.Duration);
        Argument.ThrowIfGreaterThan((int)kind, (int)DurationKind.DayTime);

        return kind switch
        {
            DurationKind.YearMonth => new(XdmItemKind.YearMonthDuration, XdmTypeId.YearMonthDuration, 0, value),
            DurationKind.DayTime => new(XdmItemKind.DayTimeDuration, XdmTypeId.DayTimeDuration, 0, value),
            _ => new(XdmItemKind.Duration, XdmTypeId.Duration, 0, value),
        };
    }

    /// <summary>Creates a node item.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The item, carrying the type annotation of the node.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="default"/>.</exception>
    public static XdmItem FromNode(in XPathNode node)
    {
        var tree = node.Node.Tree;
        Argument.ThrowIfNull(tree);

        return new(XdmItemKind.Node, node.TypeAnnotation, node.Node.Index, tree);
    }

    /// <summary>Gets the boolean value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the item is a boolean.</returns>
    public bool TryGetBoolean(out bool value)
    {
        value = _bits != 0;
        return Kind == XdmItemKind.Boolean;
    }

    /// <summary>Gets the integer value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the item is an integer.</returns>
    public bool TryGetInteger(out long value)
    {
        value = _bits;
        return Kind == XdmItemKind.XsInteger;
    }

    /// <summary>Gets the value of an <c>xs:integer</c> item of any width.</summary>
    /// <param name="value">The value, zero unless the item is an integer.</param>
    /// <returns><see langword="true"/> when the item is an integer, inline or beyond 64 bits.</returns>
    internal bool TryGetBigInteger(out BigInteger value)
    {
        if (Kind == XdmItemKind.XsInteger)
        {
            value = _bits;
            return true;
        }

        if (Kind == XdmItemKind.XsDecimal && TypeId == XdmTypeId.XsInteger)
        {
            // The value was built from an integer, so the conversion cannot fail.
            value = DecimalArithmetic.ToBigInteger((XsdDecimal)_reference!).Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Gets the double value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the item is a double.</returns>
    public bool TryGetDouble(out double value)
    {
        value = BitConverter.Int64BitsToDouble(_bits);
        return Kind == XdmItemKind.XsDouble;
    }

    /// <summary>Gets the <c>xs:float</c> value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the item is a float.</returns>
    internal bool TryGetFloat(out float value)
    {
        value = (float)BitConverter.Int64BitsToDouble(_bits);
        return Kind == XdmItemKind.XsFloat;
    }

    /// <summary>Gets the decimal value.</summary>
    /// <param name="value">The value, <see langword="default"/> unless the item is a decimal.</param>
    /// <returns><see langword="true"/> when the item is a decimal.</returns>
    public bool TryGetDecimal(out XsdDecimal value)
    {
        var found = Kind == XdmItemKind.XsDecimal;
        value = found ? (XsdDecimal)_reference! : default;
        return found;
    }

    /// <summary>Gets the date and time value.</summary>
    /// <param name="value">The value, <see langword="default"/> unless the item is a dateTime, date, or time.</param>
    /// <returns><see langword="true"/> when the item is an <c>xs:dateTime</c>, <c>xs:date</c>, or <c>xs:time</c>.</returns>
    public bool TryGetDateTime(out XsdDateTime value)
    {
        var found = Kind is XdmItemKind.DateTime or XdmItemKind.Date or XdmItemKind.Time;
        value = found ? (XsdDateTime)_reference! : default;
        return found;
    }

    /// <summary>Gets the duration value.</summary>
    /// <param name="value">The value, <see langword="default"/> unless the item is a duration.</param>
    /// <returns><see langword="true"/> when the item is an <c>xs:duration</c>, <c>xs:yearMonthDuration</c>, or <c>xs:dayTimeDuration</c>.</returns>
    public bool TryGetDuration(out XsdDuration value)
    {
        var found = Kind is XdmItemKind.Duration or XdmItemKind.YearMonthDuration or XdmItemKind.DayTimeDuration;
        value = found ? (XsdDuration)_reference! : default;
        return found;
    }

    /// <summary>Gets the value of a date and time item of any of the eight types.</summary>
    /// <param name="value">The value, <see langword="default"/> unless the item is one of them.</param>
    /// <returns><see langword="true"/> when the item is an <c>xs:dateTime</c>, <c>xs:date</c>, <c>xs:time</c>, or a Gregorian type.</returns>
    internal bool TryGetTemporal(out XsdDateTime value)
    {
        var found = Kind is >= XdmItemKind.DateTime and <= XdmItemKind.GMonth;
        value = found ? (XsdDateTime)_reference! : default;
        return found;
    }

    /// <summary>Gets the octets of an <c>xs:hexBinary</c> or <c>xs:base64Binary</c> item.</summary>
    /// <param name="value">The octets, <see langword="default"/> unless the item is binary.</param>
    /// <returns><see langword="true"/> when the item is <c>xs:hexBinary</c> or <c>xs:base64Binary</c>.</returns>
    internal bool TryGetBinary(out XsdBinary value)
    {
        var found = Kind is XdmItemKind.HexBinary or XdmItemKind.Base64Binary;
        value = found ? (XsdBinary)_reference! : default;
        return found;
    }

    /// <summary>Gets the value of an <c>xs:QName</c> item.</summary>
    /// <param name="value">The value, <see langword="null"/> unless the item is a QName.</param>
    /// <returns><see langword="true"/> when the item is an <c>xs:QName</c>.</returns>
    internal bool TryGetQName(out XdmQName? value)
    {
        value = Kind == XdmItemKind.QName ? (XdmQName?)_reference : null;
        return value is not null;
    }

    /// <summary>Gets the string value.</summary>
    /// <param name="value">The value, <see langword="null"/> unless the item is a string.</param>
    /// <returns><see langword="true"/> when the item is a string.</returns>
    public bool TryGetString(out string? value)
    {
        value = Kind == XdmItemKind.XsString ? (string?)_reference : null;
        return value is not null;
    }

    /// <summary>Gets the <c>xs:untypedAtomic</c> value.</summary>
    /// <param name="value">The value, <see langword="null"/> unless the item is untyped atomic.</param>
    /// <returns><see langword="true"/> when the item is <c>xs:untypedAtomic</c>.</returns>
    internal bool TryGetUntypedAtomic(out string? value)
    {
        value = Kind == XdmItemKind.UntypedAtomic ? (string?)_reference : null;
        return value is not null;
    }

    /// <summary>Gets the <c>xs:anyURI</c> value.</summary>
    /// <param name="value">The value, <see langword="null"/> unless the item is <c>xs:anyURI</c>.</param>
    /// <returns><see langword="true"/> when the item is <c>xs:anyURI</c>.</returns>
    internal bool TryGetAnyUri(out string? value)
    {
        value = Kind == XdmItemKind.AnyUri ? (string?)_reference : null;
        return value is not null;
    }

    /// <summary>Gets the node.</summary>
    /// <param name="node">The node, <see langword="default"/> unless the item is a node.</param>
    /// <returns><see langword="true"/> when the item is a node.</returns>
    public bool TryGetNode(out XPathNode node)
    {
        node = IsNode ? new XPathNode(new XmlNode((XmlTree)_reference!, (int)_bits)) : default;
        return IsNode;
    }

    /// <inheritdoc />
    public bool Equals(XdmItem other) =>
        Kind == other.Kind && TypeId == other.TypeId && _bits == other._bits && ReferencesEqual(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XdmItem other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked((((((((int)Kind * 31) + TypeId.Value) * 31) + _bits.GetHashCode()) * 31)) + ReferenceHash());

    // Strings compare ordinally, boxed value types by value, nodes by tree identity.
    private bool ReferencesEqual(XdmItem other) =>
        Kind switch
        {
            XdmItemKind.XsString or XdmItemKind.UntypedAtomic or XdmItemKind.AnyUri => string.Equals(
                (string?)_reference,
                (string?)other._reference,
                StringComparison.Ordinal
            ),
            XdmItemKind.Node => ReferenceEquals(_reference, other._reference),
            _ => Equals(_reference, other._reference),
        };

    private int ReferenceHash() =>
        Kind switch
        {
            XdmItemKind.XsString or XdmItemKind.UntypedAtomic or XdmItemKind.AnyUri =>
                StringComparer.Ordinal.GetHashCode((string)_reference!),
            XdmItemKind.Node => RuntimeHelpers.GetHashCode(_reference),
            _ => _reference?.GetHashCode() ?? 0,
        };

    /// <summary>Determines whether two items are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XdmItem left, XdmItem right) => left.Equals(right);

    /// <summary>Determines whether two items differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XdmItem left, XdmItem right) => !left.Equals(right);

    // The handles of the types that have no well-known handle, read from the type table on first use.
    private static class GregorianTypes
    {
        internal static readonly XdmTypeId YearMonth = XdmTypes.Get("gYearMonth");
        internal static readonly XdmTypeId Year = XdmTypes.Get("gYear");
        internal static readonly XdmTypeId MonthDay = XdmTypes.Get("gMonthDay");
        internal static readonly XdmTypeId Day = XdmTypes.Get("gDay");
        internal static readonly XdmTypeId Month = XdmTypes.Get("gMonth");
    }

    private static class BinaryTypes
    {
        internal static readonly XdmTypeId Hex = XdmTypes.Get("hexBinary");
        internal static readonly XdmTypeId Base64 = XdmTypes.Get("base64Binary");
        internal static readonly XdmTypeId QName = XdmTypes.Get("QName");
    }
}
