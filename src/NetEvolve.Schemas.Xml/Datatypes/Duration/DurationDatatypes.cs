namespace NetEvolve.Schemas.Xml.Datatypes.Duration;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>The built-in <c>duration</c>, <c>yearMonthDuration</c>, and <c>dayTimeDuration</c> types (XSD 1.1 Part 2, sections 3.3.6, 3.4.26, and 3.4.27).</summary>
/// <remarks>
/// Limitation: the order is decided only when the months or the seconds are equal, otherwise it is
/// <see cref="XsdOrder.Incomparable"/>; this is exact for the two derived types, and the partial order of <c>duration</c> against
/// the reference dateTimes is left to DT-54. <c>yearMonthDuration</c> and <c>dayTimeDuration</c> exist from XSD 1.1 on.
/// </remarks>
internal static class DurationDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        new("duration", XsdVersion.V10, static r => new AtomicDescriptor<XsdDuration>(
            "duration",
            r.Version,
            new DurationMapping()
        )),
        new("yearMonthDuration", XsdVersion.V11, static r => Derive(r, "yearMonthDuration", DurationKind.YearMonth)),
        new("dayTimeDuration", XsdVersion.V11, static r => Derive(r, "dayTimeDuration", DurationKind.DayTime)),
    ];

    private static AtomicDescriptor<XsdDuration> Derive(DatatypeRegistry registry, string name, DurationKind kind) =>
        new(name, (AtomicDescriptor<XsdDuration>)registry.Get("duration"), null, [new DurationShapeFacet(kind)]);

    // The restriction of the derived types, which XSD defines as a pattern over the duration lexical space.
    private sealed class DurationShapeFacet : ILexicalFacet
    {
        private readonly DurationKind _kind;

        public DurationShapeFacet(DurationKind kind) => _kind = kind;

        public FacetKind Kind => FacetKind.Pattern;

        public bool IsSatisfiedBy(ReadOnlySpan<char> lexical) =>
            DurationLexical.Scan(lexical, _kind, out _, out _) == DatatypeError.None;
    }

    private sealed class DurationMapping : ILexicalMapping<XsdDuration>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => true;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset) =>
            DurationLexical.Scan(normalized, DurationKind.Duration, out _, out errorOffset);

        public XsdDuration Map(ReadOnlySpan<char> normalized, XsdVersion version)
        {
            _ = DurationLexical.Scan(normalized, DurationKind.Duration, out var parts, out _);
            return XsdDuration.Map(normalized, parts);
        }

        public XsdOrder Compare(in XsdDuration a, in XsdDuration b, XsdVersion version)
        {
            if (a.Seconds.Equals(b.Seconds))
            {
                return Order(a.Months.CompareTo(b.Months));
            }

            return a.Months == b.Months ? Order(a.Seconds.CompareTo(b.Seconds)) : XsdOrder.Incomparable;
        }

        public bool IsIdentical(in XsdDuration a, in XsdDuration b, XsdVersion version) => a.Equals(b);

        private static XsdOrder Order(int comparison) =>
            comparison < 0 ? XsdOrder.Less
            : comparison > 0 ? XsdOrder.Greater
            : XsdOrder.Equal;
    }
}
