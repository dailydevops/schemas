namespace NetEvolve.Schemas.XPath.Compatibility;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Nodes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The conversions that XPath 1.0 compatibility mode adds in front of the function conversion rules (XPath 2.0, 3.1.5): when an argument
/// is not of the expected type, it is replaced by its first item if the expected type calls for one item or an optional item; by
/// <c>fn:string</c> of it if the expected type is <c>xs:string</c> or <c>xs:string?</c>; and by <c>fn:number</c> of it if the expected type is
/// <c>xs:double</c> or <c>xs:double?</c>. The result is then converted by <see cref="XPathFunction.Call"/> with the rules of the mode
/// without compatibility, which is the order that 3.1.5 gives.
/// </summary>
/// <remarks>
/// Limitation: 3.1.5 names <c>xs:string</c> and <c>xs:double</c> only, so a parameter of another numeric type, such as the <c>numeric</c>
/// parameter of <c>fn:floor</c>, keeps the first-item rule and then the rules of XPath 2.0: a string for it stays XPTY0004, an untyped value is cast
/// to <c>xs:double</c>, and the empty sequence gives the empty sequence. The XPath 1.0 results for these arguments are not restored.
/// </remarks>
internal static class CompatibilityFunctionConversion
{
    /// <summary>Applies the conversions of compatibility mode to one argument.</summary>
    /// <param name="argument">The argument as evaluated.</param>
    /// <param name="type">The expected type.</param>
    /// <param name="source">The annotation source that supplies typed values.</param>
    /// <returns>The argument for the function conversion rules; the argument itself when it is of the expected type, or when the expected type is not a single item.</returns>
    /// <exception cref="XPathException">The value cannot be converted to a number (XPTY0004 for a value of several atomic items, FOCA0001 beyond the digit cap).</exception>
    public static XdmSequence Convert(in XdmSequence argument, XPathParameterType type, IXdmTypeAnnotationSource source)
    {
        // Only a type of one item or of an optional item takes part; item()* and the other sequence types pass unread, so a lazy argument stays lazy.
        if (type.Occurrence is not (Occurrence.ExactlyOne or Occurrence.ZeroOrOne))
        {
            return argument;
        }

        var value = argument.Count > 1 ? XdmSequence.Create(argument[0]) : argument;
        switch (type.ItemType)
        {
            case XPathItemType.String:
                return IsOfType(value, type, XdmItemKind.XsString) ? value : XdmSequence.Create(AsString(value));
            case XPathItemType.Double:
                return IsOfType(value, type, XdmItemKind.XsDouble)
                    ? value
                    : XdmSequence.Create(AsNumber(value, source));
            default:
                return value;
        }
    }

    private static bool IsOfType(in XdmSequence value, XPathParameterType type, XdmItemKind kind) =>
        value.Count == 0 ? type.Occurrence == Occurrence.ZeroOrOne : value[0].Kind == kind;

    // fn:string: the string value of a node, the canonical form of an atomic value, and the zero-length string for the empty sequence.
    private static XdmItem AsString(in XdmSequence value)
    {
        if (value.IsEmpty)
        {
            return XdmItem.FromString(string.Empty);
        }

        var item = value[0];
        return XdmItem.FromString(
            item.TryGetNode(out var node) ? node.StringValue() : StringFunction.OfAtomic(in item)
        );
    }

    // fn:number: NaN for the empty sequence and for a value without a number; a node is atomized first.
    private static XdmItem AsNumber(in XdmSequence value, IXdmTypeAnnotationSource source)
    {
        var atomized = XPathAtomization.Atomize(value, source);
        return atomized.Count switch
        {
            0 => XdmItem.FromDouble(double.NaN),
            1 => CompatibilityOperands.ToNumber(atomized[0]),
            _ => throw XPathErrors.Exception(XPathErrorCodes.XPTY0004),
        };
    }
}
