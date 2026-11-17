namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>An item: a node or an atomic value. A tagged struct; boolean, integer, double, and node need no allocation.</summary>
/// <remarks>
/// Limitation: the factories and accessors for decimal, date and time, and duration values are not part of this contract; XP-06
/// adds exactly <c>FromDecimal</c>, <c>FromDateTime</c>, <c>FromDuration</c>, and the matching <c>TryGet</c> members (#185).
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

    /// <summary>Gets the double value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the item is a double.</returns>
    public bool TryGetDouble(out double value)
    {
        value = BitConverter.Int64BitsToDouble(_bits);
        return Kind == XdmItemKind.XsDouble;
    }

    /// <summary>Gets the string value.</summary>
    /// <param name="value">The value, <see langword="null"/> unless the item is a string.</param>
    /// <returns><see langword="true"/> when the item is a string.</returns>
    public bool TryGetString(out string? value)
    {
        value = Kind == XdmItemKind.XsString ? (string?)_reference : null;
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
        Kind == other.Kind
        && TypeId == other.TypeId
        && _bits == other._bits
        && (
            Kind == XdmItemKind.XsString
                ? string.Equals((string?)_reference, (string?)other._reference, StringComparison.Ordinal)
                : ReferenceEquals(_reference, other._reference)
        );

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XdmItem other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked(
            (((((((int)Kind * 31) + TypeId.Value) * 31) + _bits.GetHashCode()) * 31))
            + (
                Kind == XdmItemKind.XsString
                    ? StringComparer.Ordinal.GetHashCode((string)_reference!)
                    : RuntimeHelpers.GetHashCode(_reference)
            )
        );

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
}
