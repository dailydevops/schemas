namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class InputKindTests
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    [Test]
    [Arguments(AssemblyFixture.ValidInstance)]
    [Arguments(AssemblyFixture.InvalidInstance)]
    public async Task Validate_EveryInputKind_GivesIdenticalIssues(string instance, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(instance);
        var validator = AssemblyFixture.Build(AssemblyFixture.Schema);

        var results = await AllKinds(validator, instance, cancellationToken);

        await Assert.That(results.Length).IsEqualTo(14);
        foreach (var result in results)
        {
            await Assert.That(AssemblyFixture.Same(result, results[0])).IsTrue();
        }
    }

    [Test]
    public async Task Validate_InvalidInstance_ReportsTheProblemsWithLocations()
    {
        var result = AssemblyFixture.Run(
            AssemblyFixture.Build(AssemblyFixture.Schema),
            AssemblyFixture.InvalidInstance
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count).IsGreaterThanOrEqualTo(3);
        foreach (var issue in result.Issues)
        {
            await Assert.That(issue.Location.Source).IsEqualTo(AssemblyFixture.InstanceId);
            await Assert.That(issue.Location.Line).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task Validate_ValidInstance_IsValidWithoutIssues()
    {
        var result = AssemblyFixture.Run(AssemblyFixture.Build(AssemblyFixture.Schema), AssemblyFixture.ValidInstance);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Validate_MultiSegmentBytes_CutInsideAMultiByteCharacter_DecodesAsOneDocument()
    {
        var bytes = Utf8.GetBytes(AssemblyFixture.InvalidInstance);
        var cut = Array.IndexOf(bytes, (byte)0xC3) + 1;
        var validator = AssemblyFixture.Build(AssemblyFixture.Schema);

        var segmented = AssemblyFixture.Run(validator, AssemblyFixture.Segmented(bytes, cut));
        var contiguous = AssemblyFixture.Run(validator, bytes);

        await Assert.That(cut).IsGreaterThan(1);
        await Assert.That(AssemblyFixture.Same(segmented, contiguous)).IsTrue();
    }

    private static async Task<ValidationResult[]> AllKinds(
        IXsdValidator validator,
        string instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = Utf8.GetBytes(instance);
        var chars = instance.ToCharArray();
        var sync = Synchronous(validator, instance, bytes, chars);
        var asynchronous = await Asynchronous(validator, instance, bytes, chars, cancellationToken);

        return [.. sync, .. asynchronous];
    }

    private static ValidationResult[] Synchronous(IXsdValidator validator, string instance, byte[] bytes, char[] chars)
    {
        using var stream = new MemoryStream(bytes);

        return
        [
            validator.Validate(instance.AsSpan(), AssemblyFixture.InstanceId),
            validator.Validate(bytes.AsSpan(), AssemblyFixture.InstanceId),
            validator.Validate(new ReadOnlySequence<byte>(bytes), AssemblyFixture.InstanceId),
            validator.Validate(new ReadOnlySequence<char>(chars), AssemblyFixture.InstanceId),
            validator.Validate(AssemblyFixture.Segmented(bytes, 7), AssemblyFixture.InstanceId),
            validator.Validate(AssemblyFixture.Segmented(chars, 7), AssemblyFixture.InstanceId),
            validator.Validate(stream, AssemblyFixture.InstanceId),
        ];
    }

    private static async Task<ValidationResult[]> Asynchronous(
        IXsdValidator validator,
        string instance,
        byte[] bytes,
        char[] chars,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(bytes);

        return
        [
            await validator.ValidateAsync(stream, AssemblyFixture.InstanceId, cancellationToken),
            await validator.ValidateAsync(instance.AsMemory(), AssemblyFixture.InstanceId, cancellationToken),
            await validator.ValidateAsync(bytes.AsMemory(), AssemblyFixture.InstanceId, cancellationToken),
            await validator.ValidateAsync(
                new ReadOnlySequence<byte>(bytes),
                AssemblyFixture.InstanceId,
                cancellationToken
            ),
            await validator.ValidateAsync(
                new ReadOnlySequence<char>(chars),
                AssemblyFixture.InstanceId,
                cancellationToken
            ),
            await validator.ValidateAsync(
                AssemblyFixture.Segmented(bytes, 5),
                AssemblyFixture.InstanceId,
                cancellationToken
            ),
            await validator.ValidateAsync(
                AssemblyFixture.Segmented(chars, 5),
                AssemblyFixture.InstanceId,
                cancellationToken
            ),
        ];
    }
}
