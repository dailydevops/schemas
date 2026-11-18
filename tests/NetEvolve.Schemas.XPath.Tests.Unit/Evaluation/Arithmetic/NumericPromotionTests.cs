namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;

using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The numeric promotion of XPath 2.0, B.1, in one place for the arithmetic and the function conversion: float to double, decimal
/// and integer (inline or beyond 64 bits) to float or double, each rounded once.
/// </summary>
public class NumericPromotionTests
{
    private static readonly BigInteger Beyond = BigInteger.Parse("100000000000000000000", CultureInfo.InvariantCulture);

    [Test]
    public async Task TryToDouble_PromotesEveryNumericType()
    {
        await Assert.That(NumericPromotion.TryToDouble(XdmItem.FromDouble(1.5), out var fromDouble)).IsTrue();
        await Assert.That(fromDouble).IsEqualTo(1.5);
        await Assert.That(NumericPromotion.TryToDouble(XdmItem.FromFloat(2.5f), out var fromFloat)).IsTrue();
        await Assert.That(fromFloat).IsEqualTo(2.5);
        await Assert.That(NumericPromotion.TryToDouble(XdmItem.FromInteger(-7), out var fromInteger)).IsTrue();
        await Assert.That(fromInteger).IsEqualTo(-7d);
        await Assert
            .That(NumericPromotion.TryToDouble(XdmItem.FromDecimal(new XsdDecimal(25, 1)), out var fromDecimal))
            .IsTrue();
        await Assert.That(fromDecimal).IsEqualTo(2.5);
    }

    [Test]
    public async Task TryToDouble_PromotesAnIntegerBeyondSixtyFourBits()
    {
        await Assert.That(NumericPromotion.TryToDouble(XdmItem.FromBigInteger(Beyond), out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(1e20);
        await Assert.That(NumericPromotion.TryToDouble(XdmItem.FromBigInteger(-Beyond), out var negative)).IsTrue();
        await Assert.That(negative).IsEqualTo(-1e20);
    }

    [Test]
    public async Task TryToSingle_PromotesEveryNumericTypeButDouble()
    {
        await Assert.That(NumericPromotion.TryToSingle(XdmItem.FromFloat(2.5f), out var fromFloat)).IsTrue();
        await Assert.That(fromFloat).IsEqualTo(2.5f);
        await Assert.That(NumericPromotion.TryToSingle(XdmItem.FromInteger(3), out var fromInteger)).IsTrue();
        await Assert.That(fromInteger).IsEqualTo(3f);
        await Assert
            .That(NumericPromotion.TryToSingle(XdmItem.FromDecimal(new XsdDecimal(25, 1)), out var fromDecimal))
            .IsTrue();
        await Assert.That(fromDecimal).IsEqualTo(2.5f);
        await Assert.That(NumericPromotion.TryToSingle(XdmItem.FromDouble(1.5), out _)).IsFalse();
    }

    [Test]
    public async Task TryToSingle_PromotesAnIntegerBeyondSixtyFourBits()
    {
        await Assert.That(NumericPromotion.TryToSingle(XdmItem.FromBigInteger(Beyond), out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(1e20f);
    }

    [Test]
    public async Task TryTo_ForANonNumericItem_IsFalse()
    {
        await Assert.That(NumericPromotion.TryToDouble(XdmItem.FromString("1"), out _)).IsFalse();
        await Assert.That(NumericPromotion.TryToSingle(XdmItem.FromBoolean(true), out _)).IsFalse();
    }
}
