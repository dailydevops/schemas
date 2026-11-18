namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

/// <summary>A pool that records every rented and returned array.</summary>
internal sealed class TrackingPool<T> : ArrayPool<T>
{
    internal List<T[]> Rented { get; } = [];

    internal List<T[]> Returned { get; } = [];

    /// <summary>Gets a value indicating whether something was rented and every rented array came back exactly once.</summary>
    internal bool AllReturned =>
        Rented.Count > 0
        && Returned.Count == Rented.Count
        && Rented.All(rented => Returned.Count(returned => ReferenceEquals(returned, rented)) == 1);

    public override T[] Rent(int minimumLength)
    {
        var array = new T[minimumLength];
        Rented.Add(array);
        return array;
    }

    public override void Return(T[] array, bool clearArray = false) => Returned.Add(array);
}
