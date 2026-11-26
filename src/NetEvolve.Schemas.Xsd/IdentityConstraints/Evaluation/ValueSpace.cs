namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Ordering;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// Equality of the values of two simple types in the value space, through the datatype contract (Structures section 3.11.4,
/// key-sequence equality; design #146 DP-13): two values are equal when they belong to the same primitive type and their values
/// are equal there, which is the plain equality of the datatype in 1.0 and equality or identity in 1.1, so that a value that is
/// not equal to itself, such as NaN, still finds its twin.
/// </summary>
/// <remarks>
/// <para>
/// A type is followed to its primitive: through the restriction chain, to the accepting member of a union (the first member in
/// declaration order that accepts the value), and to the item type of a list. A type that cannot be followed, such as an unknown
/// name or a component that is no simple type, compares as a string. The values are parsed again on every comparison; the hash
/// keeps the comparisons few.
/// </para>
/// <para>
/// The hash is exact for every datatype except <c>QName</c> and <c>NOTATION</c>: date and time values hash by their instant on the
/// timeline (timezone offsets folded in), durations by months and seconds, and binary values by their octets.
/// Limitations: <c>QName</c> and <c>NOTATION</c> values are compared by
/// their collapsed text because no namespace context reaches the listener (#604).
/// </para>
/// </remarks>
internal sealed class ValueSpace
{
    private const string AnySimpleType = "anySimpleType";
    private const int FnvOffset = unchecked((int)2166136261);
    private const int FnvPrime = 16777619;

    private readonly ISimpleValueAssessor _assessor;
    private readonly Dictionary<string, XsdBuiltInType> _builtIns = new(StringComparer.Ordinal);
    private readonly XsdBuiltInType _string;

    /// <summary>Initializes a new instance of the <see cref="ValueSpace"/> class.</summary>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    /// <param name="version">The XSD version.</param>
    internal ValueSpace(ISimpleValueAssessor assessor, XsdVersion version)
    {
        ArgumentNullException.ThrowIfNull(assessor);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _assessor = assessor;
        var types = XsdBuiltInTypes.For(version);
        for (var i = 0; i < types.Count; i++)
        {
            if (types[i] is XsdBuiltInType builtIn)
            {
                _builtIns[builtIn.Name.LocalName] = builtIn;
            }
        }

        _string = _builtIns["string"];
    }

    /// <summary>Computes a hash that is the same for all values that are equal in the value space.</summary>
    /// <param name="type">The simple type of the value.</param>
    /// <param name="lexical">The lexical value.</param>
    /// <returns>The hash.</returns>
    internal int Hash(IXsdComponent type, ReadOnlySpan<char> lexical)
    {
        ArgumentNullException.ThrowIfNull(type);

        var shape = Resolve(type, lexical);
        if (shape.Primitive is not null)
        {
            return HashAtomic(shape, lexical);
        }

        var hash = FnvOffset;
        var cursor = new XsdListCursor(new TextRange(0, lexical.Length));
        while (cursor.TryNext(lexical, out var item))
        {
            hash = unchecked((hash * FnvPrime) ^ Hash(shape.Item ?? _string, lexical.Slice(item.Offset, item.Length)));
        }

        return hash;
    }

    /// <summary>Determines whether two values are equal in the value space.</summary>
    /// <param name="typeA">The simple type of the first value.</param>
    /// <param name="a">The first lexical value.</param>
    /// <param name="typeB">The simple type of the second value.</param>
    /// <param name="b">The second lexical value.</param>
    /// <returns><see langword="true"/> when the values are equal.</returns>
    internal bool AreEqual(IXsdComponent typeA, ReadOnlySpan<char> a, IXsdComponent typeB, ReadOnlySpan<char> b)
    {
        ArgumentNullException.ThrowIfNull(typeA);
        ArgumentNullException.ThrowIfNull(typeB);

        var first = Resolve(typeA, a);
        var second = Resolve(typeB, b);
        if (first.Primitive is null || second.Primitive is null)
        {
            return first.Primitive is null && second.Primitive is null && SameItems(first.Item, a, second.Item, b);
        }

        return string.Equals(first.Primitive.Name, second.Primitive.Name, StringComparison.Ordinal)
            && SameAtomic(first, second, a, b);
    }

    private static int HashAtomic(Shape shape, ReadOnlySpan<char> lexical)
    {
        switch (shape.Primitive)
        {
            case IDatatypeDescriptor<XsdDecimal> decimals:
                var number = decimals.Parse(lexical);
                return number.Success ? number.Value.GetHashCode() : 0;
            case IDatatypeDescriptor<double> doubles:
                var wide = doubles.Parse(lexical);
                return wide.Success ? HashDouble(wide.Value) : 0;
            case IDatatypeDescriptor<float> floats:
                var narrow = floats.Parse(lexical);
                return narrow.Success ? HashDouble(narrow.Value) : 0;
            case IDatatypeDescriptor<bool> booleans:
                var truth = booleans.Parse(lexical);
                return truth.Success && truth.Value ? 1 : 0;
            case IDatatypeDescriptor<XsdString>:
                var text = shape.AsString().Parse(lexical);
                return text.Success ? HashText(lexical, text.Value.Mode) : 0;
            case IDatatypeDescriptor<XsdDateTime> dates:
                var moment = dates.Parse(lexical);
                return moment.Success ? HashDateTime(moment.Value) : 0;
            case IDatatypeDescriptor<XsdDuration> durations:
                var span = durations.Parse(lexical);
                return span.Success ? span.Value.GetHashCode() : 0;
            case IDatatypeDescriptor<XsdBinary> binaries:
                var octets = binaries.Parse(lexical);
                return octets.Success ? HashOctets(octets.Value.AsSpan()) : 0;
            default:
                return HashText(lexical, WhitespaceMode.Collapse);
        }
    }

    // FNV over the octets: the multiplier 31 of the value type folds the low octets of a counter into a few hundred hash values.
    private static int HashOctets(ReadOnlySpan<byte> octets)
    {
        var hash = FnvOffset;
        for (var i = 0; i < octets.Length; i++)
        {
            hash = unchecked((hash * FnvPrime) ^ octets[i]);
        }

        return hash;
    }

    // Values equal on the timeline share the instant, so the offset is folded in; the kind keeps the Gregorian types apart.
    private static int HashDateTime(XsdDateTime value) =>
        unchecked(((int)value.Kind * 397) ^ DateTimeTimeline.TimeOnTimeline(in value, 0).GetHashCode());

    // Zero has one value, and NaN is equal or identical to itself; the hash of a double does not promise either on every target.
    private static int HashDouble(double value) =>
        double.IsNaN(value) ? 1
        : value == 0 ? 0
        : value.GetHashCode();

    // The hash of the text after the white space handling of the type, without building the normalized text.
    private static int HashText(ReadOnlySpan<char> lexical, WhitespaceMode mode)
    {
        var hash = FnvOffset;
        var pendingSpace = false;
        var started = false;
        for (var i = 0; i < lexical.Length; i++)
        {
            var c = lexical[i];
            if (mode != WhitespaceMode.Preserve && c is '\t' or '\n' or '\r' or ' ')
            {
                if (mode == WhitespaceMode.Replace)
                {
                    hash = unchecked((hash * FnvPrime) ^ ' ');
                }
                else
                {
                    pendingSpace = started;
                }

                continue;
            }

            if (pendingSpace)
            {
                hash = unchecked((hash * FnvPrime) ^ ' ');
                pendingSpace = false;
            }

            hash = unchecked((hash * FnvPrime) ^ c);
            started = true;
        }

        return hash;
    }

    private static bool SameAtomic(Shape first, Shape second, ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        switch (first.Primitive)
        {
            case IDatatypeDescriptor<XsdDecimal> decimals:
                return Same(decimals, a, b);
            case IDatatypeDescriptor<double> doubles:
                return Same(doubles, a, b);
            case IDatatypeDescriptor<float> floats:
                return Same(floats, a, b);
            case IDatatypeDescriptor<bool> booleans:
                return Same(booleans, a, b);
            case IDatatypeDescriptor<XsdDateTime> dates:
                return Same(dates, a, b);
            case IDatatypeDescriptor<XsdDuration> durations:
                return Same(durations, a, b);
            case IDatatypeDescriptor<XsdBinary> binaries:
                return Same(binaries, a, b);
            case IDatatypeDescriptor<XsdString>:
                var left = first.AsString().Parse(a);
                var right = second.AsString().Parse(b);
                return left.Success && right.Success && left.Value.Equals(a, right.Value, b);
            default:
                return SameTokens(a, b);
        }
    }

    // The same white space separated tokens, which is the text after collapsing.
    private static bool SameTokens(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        var cursorA = new XsdListCursor(new TextRange(0, a.Length));
        var cursorB = new XsdListCursor(new TextRange(0, b.Length));
        while (true)
        {
            var hasA = cursorA.TryNext(a, out var rangeA);
            var hasB = cursorB.TryNext(b, out var rangeB);
            if (!hasA || !hasB)
            {
                return hasA == hasB;
            }

            if (!a.Slice(rangeA.Offset, rangeA.Length).SequenceEqual(b.Slice(rangeB.Offset, rangeB.Length)))
            {
                return false;
            }
        }
    }

    // Equal in the value space, or identical when the values are not comparable (NaN, and the unordered types).
    private static bool Same<TValue>(IDatatypeDescriptor<TValue> type, ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        var first = type.Parse(a);
        var second = type.Parse(b);
        if (!first.Success || !second.Success)
        {
            return false;
        }

        var order = type.Compare(first.Value, second.Value);
        return order == XsdOrder.Equal
            || (order == XsdOrder.Incomparable && type.IsIdentical(first.Value, second.Value));
    }

    private bool SameItems(IXsdComponent? itemA, ReadOnlySpan<char> a, IXsdComponent? itemB, ReadOnlySpan<char> b)
    {
        var typeA = itemA ?? _string;
        var typeB = itemB ?? _string;
        var cursorA = new XsdListCursor(new TextRange(0, a.Length));
        var cursorB = new XsdListCursor(new TextRange(0, b.Length));
        while (true)
        {
            var hasA = cursorA.TryNext(a, out var rangeA);
            var hasB = cursorB.TryNext(b, out var rangeB);
            if (!hasA || !hasB)
            {
                return hasA == hasB;
            }

            if (!AreEqual(typeA, a.Slice(rangeA.Offset, rangeA.Length), typeB, b.Slice(rangeB.Offset, rangeB.Length)))
            {
                return false;
            }
        }
    }

    // Follows the type to its primitive type, to the accepting member of a union, or to the item type of a list.
    private Shape Resolve(IXsdComponent type, ReadOnlySpan<char> lexical)
    {
        var current = type;
        while (true)
        {
            switch (current)
            {
                case XsdBuiltInType builtIn:
                    return OfBuiltIn(builtIn);
                case SimpleTypeDefinition { Variety: DatatypeVariety.List } list:
                    return new Shape(null, null, list.ItemType);
                case SimpleTypeDefinition { Variety: DatatypeVariety.Union } union:
                    var member = Accepting(union, lexical);
                    if (member is null)
                    {
                        return OfBuiltIn(_string);
                    }

                    current = member;
                    break;
                case SimpleTypeDefinition { BaseType: { } baseType }:
                    current = baseType;
                    break;
                case SimpleTypeDefinition root
                    when string.Equals(root.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
                        && _builtIns.TryGetValue(root.Name.LocalName, out var named):
                    current = named;
                    break;
                default:
                    return OfBuiltIn(_string);
            }
        }
    }

    private SimpleTypeDefinition? Accepting(SimpleTypeDefinition union, ReadOnlySpan<char> lexical)
    {
        for (var i = 0; i < union.MemberTypes.Count; i++)
        {
            if (_assessor.Assess(union.MemberTypes[i], lexical, out _).IsValid)
            {
                return union.MemberTypes[i];
            }
        }

        return null;
    }

    private Shape OfBuiltIn(XsdBuiltInType builtIn)
    {
        if (builtIn.Descriptor is { Variety: DatatypeVariety.List })
        {
            return new Shape(null, null, null);
        }

        var primitive = builtIn;
        while (
            primitive.BaseType is XsdBuiltInType parent
            && parent.Descriptor is not null
            && !string.Equals(parent.Name.LocalName, AnySimpleType, StringComparison.Ordinal)
        )
        {
            primitive = parent;
        }

        return new Shape(primitive.Descriptor ?? _string.Descriptor, builtIn.Descriptor ?? _string.Descriptor, null);
    }

    // A list has no primitive; its items are of the item type, a string when the list does not name one.
    [StructLayout(LayoutKind.Auto)]
    private readonly struct Shape
    {
        internal Shape(IDatatypeDescriptor? primitive, IDatatypeDescriptor? value, IXsdComponent? item)
        {
            Primitive = primitive;
            Value = value;
            Item = item;
        }

        internal IDatatypeDescriptor? Primitive { get; }

        // The descriptor of the type itself, which knows its white space handling.
        internal IDatatypeDescriptor? Value { get; }

        internal IDatatypeDescriptor<XsdString> AsString() =>
            Value as IDatatypeDescriptor<XsdString> ?? (IDatatypeDescriptor<XsdString>)Primitive!;

        internal IXsdComponent? Item { get; }
    }
}
