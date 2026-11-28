namespace NetEvolve.Schemas.Xml.Datatypes.Floating;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>The built-in <see cref="float"/> and <see cref="double"/> types (XSD 1.1 Part 2, sections 3.3.4 and 3.3.5).</summary>
/// <remarks>Limitation: <c>NaN</c> compares equal to itself, as the value space defines, but incomparable to every other value; the <c>+INF</c> spelling is accepted in 1.1 mode only.</remarks>
internal static class FloatingDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [
        new("float", XsdVersion.V10, static r => new AtomicDescriptor<float>("float", r.Version, new SingleMapping())),
        new("double", XsdVersion.V10, static r => new AtomicDescriptor<double>(
            "double",
            r.Version,
            new DoubleMapping()
        )),
    ];

    private static XsdOrder Order(double a, double b)
    {
        if (double.IsNaN(a) || double.IsNaN(b))
        {
            return double.IsNaN(a) && double.IsNaN(b) ? XsdOrder.Equal : XsdOrder.Incomparable;
        }

        if (a < b)
        {
            return XsdOrder.Less;
        }

        return a > b ? XsdOrder.Greater : XsdOrder.Equal;
    }

    // Identical: the same bit pattern, so zero and negative zero differ; every NaN is the one NaN of the value space.
    private static bool Same(double a, double b) =>
        double.IsNaN(a) ? double.IsNaN(b) : BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

    private sealed class DoubleMapping : ILexicalMapping<double>, IFusedLexicalMapping<double>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => true;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset) =>
            XsdFloatingPoint.Parse(normalized, version, false, out _, out errorOffset);

        public double Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version)
        {
            _ = XsdFloatingPoint.Parse(normalized, version, false, out var value, out _);
            return value;
        }

        public DatatypeError TryMap(
            ReadOnlySpan<char> normalized,
            WhitespaceMode mode,
            XsdVersion version,
            out double value,
            out int errorOffset
        ) => XsdFloatingPoint.Parse(normalized, version, false, out value, out errorOffset);

        public XsdOrder Compare(in double a, in double b, XsdVersion version) => Order(a, b);

        public bool IsIdentical(in double a, in double b, XsdVersion version) => Same(a, b);
    }

    private sealed class SingleMapping : ILexicalMapping<float>, IFusedLexicalMapping<float>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => true;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset) =>
            XsdFloatingPoint.Parse(normalized, version, true, out _, out errorOffset);

        public float Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version)
        {
            _ = XsdFloatingPoint.Parse(normalized, version, true, out var value, out _);
            return (float)value;
        }

        public DatatypeError TryMap(
            ReadOnlySpan<char> normalized,
            WhitespaceMode mode,
            XsdVersion version,
            out float value,
            out int errorOffset
        )
        {
            var error = XsdFloatingPoint.Parse(normalized, version, true, out var parsed, out errorOffset);
            value = (float)parsed;
            return error;
        }

        public XsdOrder Compare(in float a, in float b, XsdVersion version) => Order(a, b);

        public bool IsIdentical(in float a, in float b, XsdVersion version) => Same(a, b);
    }
}
