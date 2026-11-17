namespace NetEvolve.Schemas.Tests.Unit.Inputs;

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Tests.Unit.Contract;

public class DocumentSourceDispatchTests
{
    private const string SystemId = "s";
    private static readonly byte[] Bytes = Encoding.UTF8.GetBytes("<a>x</a>");
    private static readonly char[] Chars = "<a>x</a>".ToCharArray();

    private static ValidationResult SyncStream(RecordingValidator validator)
    {
        using var stream = new MemoryStream(Bytes);
        return validator.Validate(stream, SystemId);
    }

    private static async Task<ValidationResult> AsyncStream(RecordingValidator validator, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(Bytes);
        return await validator.ValidateAsync(stream, SystemId, token).ConfigureAwait(false);
    }

    public static Func<OverloadCase>[] AsyncOverloads() =>
        [
            () => new("ValidateAsync(Stream)", null, AsyncStream, DocumentSourceKind.Bytes),
            () =>
                new(
                    "ValidateAsync(ReadOnlyMemory<byte>)",
                    null,
                    (v, t) => v.ValidateAsync(new ReadOnlyMemory<byte>(Bytes), SystemId, t).AsTask(),
                    DocumentSourceKind.Bytes
                ),
            () =>
                new(
                    "ValidateAsync(ReadOnlyMemory<char>)",
                    null,
                    (v, t) => v.ValidateAsync(new ReadOnlyMemory<char>(Chars), SystemId, t).AsTask(),
                    DocumentSourceKind.Chars
                ),
            () =>
                new(
                    "ValidateAsync(ReadOnlySequence<byte>)",
                    null,
                    (v, t) => v.ValidateAsync(new ReadOnlySequence<byte>(Bytes), SystemId, t).AsTask(),
                    DocumentSourceKind.Bytes
                ),
            () =>
                new(
                    "ValidateAsync(ReadOnlySequence<char>)",
                    null,
                    (v, t) => v.ValidateAsync(new ReadOnlySequence<char>(Chars), SystemId, t).AsTask(),
                    DocumentSourceKind.Chars
                ),
        ];

    public static Func<OverloadCase>[] SyncOverloads() =>
        [
            () => new("Validate(Stream)", SyncStream, null, DocumentSourceKind.Bytes),
            () =>
                new(
                    "Validate(ReadOnlySpan<byte>)",
                    v => v.Validate(Bytes.AsSpan(), SystemId),
                    null,
                    DocumentSourceKind.Bytes
                ),
            () =>
                new(
                    "Validate(ReadOnlySpan<char>)",
                    v => v.Validate(Chars.AsSpan(), SystemId),
                    null,
                    DocumentSourceKind.Chars
                ),
            () =>
                new(
                    "Validate(ReadOnlySequence<byte>)",
                    v => v.Validate(new ReadOnlySequence<byte>(Bytes), SystemId),
                    null,
                    DocumentSourceKind.Bytes
                ),
            () =>
                new(
                    "Validate(ReadOnlySequence<char>)",
                    v => v.Validate(new ReadOnlySequence<char>(Chars), SystemId),
                    null,
                    DocumentSourceKind.Chars
                ),
        ];

    [Test]
    [MethodDataSource(nameof(AsyncOverloads))]
    [MethodDataSource(nameof(SyncOverloads))]
    public async Task EveryOverload_ReachesTheCoreOnceWithTheSameContent(
        OverloadCase overload,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(overload);
        var validator = new RecordingValidator();

        var result = overload.Sync is null
            ? await overload.Async!(validator, cancellationToken).ConfigureAwait(false)
            : overload.Sync(validator);

        await Assert.That(result).IsSameReferenceAs(ValidationResult.Valid);
        await Assert.That(validator.Calls).IsEqualTo(1);
        await Assert.That(validator.Kind).IsEqualTo(overload.Kind);
        await Assert.That(validator.IsSingleSegment).IsTrue();
        await Assert.That(validator.SystemId).IsEqualTo(SystemId);
        var expected = overload.Kind == DocumentSourceKind.Bytes ? (Array)Bytes : Chars;
        await Assert.That(validator.Length).IsEqualTo((long)expected.Length);
        await Assert.That(validator.Content).IsEquivalentTo(expected);
    }

    [Test]
    public async Task OmittedSystemIdAndToken_ReachTheCoreAsNullAndNone(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = new RecordingValidator();

        _ = ValidateOmitted(validator, Bytes);

        await Assert.That(validator.SystemId).IsNull();
        await Assert.That(validator.Token).IsEqualTo(CancellationToken.None);
    }

    [Test]
    public async Task EmptyContent_ReachesTheCoreAsAnEmptyDocument(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = new RecordingValidator();

        _ = ValidateOmitted(validator, []);

        await Assert.That(validator.Calls).IsEqualTo(1);
        await Assert.That(validator.Length).IsEqualTo(0L);
        await Assert.That(validator.Kind).IsEqualTo(DocumentSourceKind.Bytes);
        await Assert.That(validator.IsSingleSegment).IsTrue();
    }

    [Test]
    public async Task ThreeSegmentByteSequence_ArrivesUnchangedAndInOrder(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sequence = Segments.Create<byte>(new byte[] { 1, 2 }, new byte[] { 3 }, new byte[] { 4, 5, 6 });
        var validator = new RecordingValidator();

        _ = ValidateOmitted(validator, sequence);

        await Assert.That(validator.IsSingleSegment).IsFalse();
        await Assert.That(validator.Kind).IsEqualTo(DocumentSourceKind.Bytes);
        await Assert.That(validator.Length).IsEqualTo(6L);
        await Assert.That(validator.Content).IsEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 });
        await Assert.That(validator.Segments.Count).IsEqualTo(3);
        await Assert.That(validator.Segments[0]).IsEquivalentTo(new byte[] { 1, 2 });
        await Assert.That(validator.Segments[1]).IsEquivalentTo(new byte[] { 3 });
        await Assert.That(validator.Segments[2]).IsEquivalentTo(new byte[] { 4, 5, 6 });
    }

    [Test]
    public async Task ThreeSegmentByteSequence_Async_ArrivesUnchangedAndInOrder(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sequence = Segments.Create<byte>(new byte[] { 1, 2 }, new byte[] { 3 }, new byte[] { 4, 5, 6 });
        var validator = new RecordingValidator();

        _ = await validator.ValidateAsync(sequence, SystemId, cancellationToken).ConfigureAwait(false);

        await Assert.That(validator.IsSingleSegment).IsFalse();
        await Assert.That(validator.Segments.Count).IsEqualTo(3);
        await Assert.That(validator.Segments[2]).IsEquivalentTo(new byte[] { 4, 5, 6 });
    }

    [Test]
    public async Task ThreeSegmentCharSequence_ArrivesUnchangedAndInOrder(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sequence = Segments.Create<char>("ab".AsMemory(), "c".AsMemory(), "def".AsMemory());
        var validator = new RecordingValidator();

        _ = ValidateOmitted(validator, sequence);

        await Assert.That(validator.IsSingleSegment).IsFalse();
        await Assert.That(validator.Kind).IsEqualTo(DocumentSourceKind.Chars);
        await Assert.That(validator.Length).IsEqualTo(6L);
        await Assert.That(validator.Segments.Count).IsEqualTo(3);
        await Assert.That(validator.Segments[0]).IsEquivalentTo("ab".ToCharArray());
        await Assert.That(validator.Segments[1]).IsEquivalentTo("c".ToCharArray());
        await Assert.That(validator.Segments[2]).IsEquivalentTo("def".ToCharArray());
    }

    [Test]
    public async Task EmptySequence_ReachesTheCoreAsSingleSegmentEmptyDocument(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = new RecordingValidator();

        _ = ValidateOmitted(validator, ReadOnlySequence<byte>.Empty);

        await Assert.That(validator.IsSingleSegment).IsTrue();
        await Assert.That(validator.Length).IsEqualTo(0L);
    }

    [Test]
    public async Task Token_ReachesTheCoreAsTheCallersInstance(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var source = new CancellationTokenSource();
        var validator = new RecordingValidator();

        _ = Validate(validator, Bytes, source.Token);

        await Assert.That(validator.Token).IsEqualTo(source.Token);
    }

    [Test]
    public async Task NullStream_ThrowsArgumentNullExceptionSynchronously(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = new RecordingValidator();
        Action sync = () => validator.Validate((Stream)null!);
        Action async = () => _ = validator.ValidateAsync((Stream)null!).AsTask();

        await Assert.That(sync).Throws<ArgumentNullException>().WithParameterName("stream");
        await Assert.That(async).Throws<ArgumentNullException>().WithParameterName("stream");
        await Assert.That(validator.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task NullLimitsOrResolver_ThrowsArgumentNullException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Action noLimits = () => _ = new NullArgsValidator(null, DenyAllResourceResolver.Instance);
        Action noResolver = () => _ = new NullArgsValidator(ValidationLimits.Default, null);

        await Assert.That(noLimits).Throws<ArgumentNullException>();
        await Assert.That(noResolver).Throws<ArgumentNullException>();
    }

    private static ValidationResult ValidateOmitted(RecordingValidator validator, byte[] bytes) =>
        validator.Validate(bytes.AsSpan());

    private static ValidationResult Validate(RecordingValidator validator, byte[] bytes, CancellationToken token) =>
        validator.Validate(bytes.AsSpan(), null, token);

    private static ValidationResult ValidateOmitted(RecordingValidator validator, ReadOnlySequence<byte> sequence) =>
        validator.Validate(sequence);

    private static ValidationResult ValidateOmitted(RecordingValidator validator, ReadOnlySequence<char> sequence) =>
        validator.Validate(sequence);

    private sealed class NullArgsValidator : SchemaValidatorBase
    {
        public NullArgsValidator(ValidationLimits? limits, IResourceResolver? resolver)
            : base(limits!, resolver!) { }

        protected override ValidationResult ValidateCore(in DocumentSource source, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValidationResult.Valid;
        }
    }
}
