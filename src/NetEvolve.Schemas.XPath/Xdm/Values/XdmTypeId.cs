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
    public static XdmTypeId XsString => new(3);

    /// <summary>Gets <c>xs:boolean</c>.</summary>
    public static XdmTypeId Boolean => new(4);

    /// <summary>Gets <c>xs:integer</c>.</summary>
    public static XdmTypeId XsInteger => new(5);

    /// <summary>Gets <c>xs:double</c>.</summary>
    public static XdmTypeId XsDouble => new(6);

    internal static XdmTypeId XsDecimal => new(7);

    internal static XdmTypeId XsFloat => new(8);

    internal static XdmTypeId Duration => new(9);

    internal static XdmTypeId YearMonthDuration => new(10);

    internal static XdmTypeId DayTimeDuration => new(11);

    internal static XdmTypeId DateTime => new(12);

    internal static XdmTypeId Date => new(13);

    internal static XdmTypeId Time => new(14);

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
