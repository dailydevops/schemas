namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// <c>Build</c> and <c>BuildAsync</c> on the real Xml and XPath engines (SCH-55, #311): the validator is immutable, every build is
/// independent, and a stream is read from its current position, never disposed, and asynchronously by <c>BuildAsync</c>.
/// </summary>
public class BuildTests
{
    [Test]
    public async Task Build_ReturnsAValidatorWhoseStateCannotBeChanged()
    {
        var builder = Builder(ParameterSchema).WithPhase("#ALL").WithParameter("limit", "500");
        var validator = builder.BuildNow();
        var before = Describe(Validate(validator, Instance));
        var parameters = validator.Parameters;

        _ = builder.WithPhase("other").WithParameter("limit", "5000").WithParameter("language", "de");

        await Assert.That(validator.Phase).IsEqualTo("#ALL");
        await Assert.That(validator.Parameters.Count).IsEqualTo(1);
        await Assert.That(validator.Parameters["limit"]).IsEqualTo("500");
        await Assert.That(Describe(Validate(validator, Instance))).IsEqualTo(before);
        await Assert.That(Validate(validator, Instance).Issues.Count).IsEqualTo(1);
        if (parameters is IDictionary<string, string> dictionary)
        {
            await Assert.That(() => dictionary["limit"] = "5000").Throws<NotSupportedException>();
        }
    }

    [Test]
    public async Task BuildAsync_ReturnsAValidatorWhoseStateCannotBeChanged(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var builder = Builder(ParameterSchema).WithParameter("limit", "500");
        var validator = await builder.BuildAsync(cancellationToken);

        _ = builder.WithParameter("limit", "5000");

        await Assert.That(validator.Parameters["limit"]).IsEqualTo("500");
        await Assert.That(Validate(validator, MediumInvoice).Issues.Count).IsEqualTo(1);
        await Assert.That(Validate(Build(ParameterSchema), MediumInvoice).Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Build_Twice_GivesIndependentValidatorsWithTheSameResults()
    {
        var builder = Builder(Schema);

        var first = builder.BuildNow();
        var second = builder.BuildNow();

        await Assert.That(second).IsNotSameReferenceAs(first);
        await Assert.That(Describe(Validate(second, Instance))).IsEqualTo(Describe(Validate(first, Instance)));
    }

    [Test]
    public async Task Build_Stream_ReadsFromTheCurrentPositionAndNeverDisposesTheStream()
    {
        var bytes = Utf8("junk" + Schema);
        using var stream = new MemoryStream(bytes);
        stream.Position = 4;

        var validator = SchemaValidator.ForSchematron(stream, SchemaSystemId).BuildNow();

        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(stream.Position).IsEqualTo(bytes.Length);
        await Assert
            .That(Describe(Validate(validator, Instance)))
            .IsEqualTo(Describe(Validate(Build(Schema), Instance)));
    }

    [Test]
    public async Task Build_NonSeekableStream_IsNotReadAgainByASecondBuild()
    {
        using var stream = new ScriptedStream(Utf8(Schema), seekable: false);
        var builder = SchemaValidator.ForSchematron(stream, SchemaSystemId);

        var first = builder.BuildNow();
        var second = builder.BuildNow();

        await Assert.That(Describe(second)).IsEqualTo(Describe(first));
        await Assert.That(stream.Reads).IsGreaterThan(0);
    }

    [Test]
    public async Task BuildAsync_Stream_ReadsAsynchronously(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ScriptedStream(Utf8(Schema), syncReadFails: true);

        var validator = await SchemaValidator.ForSchematron(stream, SchemaSystemId).BuildAsync(cancellationToken);

        await Assert.That(Describe(validator)).IsEqualTo(Describe(Build(Schema)));
        await Assert.That(stream.AsyncReads).IsGreaterThan(0);
    }

    [Test]
    public async Task BuildAsync_Stream_IsReadFromTheCurrentPositionAndNeverDisposed(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = Utf8("junk" + Schema);
        using var stream = new MemoryStream(bytes);
        stream.Position = 4;

        var validator = await SchemaValidator.ForSchematron(stream, SchemaSystemId).BuildAsync(cancellationToken);

        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(Describe(validator)).IsEqualTo(Describe(Build(Schema)));
    }

    [Test]
    public async Task BuildAsync_InMemorySource_BuildsLikeBuild(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var builder = Builder(Schema);

        var validator = await builder.BuildAsync(cancellationToken);

        await Assert.That(Describe(validator)).IsEqualTo(Describe(Build(Schema)));
    }

    [Test]
    public async Task BuildAsync_SchemaDefects_FaultWithSchemaCompilationException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var builder = Builder(TwoDefects);

        var failure = await Assert
            .That(async () => await builder.BuildAsync(cancellationToken))
            .Throws<SchemaCompilationException>();

        await Assert.That(failure!.Issues.Count).IsEqualTo(2);
    }

    [Test]
    public async Task BuildAsync_Cancelled_Cancels()
    {
        using var source = new CancellationTokenSource();
#if NET8_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        await Assert
            .That(async () => await Builder(Schema).BuildAsync(source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task BuildAsync_CancelledWhileTheStreamIsRead_Cancels()
    {
        using var source = new CancellationTokenSource();
        using var stream = new ScriptedStream(Utf8(Schema), cancel: source);
        var builder = SchemaValidator.ForSchematron(stream, SchemaSystemId);

        await Assert.That(async () => await builder.BuildAsync(source.Token)).Throws<OperationCanceledException>();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Build_SchemaLargerThanMaxResourceSize_ReportsTheResourceSizeLimit(bool asStream)
    {
        var limits = NetEvolve.Schemas.ValidationLimits.Default.WithMaxResourceSize(64);
        var bytes = Utf8(Schema);
        using var stream = new MemoryStream(bytes);
        var builder = asStream
            ? SchemaValidator.ForSchematron(stream, SchemaSystemId)
            : SchemaValidator.ForSchematron(bytes.AsSpan(), SchemaSystemId);

        var failure = Capture(() => _ = builder.WithLimits(limits).Build());

        await Assert.That(failure.Issues.Count).IsEqualTo(1);
        await Assert.That(failure.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task BuildAsync_StreamLargerThanMaxResourceSize_ReportsTheResourceSizeLimit(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var limits = NetEvolve.Schemas.ValidationLimits.Default.WithMaxResourceSize(64);
        using var stream = new ScriptedStream(Utf8(Schema), seekable: false);
        var builder = SchemaValidator.ForSchematron(stream, SchemaSystemId).WithLimits(limits);

        var failure = await Assert
            .That(async () => await builder.BuildAsync(cancellationToken))
            .Throws<SchemaCompilationException>();

        await Assert.That(failure!.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task Build_EmptySchema_IsNotWellFormed()
    {
        var failure = BuildFailure(string.Empty);

        await Assert.That(failure.Issues.Count).IsEqualTo(1);
        await Assert.That(failure.Issues[0].Location.Source).IsEqualTo(SchemaSystemId);
    }

    // A stream over bytes whose reads are scripted: it can refuse to seek, refuse the synchronous read, and cancel a token on a read.
    // It derives from Stream, so the Memory overloads reach the array overloads that it counts.
    private sealed class ScriptedStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly bool _seekable;
        private readonly bool _syncReadFails;
        private readonly CancellationTokenSource? _cancel;

        internal ScriptedStream(
            byte[] bytes,
            bool seekable = true,
            bool syncReadFails = false,
            CancellationTokenSource? cancel = null
        )
        {
            _inner = new MemoryStream(bytes);
            _seekable = seekable;
            _syncReadFails = syncReadFails;
            _cancel = cancel;
        }

        internal int Reads { get; private set; }

        internal int AsyncReads { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => _seekable;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Reads++;
            if (_syncReadFails)
            {
                throw new InvalidOperationException("The stream must be read asynchronously.");
            }

            _cancel?.Cancel();
            return _inner.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncReads++;
            if (_cancel is not null)
            {
#if NET8_0_OR_GREATER
                await _cancel.CancelAsync();
#else
                _cancel.Cancel();
#endif
            }

            return await _inner.ReadAsync(buffer, CancellationToken.None);
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
