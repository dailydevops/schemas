namespace NetEvolve.Schemas.Xml.Datatypes.Strings;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>The built-in types xs:string, xs:normalizedString, xs:token, and xs:boolean (XSD 1.1 Part 2, sections 3.3.1, 3.3.2, 3.4.1, 3.4.2).</summary>
/// <remarks>Limitation: the descriptor of xs:string has no base type, because a primitive descriptor does not take one.</remarks>
internal static class StringDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        new("string", XsdVersion.V10, static r => new AtomicDescriptor<XsdString>(
            "string",
            r.Version,
            new StringMapping()
        )),
        new("normalizedString", XsdVersion.V10, static r => new AtomicDescriptor<XsdString>(
            "normalizedString",
            (AtomicDescriptor<XsdString>)r.Get("string"),
            WhitespaceMode.Replace,
            []
        )),
        new("token", XsdVersion.V10, static r => new AtomicDescriptor<XsdString>(
            "token",
            (AtomicDescriptor<XsdString>)r.Get("normalizedString"),
            WhitespaceMode.Collapse,
            []
        )),
        new("boolean", XsdVersion.V10, static r => new AtomicDescriptor<bool>(
            "boolean",
            r.Version,
            new BooleanMapping()
        )),
    ];

    private sealed class StringMapping : ILexicalMapping<XsdString>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Preserve;

        public bool IsOrdered => false;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            errorOffset = XmlCharClass.IndexOfNonChar(normalized);
            if (errorOffset >= 0)
            {
                return DatatypeError.InvalidCharacter;
            }

            errorOffset = 0;
            return DatatypeError.None;
        }

        public XsdString Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version) =>
            new(new TextRange(0, normalized.Length), mode);

        // Values are views; equality needs the spans and lives on XsdString.
        public XsdOrder Compare(in XsdString a, in XsdString b, XsdVersion version) => XsdOrder.Incomparable;

        public bool IsIdentical(in XsdString a, in XsdString b, XsdVersion version) => false;
    }

    private sealed class BooleanMapping : ILexicalMapping<bool>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => false;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            errorOffset = 0;
            return
                normalized.SequenceEqual("true")
                || normalized.SequenceEqual("false")
                || normalized.SequenceEqual("1")
                || normalized.SequenceEqual("0")
                ? DatatypeError.None
                : DatatypeError.InvalidLexicalForm;
        }

        public bool Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version) =>
            normalized[0] is 't' or '1';

        public XsdOrder Compare(in bool a, in bool b, XsdVersion version) =>
            a == b ? XsdOrder.Equal : XsdOrder.Incomparable;

        public bool IsIdentical(in bool a, in bool b, XsdVersion version) => a == b;
    }
}
