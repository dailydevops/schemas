namespace NetEvolve.Schemas.Xml.Tests.Unit.Buffers;

using System;
using System.Buffers;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Buffers;

public class PooledBufferTests
{
    [Test]
    public async Task Append_BeyondCapacity_GrowsAndKeepsEarlierContent()
    {
        var pool = new TrackingPool<char>();
        using var buffer = new PooledBuffer<char>(4, pool);

        buffer.Append("abcd".AsSpan());
        buffer.Append('e');
        buffer.Append("fghij".AsSpan());

        await Assert.That(buffer.Capacity).IsGreaterThanOrEqualTo(10);
        await Assert.That(buffer.Length).IsEqualTo(10);
        await Assert.That(buffer.WrittenSpan.ToString()).IsEqualTo("abcdefghij");
    }

    [Test]
    public async Task Append_Bytes_GrowsAndKeepsEarlierContent()
    {
        var pool = new TrackingPool<byte>();
        using var buffer = new PooledBuffer<byte>(2, pool);

        buffer.Append([1, 2, 3]);
        buffer.Append(4);

        await Assert.That(buffer.WrittenSpan.ToArray()).IsEquivalentTo(new byte[] { 1, 2, 3, 4 });
    }

    [Test]
    public async Task EnsureCapacity_ProvidesRequestedFreeSpace()
    {
        var pool = new TrackingPool<char>();
        using var buffer = new PooledBuffer<char>(4, pool);
        buffer.Append("ab".AsSpan());

        buffer.EnsureCapacity(100);
        "xyz".AsSpan().CopyTo(buffer.GetFreeSpan());
        buffer.Advance(3);

        await Assert.That(buffer.Capacity - buffer.Length).IsGreaterThanOrEqualTo(97);
        await Assert.That(buffer.WrittenSpan.ToString()).IsEqualTo("abxyz");
    }

    [Test]
    public async Task Clear_ResetsLengthAndKeepsCapacity()
    {
        var pool = new TrackingPool<char>();
        using var buffer = new PooledBuffer<char>(4, pool);
        buffer.Append("abc".AsSpan());
        var capacity = buffer.Capacity;

        buffer.Clear();

        await Assert.That(buffer.Length).IsEqualTo(0);
        await Assert.That(buffer.Capacity).IsEqualTo(capacity);
    }

    [Test]
    public async Task Dispose_ReturnsExactlyOneArray()
    {
        var pool = new TrackingPool<char>();
        var buffer = new PooledBuffer<char>(4, pool);
        buffer.Append('a');

        buffer.Dispose();

        await Assert.That(pool.Rented).IsEqualTo(1);
        await Assert.That(pool.Returned).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Growth_ReturnsOldArray()
    {
        var pool = new TrackingPool<char>();
        var buffer = new PooledBuffer<char>(4, pool);
        var first = pool.LastRented!;

        buffer.Append("abcdefgh".AsSpan());

        await Assert.That(pool.Rented).IsEqualTo(2);
        await Assert.That(pool.Returned).Count().IsEqualTo(1);
        await Assert.That(ReferenceEquals(pool.Returned[0], first)).IsTrue();

        buffer.Dispose();

        await Assert.That(pool.Returned).Count().IsEqualTo(2);
        await Assert.That(ReferenceEquals(pool.Returned[1], pool.LastRented)).IsTrue();
    }

    [Test]
    public async Task Dispose_Twice_DoesNotReturnAgain()
    {
        var pool = new TrackingPool<char>();
        var buffer = new PooledBuffer<char>(4, pool);

        buffer.Dispose();
        buffer.Dispose();

        await Assert.That(pool.Returned).Count().IsEqualTo(1);
    }

    [Test]
    public async Task AccessAfterDispose_ThrowsObjectDisposedException()
    {
        var pool = new TrackingPool<char>();
        var buffer = new PooledBuffer<char>(4, pool);
        buffer.Dispose();

        await Assert.That(() => buffer.Append('a')).Throws<ObjectDisposedException>();
        await Assert.That(() => buffer.Append("a".AsSpan())).Throws<ObjectDisposedException>();
        await Assert.That(() => buffer.EnsureCapacity(1)).Throws<ObjectDisposedException>();
        await Assert.That(() => buffer.GetFreeSpan().Length).Throws<ObjectDisposedException>();
        await Assert.That(() => buffer.Advance(0)).Throws<ObjectDisposedException>();
        await Assert.That(() => buffer.WrittenSpan.Length).Throws<ObjectDisposedException>();
        await Assert.That(() => buffer.Length).Throws<ObjectDisposedException>();
        await Assert.That(() => buffer.Capacity).Throws<ObjectDisposedException>();
        await Assert.That(buffer.Clear).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task Constructor_NegativeCapacity_Throws()
    {
        var pool = new TrackingPool<char>();

        await Assert.That(() => new PooledBuffer<char>(-1, pool)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Advance_BeyondCapacity_Throws()
    {
        var pool = new TrackingPool<char>();
        using var buffer = new PooledBuffer<char>(4, pool);

        await Assert.That(() => buffer.Advance(buffer.Capacity + 1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task EnsureCapacity_NegativeCount_Throws()
    {
        var pool = new TrackingPool<char>();
        using var buffer = new PooledBuffer<char>(4, pool);

        await Assert.That(() => buffer.EnsureCapacity(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task EnsureCapacity_BeyondMaximumArrayLength_Throws()
    {
        var pool = new TrackingPool<char>();
        using var buffer = new PooledBuffer<char>(4, pool);
        buffer.Append("ab".AsSpan());

        await Assert.That(() => buffer.EnsureCapacity(int.MaxValue)).Throws<InvalidOperationException>();
        await Assert.That(buffer.WrittenSpan.ToString()).IsEqualTo("ab");
    }

    private sealed class TrackingPool<T> : ArrayPool<T>
    {
        public int Rented { get; private set; }

        public T[]? LastRented { get; private set; }

        public List<T[]> Returned { get; } = [];

        public override T[] Rent(int minimumLength)
        {
            Rented++;
            return LastRented = new T[minimumLength];
        }

        public override void Return(T[] array, bool clearArray = false) => Returned.Add(array);
    }
}
