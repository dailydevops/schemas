namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Testing.Fakes;

/// <summary>
/// TEMPORARY: pins the throwing contract stubs. Each test is replaced by real behavior tests when the owning
/// work package lands (FND-56). Delete the matching test then.
/// </summary>
public class TemporaryStubTests
{
    // Instance stubs throw before touching state, so an uninitialized instance reaches them without the throwing constructor.
    private static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static async Task AssertStubAsync(string workPackage, params Action[] members)
    {
        foreach (var member in members)
        {
            string? message = null;
            try
            {
                member();
            }
            catch (NotImplementedException ex)
            {
                message = ex.Message;
            }

            await Assert.That(message).IsNotNull();
            await Assert.That(message!).Contains(workPackage);
        }
    }

    [Test]
    public async Task Fnd56_DocumentSourceStubs_ThrowUntilImplemented() =>
        await AssertStubAsync(
            "FND-56",
            () => _ = default(DocumentSource).Kind,
            () => _ = default(DocumentSource).SystemId,
            () => _ = default(DocumentSource).IsSingleSegment,
            () => _ = default(DocumentSource).Length,
            () => _ = default(DocumentSource).Bytes.Length,
            () => _ = default(DocumentSource).Chars.Length,
            () => _ = default(DocumentSource).ByteSequence,
            () => _ = default(DocumentSource).CharSequence
        );

    [Test]
    public async Task Fnd56_ValidatorBaseStubs_ThrowUntilImplemented()
    {
        var validator = Blank<StubValidator>();

        await AssertStubAsync(
            "FND-56",
            () => _ = new StubValidator(),
            () => _ = validator.ExposedLimits,
            () => _ = validator.ExposedResolver,
            () => _ = validator.ValidateAsync(Stream.Null).AsTask(),
            () => _ = validator.ValidateAsync(ReadOnlyMemory<byte>.Empty).AsTask(),
            () => _ = validator.ValidateAsync(ReadOnlyMemory<char>.Empty).AsTask(),
            () => _ = validator.ValidateAsync(ReadOnlySequence<byte>.Empty).AsTask(),
            () => _ = validator.ValidateAsync(ReadOnlySequence<char>.Empty).AsTask(),
            () => _ = validator.Validate(Stream.Null),
            () => _ = validator.Validate(ReadOnlySpan<byte>.Empty),
            () => _ = validator.Validate(ReadOnlySpan<char>.Empty),
            () => _ = validator.Validate(ReadOnlySequence<byte>.Empty),
            () => _ = validator.Validate(ReadOnlySequence<char>.Empty)
        );
    }

    private sealed class StubValidator : SchemaValidatorBase
    {
        public StubValidator()
            : base(null!, null!) { }

        public ValidationLimits ExposedLimits => Limits;

        public IResourceResolver ExposedResolver => Resolver;

        protected override ValidationResult ValidateCore(
            in DocumentSource source,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}
