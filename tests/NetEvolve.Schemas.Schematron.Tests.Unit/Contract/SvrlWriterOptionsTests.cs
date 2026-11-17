namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Xml.Writer;

public class SvrlWriterOptionsTests
{
    [Test]
    public async Task Default_IsUtf8_NotIndented_WithNamespaceDeclarations_And16MiBBuffer()
    {
        var options = SvrlWriterOptions.Default;

        await Assert.That(options.Encoding).IsEqualTo(XmlWriterEncoding.Utf8);
        await Assert.That(options.Indent).IsFalse();
        await Assert.That(options.EmitNamespaceDeclarations).IsTrue();
        await Assert.That(options.MaxBufferedBytes).IsEqualTo(16 * 1024 * 1024);
        var declaredDefault = SvrlWriterOptions.DefaultMaxBufferedBytes;
        await Assert.That(declaredDefault).IsEqualTo(16 * 1024 * 1024);
    }

    [Test]
    public async Task Constructor_RoundTripsEveryOption()
    {
        var options = new SvrlWriterOptions(XmlWriterEncoding.Utf16, true, false, 4096);

        await Assert.That(options.Encoding).IsEqualTo(XmlWriterEncoding.Utf16);
        await Assert.That(options.Indent).IsTrue();
        await Assert.That(options.EmitNamespaceDeclarations).IsFalse();
        await Assert.That(options.MaxBufferedBytes).IsEqualTo(4096);
    }

    [Test]
    public async Task MaxBufferedBytes_Ceiling_IsTwoBillionAndBelowArrayMaxLength()
    {
        var declaredCeiling = SvrlWriterOptions.MaxBufferedBytesCeiling;
        await Assert.That(declaredCeiling).IsEqualTo(2_000_000_000);
        await Assert.That(new SvrlWriterOptions(maxBufferedBytes: 1).MaxBufferedBytes).IsEqualTo(1);
        await Assert
            .That(new SvrlWriterOptions(maxBufferedBytes: SvrlWriterOptions.MaxBufferedBytesCeiling).MaxBufferedBytes)
            .IsEqualTo(2_000_000_000);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(2_000_000_001)]
    public async Task MaxBufferedBytes_OutsideRange_Throws(int value) =>
        await Assert.That(() => new SvrlWriterOptions(maxBufferedBytes: value)).Throws<ArgumentOutOfRangeException>();
}
