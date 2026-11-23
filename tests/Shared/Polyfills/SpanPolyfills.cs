#if !NET8_0_OR_GREATER
#pragma warning disable IDE0130, MA0048, CS1591
namespace System;

internal static class SpanPolyfills
{
    public static int CommonPrefixLength<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other)
        where T : IEquatable<T>
    {
        var max = Math.Min(span.Length, other.Length);
        var i = 0;
        while (i < max && span[i].Equals(other[i]))
        {
            i++;
        }

        return i;
    }
}
#endif
