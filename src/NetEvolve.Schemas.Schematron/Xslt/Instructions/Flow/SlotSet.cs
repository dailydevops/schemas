namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>Collects the variable slots that the expressions and the bodies of one instruction read, for <see cref="IXsltSlotReader"/>.</summary>
internal sealed class SlotSet
{
    private readonly List<int> _slots = [];

    /// <summary>Adds the slots of an expression.</summary>
    /// <param name="expression">The expression, or <see langword="null"/> for none.</param>
    /// <returns>This set.</returns>
    internal SlotSet Add(QueryExpression? expression) => expression is null ? this : Add(expression.ReferencedSlots);

    /// <summary>Adds the slots that a compiled instruction reads, when it reports them.</summary>
    /// <param name="executable">The instruction, or <see langword="null"/> for none.</param>
    /// <returns>This set.</returns>
    internal SlotSet Add(XsltExecutable? executable) =>
        executable is IXsltSlotReader reader ? Add(reader.ReferencedSlots) : this;

    /// <summary>Adds slots.</summary>
    /// <param name="slots">The slots.</param>
    /// <returns>This set.</returns>
    internal SlotSet Add(ReadOnlySpan<int> slots)
    {
        foreach (var slot in slots)
        {
            if (!_slots.Contains(slot))
            {
                _slots.Add(slot);
            }
        }

        return this;
    }

    /// <summary>Gets the slots in ascending order.</summary>
    /// <returns>The slots.</returns>
    internal int[] ToArray()
    {
        _slots.Sort();
        return _slots.ToArray();
    }
}
