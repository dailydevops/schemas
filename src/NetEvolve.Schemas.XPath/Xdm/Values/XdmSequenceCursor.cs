namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// A forward cursor over a sequence that keeps no item: over a lazy sequence that nobody has read it pulls from the source, so a function that
/// needs only the first items does not drain the rest (#650); over any other sequence it walks the items the sequence holds.
/// </summary>
/// <remarks>Limitation: a lazy source is enumerated again when the sequence is read afterwards, because the cursor keeps nothing.</remarks>
[StructLayout(LayoutKind.Auto)]
internal struct XdmSequenceCursor : IDisposable
{
    private readonly XdmSequence _sequence;
    private readonly IEnumerator<XdmItem>? _source;
    private int _index;

    internal XdmSequenceCursor(in XdmSequence sequence, IEnumerable<XdmItem>? source)
    {
        _sequence = sequence;
        _source = source?.GetEnumerator();
        _index = -1;
    }

    /// <summary>Gets the current item.</summary>
    public readonly XdmItem Current => _source is null ? _sequence[_index] : _source.Current;

    /// <summary>Advances to the next item.</summary>
    /// <returns><see langword="true"/> when there is one.</returns>
    public bool MoveNext()
    {
        if (_source is not null)
        {
            return _source.MoveNext();
        }

        return ++_index < _sequence.Count;
    }

    /// <summary>Releases the enumerator of a lazy source.</summary>
    public readonly void Dispose() => _source?.Dispose();
}
