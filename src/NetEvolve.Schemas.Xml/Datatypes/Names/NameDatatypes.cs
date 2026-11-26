namespace NetEvolve.Schemas.Xml.Datatypes.Names;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// The built-in types derived from xs:token: language, Name, NCName, ID, IDREF, ENTITY, NMTOKEN, and the list types IDREFS, ENTITIES, and
/// NMTOKENS (XSD 1.1 Part 2, sections 3.4.3 to 3.4.12).
/// </summary>
/// <remarks>
/// The name rules come from <see cref="XmlNameClass"/>: the XSD 1.0 mode uses the XML 1.0 Second Edition classes, the XSD 1.1 mode the Fifth
/// Edition ones; the multi-character regex escapes of the pattern facet are independent of this split (Fifth Edition in both modes). Limitations: the lexical space of language is checked in the mapping instead of by the <c>pattern</c> facet of the
/// specification; uniqueness of ID, resolution of IDREF, and declared unparsed entities of ENTITY are document-level checks (XSD-14, XML-65, XML-66).
/// </remarks>
internal static class NameDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        new("language", XsdVersion.V10, static r => new AtomicDescriptor<XsdString>(
            "language",
            r.Get("token"),
            new LanguageMapping(),
            null,
            []
        )),
        new("Name", XsdVersion.V10, static r => new AtomicDescriptor<XsdString>(
            "Name",
            r.Get("token"),
            new NameMapping(NameKind.Name),
            null,
            []
        )),
        new("NCName", XsdVersion.V10, static r => new AtomicDescriptor<XsdString>(
            "NCName",
            r.Get("Name"),
            new NameMapping(NameKind.NCName),
            null,
            []
        )),
        new("NMTOKEN", XsdVersion.V10, static r => new AtomicDescriptor<XsdString>(
            "NMTOKEN",
            r.Get("token"),
            new NameMapping(NameKind.NmToken),
            null,
            []
        )),
        DeriveFromNCName("ID"),
        DeriveFromNCName("IDREF"),
        DeriveFromNCName("ENTITY"),
        ListOf("IDREFS", "IDREF"),
        ListOf("ENTITIES", "ENTITY"),
        ListOf("NMTOKENS", "NMTOKEN"),
    ];

    private static DatatypeRegistration DeriveFromNCName(string name) =>
        new(name, XsdVersion.V10, r => new AtomicDescriptor<XsdString>(
            name,
            (AtomicDescriptor<XsdString>)r.Get("NCName"),
            null,
            []
        ));

    private static DatatypeRegistration ListOf(string name, string itemName) =>
        new(name, XsdVersion.V10, r => new NameListDescriptor(name, r.Get(itemName)));

    private enum NameKind
    {
        Name,
        NCName,
        NmToken,
    }

    // Values are views of the caller span, like xs:string; equality needs the spans and lives on XsdString.
    private abstract class TokenMapping : ILexicalMapping<XsdString>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => false;

        public abstract DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset);

        public XsdString Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version) =>
            new(new TextRange(0, normalized.Length), mode);

        public XsdOrder Compare(in XsdString a, in XsdString b, XsdVersion version) => XsdOrder.Incomparable;

        public bool IsIdentical(in XsdString a, in XsdString b, XsdVersion version) => false;
    }

    private sealed class NameMapping : TokenMapping
    {
        private readonly NameKind _kind;

        public NameMapping(NameKind kind) => _kind = kind;

        public override DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            var edition = version == XsdVersion.V11 ? NameEdition.Xml10FifthEdition : NameEdition.Xml10SecondEdition;
            errorOffset = 0;
            if (normalized.IsEmpty)
            {
                return DatatypeError.InvalidLexicalForm;
            }

            var i = 0;
            while (i < normalized.Length)
            {
                var codePoint = (int)normalized[i];
                var width = 1;
                if (
                    char.IsHighSurrogate(normalized[i])
                    && i + 1 < normalized.Length
                    && char.IsLowSurrogate(normalized[i + 1])
                )
                {
                    codePoint = char.ConvertToUtf32(normalized[i], normalized[i + 1]);
                    width = 2;
                }

                var accepted =
                    _kind != NameKind.NmToken && i == 0
                        ? XmlNameClass.IsNameStartChar(edition, codePoint)
                        : XmlNameClass.IsNameChar(edition, codePoint);
                if (!accepted || (_kind == NameKind.NCName && codePoint == ':'))
                {
                    errorOffset = i;
                    return DatatypeError.InvalidLexicalForm;
                }

                i += width;
            }

            return DatatypeError.None;
        }
    }

    // [a-zA-Z]{1,8}(-[a-zA-Z0-9]{1,8})*
    private sealed class LanguageMapping : TokenMapping
    {
        private const int MaxSubtagLength = 8;

        public override DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            var subtagLength = 0;
            var first = true;
            for (var i = 0; i < normalized.Length; i++)
            {
                var c = normalized[i];
                if (c == '-' && subtagLength > 0)
                {
                    subtagLength = 0;
                    first = false;
                    continue;
                }

                var isLetter = (uint)((c | 0x20) - 'a') <= 'z' - 'a';
                if (!(isLetter || (!first && (uint)(c - '0') <= 9)) || ++subtagLength > MaxSubtagLength)
                {
                    errorOffset = i;
                    return DatatypeError.InvalidLexicalForm;
                }
            }

            errorOffset = normalized.Length;
            return subtagLength == 0 ? DatatypeError.InvalidLexicalForm : DatatypeError.None;
        }
    }
}
