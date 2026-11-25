namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using System.Buffers;

/// <summary>
/// Hands out arrays of up to <see cref="ArrayLength"/> elements from a small per-thread stack, and larger ones from the shared pool.
/// </summary>
/// <remarks>
/// Almost every list and argument array of a path evaluation holds a handful of items and lives for the length of one call, and the
/// shared pool costs more than such an array is worth: it looks up the processor on every rent and return. The stack is not shared, so
/// a rent and a return take no lock. An array that does not fit back into the stack is dropped; a thread that rents more small arrays
/// at once than the stack holds allocates the rest.
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
internal static class SmallArrayCache<T>
{
    private const int ArrayLength = 16;
    private const int Capacity = 8;

    [ThreadStatic]
    private static T[]?[]? s_arrays;

    [ThreadStatic]
    private static int s_count;

    /// <summary>Rents an array.</summary>
    /// <param name="minimumLength">The least number of elements the array holds.</param>
    /// <returns>An array that is empty: every element is the default value.</returns>
    public static T[] Rent(int minimumLength)
    {
        if (minimumLength > ArrayLength)
        {
            return ArrayPool<T>.Shared.Rent(minimumLength);
        }

        if (s_count > 0)
        {
            var arrays = s_arrays!;
            var array = arrays[--s_count]!;
            arrays[s_count] = null;
            return array;
        }

        return new T[ArrayLength];
    }

    /// <summary>Returns an array that <see cref="Rent"/> gave out.</summary>
    /// <param name="array">The array.</param>
    /// <param name="used">The number of leading elements that may hold values; they are cleared, so the array keeps nothing alive.</param>
    public static void Return(T[] array, int used)
    {
        if (array.Length != ArrayLength)
        {
            ArrayPool<T>.Shared.Return(array, clearArray: true);
            return;
        }

        if (s_count < Capacity)
        {
            Array.Clear(array, 0, used);
            (s_arrays ??= new T[Capacity][])[s_count++] = array;
        }
    }
}
