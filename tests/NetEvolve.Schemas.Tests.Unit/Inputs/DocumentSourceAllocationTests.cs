namespace NetEvolve.Schemas.Tests.Unit.Inputs;

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;

public class DocumentSourceAllocationTests
{
    private static readonly byte[] Bytes = new byte[4096];
    private static readonly char[] Chars = new char[4096];
    private static readonly SilentValidator Validator = new();
    private static bool _sink;

    private static bool Completed(ValueTask<ValidationResult> task) => task.IsCompletedSuccessfully;

    private static ValidationResult Sync(ReadOnlyMemory<byte> memory) => Validator.Validate(memory.Span);

    private static ValidationResult Sync(ReadOnlyMemory<char> memory) => Validator.Validate(memory.Span);

    [Test]
    public async Task ReadOnlyMemoryOfByte_ReachesTheCoreWithoutACopy(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var memory = new ReadOnlyMemory<byte>(Bytes);

        AllocationAssert.NoAllocation(() => _sink = Completed(Validator.ValidateAsync(memory)));
        AllocationAssert.NoAllocation(() => _sink = Sync(memory).IsValid);

        await Assert.That(_sink).IsTrue();
    }

    [Test]
    public async Task ReadOnlyMemoryOfChar_ReachesTheCoreWithoutACopy(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var memory = new ReadOnlyMemory<char>(Chars);

        AllocationAssert.NoAllocation(() => _sink = Completed(Validator.ValidateAsync(memory)));
        AllocationAssert.NoAllocation(() => _sink = Sync(memory).IsValid);

        await Assert.That(_sink).IsTrue();
    }

    [Test]
    public async Task SingleSegmentSequences_ReachTheCoreWithoutACopy(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = new ReadOnlySequence<byte>(Bytes);
        var chars = new ReadOnlySequence<char>(Chars);

        AllocationAssert.NoAllocation(() => _sink = Validator.Validate(bytes).IsValid);
        AllocationAssert.NoAllocation(() => _sink = Validator.Validate(chars).IsValid);
        AllocationAssert.NoAllocation(() => _sink = Completed(Validator.ValidateAsync(bytes)));
        AllocationAssert.NoAllocation(() => _sink = Completed(Validator.ValidateAsync(chars)));

        await Assert.That(_sink).IsTrue();
    }
}
