namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System;
using System.Numerics;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The cast of an <c>xs:untypedAtomic</c> value to the atomic type a function parameter expects: the lexical form goes through the
/// built-in datatype descriptors, which collapse whitespace and check the lexical space.
/// </summary>
/// <remarks>
/// Limitations: only the target types an <see cref="XPathItemType"/> names are covered, so the casting table of XP-22 (#265) and
/// the types that await their value type (#545, #301) are not; the XSD 1.0 lexical rules apply, as XPath 2.0 refers to XML Schema 1.0,
/// and the two XPath duration types come from the 1.1 registry that defines them; an integer beyond 64 bits raises FOCA0003, because
/// <see cref="XdmItem"/> carries 64 bits.
/// </remarks>
internal static class UntypedAtomicCast
{
    private static readonly DatatypeRegistry V10 = BuiltInDatatypes.For(XsdVersion.V10);
    private static readonly DatatypeRegistry V11 = BuiltInDatatypes.For(XsdVersion.V11);

    private static readonly IDatatypeDescriptor<bool> Boolean = (IDatatypeDescriptor<bool>)V10.Get("boolean");
    private static readonly IDatatypeDescriptor<XsdInteger> Integer =
        (IDatatypeDescriptor<XsdInteger>)V10.Get("integer");
    private static readonly IDatatypeDescriptor<XsdDecimal> Decimal =
        (IDatatypeDescriptor<XsdDecimal>)V10.Get("decimal");
    private static readonly IDatatypeDescriptor<float> Float = (IDatatypeDescriptor<float>)V10.Get("float");
    private static readonly IDatatypeDescriptor<double> Double = (IDatatypeDescriptor<double>)V10.Get("double");
    private static readonly IDatatypeDescriptor<XsdString> AnyUri = (IDatatypeDescriptor<XsdString>)V10.Get("anyURI");
    private static readonly IDatatypeDescriptor<XsdDateTime> DateTime =
        (IDatatypeDescriptor<XsdDateTime>)V10.Get("dateTime");
    private static readonly IDatatypeDescriptor<XsdDateTime> Date = (IDatatypeDescriptor<XsdDateTime>)V10.Get("date");
    private static readonly IDatatypeDescriptor<XsdDateTime> Time = (IDatatypeDescriptor<XsdDateTime>)V10.Get("time");
    private static readonly IDatatypeDescriptor<XsdDuration> Duration =
        (IDatatypeDescriptor<XsdDuration>)V10.Get("duration");
    private static readonly IDatatypeDescriptor<XsdDuration> YearMonthDuration =
        (IDatatypeDescriptor<XsdDuration>)V11.Get("yearMonthDuration");
    private static readonly IDatatypeDescriptor<XsdDuration> DayTimeDuration =
        (IDatatypeDescriptor<XsdDuration>)V11.Get("dayTimeDuration");

    /// <summary>Casts a lexical form to a target type.</summary>
    /// <param name="lexical">The lexical form of the <c>xs:untypedAtomic</c> value.</param>
    /// <param name="target">The target type, an atomic type other than <see cref="XPathItemType.AnyAtomic"/> and <see cref="XPathItemType.UntypedAtomic"/>.</param>
    /// <returns>The item.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="lexical"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">The lexical form is invalid for the type (FORG0001), or an integer is beyond 64 bits (FOCA0003).</exception>
    public static XdmItem Cast(string lexical, XPathItemType target)
    {
        Argument.ThrowIfNull(lexical);

        var text = lexical.AsSpan();
        switch (target)
        {
            case XPathItemType.String:
                return XdmItem.FromString(lexical);
            case XPathItemType.Boolean:
                return XdmItem.FromBoolean(Value(Boolean.Parse(text)));
            case XPathItemType.Integer:
                var integer = Value(Integer.Parse(text)).Value;
                return integer >= long.MinValue && integer <= long.MaxValue
                    ? XdmItem.FromInteger((long)integer)
                    : throw XPathErrors.Exception(XPathErrorCodes.FOCA0003);
            case XPathItemType.Decimal:
                return XdmItem.FromDecimal(Value(Decimal.Parse(text)));
            case XPathItemType.Float:
                return XdmItem.FromFloat(Value(Float.Parse(text)));
            case XPathItemType.Double:
                return XdmItem.FromDouble(Value(Double.Parse(text)));
            case XPathItemType.AnyUri:
                var uri = Value(AnyUri.Parse(text));
                var buffer = new char[lexical.Length];
                return XdmItem.FromAnyUri(new string(buffer, 0, uri.CopyTo(text, buffer)));
            case XPathItemType.DateTime:
                return XdmItem.FromDateTime(Value(DateTime.Parse(text)));
            case XPathItemType.Date:
                return XdmItem.FromDateTime(Value(Date.Parse(text)));
            case XPathItemType.Time:
                return XdmItem.FromDateTime(Value(Time.Parse(text)));
            case XPathItemType.Duration:
                return XdmItem.FromDuration(Value(Duration.Parse(text)));
            case XPathItemType.YearMonthDuration:
                return XdmItem.FromDuration(Value(YearMonthDuration.Parse(text)), DurationKind.YearMonth);
            case XPathItemType.DayTimeDuration:
                return XdmItem.FromDuration(Value(DayTimeDuration.Parse(text)), DurationKind.DayTime);
            default:
                throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }
    }

    private static TValue Value<TValue>(ParseResult<TValue> result) =>
        result.Success ? result.Value : throw XPathErrors.Exception(XPathErrorCodes.FORG0001);
}
