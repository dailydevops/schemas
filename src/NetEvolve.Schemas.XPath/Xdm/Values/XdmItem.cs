#pragma warning disable CA1815 // Approved shape (#123): the struct defines no value equality.

namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
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
public readonly struct XdmItem
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
    public static XdmItem FromInteger(long value) => new(XdmItemKind.Integer, XdmTypeId.Integer, value, null);

    /// <summary>Creates a double item.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item.</returns>
    public static XdmItem FromDouble(double value) =>
        new(XdmItemKind.Double, XdmTypeId.Double, BitConverter.DoubleToInt64Bits(value), null);

    /// <summary>Creates a string item.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static XdmItem FromString(string value)
    {
        Argument.ThrowIfNull(value);

        return new(XdmItemKind.String, XdmTypeId.String, 0, value);
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
        return Kind == XdmItemKind.Integer;
    }

    /// <summary>Gets the double value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the item is a double.</returns>
    public bool TryGetDouble(out double value)
    {
        value = BitConverter.Int64BitsToDouble(_bits);
        return Kind == XdmItemKind.Double;
    }

    /// <summary>Gets the string value.</summary>
    /// <param name="value">The value, <see langword="null"/> unless the item is a string.</param>
    /// <returns><see langword="true"/> when the item is a string.</returns>
    public bool TryGetString(out string? value)
    {
        value = Kind == XdmItemKind.String ? (string?)_reference : null;
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
}
