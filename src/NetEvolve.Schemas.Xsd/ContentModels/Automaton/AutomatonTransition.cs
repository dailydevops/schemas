namespace NetEvolve.Schemas.Xsd.ContentModels.Automaton;

using System.Runtime.InteropServices;

/// <summary>A move from one position to a target position with the actions on its path.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct AutomatonTransition
{
    internal AutomatonTransition(int target, int actionStart, int actionCount)
    {
        Target = target;
        ActionStart = actionStart;
        ActionCount = actionCount;
    }

    public int Target { get; }

    public int ActionStart { get; }

    public int ActionCount { get; }
}
