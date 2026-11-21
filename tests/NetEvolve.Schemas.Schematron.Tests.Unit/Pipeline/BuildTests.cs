namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        var builder = Builder(Schema).WithPhase("#ALL").WithParameter("country", "DE");
        var validator = builder.BuildNow();
        var before = Describe(validator);
        var parameters = validator.Parameters;

        _ = builder.WithPhase("other").WithParameter("country", "FR").WithParameter("language", "de");

        await Assert.That(validator.Phase).IsEqualTo("#ALL");
        await Assert.That(validator.Parameters.Count).IsEqualTo(1);
        await Assert.That(validator.Parameters["country"]).IsEqualTo("DE");
        await Assert.That(Describe(validator)).IsEqualTo(before);
        if (parameters is IDictionary<string, string> dictionary)
        {
            await Assert.That(() => dictionary["country"] = "FR").Throws<NotSupportedException>();
        }

        await Assert.That(Describe(Validate(validator, Instance))).IsEqualTo(Describe(Validate(validator, Instance)));
    }

    [Test]
    public async Task BuildAsync_ReturnsAValidatorWhoseStateCannotBeChanged(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var builder = Builder(Schema).WithParameter("country", "DE");
        var validator = await builder.BuildAsync(cancellationToken);

        _ = builder.WithParameter("country", "FR");

        await Assert.That(validator.Parameters["country"]).IsEqualTo("DE");
        await Assert.That(Describe(validator)).IsEqualTo(Describe(Build(Schema)));
        await Assert
            .That(Describe(Validate(validator, Instance)))
            .IsEqualTo(Describe(Validate(Build(Schema), Instance)));
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
        using var stream = new OneWayStream(Utf8(Schema));
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

        using var stream = new AsyncOnlyStream(Utf8(Schema));

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
        await source.CancelAsync();

        await Assert
            .That(async () => await Builder(Schema).BuildAsync(source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task BuildAsync_CancelledWhileTheStreamIsRead_Cancels()
    {
        using var source = new CancellationTokenSource();
        using var stream = new CancelOnReadStream(Utf8(Schema), source);
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
        using var stream = new OneWayStream(Utf8(Schema));
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

    // A stream that cannot seek and counts its reads.
    private sealed class OneWayStream : MemoryStream
    {
        internal OneWayStream(byte[] bytes)
            : base(bytes) { }

        internal int Reads { get; private set; }

        public override bool CanSeek => false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            Reads++;
            return base.Read(buffer, offset, count);
        }
    }

    // A stream whose synchronous read fails: only an asynchronous read succeeds.
    private sealed class AsyncOnlyStream : MemoryStream
    {
        internal AsyncOnlyStream(byte[] bytes)
            : base(bytes) { }

        internal int AsyncReads { get; private set; }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("The stream must be read asynchronously.");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncReads++;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }

    // A stream that cancels the token of the build while it is read.
    private sealed class CancelOnReadStream : MemoryStream
    {
        private readonly CancellationTokenSource _source;

        internal CancelOnReadStream(byte[] bytes, CancellationTokenSource source)
            : base(bytes) => _source = source;

        public override int Read(byte[] buffer, int offset, int count)
        {
            _source.Cancel();
            return base.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _source.CancelAsync();
            return await base.ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None);
        }
    }
}
