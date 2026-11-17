namespace NetEvolve.Schemas.Tests.Unit.Inputs;

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Tests.Unit.Contract;

public class StreamInputTests
{
    private static byte[] Pattern(int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < length; i++)
        {
            bytes[i] = (byte)(i % 251);
        }

        return bytes;
    }

    private static ValidationLimits LimitTo(long size) => ValidationLimits.Default.WithMaxInputSize(size);

    private static ValidationResult Sync(RecordingValidator validator, TrackingStream stream) =>
        validator.Validate(stream, "urn:doc");

    private static ValidationResult SyncCanceled(
        RecordingValidator validator,
        TrackingStream stream,
        CancellationToken token
    ) => validator.Validate(stream, null, token);

    private static ValidationResult SyncSpan(RecordingValidator validator, byte[] bytes) =>
        validator.Validate(bytes.AsSpan());

    private static ValidationResult SyncSpan(RecordingValidator validator, char[] chars) =>
        validator.Validate(chars.AsSpan());

    private static ValidationResult SyncSequence(RecordingValidator validator, ReadOnlySequence<byte> sequence) =>
        validator.Validate(sequence);

    [Test]
    public async Task Stream_IsNotDisposedAndIsLeftAtTheEnd(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new TrackingStream(Pattern(100));
        var validator = new RecordingValidator();

        _ = Sync(validator, stream);
        stream.Position = 0;
        _ = await validator.ValidateAsync(stream, null, cancellationToken).ConfigureAwait(false);

        await Assert.That(stream.IsDisposed).IsFalse();
        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(stream.Position).IsEqualTo(100L);
        await Assert.That(validator.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task Stream_IsReadFromItsCurrentPosition(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new TrackingStream(Pattern(100)) { Position = 40 };
        var validator = new RecordingValidator();

        _ = Sync(validator, stream);

        await Assert.That(validator.Length).IsEqualTo(60L);
        await Assert.That(((byte[])validator.Content!)[0]).IsEqualTo((byte)(40 % 251));
        await Assert.That(validator.SystemId).IsEqualTo("urn:doc");
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(4095)]
    [Arguments(4096)]
    [Arguments(4097)]
    [Arguments(100_000)]
    public async Task NonSeekableStream_GrowsTheBufferAndDeliversAllBytes(
        int length,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var expected = Pattern(length);
        var validator = new RecordingValidator();
        using var syncStream = new TrackingStream(expected, canSeek: false);
        using var asyncStream = new TrackingStream(expected, canSeek: false);

        _ = Sync(validator, syncStream);
        var syncContent = (byte[])validator.Content!;
        _ = await validator.ValidateAsync(asyncStream, null, cancellationToken).ConfigureAwait(false);

        await Assert.That(syncContent).IsEquivalentTo(expected);
        await Assert.That(validator.Content).IsEquivalentTo(expected);
        await Assert.That(validator.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task SeekableStream_AtTheLimit_ReachesTheCore(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new TrackingStream(Pattern(100));
        var validator = new RecordingValidator(LimitTo(100));

        var result = Sync(validator, stream);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(validator.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task SeekableStream_AboveTheLimit_ReturnsNes0001WithoutReading(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new TrackingStream(Pattern(101));
        var validator = new RecordingValidator(LimitTo(100));

        var result = Sync(validator, stream);
        var asyncResult = await validator.ValidateAsync(stream, null, cancellationToken).ConfigureAwait(false);

        await Assert.That(stream.BytesRead).IsEqualTo(0L);
        await Assert.That(validator.Calls).IsEqualTo(0);
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.InputSizeLimitExceeded);
        await Assert.That(asyncResult.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.InputSizeLimitExceeded);
        await Assert.That(result.Issues[0].Location.Source).IsEqualTo("urn:doc");
    }

    [Test]
    public async Task NonSeekableStream_AboveTheLimit_StopsReadingAtLimitPlusOne(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new TrackingStream(Pattern(10_000), canSeek: false);
        var validator = new RecordingValidator(LimitTo(100));

        var result = Sync(validator, stream);

        await Assert.That(stream.BytesRead).IsEqualTo(101L);
        await Assert.That(validator.Calls).IsEqualTo(0);
        await Assert.That(result.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.InputSizeLimitExceeded);
    }

    [Test]
    public async Task StreamWithUnderstatedLength_CannotBypassTheLimit(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new TrackingStream(Pattern(10_000)) { ReportedLength = 10 };
        var validator = new RecordingValidator(LimitTo(100));

        var result = await validator.ValidateAsync(stream, null, cancellationToken).ConfigureAwait(false);

        await Assert.That(stream.BytesRead).IsEqualTo(101L);
        await Assert.That(validator.Calls).IsEqualTo(0);
        await Assert.That(result.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.InputSizeLimitExceeded);
    }

    [Test]
    public async Task OtherInputs_AboveTheLimit_ReturnNes0001WithoutCallingTheCore(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = new RecordingValidator(LimitTo(4));

        var bytes = SyncSpan(validator, new byte[5]);
        var chars = SyncSpan(validator, new char[5]);
        var sequence = SyncSequence(validator, Segments.Create<byte>(new byte[2], new byte[2], new byte[1]));
        var memory = await validator
            .ValidateAsync(new ReadOnlyMemory<char>(new char[5]), null, cancellationToken)
            .ConfigureAwait(false);
        var atLimit = SyncSpan(validator, new byte[4]);

        await Assert.That(validator.Calls).IsEqualTo(1);
        await Assert.That(atLimit.IsValid).IsTrue();
        foreach (var result in new[] { bytes, chars, sequence, memory })
        {
            await Assert.That(result.IsValid).IsFalse();
            await Assert.That(result.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.InputSizeLimitExceeded);
        }
    }

    [Test]
    public async Task CanceledToken_StopsTheStreamReadLoop(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var source = new CancellationTokenSource();
        using var stream = new TrackingStream(Pattern(100_000), canSeek: false) { OnRead = source.Cancel };
        var validator = new RecordingValidator();
        Action act = () => SyncCanceled(validator, stream, source.Token);

        await Assert.That(act).Throws<OperationCanceledException>();
        await Assert.That(validator.Calls).IsEqualTo(0);
    }
}
