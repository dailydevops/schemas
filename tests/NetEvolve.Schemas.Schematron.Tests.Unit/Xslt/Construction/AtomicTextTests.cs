namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;

using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class AtomicTextTests
{
    [Test]
    public async Task TryGet_ConvertsStringBooleanAndInteger()
    {
        var s = XdmItem.FromString("s");
        var f = XdmItem.FromBoolean(false);
        var i = XdmItem.FromInteger(-7);

        await Assert.That(AtomicText.TryGet(in s, out var a)).IsTrue();
        await Assert.That(AtomicText.TryGet(in f, out var b)).IsTrue();
        await Assert.That(AtomicText.TryGet(in i, out var c)).IsTrue();
        await Assert.That(a).IsEqualTo("s");
        await Assert.That(b).IsEqualTo("false");
        await Assert.That(c).IsEqualTo("-7");
    }

    [Test]
    public async Task TryGet_ConvertsDecimalDateTimeAndDuration()
    {
        var dec = XdmItem.FromDecimal(XsdDecimal.Parse("1.5").Value);
        var date = XdmItem.FromDateTime(XsdDateTime.Parse("2026-11-17", XsdDateTimeKind.Date, XsdVersion.V10).Value);
        var duration = XdmItem.FromDuration(XsdDuration.Parse("P1D").Value);
        var yearMonth = XdmItem.FromDuration(
            XsdDuration.Parse("P1Y", DurationKind.YearMonth).Value,
            DurationKind.YearMonth
        );
        var dayTime = XdmItem.FromDuration(XsdDuration.Parse("PT1H", DurationKind.DayTime).Value, DurationKind.DayTime);

        await Assert.That(AtomicText.TryGet(in dec, out var a)).IsTrue();
        await Assert.That(AtomicText.TryGet(in date, out var b)).IsTrue();
        await Assert.That(AtomicText.TryGet(in duration, out var c)).IsTrue();
        await Assert.That(AtomicText.TryGet(in yearMonth, out var d)).IsTrue();
        await Assert.That(AtomicText.TryGet(in dayTime, out var e)).IsTrue();
        await Assert.That(a).IsEqualTo("1.5");
        await Assert.That(b).IsEqualTo("2026-11-17");
        await Assert.That(c).IsEqualTo("P1D");
        await Assert.That(d).IsEqualTo("P1Y");
        await Assert.That(e).IsEqualTo("PT1H");
    }

    [Test]
    public async Task TryGet_FloatingPoint_IsNotSupported()
    {
        var d = XdmItem.FromDouble(1.5);

        await Assert.That(AtomicText.TryGet(in d, out var text)).IsFalse();
        await Assert.That(text).IsEmpty();
    }
}
