namespace NetEvolve.Schemas.Xsd.Contracts;

using System.Runtime.InteropServices;

/// <summary>The run state of one element frame: the current position and where its counters start.</summary>
/// <remarks>The counters live in one growable array owned by the session; the state holds an index, never a reference into it.</remarks>
[StructLayout(LayoutKind.Auto)]
internal struct ContentModelState
{
    /// <summary>The current position, <c>-1</c> before the first child.</summary>
    public int Position;

    /// <summary>The index of the first counter slot of this frame.</summary>
    public int CounterOffset;
}
