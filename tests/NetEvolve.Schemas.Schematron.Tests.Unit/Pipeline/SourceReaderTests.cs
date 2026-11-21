namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Pipeline;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The read of a schema source (SCH-55, #311): a stream is read once and its bytes are kept for the next build, and a schema over
/// <c>MaxResourceSize</c> is refused whether the stream can seek or not, in memory or in a stream, synchronously or not.
/// </summary>
public class SourceReaderTests
{
    private static readonly ValidationLimits Small = ValidationLimits.Default.WithMaxResourceSize(64);

    [Test]
    public async Task Bytes_OfAStream_AreReadOnceAndKept(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Utf8(Schema));
        var source = SchemaSource.FromStream(stream, SchemaSystemId);

        var first = SchemaSourceReader.Bytes(source, ValidationLimits.Default, cancellationToken);
        stream.Position = 0;
        stream.SetLength(0);
        var second = SchemaSourceReader.Bytes(source, ValidationLimits.Default, cancellationToken);

        await Assert.That(source.HasReplay).IsTrue();
        await Assert.That(second.Length).IsEqualTo(first.Length);
        await Assert.That(first.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Bytes_OfAKeptStream_AreCheckedAgainstTheCurrentLimit(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Utf8(Schema));
        var source = SchemaSource.FromStream(stream, SchemaSystemId);
        _ = SchemaSourceReader.Bytes(source, ValidationLimits.Default, cancellationToken);

        var failure = Capture(() => _ = SchemaSourceReader.Bytes(source, Small, cancellationToken));

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task Bytes_OfASeekableStreamOverTheLimit_AreRefusedWithoutReading(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Utf8(Schema));
        var source = SchemaSource.FromStream(stream, SchemaSystemId);

        var failure = Capture(() => _ = SchemaSourceReader.Bytes(source, Small, cancellationToken));

        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(failure.Issues.Single().Location.Source).IsEqualTo(SchemaSystemId);
        await Assert.That(source.HasReplay).IsFalse();
    }

    [Test]
    public async Task Bytes_OfAStreamThatCannotSeekAndIsOverTheLimit_AreRefusedWhileReading(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new NoSeekStream(Utf8(Schema));
        var source = SchemaSource.FromStream(stream, SchemaSystemId);

        var failure = Capture(() => _ = SchemaSourceReader.Bytes(source, Small, cancellationToken));

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(source.HasReplay).IsFalse();
    }

    [Test]
    public async Task Prefetch_OfASeekableStreamOverTheLimit_IsRefused(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Utf8(Schema));
        var source = SchemaSource.FromStream(stream, SchemaSystemId);

        var failure = await Assert
            .That(async () => await SchemaSourceReader.PrefetchAsync(source, Small, cancellationToken))
            .Throws<SchemaCompilationException>();

        await Assert.That(failure!.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(stream.Position).IsEqualTo(0L);
    }

    [Test]
    public async Task Prefetch_OfAKeptStreamAndOfBytes_ReadsNothing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Utf8(Schema));
        var source = SchemaSource.FromStream(stream, SchemaSystemId);
        await SchemaSourceReader.PrefetchAsync(source, ValidationLimits.Default, cancellationToken);
        var position = stream.Position;

        await SchemaSourceReader.PrefetchAsync(source, ValidationLimits.Default, cancellationToken);

        await Assert.That(stream.Position).IsEqualTo(position);
        await Assert.That(source.HasReplay).IsTrue();
        await SchemaSourceReader.PrefetchAsync(
            SchemaSource.FromBytes(Utf8(Schema).AsSpan(), null),
            ValidationLimits.Default,
            cancellationToken
        );
    }

    [Test]
    public async Task Build_SchemaInTextOverTheLimit_IsRefused()
    {
        var builder = Builder(Schema).WithLimits(Small);

        var failure = Capture(() => _ = builder.BuildNow());

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task Build_ASecondBuildOfAStream_ReplaysTheBytes()
    {
        using var stream = new MemoryStream(Utf8(Schema));
        var builder = SchemaValidator.ForSchematron(stream, SchemaSystemId);
        var first = builder.BuildNow();

        var second = builder.BuildNow();

        await Assert.That(Describe(second)).IsEqualTo(Describe(first));
    }

    [Test]
    public async Task BuildAsync_ASecondBuildOfAStream_ReplaysTheBytes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Utf8(Schema));
        var builder = SchemaValidator.ForSchematron(stream, SchemaSystemId);
        var first = await builder.BuildAsync(cancellationToken);

        var second = await builder.BuildAsync(cancellationToken);

        await Assert.That(Describe(second)).IsEqualTo(Describe(first));
    }

    private sealed class NoSeekStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }
}
