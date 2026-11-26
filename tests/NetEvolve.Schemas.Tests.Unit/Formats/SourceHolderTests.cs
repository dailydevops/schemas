namespace NetEvolve.Schemas.Tests.Unit.Formats;

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;

/// <summary>
/// The shared schema-source holder (#529): spans and sequences are copied, a stream is read once under <c>MaxResourceSize</c> (NES0007)
/// and replayed, and the asynchronous path never reads synchronously.
/// </summary>
public class SourceHolderTests
{
    private const string SystemId = "file:///schema.xsd";
    private static readonly byte[] Content = Encoding.UTF8.GetBytes("<schema>0123456789012345678901234567890123456789</schema>");
    private static readonly ValidationLimits Small = ValidationLimits.Default.WithMaxResourceSize(16);

    [Test]
    public async Task Build_OversizeSchemaStream_ReportsNes0007()
    {
        using var stream = new ProbeStream(Content);
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);

        var failure = Capture(() => holder.GetBytes(Small));

        var issue = failure.Issues.Single();
        await Assert.That(issue.Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(issue.Location.Source).IsEqualTo(SystemId);
        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(stream.SyncReads).IsEqualTo(0);
    }

    [Test]
    public async Task Build_OversizeSchemaStreamThatCannotSeek_ReportsNes0007WhileReading()
    {
        using var stream = new ProbeStream(Content, canSeek: false);
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);

        var failure = Capture(() => holder.GetBytes(Small));

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(stream.SyncReads).IsGreaterThan(0);
    }

    [Test]
    public async Task BuildAsync_Stream_ReadsAsynchronously(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ProbeStream(Content, canSeek: false) { ThrowOnSyncRead = true };
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);

        await holder.PrefetchAsync(ValidationLimits.Default, cancellationToken);
        var bytes = holder.GetBytes(ValidationLimits.Default, cancellationToken);

        await Assert.That(stream.SyncReads).IsEqualTo(0);
        await Assert.That(stream.AsyncReads).IsGreaterThan(1);
        await Assert.That(bytes.ToArray().SequenceEqual(Content)).IsTrue();
    }

    [Test]
    public async Task Build_NonSeekableStream_SecondBuildDoesNotReread(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ProbeStream(Content, canSeek: false);
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);
        var first = holder.GetBytes(ValidationLimits.Default, cancellationToken);
        var reads = stream.SyncReads;

        var second = holder.GetBytes(ValidationLimits.Default, cancellationToken);

        await Assert.That(stream.SyncReads).IsEqualTo(reads);
        await Assert.That(second.ToArray().SequenceEqual(first.ToArray())).IsTrue();
        await Assert.That(first.Length).IsEqualTo(Content.Length);
    }

    [Test]
    public async Task BuildAsync_Cancelled_Cancels()
    {
        using var stream = new ProbeStream(Content);
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert
            .That(async () => await holder.PrefetchAsync(ValidationLimits.Default, cts.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(() => holder.GetBytes(ValidationLimits.Default, cts.Token)).Throws<OperationCanceledException>();
        await Assert.That(stream.AsyncReads + stream.SyncReads).IsEqualTo(0);
    }

    [Test]
    public async Task BuildAsync_CancelledWhileReading_Cancels()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new ProbeStream(Content, canSeek: false) { OnRead = cts.Cancel };
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);

        await Assert
            .That(async () => await holder.PrefetchAsync(ValidationLimits.Default, cts.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task BuildAsync_CancelledByTheLastRead_Cancels()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new ProbeStream(Content, canSeek: false, chunk: 1_000);
        stream.OnRead = () =>
        {
            if (stream.Position == stream.Length)
            {
                cts.Cancel();
            }
        };
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);

        await Assert
            .That(async () => await holder.PrefetchAsync(ValidationLimits.Default, cts.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Build_CancelledWhileReading_Cancels()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new ProbeStream(Content, canSeek: false) { OnRead = cts.Cancel };
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);

        await Assert.That(() => holder.GetBytes(ValidationLimits.Default, cts.Token)).Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Prefetch_OversizeStream_ReportsNes0007(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var seekable = new ProbeStream(Content);
        using var forward = new ProbeStream(Content, canSeek: false);
        using var lying = new ProbeStream(Content) { ReportedLength = 1 };

        foreach (var stream in new[] { seekable, forward, lying })
        {
            var holder = SchemaSourceHolder.FromStream(stream, SystemId);
            var failure = await Assert
                .That(async () => await holder.PrefetchAsync(Small, cancellationToken))
                .Throws<SchemaCompilationException>();
            await Assert.That(failure!.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        }

        await Assert.That(seekable.AsyncReads).IsEqualTo(0);
        await Assert.That(forward.AsyncReads).IsGreaterThan(0);
    }

    [Test]
    public async Task Prefetch_KeptStreamAndMemory_ReadNothing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ProbeStream(Content);
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);
        await holder.PrefetchAsync(ValidationLimits.Default, cancellationToken);
        var reads = stream.AsyncReads;

        await holder.PrefetchAsync(ValidationLimits.Default, cancellationToken);
        await SchemaSourceHolder.FromBytes(Content).PrefetchAsync(ValidationLimits.Default, cancellationToken);
        await SchemaSourceHolder.FromChars("<a/>").PrefetchAsync(ValidationLimits.Default, cancellationToken);

        await Assert.That(stream.AsyncReads).IsEqualTo(reads);
    }

    [Test]
    public async Task GetBytes_KeptStream_IsCheckedAgainstTheCurrentLimit(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ProbeStream(Content);
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);
        _ = holder.GetBytes(ValidationLimits.Default, cancellationToken);

        var failure = Capture(() => holder.GetBytes(Small, cancellationToken));

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task GetBytes_Stream_ReadsFromTheCurrentPositionAndKeepsTheStreamOpen()
    {
        using var stream = new ProbeStream(Content) { Position = 8 };
        var holder = SchemaSourceHolder.FromStream(stream, SystemId);

        var bytes = holder.GetBytes(ValidationLimits.Default);

        await Assert.That(bytes.ToArray().SequenceEqual(Content.Skip(8))).IsTrue();
        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(holder.Kind).IsEqualTo(DocumentSourceKind.Bytes);
    }

    [Test]
    public async Task FromBytes_Span_CopiesTheInput()
    {
        var buffer = (byte[])Content.Clone();
        var holder = SchemaSourceHolder.FromBytes(buffer, SystemId);
        Array.Clear(buffer);

        var bytes = holder.GetBytes(ValidationLimits.Default);

        await Assert.That(bytes.ToArray().SequenceEqual(Content)).IsTrue();
        await Assert.That(holder.Kind).IsEqualTo(DocumentSourceKind.Bytes);
        await Assert.That(holder.SystemId).IsEqualTo(SystemId);
    }

    [Test]
    public async Task FromBytes_MultiSegmentSequence_CopiesTheInput()
    {
        var sequence = Sequence(Content.AsMemory(0, 10), Content.AsMemory(10));

        var holder = SchemaSourceHolder.FromBytes(in sequence);

        await Assert.That(holder.GetBytes(ValidationLimits.Default).ToArray().SequenceEqual(Content)).IsTrue();
        await Assert.That(holder.SystemId).IsNull();
    }

    [Test]
    public async Task FromChars_Span_CopiesTheInput()
    {
        var buffer = "<schema/>".ToCharArray();
        var holder = SchemaSourceHolder.FromChars(buffer, SystemId);
        Array.Clear(buffer);

        var chars = holder.GetChars(ValidationLimits.Default);

        await Assert.That(chars.ToString()).IsEqualTo("<schema/>");
        await Assert.That(holder.Kind).IsEqualTo(DocumentSourceKind.Chars);
    }

    [Test]
    public async Task FromChars_MultiSegmentSequence_CopiesTheInput()
    {
        var text = "<schema/>".ToCharArray();
        var sequence = Sequence(text.AsMemory(0, 3), text.AsMemory(3));

        var holder = SchemaSourceHolder.FromChars(in sequence);

        await Assert.That(holder.GetChars(ValidationLimits.Default).ToString()).IsEqualTo("<schema/>");
    }

    [Test]
    public async Task Get_MemoryOverTheLimit_ReportsNes0007()
    {
        var bytes = SchemaSourceHolder.FromBytes(Content, SystemId);
        var chars = SchemaSourceHolder.FromChars(new string('x', 40), SystemId);

        var first = Capture(() => bytes.GetBytes(Small));
        var second = Capture(() => chars.GetChars(Small));

        await Assert.That(first.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(second.Issues.Single().Location.Source).IsEqualTo(SystemId);
    }

    [Test]
    public async Task Get_WrongKind_IsAProgrammerError()
    {
        var bytes = SchemaSourceHolder.FromBytes(Content);
        var chars = SchemaSourceHolder.FromChars("<a/>");

        await Assert.That(() => chars.GetBytes(ValidationLimits.Default)).Throws<InvalidOperationException>();
        await Assert.That(() => bytes.GetChars(ValidationLimits.Default)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Guards_NullArguments_Throw(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var holder = SchemaSourceHolder.FromBytes(Content);

        await Assert.That(() => SchemaSourceHolder.FromStream(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => holder.GetBytes(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => holder.GetChars(null!)).Throws<ArgumentNullException>();
        await Assert.That(async () => await holder.PrefetchAsync(null!, cancellationToken)).Throws<ArgumentNullException>();
    }

    private static SchemaCompilationException Capture(Action action)
    {
        try
        {
            action();
        }
        catch (SchemaCompilationException exception)
        {
            return exception;
        }

        throw new InvalidOperationException("Expected a SchemaCompilationException.");
    }

    private static ReadOnlySequence<T> Sequence<T>(ReadOnlyMemory<T> first, ReadOnlyMemory<T> second)
    {
        var head = new Segment<T>(first);
        return new ReadOnlySequence<T>(head, 0, head.Append(second), second.Length);
    }

    private sealed class Segment<T> : ReadOnlySequenceSegment<T>
    {
        public Segment(ReadOnlyMemory<T> memory) => Memory = memory;

        public Segment<T> Append(ReadOnlyMemory<T> memory)
        {
            var next = new Segment<T>(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
