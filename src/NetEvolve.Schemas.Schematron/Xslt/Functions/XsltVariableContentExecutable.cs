namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// A local <c>xsl:variable</c> with a sequence constructor (XSLT 2.0 section 9.3): without <see langword="as"/> the value is a
/// temporary tree with a document node that holds the constructed nodes, with <see langword="as"/> it is the constructed sequence after
/// the function conversion rules. It produces no output; the value belongs to the activation of the function, which restores the slot.
/// </summary>
/// <remarks>
/// Limitation: the temporary tree is built on the name table of the sink the variable runs in, and the run must hold values
/// (<see cref="IXsltFunctionRun"/>); a variable in a text-only sink or in a run without value hooks is the unsupported construct error.
/// </remarks>
internal sealed class XsltVariableContentExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly XsltBodyExecutable _body;
    private readonly XsltSequenceType? _type;
    private readonly DocumentLocation _location;
    private readonly string _name;
    private readonly int[] _slots;

    internal XsltVariableContentExecutable(
        string name,
        int slot,
        XsltSequenceType? type,
        XsltBodyExecutable body,
        DocumentLocation location,
        int slotEnd
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(slot, 0);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentOutOfRangeException.ThrowIfLessThan(slotEnd, slot + 1);

        _name = name;
        Slot = slot;
        _type = type;
        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        _slots = new SlotSet().Add(body).ToArray();
    }

    /// <summary>Gets the slot of the variable.</summary>
    internal int Slot { get; }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _slots;

    /// <inheritdoc />
    public int SlotEnd { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        if (frame.Run is not IXsltFunctionRun run)
        {
            return ConstructionOutput.Unsupported(
                frame,
                _location,
                "The run cannot hold a temporary tree as the value of a variable."
            );
        }

        // The sink of the instruction is not the target of the value, but its tree builder gives the name table of the run.
        if (!ConstructionOutput.TryBeginTree(frame, sink, _location, out var probe))
        {
            return QueryStatus.Problem;
        }

        var items = new ItemSequenceSink(probe.Names);
        _ = sink.EndTree(probe);
        var status = _type is null
            ? ElementConstruction.ConstructDocument(frame, items, _location, _body)
            : _body.TryExecute(frame, items);
        if (status == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        var value = items.Result;
        if (_type is { } type && !type.TryConvert(in value, out value, out var diagnostic))
        {
            return frame.Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    $"The value of the variable '{_name}' does not match the declared type '{type.Text}' (XTTE0570): {diagnostic.Message}",
                    _location,
                    diagnostic.Code
                )
            );
        }

        run.SetValue(Slot, in value);
        return QueryStatus.Ok;
    }
}
