namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Stream;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General.GeneralHarness;

/// <summary>An external entity is read in chunks and its stream is disposed on success, on every error, and with the reader.</summary>
public class ExternalDisposalTests
{
    [Test]
    public async Task Stream_IsDisposed_WhenTheEntityWasReadToItsEnd(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("<b/>");
        using var reader = XmlPipelineReader.Create(
            Doc(ExternalE, "&e;").AsSpan(),
            Options(LoaderHarness.Open(resolver))
        );

        var run = Pump(reader);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(resolver.Streams.Count).IsEqualTo(1);
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    [Arguments("<b>unclosed")]
    [Arguments("</doc>")]
    [Arguments("ab\u0001c")]
    [Arguments("<?xml version=\"1.0\"?>x")]
    public async Task Stream_IsDisposed_WhenTheEntityIsRejected_BeforeTheReaderIsDisposed(
        string entity,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving(entity);
        using var reader = XmlPipelineReader.Create(
            Doc(ExternalE, "&e;").AsSpan(),
            Options(LoaderHarness.Open(resolver))
        );

        var run = Pump(reader);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Stream_IsDisposed_WhenTheDocumentFailsInsideTheEntityFrame(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The element limit is crossed by the entity text; the frame is still open when the parser fails.
        var resolver = Serving("<a><b><c/></b></a>");
        var limits = XmlReaderLimits.Default.WithMaxElementDepth(2);
        using var reader = XmlPipelineReader.Create(
            Doc(ExternalE, "&e;").AsSpan(),
            Options(LoaderHarness.Open(resolver), limits)
        );

        var run = Pump(reader);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Stream_IsDisposed_WithTheReader_WhenTheSinkFailsInsideTheEntity(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("<b/>");
        var reader = XmlPipelineReader.Create(Doc(ExternalE, "&e;").AsSpan(), Options(LoaderHarness.Open(resolver)));
        var sink = new GeneralSink(throwAtElement: "b");

        var threw = false;
        try
        {
            _ = reader.Pump(ref sink);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        var openBefore = !resolver.Streams[0].IsDisposed;
        reader.Dispose();

        await Assert.That(threw).IsTrue();
        await Assert.That(openBefore).IsTrue();
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }

    [Test]
    public async Task Entity_ThatFailsEarly_IsNotReadToTheEnd(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new ReadCountingResolver(maxRead: 64).Add(
            EntityUri,
            LoaderHarness.Utf8("<</e>" + new string('x', 200_000))
        );

        var run = Read(Doc(ExternalE, "&e;"), resolver);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(resolver.Streams[0].BytesRead).IsLessThan(10_000);
    }

    [Test]
    public async Task Entity_IsReadInChunks_NotCompletelyBeforeItsFirstEventIsDelivered(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = new string('x', 500_000);
        var resolver = new ReadCountingResolver().Add(EntityUri, LoaderHarness.Utf8(text));
        using var reader = XmlPipelineReader.Create(
            Doc(ExternalE, "&e;").AsSpan(),
            Options(LoaderHarness.Open(resolver), XmlReaderLimits.Default.WithMaxTextNodeLength(1_000))
        );

        var run = Pump(reader);

        // The text node limit is crossed after the first thousand characters: the rest of the entity is never read.
        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML1107");
        await Assert.That(resolver.Streams[0].BytesRead).IsLessThan(20_000);
    }

    [Test]
    public async Task StreamReader_ReadsAnExternalEntity_AndGivesTheEventsOfTheInlineText(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string content = "a<b k='v'>t\r\n</b>\U0001D11E<!--c-->z";
        var resolver = Serving(content);
        var inline = Read(Doc(string.Empty, content), Serving(string.Empty));
        using var input = new MemoryStream(LoaderHarness.Utf8(Doc(ExternalE, "&e;")));
        using var reader = new XmlStreamReader<GeneralSink>(
            input,
            new GeneralSink(),
            Options(LoaderHarness.Open(resolver)),
            bufferSize: 5
        );

        var status = await reader.ReadAsync(cancellationToken);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(reader.Sink.Events).IsEqualTo(inline.Events);
        await Assert.That(resolver.Streams[0].IsDisposed).IsTrue();
    }
}
