#pragma warning disable CA1720 // Approved shape (#123): the members are named after the XDM atomic families.

namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using System.Runtime.InteropServices;

/// <summary>A handle for the type annotation of an item.</summary>
/// <remarks>
/// Limitation: the handles of the well-known types are fixed here until the descriptor registry of the datatype package
/// supplies them; <see langword="default"/> stands for no type.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XdmTypeId : IEquatable<XdmTypeId>
{
    /// <summary>Initializes a new instance of the <see cref="XdmTypeId"/> struct.</summary>
    /// <param name="value">The handle.</param>
    public XdmTypeId(int value) => Value = value;

    /// <summary>Gets <c>xs:untyped</c>: the annotation of elements and documents that were not validated.</summary>
    public static XdmTypeId Untyped => new(1);

    /// <summary>Gets <c>xs:untypedAtomic</c>: the annotation of attributes and text that were not validated.</summary>
    public static XdmTypeId UntypedAtomic => new(2);

    /// <summary>Gets <c>xs:string</c>.</summary>
    public static XdmTypeId String => new(3);

    /// <summary>Gets <c>xs:boolean</c>.</summary>
    public static XdmTypeId Boolean => new(4);

    /// <summary>Gets <c>xs:integer</c>.</summary>
    public static XdmTypeId Integer => new(5);

    /// <summary>Gets <c>xs:double</c>.</summary>
    public static XdmTypeId Double => new(6);

    /// <summary>Gets the handle.</summary>
    public int Value { get; }

    /// <inheritdoc />
    public bool Equals(XdmTypeId other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XdmTypeId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value;

    /// <summary>Determines whether two handles are equal.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XdmTypeId left, XdmTypeId right) => left.Equals(right);

    /// <summary>Determines whether two handles differ.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XdmTypeId left, XdmTypeId right) => !left.Equals(right);
}
