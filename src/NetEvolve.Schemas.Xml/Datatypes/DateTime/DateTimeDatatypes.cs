namespace NetEvolve.Schemas.Xml.Datatypes.DateTime;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Ordering;

/// <summary>The primitive types dateTime, date, and time (XSD 1.1 Part 2, sections 3.3.7 to 3.3.9).</summary>
/// <remarks>Limitation: the order is partial, a value without timezone is incomparable to a zoned value within 14 hours; identity compares the seven properties, so equal instants with different offsets are equal but not identical.</remarks>
internal static class DateTimeDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        Register("dateTime", XsdDateTimeKind.DateTime),
        Register("date", XsdDateTimeKind.Date),
        Register("time", XsdDateTimeKind.Time),
    ];

    private static DatatypeRegistration Register(string name, XsdDateTimeKind kind) =>
        new(name, XsdVersion.V10, r => new AtomicDescriptor<XsdDateTime>(name, r.Version, new Mapping(kind)));

    internal sealed class Mapping : ILexicalMapping<XsdDateTime>
    {
        private readonly XsdDateTimeKind _kind;

        public Mapping(XsdDateTimeKind kind) => _kind = kind;

        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => true;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            var result = XsdDateTime.Parse(normalized, _kind, version);
            errorOffset = result.ErrorOffset;
            return result.Error;
        }

        public XsdDateTime Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version) =>
            XsdDateTime.Parse(normalized, _kind, version).Value;

        public XsdOrder Compare(in XsdDateTime a, in XsdDateTime b, XsdVersion version) =>
            DateTimeTimeline.Compare(in a, in b);

        public bool IsIdentical(in XsdDateTime a, in XsdDateTime b, XsdVersion version) => a.Equals(b);
    }
}
