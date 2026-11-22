namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>
/// <c>xsl:if</c> (XSLT 2.0 section 8.1) and, as a branch, <c>xsl:when</c>: the body runs when the effective boolean value of the test
/// is true. The test and the body are evaluated with the focus of the frame.
/// </summary>
internal sealed class XsltIfExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly QueryExpression _test;
    private readonly int[] _slots;

    internal XsltIfExecutable(QueryExpression test, XsltBodyExecutable body, int slotEnd)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(body);

        _test = test;
        Body = body;
        SlotEnd = slotEnd;
        _slots = new SlotSet().Add(test).Add(body).ToArray();
    }

    /// <summary>Gets the body.</summary>
    internal XsltBodyExecutable Body { get; }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _slots;

    /// <inheritdoc />
    public int SlotEnd { get; }

    /// <summary>Evaluates the test.</summary>
    /// <param name="frame">The frame.</param>
    /// <param name="value">The effective boolean value when the status is <see cref="QueryStatus.Ok"/>.</param>
    /// <returns>The status of the evaluation.</returns>
    internal QueryStatus TryEvaluateTest(XsltFrame frame, out bool value)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return frame.Run.TryEvaluateBoolean(_test, frame.Focus, out value);
    }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        if (TryEvaluateTest(frame, out var value) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        return value ? Body.TryExecute(frame, sink) : QueryStatus.Ok;
    }
}
