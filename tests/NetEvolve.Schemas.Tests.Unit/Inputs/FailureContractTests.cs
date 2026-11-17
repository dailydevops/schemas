namespace NetEvolve.Schemas.Tests.Unit.Inputs;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>The sync and async failure and cancellation table of the input contract.</summary>
public class FailureContractTests
{
    private static readonly byte[] Bytes = [1, 2, 3];

    public static Func<Func<RecordingValidator, CancellationToken, Task<ValidationResult>>>[] AsyncOverloads() =>
        [
            () => (v, t) => v.ValidateAsync(Stream.Null, null, t).AsTask(),
            () => (v, t) => v.ValidateAsync(new ReadOnlyMemory<byte>(Bytes), null, t).AsTask(),
            () => (v, t) => v.ValidateAsync(new ReadOnlyMemory<char>(new char[3]), null, t).AsTask(),
            () => (v, t) => v.ValidateAsync(new ReadOnlySequence<byte>(Bytes), null, t).AsTask(),
            () => (v, t) => v.ValidateAsync(new ReadOnlySequence<char>(new char[3]), null, t).AsTask(),
        ];

    private static ValidationResult SyncSpan(RecordingValidator validator, CancellationToken token) =>
        validator.Validate(Bytes.AsSpan(), null, token);

    private static ValidationResult SyncStream(RecordingValidator validator, Stream stream, CancellationToken token) =>
        validator.Validate(stream, null, token);

    [Test]
    [MethodDataSource(nameof(AsyncOverloads))]
    public async Task Async_TokenCanceledOnEntry_ReturnsCanceledTaskWithoutCallingTheCore(
        Func<RecordingValidator, CancellationToken, Task<ValidationResult>> overload,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(overload);
        using var source = new CancellationTokenSource();
        await source.CancelAsync().ConfigureAwait(false);
        var validator = new RecordingValidator();

        var task = overload(validator, source.Token);

        await Assert.That(task.IsCanceled).IsTrue();
        await Assert.That(validator.Calls).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(AsyncOverloads))]
    public async Task Async_CallersTokenCancelsInsideTheCore_ReturnsCanceledTask(
        Func<RecordingValidator, CancellationToken, Task<ValidationResult>> overload,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(overload);
        using var source = new CancellationTokenSource();
        var validator = new RecordingValidator
        {
            Throw = new OperationCanceledException(source.Token),
            OnCore = source.Cancel,
        };

        var task = overload(validator, source.Token);

        await Assert.That(task.IsCanceled).IsTrue();
        await Assert.That(validator.Calls).IsEqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(AsyncOverloads))]
    public async Task Async_ForeignCancellation_ReturnsFaultedTaskWithTheSameInstance(
        Func<RecordingValidator, CancellationToken, Task<ValidationResult>> overload,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(overload);
        var foreign = new OperationCanceledException("foreign");
        var validator = new RecordingValidator { Throw = foreign };

        var task = overload(validator, CancellationToken.None);

        await Assert.That(task.IsFaulted).IsTrue();
        await Assert.That(task.IsCanceled).IsFalse();
        await Assert.That(task.Exception!.InnerException).IsSameReferenceAs(foreign);
    }

    [Test]
    [MethodDataSource(nameof(AsyncOverloads))]
    public async Task Async_OtherException_ReturnsFaultedTaskAndNeverThrowsSynchronously(
        Func<RecordingValidator, CancellationToken, Task<ValidationResult>> overload,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(overload);
        var failure = new InvalidOperationException("boom");
        var validator = new RecordingValidator { Throw = failure };

        var task = overload(validator, CancellationToken.None);

        await Assert.That(task.IsFaulted).IsTrue();
        await Assert.That(task.Exception!.InnerException).IsSameReferenceAs(failure);
    }

    [Test]
    public async Task Sync_TokenCanceledOnEntry_ThrowsOperationCanceledExceptionWithoutCallingTheCore(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var source = new CancellationTokenSource();
        await source.CancelAsync().ConfigureAwait(false);
        using var stream = new MemoryStream(Bytes);
        var validator = new RecordingValidator();
        Action span = () => SyncSpan(validator, source.Token);
        Action viaStream = () => SyncStream(validator, stream, source.Token);

        await Assert.That(span).Throws<OperationCanceledException>();
        await Assert.That(viaStream).Throws<OperationCanceledException>();
        await Assert.That(validator.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task Sync_CoreException_PropagatesUnchanged(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var failure = new InvalidOperationException("boom");
        var validator = new RecordingValidator { Throw = failure };
        Action act = () => SyncSpan(validator, CancellationToken.None);

        var thrown = await Assert.That(act).Throws<InvalidOperationException>();

        await Assert.That(thrown).IsSameReferenceAs(failure);
    }

    [Test]
    public async Task StreamReadFailure_IsCarriedByTheAsyncTaskAndThrownBySyncValidate(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = new RecordingValidator();
        using var syncStream = new UnreadableStream();
        using var asyncStream = new UnreadableStream();
        Action act = () => SyncStream(validator, syncStream, CancellationToken.None);

        await Assert.That(act).Throws<NotSupportedException>();
        var task = validator.ValidateAsync(asyncStream, null, CancellationToken.None).AsTask();

        await Assert.That(task.IsFaulted).IsTrue();
    }

    private sealed class UnreadableStream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}
