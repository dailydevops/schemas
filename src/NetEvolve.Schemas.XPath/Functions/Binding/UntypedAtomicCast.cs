namespace NetEvolve.Schemas.XPath.Functions.Binding;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The cast of an <c>xs:untypedAtomic</c> value to the atomic type a function parameter, an operator, or an aggregate expects. It has no
/// parse logic of its own: the value goes through the row of the casting table (<see cref="CastingCatalog.Default"/>), so function
/// conversion, the operators, and <c>cast as</c> give one answer for one lexical form.
/// </summary>
/// <remarks>
/// Limitations: only the target types an <see cref="XPathItemType"/> names are covered; the XSD 1.0 lexical rules apply, as XPath 2.0 refers
/// to XML Schema 1.0; an integer beyond 64 bits raises FOCA0003, because <see cref="XdmItem"/> carries 64 bits, and a lexical form beyond the
/// digit cap of the datatype layer (4,096 digits) is an implementation limit, not an invalid form: FOCA0003 for an integer, FOCA0001 for a
/// decimal, a float, or a double. The first cast reads the casting table, which freezes it (<see cref="CastingCatalog"/>).
/// </remarks>
internal static class UntypedAtomicCast
{
    /// <summary>Casts a lexical form to a target type.</summary>
    /// <param name="lexical">The lexical form of the <c>xs:untypedAtomic</c> value.</param>
    /// <param name="target">The target type, an atomic type other than <see cref="XPathItemType.AnyAtomic"/> and <see cref="XPathItemType.UntypedAtomic"/>.</param>
    /// <returns>The item.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="lexical"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">The lexical form is invalid for the type (FORG0001), an integer is beyond 64 bits (FOCA0003), a decimal, float, or double is beyond the digit cap (FOCA0001), or the target is not a cast target (XPTY0004).</exception>
    public static XdmItem Cast(string lexical, XPathItemType target)
    {
        ArgumentNullException.ThrowIfNull(lexical);

        if (TypeOf(target) is not { } type)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        var status = CastingCatalog.Default.Table.TryCast(XdmItem.FromUntypedAtomic(lexical), type, out var result);
        return status == CastStatus.Ok ? result : throw XPathErrors.Exception(status.ToErrorCode());
    }

    private static XdmTypeId? TypeOf(XPathItemType target) =>
        target switch
        {
            XPathItemType.String => XdmTypeId.XsString,
            XPathItemType.Boolean => XdmTypeId.Boolean,
            XPathItemType.Integer => XdmTypeId.XsInteger,
            XPathItemType.Decimal => XdmTypeId.XsDecimal,
            XPathItemType.Float => XdmTypeId.XsFloat,
            XPathItemType.Double => XdmTypeId.XsDouble,
            XPathItemType.AnyUri => XdmTypeId.AnyUri,
            XPathItemType.DateTime => XdmTypeId.DateTime,
            XPathItemType.Date => XdmTypeId.Date,
            XPathItemType.Time => XdmTypeId.Time,
            XPathItemType.Duration => XdmTypeId.Duration,
            XPathItemType.YearMonthDuration => XdmTypeId.YearMonthDuration,
            XPathItemType.DayTimeDuration => XdmTypeId.DayTimeDuration,
            _ => null,
        };
}
