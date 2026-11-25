namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Declaration;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;

public class Xml11DeclarationOptionTests
{
    private const string Document = "<?xml version=\"1.1\"?><a/>";

    [Test]
    public async Task Pipeline_ByDefault_RejectsVersion11(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(XmlReaderOptions.Default)).IsEqualTo("XML0010");
    }

    [Test]
    public async Task Pipeline_WithTheOption_ReadsVersion11Characters(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(XmlReaderOptions.Default.WithAllowXml11Declaration(true))).IsNull();
    }

    [Test]
    public async Task Pipeline_WithTheOption_ReadsVersion11Bytes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(Code(XmlReaderOptions.Default.WithAllowXml11Declaration(true), Encoding.UTF8.GetBytes(Document)))
            .IsNull();
    }

    [Test]
    public async Task Pipeline_WithTheOption_StillRejectsOtherVersions(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(Code(XmlReaderOptions.Default.WithAllowXml11Declaration(true), "<?xml version=\"1.7\"?><a/>"))
            .IsEqualTo("XML0010");
    }

    private static string? Code(XmlReaderOptions options, ReadOnlySpan<char> text = default)
    {
        try
        {
            _ = XmlDocumentLoader.Load(text.IsEmpty ? Document.AsSpan() : text, options);
            return null;
        }
        catch (XmlParseException exception)
        {
            return exception.Diagnostic?.Code.Alias;
        }
    }

    private static string? Code(XmlReaderOptions options, byte[] bytes)
    {
        try
        {
            _ = XmlDocumentLoader.Load(bytes.AsSpan(), options);
            return null;
        }
        catch (XmlParseException exception)
        {
            return exception.Diagnostic?.Code.Alias;
        }
    }
}
