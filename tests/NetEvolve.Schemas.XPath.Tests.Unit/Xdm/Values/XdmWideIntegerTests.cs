namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Values;

using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Tests.Unit.Types;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// An integer beyond 64 bits is an item of the decimal family that carries the <c>xs:integer</c> type (#123 AP2: the integers
/// fall back to the arbitrary-precision decimal above 64 bits), so every consumer that reads decimals keeps working.
/// </summary>
public class XdmWideIntegerTests
{
    private static readonly BigInteger Beyond = BigInteger.Parse("100000000000000000000", CultureInfo.InvariantCulture);

    [Test]
    public async Task FromBigInteger_BeyondSixtyFourBits_KeepsTheIntegerTypeAndTheExactValue()
    {
        var item = XdmItem.FromBigInteger(Beyond);

        await Assert.That(item.TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(item.TryGetBigInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(Beyond);
        await Assert.That(item.TryGetInteger(out _)).IsFalse();
    }

    [Test]
    public async Task FromBigInteger_BeyondSixtyFourBits_IsReadByTheDecimalAccessor()
    {
        var item = XdmItem.FromBigInteger(-Beyond);

        await Assert.That(item.TryGetDecimal(out var value)).IsTrue();
        await Assert.That(value.Mantissa * BigInteger.Pow(10, -value.Scale)).IsEqualTo(-Beyond);
    }

    [Test]
    [Arguments(0L)]
    [Arguments(5L)]
    [Arguments(-5L)]
    [Arguments(long.MaxValue)]
    [Arguments(long.MinValue)]
    public async Task FromBigInteger_WithinSixtyFourBits_IsTheInlineInteger(long value)
    {
        var item = XdmItem.FromBigInteger(new BigInteger(value));

        await Assert.That(item).IsEqualTo(XdmItem.FromInteger(value));
        await Assert.That(item.TryGetInteger(out var read) && read == value).IsTrue();
    }

    [Test]
    [Arguments(5L)]
    [Arguments(long.MinValue)]
    public async Task TryGetBigInteger_OnAnInlineInteger_ReturnsTheValue(long value)
    {
        await Assert.That(XdmItem.FromInteger(value).TryGetBigInteger(out var read)).IsTrue();
        await Assert.That(read).IsEqualTo(new BigInteger(value));
    }

    [Test]
    public async Task TryGetBigInteger_OnOtherItems_ReturnsFalse()
    {
        await Assert.That(XdmItem.FromDouble(5).TryGetBigInteger(out var none)).IsFalse();
        await Assert.That(none).IsEqualTo(BigInteger.Zero);
        await Assert.That(XdmItem.FromString("5").TryGetBigInteger(out _)).IsFalse();
        await Assert.That(XdmItem.FromBoolean(true).TryGetBigInteger(out _)).IsFalse();
        await Assert.That(XdmItem.FromDecimal(new XsdDecimal(5, 0)).TryGetBigInteger(out _)).IsFalse();
        await Assert.That(default(XdmItem).TryGetBigInteger(out _)).IsFalse();
    }

    [Test]
    public async Task WideIntegersOfTheSameValue_AreEqualAndHashEqual()
    {
        var first = XdmItem.FromBigInteger(Beyond);
        var second = XdmItem.FromBigInteger(Beyond);
        var other = XdmItem.FromBigInteger(Beyond + 1);

        await Assert.That(first).IsEqualTo(second);
        await Assert.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
        await Assert.That(first).IsNotEqualTo(other);
    }

    [Test]
    public async Task WideInteger_IsNotEqualToTheDecimalOfTheSameValue()
    {
        var integer = XdmItem.FromBigInteger(Beyond);
        var number = XdmItem.FromDecimal(new XsdDecimal(Beyond, 0));

        await Assert.That(integer).IsNotEqualTo(number);
    }

    [Test]
    public async Task WideInteger_IsAnIntegerAndADecimalByDerivation()
    {
        var item = XdmItem.FromBigInteger(Beyond);

        await Assert.That(XdmTypes.IsDerivedFrom(item.TypeId, XdmTypeId.XsInteger)).IsTrue();
        await Assert.That(XdmTypes.IsDerivedFrom(item.TypeId, XdmTypeId.XsDecimal)).IsTrue();
        await Assert.That(XdmTypes.IsDerivedFrom(item.TypeId, XdmTypeId.XsDouble)).IsFalse();
    }

    [Test]
    public async Task WideInteger_MatchesTheSequenceTypeXsInteger_AndNotXsDouble()
    {
        var sequence = XdmSequence.Create(XdmItem.FromBigInteger(Beyond));

        await Assert.That(TypeFixture.Matcher("xs:integer").Matches(sequence)).IsTrue();
        await Assert.That(TypeFixture.Matcher("xs:decimal").Matches(sequence)).IsTrue();
        await Assert.That(TypeFixture.Matcher("xs:double").Matches(sequence)).IsFalse();
    }

    [Test]
    public async Task WideInteger_HasTheEffectiveBooleanValueTrue()
    {
        var sequence = XdmSequence.Create(XdmItem.FromBigInteger(-Beyond));

        await Assert.That(XPathAtomization.EffectiveBooleanValue(sequence)).IsTrue();
    }

    [Test]
    public async Task FromBigInteger_PowerOfTen_RoundTripsThroughTheNormalizedDecimal()
    {
        var value = BigInteger.Pow(10, 40);

        await Assert.That(XdmItem.FromBigInteger(value).TryGetBigInteger(out var read)).IsTrue();
        await Assert.That(read).IsEqualTo(value);
    }
}
