namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Floating;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Floating;

/// <summary>Float and double parsing of at most 19 significant digits with a small exponent allocates nothing (AP-8 and the allocation budget of the datatype design).</summary>
public class XsdFloatingAllocationTests
{
    private const int Warmups = 50;

    [Test]
    [Arguments("1.5")]
    [Arguments("15")]
    [Arguments("1E3")]
    [Arguments("-0.5")]
    [Arguments("+2.25e-3")]
    [Arguments("0")]
    [Arguments("-0.0")]
    [Arguments("0.000")]
    [Arguments("100")]
    [Arguments("12345678901234567")]
    [Arguments("9999999999999999999")]
    [Arguments("0.1234567890123456789")]
    [Arguments("1E19")]
    [Arguments("1E-19")]
    [Arguments("9.999999999999999999E19")]
    [Arguments("1.7976931348623157E0")]
    [Arguments("NaN")]
    [Arguments("INF")]
    [Arguments("-INF")]
    public async Task ParseDouble_ShortLexicalForm_AllocatesNothing(string lexical)
    {
        var result = XsdFloatingPoint.ParseDouble(lexical, XsdVersion.V11);

        AllocationAssert.NoAllocation(
            () => _ = XsdFloatingPoint.ParseDouble(lexical, XsdVersion.V11),
            warmups: Warmups
        );

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    [Arguments("1.5")]
    [Arguments("15")]
    [Arguments("1E3")]
    [Arguments("-0.5")]
    [Arguments("3.4028235E38")]
    [Arguments("1.4E-45")]
    [Arguments("9999999999999999999")]
    [Arguments("1E19")]
    [Arguments("1E-19")]
    public async Task ParseSingle_ShortLexicalForm_AllocatesNothing(string lexical)
    {
        var result = XsdFloatingPoint.ParseSingle(lexical, XsdVersion.V11);

        AllocationAssert.NoAllocation(
            () => _ = XsdFloatingPoint.ParseSingle(lexical, XsdVersion.V11),
            warmups: Warmups
        );

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    [Arguments("1.5")]
    [Arguments("15")]
    [Arguments("1E3")]
    [Arguments("  2.5E-2 ")]
    public async Task Descriptor_Parse_ShortLexicalForm_AllocatesNothing(string lexical)
    {
        var d = (IDatatypeDescriptor<double>)BuiltInDatatypes.For(XsdVersion.V11).Get("double");
        var f = (IDatatypeDescriptor<float>)BuiltInDatatypes.For(XsdVersion.V11).Get("float");

        AllocationAssert.NoAllocation(() => _ = d.Parse(lexical), warmups: Warmups);
        AllocationAssert.NoAllocation(() => _ = f.Parse(lexical), warmups: Warmups);

        await Assert.That(d.Parse(lexical).Success).IsTrue();
    }

    [Test]
    public async Task ParseDouble_BeyondTheFastPath_StillRoundsCorrectly()
    {
        var wide = XsdFloatingPoint.ParseDouble(
            "1.00000000000000011102230246251565404236316680908203126",
            XsdVersion.V11
        );
        var far = XsdFloatingPoint.ParseDouble("1E-30", XsdVersion.V11);

        await Assert.That((ulong)BitConverter.DoubleToInt64Bits(wide.Value)).IsEqualTo(0x3FF0000000000001UL);
        await Assert.That(far.Value).IsEqualTo(1E-30);
    }
}
