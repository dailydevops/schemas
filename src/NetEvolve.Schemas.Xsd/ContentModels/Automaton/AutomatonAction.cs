namespace NetEvolve.Schemas.Xsd.ContentModels.Automaton;

using System.Runtime.InteropServices;

/// <summary>One step of the path a transition takes through the counted nodes (design #146 DP-7).</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct AutomatonAction
{
    internal AutomatonAction(AutomatonActionKind kind, int slot, long limit, long cap)
    {
        Kind = kind;
        Slot = slot;
        Limit = limit;
        Cap = cap;
    }

    public AutomatonActionKind Kind { get; }

    public int Slot { get; }

    /// <summary>The minimum for <see cref="AutomatonActionKind.Exit"/>, the maximum for <see cref="AutomatonActionKind.Iterate"/>.</summary>
    public long Limit { get; }

    /// <summary>The value an unbounded counter saturates at, so its set of configurations stays finite.</summary>
    public long Cap { get; }
}
