namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System.Runtime.InteropServices;

/// <summary>Enumerates the items of a sequence without allocating.</summary>
[StructLayout(LayoutKind.Auto)]
public ref struct XdmSequenceEnumerator
{
    private readonly XdmItem[] _items;
    private int _index;

    internal XdmSequenceEnumerator(XdmItem[] items)
    {
        _items = items;
        _index = -1;
    }

    /// <summary>Gets the current item.</summary>
    public readonly XdmItem Current => _items[_index];

    /// <summary>Advances to the next item.</summary>
    /// <returns><see langword="true"/> when there is one.</returns>
    public bool MoveNext() => ++_index < _items.Length;
}
