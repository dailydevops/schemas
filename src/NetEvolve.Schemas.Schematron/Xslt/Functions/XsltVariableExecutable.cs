namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// A local <c>xsl:variable</c> with a <see langword="select"/>: binds the value of the expression to its slot when it runs, and produces no output.
/// The value belongs to the activation of the function, which restores the slot when it ends.
/// </summary>
internal sealed class XsltVariableExecutable : XsltExecutable, IXsltSlotReader
{
    private readonly QueryExpression _value;

    internal XsltVariableExecutable(string name, int slot, QueryExpression value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(slot, 0);
        ArgumentNullException.ThrowIfNull(value);

        Name = name;
        Slot = slot;
        _value = value;
    }

    /// <summary>Gets the name as written.</summary>
    internal string Name { get; }

    /// <summary>Gets the slot of the variable.</summary>
    internal int Slot { get; }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _value.ReferencedSlots;

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        return frame.Run.TryBindVariable(Slot, _value, frame.Focus);
    }
}
