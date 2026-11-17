namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The special built-in types anySimpleType and anyAtomicType (XSD 1.1 Part 2, section 3.2).</summary>
/// <remarks>Limitation: both types keep the lexical form as a <see cref="TextRange"/>; their values are unordered and never compare as identical.</remarks>
internal static class SpecialDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        new("anySimpleType", XsdVersion.V10, static r => new AtomicDescriptor<TextRange>(
            "anySimpleType",
            r.Version,
            new TextMapping()
        )),
        new("anyAtomicType", XsdVersion.V11, static r => new AtomicDescriptor<TextRange>(
            "anyAtomicType",
            (AtomicDescriptor<TextRange>)r.Get("anySimpleType"),
            null,
            []
        )),
    ];

    private sealed class TextMapping : ILexicalMapping<TextRange>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Preserve;

        public bool IsOrdered => false;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            errorOffset = 0;
            return DatatypeError.None;
        }

        public TextRange Map(ReadOnlySpan<char> normalized, XsdVersion version) => new(0, normalized.Length);

        public XsdOrder Compare(in TextRange a, in TextRange b, XsdVersion version) => XsdOrder.Incomparable;

        public bool IsIdentical(in TextRange a, in TextRange b, XsdVersion version) => false;
    }
}
