namespace NetEvolve.Schemas.XPath.Casting.Other;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The conversion of the rows for <c>xs:QName</c> (Functions and Operators, 17.1.1): a lexical form is resolved against the namespaces of the cast.</summary>
/// <remarks>
/// <para>
/// The lexical form is collapsed and must be an <c>NCName</c> or two joined by a colon; the prefix is resolved by the namespaces of the cast, and
/// an unprefixed name takes their default namespace. A prefix without a binding is <see cref="CastStatus.UnboundPrefix"/> (FONS0004), a form that is not a
/// QName is <see cref="CastStatus.InvalidLexical"/> (FORG0001). Without namespaces (<see langword="null"/>) a string or untyped value cannot be cast: XPath 2.0
/// casts a lexical QName only from a string literal, and the lowering of the cast withholds the namespaces from any other operand.
/// </para>
/// <para>
/// Limitation: <c>xs:NOTATION</c> has no row, because it is abstract. The lowering of a function call hands the namespaces of the static context to
/// the constructor function <c>xs:QName</c>, with the same rule as the cast.
/// </para>
/// </remarks>
internal static class QNameConversions
{
    /// <summary>Converts to <c>xs:QName</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="namespaces">The namespaces that resolve the prefix, <see langword="null"/> when the operand may not be cast from its lexical form.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToQName(in XdmItem source, INamespaceContext? namespaces, out XdmItem result)
    {
        result = default;
        if (source.TryGetString(out var lexical) || source.TryGetUntypedAtomic(out lexical))
        {
            return namespaces is null
                ? CastStatus.NotAllowed
                : Parse(WhitespaceText.Apply(lexical!, WhitespaceMode.Collapse), namespaces, out result);
        }

        if (!source.TryGetQName(out _))
        {
            return CastStatus.NotAllowed;
        }

        result = source;
        return CastStatus.Ok;
    }

    private static CastStatus Parse(string text, INamespaceContext namespaces, out XdmItem result)
    {
        result = default;
        var parsed = XsdQName.Parse(text.AsSpan(), namespaces);
        if (!parsed.Success)
        {
            return parsed.Error == DatatypeError.UnboundPrefix ? CastStatus.UnboundPrefix : CastStatus.InvalidLexical;
        }

        var colon = text.AsSpan().IndexOf(':');
        result = XdmItem.FromQName(new XdmQName(parsed.Value, colon < 0 ? null : text.Substring(0, colon)));
        return CastStatus.Ok;
    }
}
