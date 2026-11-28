namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>The fallback bodies of an unknown instruction, run in order into the sink; it reports the slots of the bodies.</summary>
internal sealed class XsltFallbackExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly XsltBodyExecutable _bodies;

    internal XsltFallbackExecutable(XsltBodyExecutable bodies, int slotEnd)
    {
        ArgumentNullException.ThrowIfNull(bodies);

        _bodies = bodies;
        SlotEnd = slotEnd;
    }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _bodies.ReferencedSlots;

    /// <inheritdoc />
    public int SlotEnd { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink) => _bodies.TryExecute(frame, sink);
}
