namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>A slot binder that records the slots it was asked to bind and fails the ones a test names; it reports nothing, as a binder that already reported once would not.</summary>
internal sealed class RecordingSlotBinder : ISlotBinder
{
    internal List<int> Bound { get; } = [];

    internal HashSet<int> Failing { get; } = [];

    public QueryStatus EnsureBound(ReadOnlySpan<int> slots)
    {
        var status = QueryStatus.Ok;
        for (var i = 0; i < slots.Length; i++)
        {
            Bound.Add(slots[i]);
            if (Failing.Contains(slots[i]))
            {
                status = QueryStatus.Problem;
            }
        }

        return status;
    }
}
