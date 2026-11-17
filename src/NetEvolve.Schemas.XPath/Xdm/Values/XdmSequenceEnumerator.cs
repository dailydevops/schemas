namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System.Runtime.InteropServices;

/// <summary>Enumerates the items of a sequence without allocating.</summary>
[StructLayout(LayoutKind.Auto)]
public ref struct XdmSequenceEnumerator
{
    private readonly XdmSequence _sequence;
    private readonly int _count;
    private int _index;

    internal XdmSequenceEnumerator(XdmSequence sequence)
    {
        _sequence = sequence;
        _count = sequence.Count;
        _index = -1;
    }

    /// <summary>Gets the current item.</summary>
    public readonly XdmItem Current => _sequence[_index];

    /// <summary>Advances to the next item.</summary>
    /// <returns><see langword="true"/> when there is one.</returns>
    public bool MoveNext() => ++_index < _count;
}
