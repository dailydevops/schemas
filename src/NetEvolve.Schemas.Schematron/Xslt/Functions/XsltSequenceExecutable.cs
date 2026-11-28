namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary><c>xsl:sequence</c>: evaluates its <see langword="select"/> expression and pushes the items into the sink without copying nodes (XSLT 2.0 section 11.9).</summary>
/// <remarks>
/// The expression is evaluated with the focus of the frame: none in a function body, and the item, position, and size of the
/// iteration inside an <c>xsl:for-each</c> (SCH-82, #255).
/// </remarks>
internal sealed class XsltSequenceExecutable : XsltExecutable, IXsltSlotReader
{
    private readonly QueryExpression _select;

    internal XsltSequenceExecutable(QueryExpression select)
    {
        ArgumentNullException.ThrowIfNull(select);

        _select = select;
    }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _select.ReferencedSlots;

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        return frame.Run.TryEvaluate(_select, frame.Focus, sink);
    }
}
