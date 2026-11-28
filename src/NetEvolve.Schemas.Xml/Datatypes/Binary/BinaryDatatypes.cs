namespace NetEvolve.Schemas.Xml.Datatypes.Binary;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>The built-in <c>hexBinary</c> and <c>base64Binary</c> types (XSD 1.1 Part 2, sections 3.3.15 and 3.3.16).</summary>
/// <remarks>Limitation: the values are unordered; two values compare as <see cref="XsdOrder.Equal"/> or <see cref="XsdOrder.Incomparable"/>.</remarks>
internal static class BinaryDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        new("hexBinary", XsdVersion.V10, static r => new AtomicDescriptor<XsdBinary>(
            "hexBinary",
            r.Version,
            new BinaryMapping(BinaryKind.HexBinary)
        )),
        new("base64Binary", XsdVersion.V10, static r => new AtomicDescriptor<XsdBinary>(
            "base64Binary",
            r.Version,
            new BinaryMapping(BinaryKind.Base64Binary)
        )),
    ];

    private sealed class BinaryMapping : ILexicalMapping<XsdBinary>, IFusedLexicalMapping<XsdBinary>
    {
        private readonly BinaryKind _kind;

        public BinaryMapping(BinaryKind kind) => _kind = kind;

        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => false;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset) =>
            BinaryLexical.Scan(normalized, _kind, out _, out errorOffset);

        public XsdBinary Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version)
        {
            _ = BinaryLexical.Scan(normalized, _kind, out var octets, out _);
            return XsdBinary.Map(normalized, _kind, octets);
        }

        public DatatypeError TryMap(
            ReadOnlySpan<char> normalized,
            WhitespaceMode mode,
            XsdVersion version,
            out XsdBinary value,
            out int errorOffset
        )
        {
            var error = BinaryLexical.Scan(normalized, _kind, out var octets, out errorOffset);
            value = error == DatatypeError.None ? XsdBinary.Map(normalized, _kind, octets) : default;
            return error;
        }

        public XsdOrder Compare(in XsdBinary a, in XsdBinary b, XsdVersion version) =>
            a.Equals(b) ? XsdOrder.Equal : XsdOrder.Incomparable;

        public bool IsIdentical(in XsdBinary a, in XsdBinary b, XsdVersion version) => a.Equals(b);
    }
}
