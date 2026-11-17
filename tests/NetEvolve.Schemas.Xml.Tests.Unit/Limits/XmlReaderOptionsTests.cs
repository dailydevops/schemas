namespace NetEvolve.Schemas.Xml.Tests.Unit.Limits;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

public class XmlReaderOptionsTests
{
    [Test]
    public async Task Default_CarriesDefaultLimitsAndNoNameTable()
    {
        await Assert.That(XmlReaderOptions.Default.Limits).IsSameReferenceAs(XmlReaderLimits.Default);
        await Assert.That(XmlReaderOptions.Default.NameTable).IsNull();
    }

    [Test]
    public async Task WithLimits_ReturnsACopyCarryingTheLimits()
    {
        var limits = XmlReaderLimits.Default.WithMaxElementDepth(3);
        var options = XmlReaderOptions.Default.WithLimits(limits);

        await Assert.That(options.Limits).IsSameReferenceAs(limits);
        await Assert.That(XmlReaderOptions.Default.Limits.MaxElementDepth).IsEqualTo(256);
    }

    [Test]
    public async Task WithNameTable_ReturnsACopyAndAcceptsNull()
    {
        var table = new FakeNameTable();
        var options = XmlReaderOptions.Default.WithNameTable(table);

        await Assert.That(options.NameTable).IsSameReferenceAs(table);
        await Assert.That(options.WithNameTable(null).NameTable).IsNull();
        await Assert.That(options.Limits).IsSameReferenceAs(XmlReaderLimits.Default);
    }

    [Test]
    public async Task WithLimits_Null_IsRejected() =>
        await Assert.That(() => XmlReaderOptions.Default.WithLimits(null!)).Throws<ArgumentNullException>();
}
