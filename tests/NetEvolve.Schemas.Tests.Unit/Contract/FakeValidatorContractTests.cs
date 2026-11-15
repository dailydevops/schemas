#pragma warning disable CA1849, VSTHRD103, NE0010 // The synchronous overloads are the contract under test; test methods take no token.

namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Fakes;

public class FakeValidatorContractTests
{
    private const string SystemId = "file:///doc.xml";
    private const string Text = "<a/>";
    private static readonly byte[] Utf8 = [0x3C, 0x61, 0x2F, 0x3E];

    private static readonly CancellationTokenSource Source = new();

    private static ReadOnlySequence<byte> BytesInTwoSegments() =>
        Segments.Create<byte>(Utf8.AsMemory(0, 2), Utf8.AsMemory(2));

    private static ReadOnlySequence<char> CharsInTwoSegments() =>
        Segments.Create<char>(Text.AsMemory(0, 2), Text.AsMemory(2));

    private async Task AssertCall(FakeValidator fake, string method, string parameterType, bool bytes)
    {
        await Assert.That(fake.Calls).Count().IsEqualTo(1);
        var call = fake.Calls[0];
        await Assert.That(call.Method).IsEqualTo(method);
        await Assert.That(call.ParameterType).IsEqualTo(parameterType);
        await Assert.That(call.SystemId).IsEqualTo(SystemId);
        await Assert.That(call.CancellationToken).IsEqualTo(Source.Token);
        if (bytes)
        {
            await Assert.That(call.Bytes).IsEquivalentTo(Utf8);
        }
        else
        {
            await Assert.That(call.Chars).IsEqualTo(Text);
        }
    }

    [Test]
    public async Task ValidateAsync_Stream_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();
        using var stream = new MemoryStream(Utf8);

        _ = await fake.ValidateAsync(stream, SystemId, Source.Token);

        await AssertCall(fake, "ValidateAsync", "Stream", bytes: true);
    }

    [Test]
    public async Task ValidateAsync_MemoryOfByte_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();

        _ = await fake.ValidateAsync(new ReadOnlyMemory<byte>(Utf8), SystemId, Source.Token);

        await AssertCall(fake, "ValidateAsync", "ReadOnlyMemory<Byte>", bytes: true);
    }

    [Test]
    public async Task ValidateAsync_MemoryOfChar_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();

        _ = await fake.ValidateAsync(Text.AsMemory(), SystemId, Source.Token);

        await AssertCall(fake, "ValidateAsync", "ReadOnlyMemory<Char>", bytes: false);
    }

    [Test]
    public async Task ValidateAsync_SequenceOfByte_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();

        _ = await fake.ValidateAsync(BytesInTwoSegments(), SystemId, Source.Token);

        await AssertCall(fake, "ValidateAsync", "ReadOnlySequence<Byte>", bytes: true);
    }

    [Test]
    public async Task ValidateAsync_SequenceOfChar_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();

        _ = await fake.ValidateAsync(CharsInTwoSegments(), SystemId, Source.Token);

        await AssertCall(fake, "ValidateAsync", "ReadOnlySequence<Char>", bytes: false);
    }

    [Test]
    public async Task Validate_Stream_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();
        using var stream = new MemoryStream(Utf8);

        _ = fake.Validate(stream, SystemId, Source.Token);

        await AssertCall(fake, "Validate", "Stream", bytes: true);
    }

    [Test]
    public async Task Validate_SpanOfByte_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();

        _ = fake.Validate(new ReadOnlySpan<byte>(Utf8), SystemId, Source.Token);

        await AssertCall(fake, "Validate", "ReadOnlySpan<Byte>", bytes: true);
    }

    [Test]
    public async Task Validate_SpanOfChar_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();

        _ = fake.Validate(Text.AsSpan(), SystemId, Source.Token);

        await AssertCall(fake, "Validate", "ReadOnlySpan<Char>", bytes: false);
    }

    [Test]
    public async Task Validate_SequenceOfByte_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();

        _ = fake.Validate(BytesInTwoSegments(), SystemId, Source.Token);

        await AssertCall(fake, "Validate", "ReadOnlySequence<Byte>", bytes: true);
    }

    [Test]
    public async Task Validate_SequenceOfChar_FakeReceivesSameContent()
    {
        var fake = new FakeValidator();

        _ = fake.Validate(CharsInTwoSegments(), SystemId, Source.Token);

        await AssertCall(fake, "Validate", "ReadOnlySequence<Char>", bytes: false);
    }

    [Test]
    public async Task ValidateAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        var fake = new FakeValidator();
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();

        await Assert
            .That(async () => await fake.ValidateAsync(new ReadOnlyMemory<byte>(Utf8), SystemId, canceled.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(fake.Calls).IsEmpty();
    }

    [Test]
    public async Task Validate_CanceledToken_ThrowsOperationCanceledException()
    {
        var fake = new FakeValidator();
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();

        await Assert
            .That(() => fake.Validate(new ReadOnlySpan<byte>(Utf8), SystemId, canceled.Token))
            .Throws<OperationCanceledException>();
    }
}
