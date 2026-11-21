namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.Validation.Assembly;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

public class SharedCoreTests
{
    private static readonly byte[] Document = Encoding.UTF8.GetBytes("<a/>");

    [Test]
    public async Task Validate_StreamAndSpanPaths_CallTheSameCoreOnce_WithTheSameBytes(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var core = new RecordingCore();
        var validator = Validator(core);

        Synchronous(validator);
        await Asynchronous(validator, cancellationToken);

        await Assert.That(core.Calls.Count).IsEqualTo(4);
        foreach (var call in core.Calls)
        {
            await Assert.That(call.Kind).IsEqualTo(DocumentSourceKind.Bytes);
            await Assert.That(call.SystemId).IsEqualTo("id:1");
            await Assert.That(call.Bytes).IsEquivalentTo(Document);
        }
    }

    [Test]
    public async Task Validate_CharacterPath_CallsTheSameCore_WithCharacters(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var core = new RecordingCore();
        var validator = Validator(core);

        Characters(validator);
        _ = await validator.ValidateAsync("<a/>".AsMemory(), "id:2", cancellationToken);

        await Assert.That(core.Calls.Count).IsEqualTo(2);
        await Assert.That(core.Calls[0].Kind).IsEqualTo(DocumentSourceKind.Chars);
        await Assert.That(core.Calls[1].Kind).IsEqualTo(DocumentSourceKind.Chars);
    }

    [Test]
    public async Task Validate_MultiSegmentSequence_CallsTheCore_WithTheSegmentedSource()
    {
        var core = new RecordingCore();
        var validator = Validator(core);

        MultiSegment(validator);

        await Assert.That(core.Calls.Count).IsEqualTo(1);
        await Assert.That(core.Calls[0].IsSingleSegment).IsFalse();
        await Assert.That(core.Calls[0].Length).IsEqualTo((long)Document.Length);
    }

    [Test]
    public async Task Validate_ReturnsTheResultOfTheCore_AndPassesTheToken(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var core = new RecordingCore();
        var validator = Validator(core);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var result = WithToken(validator, source.Token);

        await Assert.That(result).IsSameReferenceAs(core.Result);
        await Assert.That(core.Tokens[0]).IsEqualTo(source.Token);
        await Assert.That(validator.Version).IsEqualTo(XsdVersion.V10);
    }

    [Test]
    public async Task Constructor_NullCore_Throws() =>
        await Assert
            .That(() =>
                new XsdValidator(null!, XsdVersion.V11, ValidationLimits.Default, DenyAllResourceResolver.Instance)
            )
            .Throws<ArgumentNullException>();

    private static XsdValidator Validator(IValidationCore core) =>
        new(core, XsdVersion.V10, ValidationLimits.Default, DenyAllResourceResolver.Instance);

    private static void Characters(XsdValidator validator) => validator.Validate("<a/>".AsSpan(), "id:2");

    private static void MultiSegment(XsdValidator validator) =>
        validator.Validate(AssemblyFixture.Segmented(Document, 1), "id:3");

    private static ValidationResult WithToken(XsdValidator validator, CancellationToken token) =>
        validator.Validate(Document.AsSpan(), null, token);

    private static void Synchronous(XsdValidator validator)
    {
        using var stream = new MemoryStream(Document);

        _ = validator.Validate(Document.AsSpan(), "id:1");
        _ = validator.Validate(stream, "id:1");
    }

    private static async Task Asynchronous(XsdValidator validator, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Document);

        _ = await validator.ValidateAsync(stream, "id:1", cancellationToken);
        _ = await validator.ValidateAsync(Document.AsMemory(), "id:1", cancellationToken);
    }

    private sealed class RecordingCore : IValidationCore
    {
        public ValidationResult Result { get; } = ValidationResult.Valid;

        public List<Call> Calls { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public ValidationResult Validate(in DocumentSource source, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Tokens.Add(cancellationToken);
            Calls.Add(
                new Call(
                    source.Kind,
                    source.SystemId,
                    source.IsSingleSegment && source.Kind == DocumentSourceKind.Bytes ? source.Bytes.ToArray() : [],
                    source.IsSingleSegment,
                    source.Length
                )
            );
            return Result;
        }
    }

    private sealed record Call(
        DocumentSourceKind Kind,
        string? SystemId,
        byte[] Bytes,
        bool IsSingleSegment,
        long Length
    );
}
