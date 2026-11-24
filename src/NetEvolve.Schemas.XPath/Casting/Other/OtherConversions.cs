namespace NetEvolve.Schemas.XPath.Casting.Other;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The conversions of the rows for <c>xs:hexBinary</c>, <c>xs:base64Binary</c>, and <c>xs:anyURI</c> (Functions and Operators, 17.1.2 and
/// 17.1.5 to 17.1.6). There is one conversion per target type; the source type is told by the item. A failure is a <see cref="CastStatus"/>,
/// never an exception.
/// </summary>
/// <remarks>
/// <para>
/// A lexical form goes through the built-in datatype of the target, which collapses whitespace and checks the lexical space. A cast between
/// the two binary types keeps the octets, and a binary value as a string is its canonical form: upper-case digits for <c>xs:hexBinary</c>,
/// the characters of the alphabet without whitespace for <c>xs:base64Binary</c>.
/// </para>
/// <para>
/// Limitation: the cell of a string to <c>xs:anyURI</c> is value dependent, but the lexical check of the datatype layer is lax until the strict
/// checkers land (#411, #412), so the cast adds a minimal check of its own: a percent sign needs two hexadecimal digits, and a colon that ends a
/// scheme needs a scheme before it. Every other string of XML characters is a URI, and the value is the string with its whitespace collapsed; a
/// string with a character outside the XML <c>Char</c> production or one of the two malformed forms is rejected (FORG0001).
/// </para>
/// </remarks>
internal static class OtherConversions
{
    private static readonly DatatypeRegistry V10 = BuiltInDatatypes.For(XsdVersion.V10);

    private static readonly IDatatypeDescriptor<XsdBinary> HexBinaryType =
        (IDatatypeDescriptor<XsdBinary>)V10.Get("hexBinary");
    private static readonly IDatatypeDescriptor<XsdBinary> Base64BinaryType =
        (IDatatypeDescriptor<XsdBinary>)V10.Get("base64Binary");
    private static readonly IDatatypeDescriptor AnyUriType = V10.Get("anyURI");

    /// <summary>Converts to <c>xs:hexBinary</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToHexBinary(in XdmItem source, out XdmItem result) =>
        ToBinary(in source, BinaryKind.HexBinary, HexBinaryType, out result);

    /// <summary>Converts to <c>xs:base64Binary</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToBase64Binary(in XdmItem source, out XdmItem result) =>
        ToBinary(in source, BinaryKind.Base64Binary, Base64BinaryType, out result);

    // The lexical check of Functions and Operators 17.1.2 is implementation-defined: a percent sign needs two hexadecimal digits, and a colon that
    // ends a scheme (before the first '/', '?', or '#') needs a scheme before it.
    private static bool IsUriReference(string text)
    {
        var schemeEnds = true;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '%')
            {
                if (i + 2 >= text.Length || !Uri.IsHexDigit(text[i + 1]) || !Uri.IsHexDigit(text[i + 2]))
                {
                    return false;
                }
            }
            else if (schemeEnds && c == ':')
            {
                if (i == 0)
                {
                    return false;
                }

                schemeEnds = false;
            }
            else if (c is '/' or '?' or '#')
            {
                schemeEnds = false;
            }
        }

        return true;
    }

    /// <summary>Converts to <c>xs:anyURI</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToAnyUri(in XdmItem source, out XdmItem result)
    {
        result = default;
        if (source.TryGetString(out var lexical) || source.TryGetUntypedAtomic(out lexical))
        {
            if (!AnyUriType.Validate(lexical.AsSpan()).IsValid)
            {
                return CastStatus.InvalidLexical;
            }

            var collapsed = WhitespaceText.Apply(lexical!, WhitespaceMode.Collapse);
            if (!IsUriReference(collapsed))
            {
                return CastStatus.InvalidLexical;
            }

            result = XdmItem.FromAnyUri(collapsed);
            return CastStatus.Ok;
        }

        if (!source.TryGetAnyUri(out _))
        {
            return CastStatus.NotAllowed;
        }

        result = source;
        return CastStatus.Ok;
    }

    /// <summary>Converts to <c>xs:string</c>.</summary>
    /// <param name="source">The source value, a binary, URI, or QName value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToStringType(in XdmItem source, out XdmItem result)
    {
        if (TryFormat(in source, out var text))
        {
            result = XdmItem.FromString(text!);
            return CastStatus.Ok;
        }

        result = default;
        return CastStatus.NotAllowed;
    }

    /// <summary>Converts to <c>xs:untypedAtomic</c>.</summary>
    /// <param name="source">The source value, a binary, URI, or QName value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToUntypedAtomic(in XdmItem source, out XdmItem result)
    {
        if (TryFormat(in source, out var text))
        {
            result = XdmItem.FromUntypedAtomic(text!);
            return CastStatus.Ok;
        }

        result = default;
        return CastStatus.NotAllowed;
    }

    /// <summary>Gets the canonical lexical form of a binary, URI, or QName value.</summary>
    /// <param name="source">The item.</param>
    /// <param name="text">The canonical form; <see langword="null"/> when the item is none of them.</param>
    /// <returns><see langword="true"/> when the item has one of these types.</returns>
    internal static bool TryFormat(in XdmItem source, out string? text)
    {
        if (source.TryGetBinary(out var binary))
        {
            text = binary.ToString(
                source.Kind == XdmItemKind.HexBinary ? BinaryKind.HexBinary : BinaryKind.Base64Binary
            );
        }
        else if (source.TryGetQName(out var name))
        {
            text = name!.ToString();
        }
        else
        {
            _ = source.TryGetAnyUri(out text);
        }

        return text is not null;
    }

    private static CastStatus ToBinary(
        in XdmItem source,
        BinaryKind target,
        IDatatypeDescriptor<XsdBinary> type,
        out XdmItem result
    )
    {
        result = default;
        if (source.TryGetString(out var lexical) || source.TryGetUntypedAtomic(out lexical))
        {
            var parsed = type.Parse(lexical.AsSpan());
            if (!parsed.Success)
            {
                return CastStatus.InvalidLexical;
            }

            result = XdmItem.FromBinary(parsed.Value, target);
            return CastStatus.Ok;
        }

        if (!source.TryGetBinary(out var octets))
        {
            return CastStatus.NotAllowed;
        }

        // Both types have the same value space, so only the type of the item changes.
        result = XdmItem.FromBinary(octets, target);
        return CastStatus.Ok;
    }
}
