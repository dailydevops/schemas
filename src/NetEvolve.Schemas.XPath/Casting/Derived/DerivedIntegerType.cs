namespace NetEvolve.Schemas.XPath.Casting.Derived;

using System.Numerics;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The conversion into one built-in type derived from <c>xs:integer</c> (Functions and Operators, 17.2, 17.4, and 17.5): the value is read as an
/// integer of any width, and the facets of the type, which the datatype layer accumulates over the whole chain of its bases, decide whether it is
/// in the value space. The result is an <c>xs:integer</c> item with the annotation of the type.
/// </summary>
/// <remarks>
/// <para>
/// A source of the seven types of the casting core is read as <c>xs:integer</c> is, with one difference: the width is not limited to the 64 bits of
/// the integer item, so a value beyond them reaches the facets (FORG0001) or, for an unbounded type such as <c>xs:positiveInteger</c> and for
/// <c>xs:unsignedLong</c> above 2^63, is a value of the type. The cast to <c>xs:integer</c> itself keeps its limit (FOCA0003, #265).
/// A lexical form beyond the digit cap of the datatype layer is an implementation limit, FOCA0003, as for <c>xs:integer</c>.
/// </para>
/// <para>Immutable and thread-safe; one instance serves every row of the type.</para>
/// </remarks>
internal sealed class DerivedIntegerType
{
    private static readonly IDatatypeDescriptor<XsdInteger> IntegerType =
        (IDatatypeDescriptor<XsdInteger>)BuiltInDatatypes.For(XsdVersion.V10).Get("integer");

    private readonly XdmTypeId _type;
    private readonly IDatatypeDescriptor<XsdInteger> _descriptor;

    /// <summary>Initializes a new instance of the <see cref="DerivedIntegerType"/> class.</summary>
    /// <param name="type">The type.</param>
    /// <param name="descriptor">The datatype of the type, whose facets bound the value.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="descriptor"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="type"/> is <see langword="default"/>.</exception>
    public DerivedIntegerType(XdmTypeId type, IDatatypeDescriptor<XsdInteger> descriptor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(type.Value, 1);
        ArgumentNullException.ThrowIfNull(descriptor);

        _type = type;
        _descriptor = descriptor;
    }

    /// <summary>Converts a value to the type.</summary>
    /// <param name="source">The source value, an item of one of the seven types of the casting core.</param>
    /// <param name="result">The result, <see langword="default"/> unless the status is <see cref="CastStatus.Ok"/>.</param>
    /// <returns>The status; a value outside the facets of the type is <see cref="CastStatus.InvalidLexical"/> (FORG0001).</returns>
    public CastStatus Convert(in XdmItem source, out XdmItem result)
    {
        result = default;
        var status = ReadInteger(in source, out var value);
        if (status != CastStatus.Ok)
        {
            return status;
        }

        var integer = new XsdInteger(value);
        var facets = _descriptor.Facets;
        for (var i = 0; i < facets.Count; i++)
        {
            if (facets[i] is IValueFacet<XsdInteger> bound && !bound.IsSatisfiedBy(in integer))
            {
                return CastStatus.InvalidLexical;
            }
        }

        result = XdmItem.FromBigInteger(value).WithTypeId(_type);
        return CastStatus.Ok;
    }

    // The value of the source as an integer of any width: a lexical form by the datatype of xs:integer, a floating-point number and a decimal by
    // truncation toward zero, a boolean as 0 or 1.
    private static CastStatus ReadInteger(in XdmItem source, out BigInteger value)
    {
        value = default;
        if (source.TryGetString(out var lexical) || source.TryGetUntypedAtomic(out lexical))
        {
            var parsed = IntegerType.Parse(lexical.AsSpan());
            if (!parsed.Success)
            {
                return parsed.Error == DatatypeError.DigitCap ? CastStatus.IntegerOverflow : CastStatus.InvalidLexical;
            }

            value = parsed.Value.Value;
            return CastStatus.Ok;
        }

        if (source.TryGetBigInteger(out value))
        {
            return CastStatus.Ok;
        }

        if (source.TryGetDouble(out var wide))
        {
            return Truncate(wide, out value);
        }

        if (source.TryGetFloat(out var narrow))
        {
            return Truncate(narrow, out value);
        }

        if (source.TryGetDecimal(out var number))
        {
            var truncated = DecimalArithmetic.ToBigInteger(number);
            value = truncated.Success ? truncated.Value : default;
            return truncated.Success ? CastStatus.Ok : CastStatus.IntegerOverflow;
        }

        if (source.TryGetBoolean(out var boolean))
        {
            value = boolean ? BigInteger.One : BigInteger.Zero;
            return CastStatus.Ok;
        }

        return CastStatus.NotAllowed;
    }

    private static CastStatus Truncate(double number, out BigInteger value)
    {
        value = default;
        if (double.IsNaN(number) || double.IsInfinity(number))
        {
            return CastStatus.InvalidValue;
        }

        // The conversion truncates toward zero and is exact for a double.
        value = new BigInteger(number);
        return CastStatus.Ok;
    }
}
