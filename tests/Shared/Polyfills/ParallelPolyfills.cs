#pragma warning disable IDE0130, CA2007, MA0048, MA0004, CS1591
namespace System.Threading.Tasks;

using System.Collections.Generic;

// Parallel is a static class and cannot be extended: call ParallelPolyfills.ForEachAsync (BCL on net6.0+).
internal static class ParallelPolyfills
{
    public static Task ForEachAsync<T>(IEnumerable<T> source, Func<T, CancellationToken, ValueTask> body) =>
        ForEachAsync(source, new ParallelOptions(), body);

    public static Task ForEachAsync<T>(
        IEnumerable<T> source,
        ParallelOptions options,
        Func<T, CancellationToken, ValueTask> body
    ) => Parallel.ForEachAsync(source, options, body);
}
