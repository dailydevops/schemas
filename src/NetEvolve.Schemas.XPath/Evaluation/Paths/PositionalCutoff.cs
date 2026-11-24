namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
using NetEvolve.Arguments;

/// <summary>
/// A positional predicate that the optimization stage recognized (XPath 2.0, 3.2.2): <c>[N]</c>, <c>[position() le N]</c>, or
/// <c>[last()]</c>. It keeps a range of positions, or the last item, without evaluating the predicate once per item, and tells the
/// walk of the items how many it has to read.
/// </summary>
internal readonly record struct PositionalCutoff
{
    private PositionalCutoff(int first, int last, bool isLast)
    {
        First = first;
        Last = last;
        IsLast = isLast;
    }

    /// <summary>Gets the first position that is kept, one-based.</summary>
    public int First { get; }

    /// <summary>Gets the last position that is kept, <see cref="int.MaxValue"/> for no end; below <see cref="First"/> for no item.</summary>
    public int Last { get; }

    /// <summary>Gets a value indicating whether only the last item is kept, which is <c>[last()]</c>.</summary>
    public bool IsLast { get; }

    /// <summary>Gets the number of items that have to be read before the walk can stop, zero when every item has to be read.</summary>
    public int Budget => IsLast || Last == int.MaxValue ? 0 : Math.Max(Last, 1);

    /// <summary>Creates a cutoff that keeps a range of positions.</summary>
    /// <param name="first">The first position, one-based.</param>
    /// <param name="last">The last position; <see cref="int.MaxValue"/> for no end.</param>
    /// <returns>The cutoff.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="first"/> is below one.</exception>
    public static PositionalCutoff Range(int first, int last)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(first, 1);

        return new PositionalCutoff(first, last, false);
    }

    /// <summary>Creates a cutoff that keeps the last item.</summary>
    /// <returns>The cutoff.</returns>
    public static PositionalCutoff LastItem() => new(1, int.MaxValue, true);

    /// <summary>Keeps the items the cutoff selects.</summary>
    /// <param name="items">The items, in the order that positions count in.</param>
    public void Apply(ref XdmItemList items)
    {
        var count = items.Count;
        if (IsLast)
        {
            if (count > 1)
            {
                var span = items.AsSpan();
                span[0] = span[count - 1];
                items.Truncate(1);
            }

            return;
        }

        var from = First - 1;
        var to = Math.Min(Last, count);
        if (from >= to)
        {
            items.Clear();
            return;
        }

        if (from > 0)
        {
            items.AsSpan().Slice(from, to - from).CopyTo(items.AsSpan());
        }

        items.Truncate(to - from);
    }
}
