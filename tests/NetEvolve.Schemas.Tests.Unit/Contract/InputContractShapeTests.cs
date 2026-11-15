namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;

public class InputContractShapeTests
{
    private static readonly string[] AsyncInputs =
    [
        "Stream",
        "ReadOnlyMemory<Byte>",
        "ReadOnlyMemory<Char>",
        "ReadOnlySequence<Byte>",
        "ReadOnlySequence<Char>",
    ];

    private static readonly string[] SyncInputs =
    [
        "Stream",
        "ReadOnlySpan<Byte>",
        "ReadOnlySpan<Char>",
        "ReadOnlySequence<Byte>",
        "ReadOnlySequence<Char>",
    ];

    private static List<string> TenSignatures()
    {
        var signatures = new List<string>();
        foreach (var input in AsyncInputs)
        {
            signatures.Add($"ValueTask<ValidationResult> ValidateAsync({input},String,CancellationToken)");
        }

        foreach (var input in SyncInputs)
        {
            signatures.Add($"ValidationResult Validate({input},String,CancellationToken)");
        }

        return signatures;
    }

    [Test]
    public async Task ISchemaValidator_HasExactlyTheTenSignatures() =>
        await Assert.That(Shape.Of(typeof(ISchemaValidator))).IsEquivalentTo(Shape.Sorted([.. TenSignatures()]));

    [Test]
    public async Task SchemaValidatorBase_ImplementsTenMembersNonVirtuallyAndDeclaresValidateCore()
    {
        var expected = TenSignatures();
        expected.Add("protected abstract ValidationResult ValidateCore(in DocumentSource,CancellationToken)");
        expected.Add("protected .ctor(ValidationLimits,IResourceResolver)");
        expected.Add("protected ValidationLimits Limits { get }");
        expected.Add("protected IResourceResolver Resolver { get }");

        await Assert.That(typeof(SchemaValidatorBase).IsAbstract).IsTrue();
        await Assert.That(typeof(SchemaValidatorBase)).IsAssignableTo<ISchemaValidator>();
        await Assert.That(Shape.Of(typeof(SchemaValidatorBase))).IsEquivalentTo(Shape.Sorted([.. expected]));
    }

    [Test]
    public async Task DocumentSource_IsReadOnlyRefStructWithoutPublicConstructor()
    {
        var type = typeof(DocumentSource);

        await Assert.That(type.IsValueType).IsTrue();
        await Assert.That(type.IsByRefLike).IsTrue();
        await Assert.That(type.GetConstructors()).IsEmpty();
        await Assert
            .That(Shape.Of(type))
            .IsEquivalentTo(
                Shape.Sorted(
                    "DocumentSourceKind Kind { get }",
                    "String SystemId { get }",
                    "Boolean IsSingleSegment { get }",
                    "Int64 Length { get }",
                    "ReadOnlySpan<Byte> Bytes { get }",
                    "ReadOnlySpan<Char> Chars { get }",
                    "ReadOnlySequence<Byte> ByteSequence { get }",
                    "ReadOnlySequence<Char> CharSequence { get }"
                )
            );
    }

    [Test]
    public async Task DocumentSourceKind_HasBytesAndCharsOverAByte()
    {
        await Assert.That(Enum.GetUnderlyingType(typeof(DocumentSourceKind))).IsEqualTo(typeof(byte));
        await Assert.That(Shape.Of(typeof(DocumentSourceKind))).IsEquivalentTo(Shape.Sorted("Bytes=0", "Chars=1"));
    }
}
