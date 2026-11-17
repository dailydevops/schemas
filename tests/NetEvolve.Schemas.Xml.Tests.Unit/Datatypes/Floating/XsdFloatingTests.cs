namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Floating;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Floating;

public class XsdFloatingTests
{
    private static IDatatypeDescriptor<double> Double(XsdVersion version) =>
        (IDatatypeDescriptor<double>)BuiltInDatatypes.For(version).Get("double");

    private static IDatatypeDescriptor<float> Single(XsdVersion version) =>
        (IDatatypeDescriptor<float>)BuiltInDatatypes.For(version).Get("float");

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Registry_ContainsFloatAndDouble_InBothVersions(XsdVersion version)
    {
        var registry = BuiltInDatatypes.For(version);

        await Assert.That(registry.Get("float").Name).IsEqualTo("float");
        await Assert.That(registry.Get("double").Name).IsEqualTo("double");
        await Assert.That(registry.Get("float").LexicalMode).IsEqualTo(version);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Parse_NaNAndInfinities_AreAcceptedInBothModes(XsdVersion version)
    {
        var d = Double(version);
        var f = Single(version);

        await Assert.That(double.IsNaN(d.Parse("NaN").Value)).IsTrue();
        await Assert.That(double.IsPositiveInfinity(d.Parse("INF").Value)).IsTrue();
        await Assert.That(double.IsNegativeInfinity(d.Parse("-INF").Value)).IsTrue();
        await Assert.That(float.IsNaN(f.Parse("NaN").Value)).IsTrue();
        await Assert.That(float.IsPositiveInfinity(f.Parse("INF").Value)).IsTrue();
        await Assert.That(float.IsNegativeInfinity(f.Parse("-INF").Value)).IsTrue();
    }

    [Test]
    public async Task Parse_PlusInf_IsAcceptedOnlyInVersion11()
    {
        await Assert.That(double.IsPositiveInfinity(Double(XsdVersion.V11).Parse("+INF").Value)).IsTrue();
        await Assert.That(float.IsPositiveInfinity(Single(XsdVersion.V11).Parse("+INF").Value)).IsTrue();

        var d10 = Double(XsdVersion.V10).Parse("+INF");
        await Assert.That(d10.Success).IsFalse();
        await Assert.That(d10.Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(Single(XsdVersion.V10).Parse("+INF").Success).IsFalse();
    }

    [Test]
    [Arguments("Infinity")]
    [Arguments("-Infinity")]
    [Arguments("nan")]
    [Arguments("NAN")]
    [Arguments("inf")]
    [Arguments("-NaN")]
    [Arguments("+NaN")]
    [Arguments("1e")]
    [Arguments("1e+")]
    [Arguments("e5")]
    [Arguments(".")]
    [Arguments("")]
    [Arguments("+")]
    [Arguments("--1")]
    [Arguments("1.2.3")]
    [Arguments("1e2.5")]
    [Arguments("1e2e3")]
    [Arguments("0x10")]
    [Arguments("1,5")]
    [Arguments("1 2")]
    [Arguments("INFx")]
    public async Task Parse_InvalidLexicalForms_AreRejected(string lexical)
    {
        foreach (var version in new[] { XsdVersion.V10, XsdVersion.V11 })
        {
            await Assert.That(Double(version).Parse(lexical).Success).IsFalse();
            await Assert.That(Single(version).Parse(lexical).Success).IsFalse();
        }
    }

    [Test]
    public async Task Parse_ErrorOffset_PointsAtTheFault()
    {
        await Assert.That(Double(XsdVersion.V11).Parse("1e").ErrorOffset).IsEqualTo(2);
        await Assert.That(Double(XsdVersion.V11).Parse("12x").ErrorOffset).IsEqualTo(2);
        await Assert.That(Double(XsdVersion.V11).Parse("1e2x").ErrorOffset).IsEqualTo(3);
        await Assert.That(Double(XsdVersion.V11).Parse(new string('1', 5000)).Error).IsEqualTo(DatatypeError.DigitCap);
    }

    [Test]
    public async Task Parse_SurroundingWhitespace_IsCollapsed()
    {
        await Assert.That(Double(XsdVersion.V11).Parse("  1.5\t\n").Value).IsEqualTo(1.5);
        await Assert.That(Single(XsdVersion.V11).Parse("\r 2.5 ").Value).IsEqualTo(2.5f);
    }

    [Test]
    public async Task Parse_NegativeZero_KeepsItsSign()
    {
        var d = Double(XsdVersion.V10).Parse("-0").Value;
        var f = Single(XsdVersion.V10).Parse("-0.0e5").Value;

        await Assert.That(BitConverter.DoubleToInt64Bits(d)).IsEqualTo(unchecked((long)0x8000000000000000UL));
        await Assert.That(BitConverter.SingleToInt32Bits(f)).IsEqualTo(unchecked((int)0x80000000U));
        await Assert.That(BitConverter.DoubleToInt64Bits(Double(XsdVersion.V10).Parse("0").Value)).IsEqualTo(0L);
    }

    [Test]
    public async Task Parse_SmallestSubnormalAndLargestFinite_ParseExactly()
    {
        await Assert
            .That(BitConverter.DoubleToInt64Bits(Double(XsdVersion.V11).Parse("4.9406564584124654E-324").Value))
            .IsEqualTo(1L);
        await Assert.That(Double(XsdVersion.V11).Parse("1.7976931348623157E308").Value).IsEqualTo(double.MaxValue);
        await Assert.That(Double(XsdVersion.V11).Parse("-1.7976931348623157E308").Value).IsEqualTo(double.MinValue);
        await Assert.That(BitConverter.SingleToInt32Bits(Single(XsdVersion.V11).Parse("1.4E-45").Value)).IsEqualTo(1);
        await Assert.That(Single(XsdVersion.V11).Parse("3.4028235E38").Value).IsEqualTo(float.MaxValue);
        await Assert.That(Single(XsdVersion.V11).Parse("-3.4028235E38").Value).IsEqualTo(float.MinValue);
    }

    [Test]
    [Arguments("0", 0x0000000000000000UL)]
    [Arguments("1", 0x3FF0000000000000UL)]
    [Arguments("0.1", 0x3FB999999999999AUL)]
    [Arguments("1e23", 0x44B52D02C7E14AF6UL)]
    [Arguments("123456789012345678901234567890", 0x45F8EE90FF6C373EUL)]
    [Arguments("9007199254740993", 0x4340000000000000UL)]
    [Arguments("9007199254740995", 0x4340000000000002UL)]
    [Arguments("9007199254740992.5", 0x4340000000000000UL)]
    [Arguments("2.2250738585072011e-308", 0x000FFFFFFFFFFFFFUL)]
    [Arguments("2.2250738585072012e-308", 0x0010000000000000UL)]
    [Arguments("1.7976931348623157e308", 0x7FEFFFFFFFFFFFFFUL)]
    [Arguments("1.7976931348623158e308", 0x7FEFFFFFFFFFFFFFUL)]
    [Arguments("1.7976931348623159e308", 0x7FF0000000000000UL)]
    [Arguments("4.9406564584124654e-324", 0x0000000000000001UL)]
    [Arguments("2.4703282292062327e-324", 0x0000000000000000UL)]
    [Arguments("2.4703282292062328e-324", 0x0000000000000001UL)]
    [Arguments("1e-400", 0x0000000000000000UL)]
    [Arguments("1e400", 0x7FF0000000000000UL)]
    [Arguments("8.41e21", 0x447C7E83209E90B2UL)]
    [Arguments("5e-324", 0x0000000000000001UL)]
    [Arguments(
        "0.000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "000000000000000000000000000000001",
        0x28E33D4032C2C7F5UL
    )]
    [Arguments("4.35e1", 0x4045C00000000000UL)]
    [Arguments("1.00000000000000011102230246251565404236316680908203125", 0x3FF0000000000000UL)]
    [Arguments("1.00000000000000011102230246251565404236316680908203126", 0x3FF0000000000001UL)]
    [Arguments(
        "0.000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000247032822920623272088284396434110686182529901307162382212792841250337753635"
            + "10437593264991818081799618989828234772285886546332835517796989819938739800539093"
            + "90631503565951557022639229085839244910518443593180284993653615250031937045767824"
            + "92193656236698636584807570015857692699037063119282795585513329278343384093519780"
            + "15531246597263579574622766465272827220056374006485499977096599470454020828166226"
            + "23785739345073633900796776193057750674017632467360096895134053553745851666113422"
            + "37666786041621596804619144672918403005300575308490487653917113865916462395249126"
            + "23653881879636239373280423891018672348497668235089863388587925628302755995657524"
            + "45550725518931369083625477918694866799496832404970582102851318545139621383772282"
            + "6145437693412532098591327667236328125",
        0x0000000000000000UL
    )]
    [Arguments(
        "0.000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000247032822920623272088284396434110686182529901307162382212792841250337753635"
            + "10437593264991818081799618989828234772285886546332835517796989819938739800539093"
            + "90631503565951557022639229085839244910518443593180284993653615250031937045767824"
            + "92193656236698636584807570015857692699037063119282795585513329278343384093519780"
            + "15531246597263579574622766465272827220056374006485499977096599470454020828166226"
            + "23785739345073633900796776193057750674017632467360096895134053553745851666113422"
            + "37666786041621596804619144672918403005300575308490487653917113865916462395249126"
            + "23653881879636239373280423891018672348497668235089863388587925628302755995657524"
            + "45550725518931369083625477918694866799496832404970582102851318545139621383772282"
            + "61454376934125320985913276672363281251",
        0x0000000000000001UL
    )]
    [Arguments("1.000000000000000111022302462515654042363166809082031251", 0x3FF0000000000001UL)]
    [Arguments(
        "10000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000000000000000000000"
            + "0e-400",
        0x3FF0000000000000UL
    )]
    [Arguments("0.000000000000000000000000000000000000000000000000001e50", 0x3FB999999999999AUL)]
    [Arguments("00012.5000e+0001", 0x405F400000000000UL)]
    [Arguments("1E2", 0x4059000000000000UL)]
    [Arguments("1.e1", 0x4024000000000000UL)]
    [Arguments(".5E0", 0x3FE0000000000000UL)]
    [Arguments("+1.5", 0x3FF8000000000000UL)]
    [Arguments("0e0", 0x0000000000000000UL)]
    [Arguments("1e99999999999", 0x7FF0000000000000UL)]
    [Arguments("1e-99999999999", 0x0000000000000000UL)]
    public async Task ParseDouble_HardRoundingCases_YieldTheCorrectlyRoundedBits(string lexical, ulong bits)
    {
        foreach (var version in new[] { XsdVersion.V10, XsdVersion.V11 })
        {
            var result = XsdFloatingPoint.ParseDouble(lexical, version);

            await Assert.That(result.Success).IsTrue();
            await Assert.That((ulong)BitConverter.DoubleToInt64Bits(result.Value)).IsEqualTo(bits);
            await Assert
                .That((ulong)BitConverter.DoubleToInt64Bits(Double(version).Parse(lexical).Value))
                .IsEqualTo(bits);
        }
    }

    [Test]
    [Arguments("0", 0x00000000U)]
    [Arguments("1", 0x3F800000U)]
    [Arguments("0.1", 0x3DCCCCCDU)]
    [Arguments("16777217", 0x4B800000U)]
    [Arguments("16777219", 0x4B800002U)]
    [Arguments("16777218", 0x4B800001U)]
    [Arguments("3.4028235e38", 0x7F7FFFFFU)]
    [Arguments("3.4028236e38", 0x7F800000U)]
    [Arguments("3.4028234663852886e38", 0x7F7FFFFFU)]
    [Arguments("3.40282356779733661637539395458142568447e38", 0x7F7FFFFFU)]
    [Arguments("1.401298464324817e-45", 0x00000001U)]
    [Arguments("1e-46", 0x00000000U)]
    [Arguments("1.17549435e-38", 0x00800000U)]
    [Arguments("1.1754942e-38", 0x007FFFFFU)]
    [Arguments(
        "0.000000000000000000000000000000000000000000000700649232162408535461864791644958"
            + "065640130970938257885878534141944895541342930300743319094181060791015625",
        0x00000000U
    )]
    [Arguments(
        "0.000000000000000000000000000000000000000000000700649232162408535461864791644958"
            + "0656401309709382578858785341419448955413429303007433190941810607910156251",
        0x00000001U
    )]
    [Arguments("8388608.5", 0x4B000000U)]
    [Arguments("8388609.5", 0x4B000002U)]
    [Arguments("0.3", 0x3E99999AU)]
    [Arguments("16777216.5", 0x4B800000U)]
    [Arguments("123456789", 0x4CEB79A3U)]
    public async Task ParseSingle_HardRoundingCases_YieldTheCorrectlyRoundedBits(string lexical, uint bits)
    {
        foreach (var version in new[] { XsdVersion.V10, XsdVersion.V11 })
        {
            var result = XsdFloatingPoint.ParseSingle(lexical, version);

            await Assert.That(result.Success).IsTrue();
            await Assert.That((uint)BitConverter.SingleToInt32Bits(result.Value)).IsEqualTo(bits);
            await Assert
                .That((uint)BitConverter.SingleToInt32Bits(Single(version).Parse(lexical).Value))
                .IsEqualTo(bits);
        }
    }

    [Test]
    public async Task Parse_NegativeHardCase_FlipsOnlyTheSign()
    {
        var result = XsdFloatingPoint.ParseDouble("-2.2250738585072011e-308", XsdVersion.V11);

        await Assert.That((ulong)BitConverter.DoubleToInt64Bits(result.Value)).IsEqualTo(0x800FFFFFFFFFFFFFUL);
    }

    [Test]
    [Arguments(0x3FF0000000000000UL, "1.0E0")]
    [Arguments(0x3FB999999999999AUL, "1.0E-1")]
    [Arguments(0x44B52D02C7E14AF6UL, "1.0E23")]
    [Arguments(0x0000000000000001UL, "5.0E-324")]
    [Arguments(0x7FEFFFFFFFFFFFFFUL, "1.7976931348623157E308")]
    [Arguments(0x0010000000000000UL, "2.2250738585072014E-308")]
    [Arguments(0x000FFFFFFFFFFFFFUL, "2.225073858507201E-308")]
    [Arguments(0x3FD3333333333333UL, "3.0E-1")]
    [Arguments(0x3FD5555555555555UL, "3.333333333333333E-1")]
    [Arguments(0x4340000000000000UL, "9.007199254740992E15")]
    [Arguments(0x43E0000000000000UL, "9.223372036854776E18")]
    [Arguments(0x3EB0000000000000UL, "9.5367431640625E-7")]
    [Arguments(0x3D30000000000000UL, "5.684341886080802E-14")]
    [Arguments(0x447C7E83209E90B2UL, "8.41E21")]
    [Arguments(0x4045C00000000000UL, "4.35E1")]
    [Arguments(0x3FF0000000000001UL, "1.0000000000000002E0")]
    [Arguments(0x405EDD3C07FB4C0AUL, "1.2345678901234365E2")]
    [Arguments(0x4197D78400000000UL, "1.0E8")]
    [Arguments(0x7FE0000000000000UL, "8.98846567431158E307")]
    [Arguments(0x0000000000000002UL, "1.0E-323")]
    public async Task ToCanonicalString_Double_IsTheShortestRoundTripForm(ulong bits, string expected)
    {
        var value = BitConverter.Int64BitsToDouble((long)bits);

        await Assert.That(XsdFloatingPoint.ToCanonicalString(value)).IsEqualTo(expected);
        await Assert.That(XsdFloatingPoint.ToCanonicalString(-value)).IsEqualTo("-" + expected);
    }

    [Test]
    [Arguments(0x3F800000U, "1.0E0")]
    [Arguments(0x3DCCCCCDU, "1.0E-1")]
    [Arguments(0x4B800000U, "1.6777216E7")]
    [Arguments(0x7F7FFFFFU, "3.4028235E38")]
    [Arguments(0x00000001U, "1.0E-45")]
    [Arguments(0x00800000U, "1.1754944E-38")]
    [Arguments(0x007FFFFFU, "1.1754942E-38")]
    [Arguments(0x3E99999AU, "3.0E-1")]
    [Arguments(0x4CEB79A3U, "1.2345679E8")]
    [Arguments(0x4B000001U, "8.388609E6")]
    [Arguments(0x3F800001U, "1.0000001E0")]
    [Arguments(0x1A000000U, "2.646978E-23")]
    [Arguments(0x00000002U, "3.0E-45")]
    [Arguments(0x5F000000U, "9.223372E18")]
    public async Task ToCanonicalString_Single_IsTheShortestRoundTripForm(uint bits, string expected)
    {
        var value = BitConverter.Int32BitsToSingle((int)bits);

        await Assert.That(XsdFloatingPoint.ToCanonicalString(value)).IsEqualTo(expected);
        await Assert.That(XsdFloatingPoint.ToCanonicalString(-value)).IsEqualTo("-" + expected);
    }

    [Test]
    public async Task ToCanonicalString_SpecialValues_UseTheXsdSpellings()
    {
        await Assert.That(XsdFloatingPoint.ToCanonicalString(double.NaN)).IsEqualTo("NaN");
        await Assert.That(XsdFloatingPoint.ToCanonicalString(double.PositiveInfinity)).IsEqualTo("INF");
        await Assert.That(XsdFloatingPoint.ToCanonicalString(double.NegativeInfinity)).IsEqualTo("-INF");
        await Assert.That(XsdFloatingPoint.ToCanonicalString(0.0)).IsEqualTo("0.0E0");
        await Assert.That(XsdFloatingPoint.ToCanonicalString(-0.0)).IsEqualTo("-0.0E0");
        await Assert.That(XsdFloatingPoint.ToCanonicalString(float.NaN)).IsEqualTo("NaN");
        await Assert.That(XsdFloatingPoint.ToCanonicalString(float.PositiveInfinity)).IsEqualTo("INF");
        await Assert.That(XsdFloatingPoint.ToCanonicalString(float.NegativeInfinity)).IsEqualTo("-INF");
        await Assert.That(XsdFloatingPoint.ToCanonicalString(0.0f)).IsEqualTo("0.0E0");
        await Assert.That(XsdFloatingPoint.ToCanonicalString(-0.0f)).IsEqualTo("-0.0E0");
    }

    [Test]
    public async Task CanonicalForm_OfPseudoRandomBitPatterns_ParsesBackToTheSameBits()
    {
        var random = new Random(172);
        var buffer = new byte[8];
        for (var i = 0; i < 300; i++)
        {
            random.NextBytes(buffer);
            var dbits = BitConverter.ToInt64(buffer, 0);
            var d = BitConverter.Int64BitsToDouble(dbits);
            if (!double.IsNaN(d))
            {
                var parsed = XsdFloatingPoint.ParseDouble(XsdFloatingPoint.ToCanonicalString(d), XsdVersion.V10);
                await Assert.That(BitConverter.DoubleToInt64Bits(parsed.Value)).IsEqualTo(dbits);
            }

            var fbits = BitConverter.ToInt32(buffer, 0);
            var f = BitConverter.Int32BitsToSingle(fbits);
            if (!float.IsNaN(f))
            {
                var parsed = XsdFloatingPoint.ParseSingle(XsdFloatingPoint.ToCanonicalString(f), XsdVersion.V10);
                await Assert.That(BitConverter.SingleToInt32Bits(parsed.Value)).IsEqualTo(fbits);
            }
        }
    }

    [Test]
    public async Task Compare_FollowsTheValueSpace_IncludingNaNAndZeros()
    {
        var d = Double(XsdVersion.V11);

        await Assert.That(d.IsOrdered).IsTrue();
        await Assert.That(d.Compare(1.0, 2.0)).IsEqualTo(XsdOrder.Less);
        await Assert.That(d.Compare(2.0, 1.0)).IsEqualTo(XsdOrder.Greater);
        await Assert.That(d.Compare(0.0, -0.0)).IsEqualTo(XsdOrder.Equal);
        await Assert.That(d.Compare(double.NegativeInfinity, double.MinValue)).IsEqualTo(XsdOrder.Less);
        await Assert.That(d.Compare(double.NaN, 1.0)).IsEqualTo(XsdOrder.Incomparable);
        await Assert.That(d.Compare(1.0, double.NaN)).IsEqualTo(XsdOrder.Incomparable);
        await Assert.That(d.Compare(double.NaN, double.NaN)).IsEqualTo(XsdOrder.Equal);
        await Assert.That(Single(XsdVersion.V10).Compare(1f, 2f)).IsEqualTo(XsdOrder.Less);
        await Assert.That(Single(XsdVersion.V10).Compare(float.NaN, 1f)).IsEqualTo(XsdOrder.Incomparable);
    }

    [Test]
    public async Task IsIdentical_NaNIsIdenticalToItself_ZeroIsNotIdenticalToNegativeZero()
    {
        var d = Double(XsdVersion.V11);
        var f = Single(XsdVersion.V11);

        await Assert.That(d.IsIdentical(double.NaN, BitConverter.Int64BitsToDouble(0x7FF8000000000001L))).IsTrue();
        await Assert.That(d.IsIdentical(0.0, -0.0)).IsFalse();
        await Assert.That(d.IsIdentical(1.5, 1.5)).IsTrue();
        await Assert.That(d.IsIdentical(1.5, 2.5)).IsFalse();
        await Assert.That(f.IsIdentical(float.NaN, -float.NaN)).IsTrue();
        await Assert.That(f.IsIdentical(0f, -0f)).IsFalse();
        await Assert.That(f.IsIdentical(1.5f, 1.5f)).IsTrue();
    }

    [Test]
    public async Task Validate_UsesTheLexicalRulesOfTheVersion()
    {
        await Assert.That(Double(XsdVersion.V11).Validate("+INF").IsValid).IsTrue();
        await Assert.That(Double(XsdVersion.V10).Validate("+INF").IsValid).IsFalse();
        await Assert.That(Single(XsdVersion.V10).Validate("1.5E3").IsValid).IsTrue();
    }
}
