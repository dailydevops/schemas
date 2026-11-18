namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A pull source over the items of a sequence that must all be nodes, as the operands of union, intersect, and except are (XPath 2.0, 3.3.3).</summary>
[StructLayout(LayoutKind.Auto)]
internal struct XdmSequenceNodeSource : IXPathNodeSource
{
    private readonly XdmSequence _sequence;
    private int _index;

    internal XdmSequenceNodeSource(in XdmSequence sequence)
    {
        _sequence = sequence;
        _index = 0;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">An item is not a node (XPTY0004).</exception>
    public bool TryGetNext(out XPathNode node)
    {
        if (_index < _sequence.Count)
        {
            if (!_sequence[_index++].TryGetNode(out node))
            {
                throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
            }

            return true;
        }

        node = default;
        return false;
    }
}
