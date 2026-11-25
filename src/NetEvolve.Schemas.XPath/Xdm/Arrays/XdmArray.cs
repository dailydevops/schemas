namespace NetEvolve.Schemas.XPath.Xdm.Arrays;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// An array item of the data model (XPath 3.1; Data Model 3.1, arrays): an ordered list of members, each of them a sequence. An array is a
/// function item of arity 1 that maps a one-based position to the member, so it is an <see cref="XdmFunction"/> of the existing
/// function kind and needs no public member. Immutable.
/// </summary>
/// <remarks>
/// <see cref="Slice"/> shares the member storage of the source array; an update that changes a member or the length (<see cref="Put"/>,
/// <see cref="Append"/>) copies the list of members, one reference per member, and shares the members themselves. Limitation: the
/// copy is linear in the size of the array, so a loop of single updates is quadratic; a persistent tree is the upgrade when that matters.
/// </remarks>
internal sealed class XdmArray : XdmFunction
{
    private static readonly XPathParameterType Position = new(XPathItemType.Integer, Occurrence.ExactlyOne);

    private static readonly FunctionSignature ArraySignature = new(
        [SequenceTypeInfo.FromParameter(Position)],
        SequenceTypeInfo.AnyItems
    );

    private readonly XdmSequence[] _members;
    private readonly int _offset;

    private XdmArray(XdmSequence[] members, int offset, int size)
        : base(null, ArraySignature)
    {
        _members = members;
        _offset = offset;
        Size = size;
    }

    /// <summary>Gets the array without members.</summary>
    public static XdmArray Empty { get; } = new([], 0, 0);

    /// <summary>Gets the number of members.</summary>
    public int Size { get; }

    /// <summary>Gets a member.</summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The member.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the array.</exception>
    public XdmSequence this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Size);

            return _members[_offset + index];
        }
    }

    /// <summary>Creates an array that owns a list of members.</summary>
    /// <param name="members">The members; the caller does not change the list afterwards.</param>
    /// <returns>The array.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="members"/> is <see langword="null"/>.</exception>
    public static XdmArray Create(XdmSequence[] members)
    {
        ArgumentNullException.ThrowIfNull(members);

        return members.Length == 0 ? Empty : new(members, 0, members.Length);
    }

    /// <summary>Gets the array of an item.</summary>
    /// <param name="item">The item.</param>
    /// <param name="array">The array, <see langword="null"/> unless the item is an array.</param>
    /// <returns><see langword="true"/> when the item is an array.</returns>
    public static bool TryGet(in XdmItem item, out XdmArray? array)
    {
        array = item.TryGetFunction(out var function) ? function as XdmArray : null;
        return array is not null;
    }

    /// <summary>Gets the item that holds this array.</summary>
    /// <returns>The function item.</returns>
    public XdmItem ToItem() => XdmItem.FromFunction(this);

    /// <summary>Gets the members from <paramref name="start"/> on, without a copy.</summary>
    /// <param name="start">The zero-based position of the first member.</param>
    /// <param name="length">The number of members.</param>
    /// <returns>The array of the range; it shares the storage of this array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The range is not inside the array.</exception>
    public XdmArray Slice(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, Size);

        return length == 0 ? Empty
            : length == Size ? this
            : new(_members, _offset + start, length);
    }

    /// <summary>Replaces a member.</summary>
    /// <param name="index">The zero-based position.</param>
    /// <param name="member">The new member.</param>
    /// <returns>A new array; this array is unchanged.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the array.</exception>
    public XdmArray Put(int index, in XdmSequence member)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Size);

        var members = new XdmSequence[Size];
        Array.Copy(_members, _offset, members, 0, Size);
        members[index] = member;
        return new(members, 0, Size);
    }

    /// <summary>Adds a member at the end.</summary>
    /// <param name="member">The new member.</param>
    /// <returns>A new array; this array is unchanged.</returns>
    public XdmArray Append(in XdmSequence member)
    {
        var members = new XdmSequence[Size + 1];
        Array.Copy(_members, _offset, members, 0, Size);
        members[Size] = member;
        return new(members, 0, Size + 1);
    }

    /// <summary>Copies the members into a list.</summary>
    /// <param name="destination">The list, at least <see cref="Size"/> long.</param>
    /// <param name="start">The position in <paramref name="destination"/> of the first member.</param>
    public void CopyTo(XdmSequence[] destination, int start)
    {
        ArgumentNullException.ThrowIfNull(destination);

        Array.Copy(_members, _offset, destination, start, Size);
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The argument is no <c>xs:integer</c> (XPTY0004), or the position is outside the array (FOAY0001).</exception>
    public override XdmSequence Invoke(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);

        var position = FunctionConversion.Convert(arguments[0], Position)[0].GetSaturatedInteger();
        return position >= 1 && position <= Size
            ? _members[_offset + (int)position - 1]
            : throw XPathErrors.Exception(XPathErrorCodes.FOAY0001);
    }
}
