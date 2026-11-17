namespace NetEvolve.Schemas.Xml.Tests.Unit.Writer;

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Writer;
using static NetEvolve.Schemas.Xml.Tests.Unit.Writer.WriterHarness;

public class OutputTests
{
    private static void Script(XmlWriter w)
    {
        var t = w.Names;
        w.StartDocument(new XmlDeclaration("1.0", "", null));
        w.Start("a", "p", "urn:x", attrs: [new Att("b", "é&\U0001F600")]);
        w.Text("€<\U0001F600>", XmlTextKind.Text);
        w.Comment("c");
        w.ProcessingInstruction(t.Intern("pi"), "d");
        w.End("a", "p", "urn:x");
        w.EndDocument();
    }

    private const string Expected =
        "<?xml version=\"1.0\"?><p:a xmlns:p=\"urn:x\" b=\"é&amp;\U0001F600\">€&lt;\U0001F600&gt;<!--c--><?pi d?></p:a>";

    [Test]
    public async Task BufferWriterAndAsyncStream_ProduceIdenticalBytes_AndTheStreamIsNotWrittenSynchronously(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var table = NewTable();
        var buffer = new ArrayBufferWriter<byte>();
        using (var direct = new XmlWriter(buffer, table))
        {
            Script(direct);
        }

        var stream = new AsyncOnlyStream();
        using var viaStream = new XmlWriter(stream, NewTable());
        Script(viaStream);
        var writesBeforeFlush = stream.AsyncWrites;
        await viaStream.FlushAsync(cancellationToken);

        await Assert.That(stream.SyncWrites).IsEqualTo(0);
        await Assert.That(writesBeforeFlush).IsEqualTo(0);
        await Assert.That(stream.AsyncWrites).IsEqualTo(1);
        await Assert.That(stream.Written.ToArray()).IsEquivalentTo(buffer.WrittenSpan.ToArray());
        await Assert
            .That(Encoding.UTF8.GetString(buffer.WrittenSpan))
            .StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    }

    [Test]
    public async Task Stream_FlushAsyncTwice_WritesEachChunkOnce(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = new AsyncOnlyStream();
        using var writer = new XmlWriter(stream, NewTable());
        writer.Start("a");
        await writer.FlushAsync(cancellationToken);
        writer.Text("x", XmlTextKind.Text);
        writer.End("a");
        await writer.FlushAsync(cancellationToken);
        await writer.FlushAsync(cancellationToken);

        await Assert.That(Encoding.UTF8.GetString(stream.Written.ToArray())).IsEqualTo("<a>x</a>");
        await Assert.That(stream.SyncWrites).IsEqualTo(0);
    }

    [Test]
    public async Task CharacterOutput_AndUtf8Bytes_AgreeForTheSameEvents()
    {
        var chars = Chars((w, _) => Script(w));
        var bytes = Bytes((w, _) => Script(w));

        await Assert.That(chars).IsEqualTo(Expected);
        await Assert
            .That(Encoding.UTF8.GetString(bytes))
            .IsEqualTo(Expected.Replace("1.0\"?>", "1.0\" encoding=\"UTF-8\"?>", StringComparison.Ordinal));
    }

    [Test]
    public async Task Utf16Output_StartsWithByteOrderMark_AndDecodesToTheSameText()
    {
        var bytes = Bytes((w, _) => Script(w), encoding: XmlWriterEncoding.Utf16);

        await Assert.That(bytes[0]).IsEqualTo((byte)0xFF);
        await Assert.That(bytes[1]).IsEqualTo((byte)0xFE);
        await Assert
            .That(Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2))
            .IsEqualTo(Expected.Replace("1.0\"?>", "1.0\" encoding=\"UTF-16\"?>", StringComparison.Ordinal));
    }

    [Test]
    public async Task SurrogatePair_SplitAcrossWrites_IsEncodedAsOneCodePoint()
    {
        var bytes = Bytes(
            (w, _) =>
            {
                w.Start("a");
                w.Text("\U0001F600", XmlTextKind.Text);
                w.Text("\U0001F601", XmlTextKind.Text);
                w.End("a");
            }
        );

        await Assert.That(Encoding.UTF8.GetString(bytes)).IsEqualTo("<a>\U0001F600\U0001F601</a>");
    }

    [Test]
    public async Task BufferWriterOutput_AllocatesNothingPerEvent_AfterWarmUp()
    {
        var table = NewTable();
        var output = new RewindableCharWriter();
        using var writer = new XmlWriter(output, table);
        var root = N(table, "r");
        var item = N(table, "item");
        var ns = N(table, "n", "p", "urn:x");
        var attr = N(table, "id");
        var nsAttr = N(table, "k", "p", "urn:x");
        var pi = table.Intern("pi");
        writer.StartElement(root, default);
        writer.EndAttributes();
        var value = "v<1>";
        var text = "some text & more";

        AllocationAssert.NoAllocation(
            () =>
            {
                output.Rewind();
                writer.StartElement(item, default);
                writer.Attribute(attr, value);
                writer.Attribute(nsAttr, value);
                writer.EndAttributes();
                writer.Text(text, XmlTextKind.Text);
                writer.Comment(text);
                writer.ProcessingInstruction(pi, text);
                writer.StartElement(ns, default);
                writer.EndAttributes();
                writer.EndElement(ns);
                writer.EndElement(item);
            },
            warmups: 50
        );
    }

    private sealed class AsyncOnlyStream : Stream
    {
        private readonly MemoryStream _inner = new();

        public int SyncWrites { get; private set; }

        public int AsyncWrites { get; private set; }

        public ReadOnlySpan<byte> Written => _inner.ToArray();

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => SyncWrites++;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => SyncWrites++;

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncWrites++;
            _inner.Write(buffer, offset, count);

            return Task.CompletedTask;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncWrites++;
            _inner.Write(buffer.Span);

            return ValueTask.CompletedTask;
        }
    }
}
