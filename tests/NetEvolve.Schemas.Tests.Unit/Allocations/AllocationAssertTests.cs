namespace NetEvolve.Schemas.Tests.Unit.Allocations;

using NetEvolve.Schemas.Testing.Allocations;

public class AllocationAssertTests
{
    private static byte[]? _sink;
    private static int _intSink;

    private static void SeededAllocation() => _sink = new byte[128];

    private static readonly int[] Values = [1, 2, 3, 4, 5, 6, 7, 8];

    private static void SpanLoop()
    {
        ReadOnlySpan<int> span = Values;
        var sum = 0;
        for (var i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }

        _intSink = sum;
    }

    [Test]
    public async Task NoAllocation_SeededArrayAllocation_Throws() =>
        await Assert.That(() => AllocationAssert.NoAllocation(SeededAllocation)).Throws<AllocationAssertionException>();

    [Test]
    public async Task NoAllocation_SpanLoop_Passes()
    {
        AllocationAssert.NoAllocation(SpanLoop);

        await Assert.That(_intSink).IsEqualTo(36);
    }

    [Test]
    public async Task AtMost_ExactSeededBytes_Passes()
    {
        var bytes = AllocationAssert.Measure(SeededAllocation);

        AllocationAssert.AtMost(bytes, SeededAllocation);

        await Assert.That(bytes).IsGreaterThanOrEqualTo(128);
    }

    [Test]
    public async Task AtMost_OneByteBelowSeededBytes_Throws()
    {
        var bytes = AllocationAssert.Measure(SeededAllocation);

        await Assert
            .That(() => AllocationAssert.AtMost(bytes - 1, SeededAllocation))
            .Throws<AllocationAssertionException>();
    }

    [Test]
    public async Task Measure_MeterReturnsNegative_ThrowsUnavailable() =>
        await Assert
            .That(() => AllocationAssert.Measure(SpanLoop, allocatedBytes: static () => -1))
            .Throws<AllocationMeasurementUnavailableException>();

    [Test]
    public async Task NoAllocation_MeterNotSupported_ThrowsUnavailable() =>
        await Assert
            .That(() =>
                AllocationAssert.NoAllocation(SpanLoop, allocatedBytes: static () => throw new NotSupportedException())
            )
            .Throws<AllocationMeasurementUnavailableException>();
}
