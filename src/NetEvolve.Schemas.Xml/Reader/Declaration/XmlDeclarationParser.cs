namespace NetEvolve.Schemas.Xml.Reader.Declaration;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Parses the XML declaration and the text declaration.</summary>
internal static class XmlDeclarationParser
{
    /// <summary>Parses a declaration at the very start of <paramref name="input"/>.</summary>
    /// <param name="input">The decoded characters.</param>
    /// <param name="kind">The grammar to apply.</param>
    /// <returns>The result.</returns>
    internal static XmlDeclarationParseResult Parse(ReadOnlySpan<char> input, XmlDeclarationKind kind) =>
        kind == XmlDeclarationKind.Text
            ? XmlDeclarationParseResult.Invalid(XmlErrorCodes.XML0011, input.Length)
            : XmlDeclarationParseResult.Absent;
}
