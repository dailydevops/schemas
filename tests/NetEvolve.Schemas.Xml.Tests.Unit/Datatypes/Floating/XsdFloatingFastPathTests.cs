namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Floating;

using System;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Floating;

/// <summary>The allocation-free path rounds exactly like the runtime parser, which is correctly rounded on every tested runtime.</summary>
public class XsdFloatingFastPathTests
{
    private static string Lexical(Sequence random)
    {
        var digits = random.Next(1, 21);
        var builder = new StringBuilder();
        if (random.Next(4) == 0)
        {
            builder.Append('-');
        }

        for (var i = 0; i < digits; i++)
        {
            builder.Append((char)('0' + random.Next(i == 0 ? 1 : 0, 10)));
        }

        if (random.Next(2) == 0)
        {
            builder.Insert(builder.Length - random.Next(0, digits), '.');
            if (builder[^1] == '.')
            {
                builder.Append('0');
            }
        }

        if (random.Next(3) != 0)
        {
            builder.Append('E').Append(random.Next(-30, 31).ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    [Test]
    public async Task Parse_OfRandomLexicalForms_EqualsTheRuntimeParser()
    {
        var random = new Sequence(591);
        for (var i = 0; i < 20000; i++)
        {
            var lexical = Lexical(random);
            var expectedDouble = double.Parse(lexical, NumberStyles.Float, CultureInfo.InvariantCulture);
            var expectedSingle = float.Parse(lexical, NumberStyles.Float, CultureInfo.InvariantCulture);

            await Assert
                .That(BitConverter.DoubleToInt64Bits(XsdFloatingPoint.ParseDouble(lexical, XsdVersion.V11).Value))
                .IsEqualTo(BitConverter.DoubleToInt64Bits(expectedDouble));
            await Assert
                .That(BitConverter.SingleToInt32Bits(XsdFloatingPoint.ParseSingle(lexical, XsdVersion.V11).Value))
                .IsEqualTo(BitConverter.SingleToInt32Bits(expectedSingle));
        }
    }

    [Test]
    [Arguments("4503599627370496.5")]
    [Arguments("4503599627370497.5")]
    [Arguments("9007199254740993")]
    [Arguments("9007199254740995")]
    [Arguments("18014398509481985")]
    [Arguments("18014398509481987")]
    [Arguments("16777217")]
    [Arguments("16777219")]
    [Arguments("33554433")]
    [Arguments("8388608.5")]
    [Arguments("8388609.5")]
    [Arguments("0.5")]
    [Arguments("0.1")]
    [Arguments("0.3")]
    [Arguments("18446744073709551615")]
    [Arguments("18446744073709551615E19")]
    [Arguments("18446744073709551615E-19")]
    [Arguments("9999999999999999999E19")]
    [Arguments("1E-19")]
    [Arguments("1E19")]
    [Arguments("1E20")]
    [Arguments("1E-20")]
    [Arguments("5E-324")]
    public async Task Parse_OfTiesAndWindowBoundaries_EqualsTheRuntimeParser(string lexical)
    {
        var expectedDouble = double.Parse(lexical, NumberStyles.Float, CultureInfo.InvariantCulture);
        var expectedSingle = float.Parse(lexical, NumberStyles.Float, CultureInfo.InvariantCulture);

        await Assert
            .That(BitConverter.DoubleToInt64Bits(XsdFloatingPoint.ParseDouble(lexical, XsdVersion.V11).Value))
            .IsEqualTo(BitConverter.DoubleToInt64Bits(expectedDouble));
        await Assert
            .That(BitConverter.SingleToInt32Bits(XsdFloatingPoint.ParseSingle(lexical, XsdVersion.V11).Value))
            .IsEqualTo(BitConverter.SingleToInt32Bits(expectedSingle));
    }

    // A fixed xorshift sequence: the same lexical forms on every run and runtime.
    private sealed class Sequence
    {
        private ulong _state;

        internal Sequence(ulong seed) => _state = (seed * 0x9E3779B97F4A7C15UL) | 1;

        internal int Next(int minimum, int maximum)
        {
            _state ^= _state << 13;
            _state ^= _state >> 7;
            _state ^= _state << 17;
            return minimum + (int)((_state >> 11) % (ulong)(maximum - minimum));
        }

        internal int Next(int maximum) => Next(0, maximum);
    }
}
