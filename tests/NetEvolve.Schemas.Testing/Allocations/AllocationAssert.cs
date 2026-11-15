namespace NetEvolve.Schemas.Testing.Allocations;

/// <summary>
/// Asserts how many bytes a piece of code allocates on the current thread, using
/// <see cref="GC.GetAllocatedBytesForCurrentThread"/>.
/// </summary>
/// <remarks>
/// The action runs <c>warmups</c> times unmeasured so JIT and tiering do not count, then <c>repeats</c> times
/// measured; the smallest measurement is used so one-off noise cannot cause false failures. If the meter reports a
/// negative value or throws <see cref="NotSupportedException"/>, an
/// <see cref="AllocationMeasurementUnavailableException"/> is thrown. Measurement is per thread: allocations on
/// other threads are not seen.
/// </remarks>
public static class AllocationAssert
{
    private const int DefaultWarmups = 5;
    private const int DefaultRepeats = 5;

    /// <summary>Asserts that <paramref name="action"/> allocates no bytes.</summary>
    /// <param name="action">The code under test.</param>
    /// <param name="warmups">Unmeasured runs before measuring.</param>
    /// <param name="repeats">Measured runs; the smallest result counts.</param>
    public static void NoAllocation(Action action, int warmups = DefaultWarmups, int repeats = DefaultRepeats) =>
        AtMost(0, action, warmups, repeats);

    /// <summary>Asserts that <paramref name="action"/> allocates at most <paramref name="maxBytes"/> bytes per run.</summary>
    /// <param name="maxBytes">The inclusive upper bound.</param>
    /// <param name="action">The code under test.</param>
    /// <param name="warmups">Unmeasured runs before measuring.</param>
    /// <param name="repeats">Measured runs; the smallest result counts.</param>
    /// <param name="allocatedBytes">Optional meter replacing <see cref="GC.GetAllocatedBytesForCurrentThread"/>; a negative value means unavailable.</param>
    public static void AtMost(
        long maxBytes,
        Action action,
        int warmups = DefaultWarmups,
        int repeats = DefaultRepeats,
        Func<long>? allocatedBytes = null
    )
    {
        var measured = Measure(action, warmups, repeats, allocatedBytes);
        if (measured > maxBytes)
        {
            throw new AllocationAssertionException(
                $"Expected at most {maxBytes} allocated bytes per run, but measured {measured}."
            );
        }
    }

    /// <summary>Returns the smallest number of bytes <paramref name="action"/> allocates per run.</summary>
    /// <param name="action">The code under test.</param>
    /// <param name="warmups">Unmeasured runs before measuring.</param>
    /// <param name="repeats">Measured runs; the smallest result counts.</param>
    /// <param name="allocatedBytes">Optional meter replacing <see cref="GC.GetAllocatedBytesForCurrentThread"/>; a negative value means unavailable.</param>
    /// <returns>The allocated bytes per run.</returns>
    public static long Measure(
        Action action,
        int warmups = DefaultWarmups,
        int repeats = DefaultRepeats,
        Func<long>? allocatedBytes = null
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegative(warmups);
        ArgumentOutOfRangeException.ThrowIfLessThan(repeats, 1);

        var meter = allocatedBytes ?? GC.GetAllocatedBytesForCurrentThread;

        for (var i = 0; i < warmups; i++)
        {
            action();
        }

        var smallest = long.MaxValue;
        for (var i = 0; i < repeats; i++)
        {
            var before = Read(meter);
            action();
            var after = Read(meter);
            var delta = after - before;
            if (delta < smallest)
            {
                smallest = delta;
            }
        }

        return smallest;
    }

    private static long Read(Func<long> meter)
    {
        long value;
        try
        {
            value = meter();
        }
        catch (NotSupportedException ex)
        {
            throw new AllocationMeasurementUnavailableException(
                "Per-thread allocation measurement is not supported on this runtime.",
                ex
            );
        }

        return value < 0
            ? throw new AllocationMeasurementUnavailableException(
                "Per-thread allocation measurement is unavailable (meter returned a negative value)."
            )
            : value;
    }
}
