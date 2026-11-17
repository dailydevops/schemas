namespace NetEvolve.Schemas.Xml.Buffers;

using System;
using System.Buffers;
using NetEvolve.Arguments;

/// <summary>
/// Growable scratch buffer that rents its backing array from an <see cref="ArrayPool{T}"/> and returns it on dispose.
/// Use <c>PooledBuffer&lt;char&gt;</c> for characters and <c>PooledBuffer&lt;byte&gt;</c> for bytes.
/// </summary>
/// <remarks>
/// Not thread-safe. Every member except <see cref="Dispose"/> throws <see cref="ObjectDisposedException"/> after dispose;
/// <see cref="Dispose"/> is idempotent. Limit accounting for growth is out of scope (XML-08).
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
// Decision (#449): ThrowIfDisposed has no NetEvolve.Arguments.Argument equivalent and ObjectDisposedException.ThrowIf does not exist on netstandard; it stays the single explicit throw.
#pragma warning disable CA1513
internal sealed class PooledBuffer<T> : IDisposable
{
    private const int DefaultCapacity = 256;
    private const int MaxArrayLength = 0X7FFFFFC7;

    private readonly ArrayPool<T> _pool;
    private T[]? _array;
    private int _length;

    /// <summary>Initializes a new instance of the <see cref="PooledBuffer{T}"/> class.</summary>
    /// <param name="initialCapacity">The minimum initial capacity.</param>
    /// <param name="pool">The pool to rent from, or <see langword="null"/> for <see cref="ArrayPool{T}.Shared"/>.</param>
    internal PooledBuffer(int initialCapacity = DefaultCapacity, ArrayPool<T>? pool = null)
    {
        Argument.ThrowIfLessThan(initialCapacity, 0);

        _pool = pool ?? ArrayPool<T>.Shared;
        _array = _pool.Rent(initialCapacity);
    }

    /// <summary>Gets the number of written elements.</summary>
    internal int Length
    {
        get
        {
            ThrowIfDisposed();
            return _length;
        }
    }

    /// <summary>Gets the capacity of the current backing array.</summary>
    internal int Capacity => Array().Length;

    /// <summary>Gets the written elements.</summary>
    internal ReadOnlySpan<T> WrittenSpan => new(Array(), 0, _length);

    /// <summary>Gets the written elements as memory; valid until the buffer is changed or disposed.</summary>
    internal ReadOnlyMemory<T> WrittenMemory => new(Array(), 0, _length);

    /// <summary>Appends one element.</summary>
    /// <param name="value">The element.</param>
    internal void Append(T value)
    {
        var array = Array();
        if (_length == array.Length)
        {
            array = Grow(1);
        }

        array[_length++] = value;
    }

    /// <summary>Appends a span of elements.</summary>
    /// <param name="values">The elements.</param>
    internal void Append(ReadOnlySpan<T> values)
    {
        var array = Array();
        var old = array;
        if (array.Length - _length < values.Length)
        {
            array = GrowCore(values.Length);
        }

        values.CopyTo(new Span<T>(array, _length, values.Length));
        _length += values.Length;

        // Return the old array only after the copy: values may alias it.
        if (!ReferenceEquals(old, array))
        {
            _pool.Return(old);
        }
    }

    /// <summary>Ensures at least <paramref name="additionalCount"/> free elements after the written ones.</summary>
    /// <param name="additionalCount">The number of free elements required.</param>
    internal void EnsureCapacity(int additionalCount)
    {
        Argument.ThrowIfLessThan(additionalCount, 0);

        var array = Array();
        if (array.Length - _length < additionalCount)
        {
            _ = Grow(additionalCount);
        }
    }

    /// <summary>Gets the unused tail of the backing array; call <see cref="EnsureCapacity"/> first and <see cref="Advance"/> after writing.</summary>
    /// <returns>The free span.</returns>
    internal Span<T> GetFreeSpan()
    {
        var array = Array();
        return new Span<T>(array, _length, array.Length - _length);
    }

    /// <summary>Marks <paramref name="count"/> elements of the free span as written.</summary>
    /// <param name="count">The number of elements written.</param>
    internal void Advance(int count)
    {
        var array = Array();
        Argument.ThrowIfLessThan(count, 0);
        Argument.ThrowIfGreaterThan(count, array.Length - _length);

        _length += count;
    }

    /// <summary>Resets the length to zero and keeps the backing array.</summary>
    internal void Clear()
    {
        _ = Array();
        _length = 0;
    }

    /// <summary>Returns the backing array to the pool. Subsequent calls are no-ops.</summary>
    public void Dispose()
    {
        var array = _array;
        if (array is null)
        {
            return;
        }

        _array = null;
        _length = 0;
        _pool.Return(array);
    }

    private T[] Array()
    {
        ThrowIfDisposed();
        return _array!;
    }

    private T[] Grow(int additionalCount)
    {
        var old = _array!;
        var grown = GrowCore(additionalCount);
        _pool.Return(old);
        return grown;
    }

    // Swaps in a larger array without returning the old one; the caller returns it.
    private T[] GrowCore(int additionalCount)
    {
        var old = _array!;
        var required = (long)_length + additionalCount;
        if (required > MaxArrayLength)
        {
            throw new InvalidOperationException("The buffer cannot grow beyond the maximum array length.");
        }

        var target = (int)Math.Min(Math.Max(required, (long)old.Length * 2), MaxArrayLength);
        var grown = _pool.Rent(target);
        new ReadOnlySpan<T>(old, 0, _length).CopyTo(grown);
        _array = grown;
        return grown;
    }

    private void ThrowIfDisposed()
    {
        if (_array is null)
        {
            throw new ObjectDisposedException(nameof(PooledBuffer<T>));
        }
    }
}
#pragma warning restore CA1513
