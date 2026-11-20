namespace NetEvolve.Schemas.XPath.Casting.Core;

using System;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The conversions of the rows among <c>xs:string</c>, <c>xs:untypedAtomic</c>, <c>xs:float</c>, <c>xs:double</c>, <c>xs:decimal</c>,
/// <c>xs:integer</c>, and <c>xs:boolean</c> (Functions and Operators, 17.1.1 to 17.1.2). There is one conversion per target type; the
/// source type is told by the item. A failure is a <see cref="CastStatus"/>, never an exception.
/// </summary>
/// <remarks>
/// <para>
/// A lexical form goes through the built-in datatype of the target, which collapses whitespace and checks the lexical space by the
/// XSD 1.0 rules, as XPath 2.0 refers to XML Schema 1.0, so <c>+INF</c> is not a double. A form beyond the digit cap of the datatype
/// layer (4,096 digits) is an implementation limit: FOCA0003 for an integer, FOCA0001 for a decimal, a float, or a double, whose digit
/// cap applies to the mantissa.
/// </para>
/// <para>
/// A double or a float becomes a decimal by its exact binary value, and an integer by truncation toward zero. A decimal becomes a float
/// or a double by one rounding of its exact value. An integer beyond 64 bits is FOCA0003 for every source, because the integer item
/// carries 64 bits. Limitation: a decimal cast to a float or a double allocates its string.
/// </para>
/// </remarks>
internal static class PrimitiveConversions
{
    private static readonly DatatypeRegistry V10 = BuiltInDatatypes.For(XsdVersion.V10);

    private static readonly IDatatypeDescriptor<bool> BooleanType = (IDatatypeDescriptor<bool>)V10.Get("boolean");
    private static readonly IDatatypeDescriptor<XsdInteger> IntegerType =
        (IDatatypeDescriptor<XsdInteger>)V10.Get("integer");
    private static readonly IDatatypeDescriptor<XsdDecimal> DecimalType =
        (IDatatypeDescriptor<XsdDecimal>)V10.Get("decimal");
    private static readonly IDatatypeDescriptor<float> FloatType = (IDatatypeDescriptor<float>)V10.Get("float");
    private static readonly IDatatypeDescriptor<double> DoubleType = (IDatatypeDescriptor<double>)V10.Get("double");

    // 2^63 as a double, exactly: the integer range is [-2^63, 2^63).
    private const double LongRange = 9223372036854775808.0;

    /// <summary>Converts to <c>xs:string</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToStringType(in XdmItem source, out XdmItem result)
    {
        if (NumberText.TryFormat(source, out var text))
        {
            result = XdmItem.FromString(text!);
            return CastStatus.Ok;
        }

        result = default;
        return CastStatus.NotAllowed;
    }

    /// <summary>Converts to <c>xs:untypedAtomic</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToUntypedAtomic(in XdmItem source, out XdmItem result)
    {
        if (NumberText.TryFormat(source, out var text))
        {
            result = XdmItem.FromUntypedAtomic(text!);
            return CastStatus.Ok;
        }

        result = default;
        return CastStatus.NotAllowed;
    }

    /// <summary>Converts to <c>xs:float</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToFloat(in XdmItem source, out XdmItem result)
    {
        result = default;
        if (TryGetLexical(source, out var lexical))
        {
            var parsed = FloatType.Parse(lexical.AsSpan());
            if (!parsed.Success)
            {
                return Failure(parsed.Error, CastStatus.DecimalOverflow);
            }

            result = XdmItem.FromFloat(parsed.Value);
        }
        else if (source.TryGetFloat(out _))
        {
            result = source;
        }
        else if (source.TryGetDouble(out var wide))
        {
            result = XdmItem.FromFloat((float)wide);
        }
        else if (source.TryGetDecimal(out var number))
        {
            // Rounded once from the exact decimal, by the step that the promotion shares.
            var parsed = NumericPromotion.DecimalToSingle(number);
            if (!parsed.Success)
            {
                return Failure(parsed.Error, CastStatus.DecimalOverflow);
            }

            result = XdmItem.FromFloat(parsed.Value);
        }
        else if (source.TryGetInteger(out var integer))
        {
            result = XdmItem.FromFloat(integer);
        }
        else if (source.TryGetBoolean(out var boolean))
        {
            result = XdmItem.FromFloat(boolean ? 1 : 0);
        }
        else
        {
            return CastStatus.NotAllowed;
        }

        return CastStatus.Ok;
    }

    /// <summary>Converts to <c>xs:double</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToDouble(in XdmItem source, out XdmItem result)
    {
        result = default;
        if (TryGetLexical(source, out var lexical))
        {
            var parsed = DoubleType.Parse(lexical.AsSpan());
            if (!parsed.Success)
            {
                return Failure(parsed.Error, CastStatus.DecimalOverflow);
            }

            result = XdmItem.FromDouble(parsed.Value);
        }
        else if (source.TryGetDouble(out _))
        {
            result = source;
        }
        else if (source.TryGetFloat(out var narrow))
        {
            result = XdmItem.FromDouble(narrow);
        }
        else if (source.TryGetDecimal(out var number))
        {
            result = XdmItem.FromDouble(DecimalArithmetic.ToDouble(number));
        }
        else if (source.TryGetInteger(out var integer))
        {
            result = XdmItem.FromDouble(integer);
        }
        else if (source.TryGetBoolean(out var boolean))
        {
            result = XdmItem.FromDouble(boolean ? 1 : 0);
        }
        else
        {
            return CastStatus.NotAllowed;
        }

        return CastStatus.Ok;
    }

    /// <summary>Converts to <c>xs:decimal</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToDecimal(in XdmItem source, out XdmItem result)
    {
        result = default;
        if (TryGetLexical(source, out var lexical))
        {
            var parsed = DecimalType.Parse(lexical.AsSpan());
            if (!parsed.Success)
            {
                return Failure(parsed.Error, CastStatus.DecimalOverflow);
            }

            result = XdmItem.FromDecimal(parsed.Value);
        }
        else if (source.TryGetDecimal(out var number))
        {
            // An integer beyond 64 bits is an item of the decimal family with an integer annotation; the cast drops the annotation.
            result = source.TryGetBigInteger(out _) ? XdmItem.FromDecimal(number) : source;
        }
        else if (source.TryGetDouble(out var wide))
        {
            return FromFloating(wide, out result);
        }
        else if (source.TryGetFloat(out var narrow))
        {
            return FromFloating(narrow, out result);
        }
        else if (source.TryGetInteger(out var integer))
        {
            result = XdmItem.FromDecimal(DecimalArithmetic.FromBigInteger(integer));
        }
        else if (source.TryGetBoolean(out var boolean))
        {
            result = XdmItem.FromDecimal(DecimalArithmetic.FromBigInteger(boolean ? BigInteger.One : BigInteger.Zero));
        }
        else
        {
            return CastStatus.NotAllowed;
        }

        return CastStatus.Ok;
    }

    /// <summary>Converts to <c>xs:integer</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToInteger(in XdmItem source, out XdmItem result)
    {
        result = default;
        if (TryGetLexical(source, out var lexical))
        {
            var parsed = IntegerType.Parse(lexical.AsSpan());
            return parsed.Success
                ? FromBigInteger(parsed.Value.Value, out result)
                : Failure(parsed.Error, CastStatus.IntegerOverflow);
        }

        if (source.TryGetInteger(out _))
        {
            result = source;
            return CastStatus.Ok;
        }

        if (source.TryGetDouble(out var wide))
        {
            return Truncate(wide, out result);
        }

        if (source.TryGetFloat(out var narrow))
        {
            return Truncate(narrow, out result);
        }

        if (source.TryGetDecimal(out var number))
        {
            var truncated = DecimalArithmetic.ToBigInteger(number);
            return truncated.Success ? FromBigInteger(truncated.Value, out result) : CastStatus.IntegerOverflow;
        }

        if (source.TryGetBoolean(out var boolean))
        {
            result = XdmItem.FromInteger(boolean ? 1 : 0);
            return CastStatus.Ok;
        }

        return CastStatus.NotAllowed;
    }

    /// <summary>Converts to <c>xs:boolean</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToBoolean(in XdmItem source, out XdmItem result)
    {
        result = default;
        bool value;
        if (TryGetLexical(source, out var lexical))
        {
            var parsed = BooleanType.Parse(lexical.AsSpan());
            if (!parsed.Success)
            {
                return CastStatus.InvalidLexical;
            }

            value = parsed.Value;
        }
        else if (source.TryGetBoolean(out var boolean))
        {
            value = boolean;
        }
        else if (source.TryGetInteger(out var integer))
        {
            value = integer != 0;
        }
        else if (source.TryGetDecimal(out var number))
        {
            value = number.Sign != 0;
        }
        else if (source.TryGetDouble(out var wide))
        {
            value = wide != 0 && !double.IsNaN(wide);
        }
        else if (source.TryGetFloat(out var narrow))
        {
            value = narrow != 0 && !float.IsNaN(narrow);
        }
        else
        {
            return CastStatus.NotAllowed;
        }

        result = XdmItem.FromBoolean(value);
        return CastStatus.Ok;
    }

    // The lexical form of a source that is read through the datatype of the target: xs:string and xs:untypedAtomic.
    private static bool TryGetLexical(in XdmItem source, out string? lexical) =>
        source.TryGetString(out lexical) || source.TryGetUntypedAtomic(out lexical);

    private static CastStatus Failure(DatatypeError error, CastStatus overflow) =>
        error == DatatypeError.DigitCap ? overflow : CastStatus.InvalidLexical;

    private static CastStatus FromFloating(double value, out XdmItem result)
    {
        var converted = DecimalArithmetic.FromDouble(value);
        result = converted.Success ? XdmItem.FromDecimal(converted.Value) : default;
        return converted.Success ? CastStatus.Ok : CastStatus.InvalidValue;
    }

    private static CastStatus Truncate(double value, out XdmItem result)
    {
        result = default;
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return CastStatus.InvalidValue;
        }

        // Truncation toward zero by the conversion itself; the bounds are exact doubles.
        if (value is < -LongRange or >= LongRange)
        {
            return CastStatus.IntegerOverflow;
        }

        result = XdmItem.FromInteger((long)value);
        return CastStatus.Ok;
    }

    private static CastStatus FromBigInteger(BigInteger value, out XdmItem result)
    {
        if (value < long.MinValue || value > long.MaxValue)
        {
            result = default;
            return CastStatus.IntegerOverflow;
        }

        result = XdmItem.FromInteger((long)value);
        return CastStatus.Ok;
    }
}
