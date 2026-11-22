namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

using NetEvolve.Arguments;

/// <summary>
/// A convex set of values of counters, kept as difference bounds: for every two counters (and the constant zero) an upper
/// bound of their difference. The size of a zone depends on the number of counters, never on their values, so a counter
/// that counts to one billion costs the same as one that counts to two. Immutable; every operation returns a new zone.
/// </summary>
/// <remarks>
/// <para>
/// Variable 0 is the constant zero, counters are numbered from 1. Every zone is kept in closed form (every bound is the
/// tightest one implied by the others), so an operation is exact: guards, assignments, and the increment of one counter keep
/// the set exactly. Only <see cref="Hull"/> and <see cref="Widen"/> over-approximate.
/// </para>
/// <para>Limitation: values stay below 2^60 (the caller refuses larger occurrence bounds), so the sums of a few bounds cannot overflow.</para>
/// </remarks>
internal sealed class CounterZone
{
    /// <summary>The bound that stands for no bound.</summary>
    internal const long Infinity = long.MaxValue;

    private readonly int _size;
    private readonly long[] _bounds;

    private CounterZone(int size, long[] bounds)
    {
        _size = size;
        _bounds = bounds;
    }

    /// <summary>Gets the number of counters.</summary>
    internal int Variables => _size - 1;

    /// <summary>Creates the zone in which every counter is zero.</summary>
    /// <param name="variables">The number of counters.</param>
    /// <returns>The zone.</returns>
    internal static CounterZone Origin(int variables)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variables, 0);

        var size = variables + 1;
        return new(size, new long[size * size]);
    }

    /// <summary>Creates the zone that holds one valuation of the counters.</summary>
    /// <param name="values">The value of each counter, counter 1 first.</param>
    /// <returns>The zone.</returns>
    internal static CounterZone Point(long[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var zone = Origin(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            zone = zone.Assign(i + 1, values[i]);
        }

        return zone;
    }

    /// <summary>Gets the values of the counters when the zone holds only one valuation.</summary>
    /// <param name="values">The value of each counter, counter 1 first.</param>
    /// <returns><see langword="true"/> when every counter has one value.</returns>
    internal bool TryPoint(out long[] values)
    {
        values = new long[Variables];
        for (var v = 1; v < _size; v++)
        {
            if (Minimum(v) != Maximum(v))
            {
                return false;
            }

            values[v - 1] = Minimum(v);
        }

        return true;
    }

    /// <summary>Gets the smallest value of a counter in the zone.</summary>
    /// <param name="variable">The counter, from 1.</param>
    /// <returns>The smallest value.</returns>
    internal long Minimum(int variable)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variable, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(variable, Variables);
        return -_bounds[Index(0, variable)];
    }

    /// <summary>Gets the largest value of a counter in the zone.</summary>
    /// <param name="variable">The counter, from 1.</param>
    /// <returns>The largest value, <see cref="Infinity"/> when it is not bounded.</returns>
    internal long Maximum(int variable)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variable, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(variable, Variables);
        return _bounds[Index(variable, 0)];
    }

    /// <summary>Determines whether a valuation of the counters is in the zone.</summary>
    /// <param name="values">The value of each counter, counter 1 first.</param>
    /// <returns><see langword="true"/> when every bound holds.</returns>
    internal bool Contains(long[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, Variables);

        for (var i = 0; i < _size; i++)
        {
            for (var j = 0; j < _size; j++)
            {
                var bound = _bounds[Index(i, j)];
                if (bound != Infinity && ValueOf(values, i) - ValueOf(values, j) > bound)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>Restricts a counter to at least a value.</summary>
    /// <param name="variable">The counter, from 1.</param>
    /// <param name="value">The lower bound.</param>
    /// <returns>The zone, <see langword="null"/> when it is empty.</returns>
    internal CounterZone? AtLeast(int variable, long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variable, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(variable, Variables);
        return Tighten(0, variable, -value);
    }

    /// <summary>Restricts a counter to at most a value.</summary>
    /// <param name="variable">The counter, from 1.</param>
    /// <param name="value">The upper bound.</param>
    /// <returns>The zone, <see langword="null"/> when it is empty.</returns>
    internal CounterZone? AtMost(int variable, long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variable, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(variable, Variables);
        return Tighten(variable, 0, value);
    }

    /// <summary>Sets a counter to a value and forgets what it was.</summary>
    /// <param name="variable">The counter, from 1.</param>
    /// <param name="value">The new value.</param>
    /// <returns>The zone.</returns>
    internal CounterZone Assign(int variable, long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variable, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(variable, Variables);

        var next = (long[])_bounds.Clone();
        for (var j = 0; j < _size; j++)
        {
            if (j != variable)
            {
                next[Index(variable, j)] = Add(_bounds[Index(0, j)], value);
                next[Index(j, variable)] = Add(_bounds[Index(j, 0)], -value);
            }
        }

        next[Index(variable, variable)] = 0;
        return new(_size, next);
    }

    /// <summary>Adds one to a counter.</summary>
    /// <param name="variable">The counter, from 1.</param>
    /// <returns>The zone.</returns>
    internal CounterZone Increment(int variable)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variable, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(variable, Variables);

        var next = (long[])_bounds.Clone();
        for (var j = 0; j < _size; j++)
        {
            if (j != variable)
            {
                next[Index(variable, j)] = Add(_bounds[Index(variable, j)], 1);
                next[Index(j, variable)] = Add(_bounds[Index(j, variable)], -1);
            }
        }

        return new(_size, next);
    }

    /// <summary>Determines whether every valuation of this zone is in another.</summary>
    /// <param name="other">The other zone with the same counters.</param>
    /// <returns><see langword="true"/> when this zone is contained.</returns>
    internal bool IsSubsetOf(CounterZone other)
    {
        ArgumentNullException.ThrowIfNull(other);

        for (var i = 0; i < _bounds.Length; i++)
        {
            if (_bounds[i] > other._bounds[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Computes the smallest zone that holds both.</summary>
    /// <param name="other">The other zone with the same counters.</param>
    /// <returns>The hull.</returns>
    internal CounterZone Hull(CounterZone other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var next = new long[_bounds.Length];
        for (var i = 0; i < next.Length; i++)
        {
            next[i] = _bounds[i] > other._bounds[i] ? _bounds[i] : other._bounds[i];
        }

        return new(_size, next);
    }

    /// <summary>
    /// Widens this zone towards a newer one: a bound that the newer zone exceeds is given up, then every counter is kept
    /// between zero and its largest value. Repeated widening gives up each bound once, so it ends.
    /// </summary>
    /// <param name="newer">The zone that is not inside this one.</param>
    /// <param name="caps">The largest value of each counter, counter 1 first.</param>
    /// <returns>The widened zone.</returns>
    internal CounterZone Widen(CounterZone newer, long[] caps)
    {
        ArgumentNullException.ThrowIfNull(newer);
        ArgumentNullException.ThrowIfNull(caps);
        ArgumentOutOfRangeException.ThrowIfNotEqual(caps.Length, Variables);

        var next = new long[_bounds.Length];
        for (var i = 0; i < next.Length; i++)
        {
            next[i] = newer._bounds[i] <= _bounds[i] ? _bounds[i] : Infinity;
        }

        var closed = Close(_size, next);
        for (var v = 1; v < _size; v++)
        {
            // The zone holds this zone, so it is not empty and the bounds below always apply.
            closed = closed.Tighten(0, v, 0)!.Tighten(v, 0, caps[v - 1])!;
        }

        return closed;
    }

    private static long Add(long left, long right) => left == Infinity || right == Infinity ? Infinity : left + right;

    private static long ValueOf(long[] values, int variable) => variable == 0 ? 0 : values[variable - 1];

    // Floyd and Warshall: the tightest bounds that follow from the others.
    private static CounterZone Close(int size, long[] bounds)
    {
        for (var k = 0; k < size; k++)
        {
            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j < size; j++)
                {
                    var through = Add(bounds[(i * size) + k], bounds[(k * size) + j]);
                    if (through < bounds[(i * size) + j])
                    {
                        bounds[(i * size) + j] = through;
                    }
                }
            }
        }

        return new(size, bounds);
    }

    private int Index(int row, int column) => (row * _size) + column;

    // Adds the bound x(left) - x(right) <= bound to a closed zone and closes it again through the new bound only.
    private CounterZone? Tighten(int left, int right, long bound)
    {
        if (bound >= _bounds[Index(left, right)])
        {
            return this;
        }

        if (Add(_bounds[Index(right, left)], bound) < 0)
        {
            return null;
        }

        var next = (long[])_bounds.Clone();
        for (var i = 0; i < _size; i++)
        {
            for (var j = 0; j < _size; j++)
            {
                var through = Add(Add(_bounds[Index(i, left)], bound), _bounds[Index(right, j)]);
                if (through < next[Index(i, j)])
                {
                    next[Index(i, j)] = through;
                }
            }
        }

        return new(_size, next);
    }
}
