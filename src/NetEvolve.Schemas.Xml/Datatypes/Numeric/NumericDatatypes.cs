namespace NetEvolve.Schemas.Xml.Datatypes.Numeric;

using System;
using System.Collections.Generic;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>The built-in xs:decimal and the integer family (XSD 1.1 Part 2, sections 3.3.3 and 3.4.13 to 3.4.24).</summary>
/// <remarks>
/// Limitations: the lexical forms are capped at <see cref="XsdDecimal.DefaultMaxDigits"/> digits (DT0010); <c>integer</c> checks its lexical
/// space in the mapping instead of carrying the <c>fractionDigits</c> and <c>pattern</c> facets of the specification; the bounded types are
/// carried on <see cref="XsdInteger"/>, a <see cref="BigInteger"/> wrapper, and checked with inclusive bounds, so values beyond 32 bits allocate when mapped.
/// </remarks>
internal static class NumericDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        new("decimal", XsdVersion.V10, static r => new AtomicDescriptor<XsdDecimal>(
            "decimal",
            r.Version,
            new DecimalMapping()
        )),
        new("integer", XsdVersion.V10, static r => new AtomicDescriptor<XsdInteger>(
            "integer",
            r.Get("decimal"),
            new IntegerMapping(),
            null,
            []
        )),
        Derive("nonPositiveInteger", "integer", null, BigInteger.Zero),
        Derive("negativeInteger", "nonPositiveInteger", null, BigInteger.MinusOne),
        Derive("long", "integer", long.MinValue, long.MaxValue),
        Derive("int", "long", int.MinValue, int.MaxValue),
        Derive("short", "int", short.MinValue, short.MaxValue),
        Derive("byte", "short", sbyte.MinValue, sbyte.MaxValue),
        Derive("nonNegativeInteger", "integer", BigInteger.Zero, null),
        Derive("unsignedLong", "nonNegativeInteger", null, ulong.MaxValue),
        Derive("unsignedInt", "unsignedLong", null, uint.MaxValue),
        Derive("unsignedShort", "unsignedInt", null, ushort.MaxValue),
        Derive("unsignedByte", "unsignedShort", null, byte.MaxValue),
        Derive("positiveInteger", "nonNegativeInteger", BigInteger.One, null),
    ];

    // Each type restricts its immediate base with the bounds that differ from it; the inherited ones accumulate in the descriptor.
    private static DatatypeRegistration Derive(string name, string baseName, BigInteger? min, BigInteger? max) =>
        new(name, XsdVersion.V10, r =>
        {
            var facets = new List<IFacet>(2);
            if (min is { } lower)
            {
                facets.Add(new IntegerBound(FacetKind.MinInclusive, lower));
            }

            if (max is { } upper)
            {
                facets.Add(new IntegerBound(FacetKind.MaxInclusive, upper));
            }

            return new AtomicDescriptor<XsdInteger>(name, (AtomicDescriptor<XsdInteger>)r.Get(baseName), null, facets);
        });

    private sealed class DecimalMapping : ILexicalMapping<XsdDecimal>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => true;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            var result = XsdDecimal.Parse(normalized);
            errorOffset = result.ErrorOffset;
            return result.Error;
        }

        public XsdDecimal Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version) =>
            XsdDecimal.Parse(normalized).Value;

        public XsdOrder Compare(in XsdDecimal a, in XsdDecimal b, XsdVersion version)
        {
            var order = a.CompareTo(b);
            return order < 0 ? XsdOrder.Less
                : order > 0 ? XsdOrder.Greater
                : XsdOrder.Equal;
        }

        public bool IsIdentical(in XsdDecimal a, in XsdDecimal b, XsdVersion version) => a.Equals(b);
    }

    private sealed class IntegerMapping : ILexicalMapping<XsdInteger>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => true;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            var start = normalized.Length > 0 && normalized[0] is '+' or '-' ? 1 : 0;
            errorOffset = normalized.Length;
            if (start == normalized.Length)
            {
                return DatatypeError.InvalidLexicalForm;
            }

            for (var i = start; i < normalized.Length; i++)
            {
                if (normalized[i] is < '0' or > '9')
                {
                    errorOffset = i;
                    return DatatypeError.InvalidCharacter;
                }

                if (i - start == XsdDecimal.DefaultMaxDigits)
                {
                    errorOffset = i;
                    return DatatypeError.DigitCap;
                }
            }

            errorOffset = 0;
            return DatatypeError.None;
        }

        // The decimal parser drops trailing zeros into a negative scale; the integer value restores them.
        public XsdInteger Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version)
        {
            // The form passed Check: up to 18 digits fit a long, so the decimal parser is not needed.
            var negative = normalized[0] == '-';
            var digits = normalized[0] is '+' or '-' ? normalized.Slice(1) : normalized;
            if (digits.Length <= 18)
            {
                long small = 0;
                foreach (var c in digits)
                {
                    small = (small * 10) + (c - '0');
                }

                return new(negative ? -small : small);
            }

            var value = XsdDecimal.Parse(normalized).Value;
            return new(value.Scale < 0 ? value.Mantissa * BigInteger.Pow(10, -value.Scale) : value.Mantissa);
        }

        public XsdOrder Compare(in XsdInteger a, in XsdInteger b, XsdVersion version)
        {
            var order = a.CompareTo(b);
            return order < 0 ? XsdOrder.Less
                : order > 0 ? XsdOrder.Greater
                : XsdOrder.Equal;
        }

        public bool IsIdentical(in XsdInteger a, in XsdInteger b, XsdVersion version) => a.Equals(b);
    }
}
