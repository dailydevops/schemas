namespace NetEvolve.Schemas.XPath.Compatibility;

using NetEvolve.Schemas.XPath.Functions.Numerics;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The operand conversions of XPath 1.0 compatibility mode that the arithmetic and the comparisons share: the conversion of one atomic
/// value by <c>fn:number</c>, which is NaN for a value that has no number.
/// </summary>
/// <remarks>
/// Limitation: the conversion is the one of <see cref="NumberFunction"/>, so a string is read by the lexical form of <c>xs:double</c> of
/// XPath 2.0: <c>+4</c>, <c>1e2</c>, and <c>INF</c> are numbers, and <c>Infinity</c> is NaN, as XPath 2.0, Appendix I lists.
/// </remarks>
internal static class CompatibilityOperands
{
    /// <summary>Converts an atomic value to <c>xs:double</c> by <c>fn:number</c>.</summary>
    /// <param name="atomic">The atomic value.</param>
    /// <returns>The <c>xs:double</c>; NaN when the value cannot be converted.</returns>
    /// <exception cref="Errors.XPathException">The text is beyond the digit cap of the datatype layer (FOCA0001).</exception>
    public static XdmItem ToNumber(in XdmItem atomic) => NumberFunction.ToDouble(atomic);

    /// <summary>
    /// Prepares an operand of an arithmetic operator (XPath 2.0, 3.4): a boolean, a string, a decimal (an integer included), a float,
    /// or an untyped value becomes an <c>xs:double</c> by <c>fn:number</c>; every other value, a double and the date, time, and
    /// duration types among them, stays as it is.
    /// </summary>
    /// <param name="atomic">The atomic value.</param>
    /// <returns>The prepared value.</returns>
    /// <exception cref="Errors.XPathException">The text is beyond the digit cap of the datatype layer (FOCA0001).</exception>
    public static XdmItem ArithmeticOperand(in XdmItem atomic) =>
        atomic.Kind
            is XdmItemKind.Boolean
                or XdmItemKind.XsString
                or XdmItemKind.XsDecimal
                or XdmItemKind.XsInteger
                or XdmItemKind.XsFloat
                or XdmItemKind.UntypedAtomic
            ? ToNumber(atomic)
            : atomic;

    /// <summary>Tells whether an atomic value has a numeric type: an integer, a decimal, a float, or a double.</summary>
    /// <param name="atomic">The atomic value.</param>
    /// <returns><see langword="true"/> for a numeric value.</returns>
    public static bool IsNumeric(in XdmItem atomic) =>
        atomic.Kind is XdmItemKind.XsInteger or XdmItemKind.XsDecimal or XdmItemKind.XsFloat or XdmItemKind.XsDouble;
}
