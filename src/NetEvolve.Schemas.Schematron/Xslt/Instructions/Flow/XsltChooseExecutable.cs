namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>
/// <c>xsl:choose</c>: runs the body of the first <c>xsl:when</c> whose test is true, and no later test is evaluated; when none is
/// true, the <c>xsl:otherwise</c> body runs, or nothing when there is none.
/// </summary>
internal sealed class XsltChooseExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly XsltIfExecutable[] _branches;
    private readonly XsltBodyExecutable? _otherwise;
    private readonly int[] _slots;

    internal XsltChooseExecutable(XsltIfExecutable[] branches, XsltBodyExecutable? otherwise, int slotEnd)
    {
        ArgumentNullException.ThrowIfNull(branches);

        _branches = branches;
        _otherwise = otherwise;
        SlotEnd = slotEnd;
        var slots = new SlotSet();
        foreach (var branch in branches)
        {
            _ = slots.Add(branch);
        }

        _slots = slots.Add(otherwise).ToArray();
    }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _slots;

    /// <inheritdoc />
    public int SlotEnd { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        for (var i = 0; i < _branches.Length; i++)
        {
            if (_branches[i].TryEvaluateTest(frame, out var value) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            if (value)
            {
                return _branches[i].Body.TryExecute(frame, sink);
            }
        }

        return _otherwise is null ? QueryStatus.Ok : _otherwise.TryExecute(frame, sink);
    }
}
