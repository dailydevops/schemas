namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// A compiled sequence constructor: its instructions run in order into one sink, and the first problem ends it. It reports the
/// slots its instructions read, for the function that binds them at entry.
/// </summary>
internal sealed class XsltBodyExecutable : XsltExecutable, IXsltSlotReader
{
    private readonly XsltExecutable[] _instructions;
    private readonly int[] _referencedSlots;

    internal XsltBodyExecutable(XsltExecutable[] instructions)
    {
        Argument.ThrowIfNull(instructions);

        _instructions = instructions;
        var slots = new List<int>();
        foreach (var instruction in instructions)
        {
            if (instruction is IXsltSlotReader reader)
            {
                foreach (var slot in reader.ReferencedSlots)
                {
                    if (!slots.Contains(slot))
                    {
                        slots.Add(slot);
                    }
                }
            }
        }

        slots.Sort();
        _referencedSlots = slots.ToArray();
    }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _referencedSlots;

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        for (var i = 0; i < _instructions.Length; i++)
        {
            if (_instructions[i].TryExecute(frame, sink) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }
        }

        return QueryStatus.Ok;
    }
}
