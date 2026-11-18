namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Stream;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

/// <summary>Async stream reading (XML-17): a buffering adapter over the resumable parser, equal to the span path.</summary>
public class XmlStreamReaderTests
{
    // A leading byte order mark, a declaration, CR and CRLF, surrogate pairs and two-byte characters in text, references,
    // CDATA, a comment, and a processing instruction: every hazard of a read edge.
    private const string Hazards =
        "﻿<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<!-- cé --><?pi däta?>\n"
        + "<r xmlns=\"urn:d\" xmlns:p=\"urn:p\" a=\"x&amp;y\t\r\nz&#10;\" p:b='\U0001F600'>\r\n  café \U0001D11E &lt; &#x1F600;\r"
        + "<p:e/><x\U0001F600y/><![CDATA[a]b]]c]]]]><![CDATA[\r\nq]]>t<!-- - --><?pi2 ? ??>?>d?>  </r>\r\n<!--tail-->\n";

    private static readonly int[] AllBufferSizes = [1, 3, 4096];

    private static byte[] Utf8(string xml) => Encoding.UTF8.GetBytes(xml);

    private static string Big()
    {
        var builder = new StringBuilder("<?xml version=\"1.0\"?><root xmlns:p=\"urn:p\">");
        for (var i = 0; i < 400; i++)
        {
            builder
                .Append("<p:item id=\"")
                .Append(i)
                .Append("\" t='é&lt;'>text ")
                .Append(i)
                .Append(" €\U0001D11E</p:item>\n");
        }

        return builder.Append("</root>").ToString();
    }

    public static IEnumerable<Func<(string Name, byte[] Bytes)>> Samples()
    {
        yield return () => ("hazards utf-8 with bom", Utf8(Hazards));
        yield return () => ("big utf-8", Utf8(Big()));
        yield return () => ("no declaration", Utf8("<a x=\"1\"><b/>café</a>"));
        yield return () =>
            (
                "utf-16le with bom",
                [0xFF, 0xFE, .. new UnicodeEncoding(false, false).GetBytes("<a b=\"é\">é\U0001D11E</a>")]
            );
        yield return () => ("utf-16be with bom", [0xFE, 0xFF, .. new UnicodeEncoding(true, false).GetBytes(Big())]);
        yield return () =>
            (
                "declared iso-8859-1",
                Encoding
                    .GetEncoding("ISO-8859-1")
                    .GetBytes("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><a b=\"é\">café</a>")
            );
        yield return () => ("declared us-ascii", Utf8("<?xml version='1.0' encoding='us-ascii'?><a>plain</a>"));
    }

    public static IEnumerable<Func<(string Name, byte[] Bytes)>> Failures()
    {
        yield return () => ("conflicting declared encoding", Utf8("<?xml version=\"1.0\" encoding=\"UTF-16\"?><a/>"));
        yield return () =>
            ("unsupported declared encoding", Utf8("<?xml version=\"1.0\" encoding=\"windows-1252\"?><a/>"));
        yield return () => ("invalid utf-8 after valid prefix", [.. Utf8("<a>ok"), 0xFF, .. Utf8("</a>")]);
        yield return () => ("truncated utf-8 at the end", [.. Utf8("<a>ok</a>"), 0xC3]);
        yield return () => ("not well-formed before invalid bytes", [.. Utf8("<a></b>"), 0xFF]);
        yield return () => ("unclosed element", Utf8("<a><b>"));
        yield return () => ("empty stream", []);
        yield return () => ("byte order mark only", [0xEF, 0xBB, 0xBF]);
        yield return () => ("three bytes", Utf8("<a/"));
        yield return () => ("ebcdic detection", [0x4C, 0x6F, 0xA7, 0x94, 0x40]);
        yield return () => ("declaration with a bad version", Utf8("<?xml version=\"x\"?><a/>"));
        yield return () => ("doctype without opt-in", Utf8("<!DOCTYPE a><a/>"));

        // The first pump must see a buffer of characters (AP2), not just the head bytes; the core tokenizer gives the same
        // events, code, and position at every buffer size (#598).
        yield return () => ("whitespace before the declaration", Utf8(" <?xml version=\"1.0\"?><a/>"));
        yield return () =>
            ("invalid byte after the declaration start", [.. Utf8(" <?xml"), 0xFF, .. Utf8(" version=\"1.0\"?><a/>")]);
    }

    [Test]
    [MethodDataSource(nameof(Samples))]
    public async Task ReadAsync_OneByteStreams_GiveTheEventsOfTheSpanPath(
        string _,
        byte[] bytes,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expected = StreamRuns.Span(bytes);

        // A buffer size of one makes every read, decode, and pump a single byte; the default size coalesces them.
        var tiny = await StreamRuns.StreamAsync(bytes, _ => 1, 1, null, cancellationToken);
        var coalesced = await StreamRuns.StreamAsync(bytes, _ => 1, 4096, null, cancellationToken);

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(tiny).IsEqualTo(expected);
        await Assert.That(coalesced).IsEqualTo(expected);
    }

    [Test]
    [MethodDataSource(nameof(Samples))]
    public async Task ReadAsync_SeededRandomChunkSizes_GiveTheEventsOfTheSpanPath(
        string _,
        byte[] bytes,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expected = StreamRuns.Span(bytes);

        foreach (var seed in new[] { 17, 2026, 31337 })
        {
            foreach (var bufferSize in new[] { 1, 5, 64, 4096 })
            {
                var actual = await StreamRuns.StreamAsync(
                    bytes,
                    StreamRuns.Seeded(seed, 11),
                    bufferSize,
                    null,
                    cancellationToken
                );

                await Assert.That(actual).IsEqualTo(expected);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Failures))]
    public async Task ReadAsync_Failures_GiveTheCodePositionAndEventsOfTheSpanPath(
        string _,
        byte[] bytes,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = XmlReaderOptions.Default;
        var expected = StreamRuns.Span(bytes, options);

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Error);
        foreach (var bufferSize in AllBufferSizes)
        {
            var oneByte = await StreamRuns.StreamAsync(bytes, _ => 1, bufferSize, options, cancellationToken);
            var whole = await StreamRuns.StreamAsync(bytes, _ => int.MaxValue, bufferSize, options, cancellationToken);

            await Assert.That(oneByte.Status).IsEqualTo(XmlReadStatus.Error);
            await Assert.That(oneByte.Code).IsSameReferenceAs(expected.Code);
            await Assert.That(oneByte.ErrorPosition).IsEqualTo(expected.ErrorPosition);
            await Assert.That(oneByte.Events).IsEqualTo(expected.Events);
            await Assert.That(whole).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task ReadAsync_DeclarationLongerThanOneRead_SwitchesToTheDeclaredEncoding(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var xml = "<?xml version=\"1.0\"" + new string(' ', 300) + "encoding=\"ISO-8859-1\"?><a>café ü</a>";
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(xml);

        var run = await StreamRuns.StreamAsync(bytes, _ => 7, 16, null, cancellationToken);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run).IsEqualTo(StreamRuns.Span(bytes));
        await Assert.That(run.Events).Contains("café ü");
    }

    [Test]
    public async Task ReadAsync_TokenLargerThanTheBuffer_GrowsAndEqualsTheSpanPath(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = Utf8("<a><!--" + new string('x', 300_000) + "--><b>" + new string('y', 300_000) + "</b></a>");

        var run = await StreamRuns.StreamAsync(bytes, _ => 1, 16, null, cancellationToken);

        await Assert.That(run).IsEqualTo(StreamRuns.Span(bytes));
        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
    }

    [Test]
    public async Task ReadAsync_OneByteReads_AreCoalescedIntoChunksOfTheBufferSize(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The feeder rule of #108 AP2: the parser is pumped once per bufferSize characters, not once per byte.
        var bytes = Utf8("<a>" + new string('x', 640) + "</a>");
        using var stream = new ChunkedReadStream(bytes, _ => 1);
        using var reader = new XmlStreamReader<RecordingXmlSink>(stream, StreamRuns.NewSink(), null, 64);

        var status = await reader.ReadAsync(cancellationToken);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(stream.Reads).IsEqualTo(bytes.Length + 1);
        await Assert.That(reader.Sink.Events.Count(e => e.Kind == XmlEventKind.Text)).IsBetween(9, 12);
    }

    [Test]
    public async Task ReadAsync_TokenLimitInOptions_IsEnforcedThroughTheStream(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxTokenLength(1_000));
        var bytes = Utf8("<a><!--" + new string('x', 5_000) + "--></a>");

        var run = await StreamRuns.StreamAsync(bytes, _ => 3, 16, options, cancellationToken);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsSameReferenceAs(StreamRuns.Span(bytes, options).Code);
    }

    [Test]
    public async Task ReadAsync_DocumentCharacterLimit_StopsReading(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxDocumentCharacters(8));
        var bytes = Utf8("<a>" + new string('x', 100_000) + "</a>");
        using var stream = new ChunkedReadStream(bytes, _ => 100);
        using var reader = new XmlStreamReader<RecordingXmlSink>(stream, StreamRuns.NewSink(options), options, 64);

        var status = await reader.ReadAsync(cancellationToken);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(reader.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML1101);
        await Assert.That(stream.Reads).IsLessThan(10);
    }

    [Test]
    public async Task ReadAsync_DtdProcessingInOptions_ReachesTheParser(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = XmlReaderOptions.Default.WithDtdProcessing(true);
        var bytes = Utf8("<!DOCTYPE a [<!ELEMENT a (#PCDATA)>]><a>x</a>");

        var run = await StreamRuns.StreamAsync(bytes, _ => 2, 8, options, cancellationToken);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run).IsEqualTo(StreamRuns.Span(bytes, options));
    }

    [Test]
    public async Task ReadAsync_StructSink_KeepsItsStateAcrossAwaits(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = Utf8(Big());
        using var stream = new ChunkedReadStream(bytes, _ => 9);
        var names = StreamRuns.NewSink().Names;
        using var reader = new XmlStreamReader<CountingStructSink>(stream, new CountingStructSink(names), null, 32);

        var status = await reader.ReadAsync(cancellationToken);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(stream.Reads).IsGreaterThan(100);
        await Assert.That(reader.Sink.Count).IsGreaterThan(1_000);
    }

    [Test]
    public async Task ReadAsync_AfterTheEnd_ReturnsTheSameStatusWithoutReading(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ChunkedReadStream(Utf8("<a/>"), _ => 2);
        using var reader = new XmlStreamReader<RecordingXmlSink>(stream, StreamRuns.NewSink());
        var first = await reader.ReadAsync(cancellationToken);
        var reads = stream.Reads;

        var second = await reader.ReadAsync(cancellationToken);

        await Assert.That(first).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(second).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(stream.Reads).IsEqualTo(reads);
    }

    [Test]
    public async Task ReadAsync_CancelDuringARead_ThrowsOperationCanceledAndReturnsEveryBuffer(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytePool = new TrackingPool<byte>();
        var charPool = new TrackingPool<char>();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var stream = new ChunkedReadStream(Utf8(Big()), _ => 50) { CancelOnRead = 5, Source = source };
        using var reader = new XmlStreamReader<RecordingXmlSink>(
            stream,
            StreamRuns.NewSink(),
            null,
            64,
            true,
            bytePool,
            charPool
        );

        await Assert.That(async () => await reader.ReadAsync(source.Token)).Throws<OperationCanceledException>();

        await Assert.That(bytePool.AllReturned).IsTrue();
        await Assert.That(charPool.AllReturned).IsTrue();
    }

    [Test]
    public async Task ReadAsync_StreamThatIgnoresTheToken_IsStoppedByTheReader(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytePool = new TrackingPool<byte>();
        var charPool = new TrackingPool<char>();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var stream = new ChunkedReadStream(Utf8(Big()), _ => 50)
        {
            CancelOnRead = 5,
            Source = source,
            ObserveToken = false,
        };
        using var reader = new XmlStreamReader<RecordingXmlSink>(
            stream,
            StreamRuns.NewSink(),
            null,
            64,
            true,
            bytePool,
            charPool
        );

        await Assert.That(async () => await reader.ReadAsync(source.Token)).Throws<OperationCanceledException>();

        await Assert.That(stream.Reads).IsLessThan(10);
        await Assert.That(bytePool.AllReturned).IsTrue();
        await Assert.That(charPool.AllReturned).IsTrue();
    }

    [Test]
    public async Task ReadAsync_AlreadyCanceledToken_ThrowsBeforeRentingAndLeavesTheReaderUsable(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytePool = new TrackingPool<byte>();
        var charPool = new TrackingPool<char>();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await source.CancelAsync();
        using var stream = new ChunkedReadStream(Utf8("<a/>"), _ => 2);
        using var reader = new XmlStreamReader<RecordingXmlSink>(
            stream,
            StreamRuns.NewSink(),
            null,
            64,
            true,
            bytePool,
            charPool
        );

        await Assert.That(async () => await reader.ReadAsync(source.Token)).Throws<OperationCanceledException>();
        await Assert.That(bytePool.Rented.Count).IsEqualTo(0);
        await Assert.That(charPool.Rented.Count).IsEqualTo(0);
        var status = await reader.ReadAsync(cancellationToken);

        await Assert.That(stream.Reads).IsGreaterThan(0);
        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(bytePool.AllReturned).IsTrue();
        await Assert.That(charPool.AllReturned).IsTrue();
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(7)]
    public async Task ReadAsync_StreamFault_PropagatesAndReturnsEveryRentedBuffer(
        int faultOnRead,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytePool = new TrackingPool<byte>();
        var charPool = new TrackingPool<char>();
        using var stream = new ChunkedReadStream(Utf8(Big()), _ => 30) { FaultOnRead = faultOnRead };
        using var reader = new XmlStreamReader<RecordingXmlSink>(
            stream,
            StreamRuns.NewSink(),
            null,
            32,
            true,
            bytePool,
            charPool
        );

        await Assert.That(async () => await reader.ReadAsync(cancellationToken)).Throws<IOException>();

        await Assert.That(bytePool.AllReturned).IsTrue();
        await Assert.That(charPool.AllReturned).IsTrue();
    }

    [Test]
    public async Task ReadAsync_AfterACancelOrFault_ThrowsInvalidOperation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ChunkedReadStream(Utf8("<a/>"), _ => 1) { FaultOnRead = 1 };
        using var reader = new XmlStreamReader<RecordingXmlSink>(stream, StreamRuns.NewSink());
        await Assert.That(async () => await reader.ReadAsync(cancellationToken)).Throws<IOException>();

        await Assert.That(async () => await reader.ReadAsync(cancellationToken)).Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments("<a><b/></a>")]
    [Arguments("<a></b>")]
    public async Task ReadAsync_CompletionAndParseError_ReturnEveryBufferWithoutDispose(
        string xml,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytePool = new TrackingPool<byte>();
        var charPool = new TrackingPool<char>();
        using var stream = new ChunkedReadStream(Utf8(xml), _ => 2);
        var reader = new XmlStreamReader<RecordingXmlSink>(
            stream,
            StreamRuns.NewSink(),
            null,
            4,
            true,
            bytePool,
            charPool
        );

        _ = await reader.ReadAsync(cancellationToken);

        await Assert.That(bytePool.AllReturned).IsTrue();
        await Assert.That(charPool.AllReturned).IsTrue();
        reader.Dispose();
        await Assert.That(bytePool.AllReturned).IsTrue();
        await Assert.That(charPool.AllReturned).IsTrue();
    }

    [Test]
    public async Task Dispose_ByDefault_LeavesTheStreamOpen(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ChunkedReadStream(Utf8("<a/>"), _ => 4);
        var reader = new XmlStreamReader<RecordingXmlSink>(stream, StreamRuns.NewSink());
        _ = await reader.ReadAsync(cancellationToken);

        reader.Dispose();

        await Assert.That(stream.IsDisposed).IsFalse();
    }

    [Test]
    public async Task Dispose_WhenTheReaderOwnsTheStream_DisposesIt(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stream = new ChunkedReadStream(Utf8("<a/>"), _ => 4);
        var reader = new XmlStreamReader<RecordingXmlSink>(stream, StreamRuns.NewSink(), null, 4096, leaveOpen: false);
        _ = await reader.ReadAsync(cancellationToken);
        await Assert.That(stream.IsDisposed).IsFalse();

        reader.Dispose();
        reader.Dispose();

        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task ReadAsync_AfterDispose_ThrowsObjectDisposed(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ChunkedReadStream(Utf8("<a/>"), _ => 4);
        var reader = new XmlStreamReader<RecordingXmlSink>(stream, StreamRuns.NewSink());
        reader.Dispose();

        await Assert.That(async () => await reader.ReadAsync(cancellationToken)).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task Constructor_NullStream_Throws() =>
        await Assert
            .That(() => new XmlStreamReader<RecordingXmlSink>(null!, StreamRuns.NewSink()))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Constructor_NullSink_Throws()
    {
        using var stream = new MemoryStream();

        await Assert.That(() => new XmlStreamReader<IXmlSink>(stream, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task Constructor_NonPositiveBufferSize_Throws(int bufferSize)
    {
        using var stream = new MemoryStream();

        await Assert
            .That(() => new XmlStreamReader<RecordingXmlSink>(stream, StreamRuns.NewSink(), null, bufferSize))
            .Throws<ArgumentOutOfRangeException>();
    }
}
