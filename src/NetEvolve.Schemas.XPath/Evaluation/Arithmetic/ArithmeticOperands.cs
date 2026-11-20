namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The operand preparation that every arithmetic operator shares (XPath 2.0, 3.4): atomization, the empty sequence, the single
/// item, and the cast of <c>xs:untypedAtomic</c> to <c>xs:double</c>.
/// </summary>
/// <remarks>XPath 1.0 compatibility mode, which converts the first item and every operand to a number, is not applied here: its nodes are <see cref="Compatibility.CompatibilityArithmeticNode"/> and <see cref="Compatibility.CompatibilityUnaryNode"/>.</remarks>
internal static class ArithmeticOperands
{
    /// <summary>Checks that an atomized operand is one item and casts it when it is <c>xs:untypedAtomic</c>.</summary>
    /// <param name="atomized">The atomized operand, not empty.</param>
    /// <returns>The item, an <c>xs:double</c> when the operand was <c>xs:untypedAtomic</c>.</returns>
    /// <exception cref="XPathException">The operand has more than one item (XPTY0004), or its text is not a double (FORG0001).</exception>
    public static XdmItem Single(in XdmSequence atomized)
    {
        if (atomized.Count != 1)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        var item = atomized[0];
        if (!item.TryGetUntypedAtomic(out var lexical))
        {
            return item;
        }

        return UntypedAtomicCast.Cast(lexical!, XPathItemType.Double);
    }
}
