namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// <c>xsl:processing-instruction</c>: evaluates the target, which must be an NCName other than <c>xml</c> in any case (XTDE0890), and
/// the simple content without its leading white space, and pushes a processing instruction node (XSLT 2.0 section 11.6).
/// </summary>
internal sealed class XsltProcessingInstructionExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private static readonly char[] LeadingWhiteSpace = [' ', '\t', '\r', '\n'];

    private readonly AttributeValueTemplate _name;
    private readonly QueryExpression? _select;
    private readonly XsltBodyExecutable? _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltProcessingInstructionExecutable(
        AttributeValueTemplate name,
        QueryExpression? select,
        XsltBodyExecutable? body,
        DocumentLocation location,
        int slotEnd
    )
    {
        Argument.ThrowIfNull(name);

        _name = name;
        _select = select;
        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        _slots = new SlotSet().Add(name.ReferencedSlots).Add(select).Add(body).ToArray();
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

        if (_name.TryEvaluate(frame, _location, out var written) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        var name = written.Trim();
        if (
            !XsltElementReader.TryParseQName(name, out var prefix, out _)
            || prefix is not null
            || string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase)
        )
        {
            return ConstructionOutput.Error(
                frame,
                _location,
                "XTDE0890",
                $"The target '{name}' of a processing instruction is not an NCName other than xml."
            );
        }

        return SimpleContent.TryEvaluate(frame, _select, _body, " ", _location, out var value) == QueryStatus.Problem
            ? QueryStatus.Problem
            : ConstructionOutput.PushProcessingInstruction(
                frame,
                sink,
                name,
                value.TrimStart(LeadingWhiteSpace),
                _location
            );
    }
}
