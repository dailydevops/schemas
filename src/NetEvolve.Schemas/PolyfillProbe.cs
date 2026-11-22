namespace NetEvolve.Schemas;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Arguments;

/// <summary>Compile-time probe proving the polyfilled API surface on every target framework.</summary>
internal static class PolyfillProbe
{
    internal static int Sum(ReadOnlySpan<int> values)
    {
        var sum = 0;
        for (var i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }

        return sum;
    }

    internal static int RequirePositive(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return value;
    }

    internal static ValueTask<int> SumAsync(int a, int b) => new(a + b);

    internal static async IAsyncEnumerable<int> RangeAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return await SumAsync(i, 0).ConfigureAwait(false);
        }
    }
}
