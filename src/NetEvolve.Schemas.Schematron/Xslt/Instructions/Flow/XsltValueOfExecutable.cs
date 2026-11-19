namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>
/// <c>xsl:value-of</c>: evaluates the <see langword="select"/> expression, or runs the content, into a <see cref="JoinSink"/> that
/// pushes the joined text into the sink (design #150 EP-8). The content form has no local variable that outlives it.
/// </summary>
internal sealed class XsltValueOfExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly QueryExpression? _select;
    private readonly XsltBodyExecutable? _body;
    private readonly string _separator;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    /// <summary>Initializes a new instance of the <see cref="XsltValueOfExecutable"/> class; exactly one of <paramref name="select"/> and <paramref name="body"/> is set.</summary>
    /// <param name="select">The <see langword="select"/> expression, or <see langword="null"/>.</param>
    /// <param name="body">The content, or <see langword="null"/>.</param>
    /// <param name="separator">The text between two items.</param>
    /// <param name="location">The location of the element, for a problem.</param>
    /// <param name="slotEnd">The first slot above every slot of the content.</param>
    internal XsltValueOfExecutable(
        QueryExpression? select,
        XsltBodyExecutable? body,
        string separator,
        DocumentLocation location,
        int slotEnd
    )
    {
        Argument.ThrowIfNull(separator);

        _select = select;
        _body = body;
        _separator = separator;
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
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        using var join = new JoinSink(sink, _separator);
        var status = _select is not null
            ? frame.Run.TryEvaluate(_select, frame.Focus, join)
            : _body!.TryExecute(frame, join);
        if (status == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        join.Complete();
        return join.Failure is { } failure
            ? frame.Fail(new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, failure, _location))
            : QueryStatus.Ok;
    }
}
