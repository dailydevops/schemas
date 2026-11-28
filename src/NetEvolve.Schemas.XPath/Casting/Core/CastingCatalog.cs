namespace NetEvolve.Schemas.XPath.Casting.Core;

using System;
using System.Threading;

/// <summary>
/// Collects the rows of the casting table through registration files: each package that owns rows stages a
/// <c>RegisterRows(CastingTableBuilder)</c> method, and the first read of <see cref="Table"/> freezes the table. There is no shared list
/// of rows. An instance per test keeps the global <see cref="Default"/> untouched.
/// </summary>
/// <remarks>
/// Thread-safe and lock-free, like the function catalog. Limitation: a registration that never runs is not found; the module initializer of its registration file runs it (#123 AP8).
/// Staging order: the function catalog stages the constructor functions with a registration that reads <see cref="Table"/> when the function
/// catalog freezes, so the first read of the function catalog freezes this table too, and <see cref="Stage"/> throws afterwards.
/// </remarks>
internal sealed class CastingCatalog
{
    private State _state = new([], null);

    /// <summary>Gets the catalog that the module initializers of the registration files stage.</summary>
    public static CastingCatalog Default { get; } = new();

    /// <summary>Gets the frozen table; the first read runs every staged registration.</summary>
    /// <exception cref="InvalidOperationException">Two rows register the same source and target type.</exception>
    public CastingTable Table
    {
        get
        {
            while (true)
            {
                var current = Volatile.Read(ref _state);
                if (current.Table is { } frozen)
                {
                    return frozen;
                }

                var builder = new CastingTableBuilder();
                for (var i = 0; i < current.Staged.Length; i++)
                {
                    current.Staged[i](builder);
                }

                // A registration staged in the meantime changes the state, so the freeze runs again over the longer list.
                var next = new State(current.Staged, builder.Build());
                if (ReferenceEquals(Interlocked.CompareExchange(ref _state, next, current), current))
                {
                    return next.Table!;
                }
            }
        }
    }

    /// <summary>Stages a registration.</summary>
    /// <param name="registration">The registration; it runs once, when the table freezes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registration"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The table is frozen.</exception>
    public void Stage(Action<CastingTableBuilder> registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        while (true)
        {
            var current = Volatile.Read(ref _state);
            if (current.Table is not null)
            {
                throw new InvalidOperationException(
                    "The casting table is frozen; a registration must be staged before the first cast."
                );
            }

            var staged = new Action<CastingTableBuilder>[current.Staged.Length + 1];
            Array.Copy(current.Staged, staged, current.Staged.Length);
            staged[current.Staged.Length] = registration;
            if (ReferenceEquals(Interlocked.CompareExchange(ref _state, new State(staged, null), current), current))
            {
                return;
            }
        }
    }

    private sealed class State(Action<CastingTableBuilder>[] staged, CastingTable? table)
    {
        public Action<CastingTableBuilder>[] Staged { get; } = staged;

        public CastingTable? Table { get; } = table;
    }
}
