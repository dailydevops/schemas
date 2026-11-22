namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>
/// <c>xsl:message</c>: builds the message into a pooled text sink, sends it to the diagnostic channel of the frame, and fails with
/// the coded error XTMM9000 when <c>terminate</c> is yes. The result of the instruction is the empty sequence.
/// </summary>
internal sealed class XsltMessageExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private const string TerminateConstraint = "XTMM9000";

    private readonly QueryExpression? _select;
    private readonly XsltBodyExecutable _body;
    private readonly bool _terminate;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltMessageExecutable(
        QueryExpression? select,
        XsltBodyExecutable body,
        bool terminate,
        int slotEnd,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(body);

        _select = select;
        _body = body;
        _terminate = terminate;
        _location = location;
        SlotEnd = slotEnd;
        _slots = new SlotSet().Add(select).Add(body).ToArray();
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

        using var text = new TextSink(frame.MaxMessageLength);
        if (_select is not null && frame.Run.TryEvaluate(_select, frame.Focus, text) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        if (_body.TryExecute(frame, text) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        if (text.Failure is { } failure)
        {
            return frame.Fail(new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, failure, _location));
        }

        if (text.IsTruncated)
        {
            return frame.Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationLimitStop,
                    $"The message of xsl:message is longer than the limit of {text.MaxLength} characters.",
                    _location
                )
            );
        }

        var message = text.ToString();
        frame.Messages?.Message(message, _location);
        return _terminate
            ? frame.Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    $"Processing was terminated by xsl:message: {message}",
                    _location,
                    TerminateConstraint
                )
            )
            : QueryStatus.Ok;
    }
}
