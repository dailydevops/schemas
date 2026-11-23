#if !NET6_0_OR_GREATER
#pragma warning disable IDE0130, CA1707, CA1062, CA2007, CA1307, MA0048, MA0004, CS1591, VSTHRD003
namespace System;

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

internal static class Net6Polyfills
{
    public static string ReplaceLineEndings(this string value, string replacement) =>
        value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", replacement);

    public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cancelled = new TaskCompletionSource<T>();
        using (cancellationToken.Register(() => cancelled.TrySetCanceled(cancellationToken)))
        {
            return await await Task.WhenAny(task, cancelled.Task);
        }
    }

    public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
    {
        var buffer = new List<T>(size);
        foreach (var item in source)
        {
            buffer.Add(item);
            if (buffer.Count == size)
            {
                yield return [.. buffer];
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            yield return [.. buffer];
        }
    }

    public static StringBuilder Append(
        this StringBuilder builder,
        IFormatProvider? provider,
        FormattableString value
    ) => builder.Append(value.ToString(provider));
}
#endif
