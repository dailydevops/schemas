namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary><c>xsl:sequence</c>: evaluates its <see langword="select"/> expression and pushes the items into the sink without copying nodes (XSLT 2.0 section 11.9).</summary>
/// <remarks>
/// Limitation: the frame carries no focus yet (design #150 EP-13 adds it with the evaluation package), so the expression is
/// evaluated without one, which is what a function body needs; an instruction that sets a focus (<c>xsl:for-each</c>, SCH-82) needs
/// the frame to carry it and then changes this call.
/// </remarks>
internal sealed class XsltSequenceExecutable : XsltExecutable, IXsltSlotReader
{
    private readonly QueryExpression _select;

    internal XsltSequenceExecutable(QueryExpression select)
    {
        Argument.ThrowIfNull(select);

        _select = select;
    }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _select.ReferencedSlots;

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        return frame.Run.TryEvaluate(_select, XsltFunctionFocus.None, sink);
    }
}
