namespace NetEvolve.Schemas.Xml.Datatypes.Gregorian;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;

/// <summary>The primitive types gYear, gYearMonth, gMonth, gMonthDay, and gDay (XSD 1.1 Part 2, sections 3.3.10 to 3.3.14), and dateTimeStamp (section 3.4.28).</summary>
/// <remarks>
/// They share the seven-property value <see cref="XsdDateTime"/>; the properties a type does not have are zero.
/// Limitations: the values are reported as unordered (<see cref="XsdOrder.Incomparable"/>) until the timeline order is added (DT-54);
/// dateTimeStamp enforces its required timezone in the lexical mapping, not through the explicitTimezone facet (DT-55);
/// the day of gMonthDay is checked against a leap year, so <c>--02-29</c> is valid.
/// </remarks>
internal static class GregorianDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        Register("gYear", XsdDateTimeKind.GYear),
        Register("gYearMonth", XsdDateTimeKind.GYearMonth),
        Register("gMonth", XsdDateTimeKind.GMonth),
        Register("gMonthDay", XsdDateTimeKind.GMonthDay),
        Register("gDay", XsdDateTimeKind.GDay),
        new("dateTimeStamp", XsdVersion.V11, CreateDateTimeStamp),
    ];

    private static DatatypeRegistration Register(string name, XsdDateTimeKind kind) =>
        new(name, XsdVersion.V10, r => new AtomicDescriptor<XsdDateTime>(
            name,
            r.Version,
            new DateTimeDatatypes.Mapping(kind)
        ));

    private static AtomicDescriptor<XsdDateTime> CreateDateTimeStamp(DatatypeRegistry registry) =>
        new(
            "dateTimeStamp",
            registry.Get("dateTime"),
            new TimestampMapping(new DateTimeDatatypes.Mapping(XsdDateTimeKind.DateTime)),
            null,
            []
        );

    /// <summary>The dateTime mapping with the explicit timezone required.</summary>
    private sealed class TimestampMapping : ILexicalMapping<XsdDateTime>
    {
        private readonly DateTimeDatatypes.Mapping _inner;

        public TimestampMapping(DateTimeDatatypes.Mapping inner) => _inner = inner;

        public WhitespaceMode Whitespace => _inner.Whitespace;

        public bool IsOrdered => _inner.IsOrdered;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            var error = _inner.Check(normalized, version, out errorOffset);
            if (error != DatatypeError.None)
            {
                return error;
            }

            if (_inner.Map(normalized, version).TimezoneOffsetMinutes is null)
            {
                errorOffset = normalized.Length;
                return DatatypeError.InvalidLexicalForm;
            }

            return DatatypeError.None;
        }

        public XsdDateTime Map(ReadOnlySpan<char> normalized, XsdVersion version) => _inner.Map(normalized, version);

        public XsdOrder Compare(in XsdDateTime a, in XsdDateTime b, XsdVersion version) =>
            _inner.Compare(in a, in b, version);

        public bool IsIdentical(in XsdDateTime a, in XsdDateTime b, XsdVersion version) =>
            _inner.IsIdentical(in a, in b, version);
    }
}
