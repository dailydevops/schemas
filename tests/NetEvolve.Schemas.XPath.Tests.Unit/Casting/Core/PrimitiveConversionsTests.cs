namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The conversions are told the source type by the item alone, so a conversion that a custom row hands an item of another type
/// reports <see cref="CastStatus.NotAllowed"/>, and the limits of an allowed cell are statuses, never exceptions.
/// </summary>
public class PrimitiveConversionsTests
{
    private static readonly CastConversion[] Conversions =
    [
        PrimitiveConversions.ToStringType,
        PrimitiveConversions.ToUntypedAtomic,
        PrimitiveConversions.ToFloat,
        PrimitiveConversions.ToDouble,
        PrimitiveConversions.ToDecimal,
        PrimitiveConversions.ToInteger,
        PrimitiveConversions.ToBoolean,
    ];

    [Test]
    [MethodDataSource(typeof(CastErrorTests), nameof(CastErrorTests.UncastableSources))]
    public async Task Conversion_OfASourceOutsideTheSevenTypes_IsNotAllowed(XdmItem source)
    {
        foreach (var conversion in Conversions)
        {
            var status = conversion(in source, out var result);

            await Assert.That(status).IsEqualTo(CastStatus.NotAllowed);
            await Assert.That(result).IsEqualTo(default(XdmItem));
        }
    }

    [Test]
    [MethodDataSource(typeof(CastErrorTests), nameof(CastErrorTests.UncastableSources))]
    public async Task TryFormat_OfASourceOutsideTheSevenTypes_ReturnsFalse(XdmItem source)
    {
        await Assert.That(NumberText.TryFormat(in source, out var text)).IsFalse();
        await Assert.That(text).IsNull();
    }

    [Test]
    public async Task DecimalToFloat_BeyondTheDigitCap_IsADecimalOverflow()
    {
        // 5,001 digits: the exact value is parsed as the form of a float, whose digit cap is 4,096.
        var source = XdmItem.FromDecimal(new XsdDecimal(BigInteger.Pow(10, 5000) + 1, 0));

        await Assert.That(PrimitiveConversions.ToFloat(in source, out _)).IsEqualTo(CastStatus.DecimalOverflow);
    }

    [Test]
    public async Task DecimalToInteger_WithAScaleThatCannotBeExpanded_IsAnIntegerOverflow()
    {
        var source = XdmItem.FromDecimal(new XsdDecimal(BigInteger.One, int.MinValue));

        await Assert.That(PrimitiveConversions.ToInteger(in source, out _)).IsEqualTo(CastStatus.IntegerOverflow);
    }
}
