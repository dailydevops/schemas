namespace NetEvolve.Schemas.Xml.Datatypes.QNames;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>The built-in <c>QName</c> and <c>NOTATION</c> types (XSD 1.1 Part 2, sections 3.3.18 and 3.3.19).</summary>
/// <remarks>
/// Limitation: a prefix needs a namespace context, which the context-free descriptor pipeline does not have. The registered
/// descriptors therefore check the lexical form only and map to its <see cref="TextRange"/>; the resolved value comes from
/// <see cref="XsdQName.Parse"/>.
/// </remarks>
internal static class QNameDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        new("QName", XsdVersion.V10, static r => new AtomicDescriptor<TextRange>(
            "QName",
            r.Version,
            new QNameMapping()
        )),
        new("NOTATION", XsdVersion.V10, static r => new AtomicDescriptor<TextRange>(
            "NOTATION",
            r.Version,
            new QNameMapping()
        )),
    ];

    private sealed class QNameMapping : ILexicalMapping<TextRange>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => false;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset) =>
            XsdQName.Scan(normalized, out _, out errorOffset);

        public TextRange Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version) =>
            new(0, normalized.Length);

        public XsdOrder Compare(in TextRange a, in TextRange b, XsdVersion version) => XsdOrder.Incomparable;

        public bool IsIdentical(in TextRange a, in TextRange b, XsdVersion version) => false;
    }
}
