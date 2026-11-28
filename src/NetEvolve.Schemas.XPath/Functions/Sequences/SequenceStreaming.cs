namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Reads only as much of an unread lazy sequence as a cardinality check needs (#650).</summary>
internal static class SequenceStreaming
{
    /// <summary>Reads at most two items of a sequence and keeps them, so that a check for one item sees "none", "one", or "more than one".</summary>
    /// <param name="sequence">The sequence.</param>
    /// <returns>A sequence of the first two items; of the first one or none when the source ends earlier.</returns>
    internal static XdmSequence TakeUpToTwo(in XdmSequence sequence)
    {
        using var cursor = sequence.Open();
        if (!cursor.MoveNext())
        {
            return XdmSequence.Empty;
        }

        var first = cursor.Current;
        if (!cursor.MoveNext())
        {
            return XdmSequence.Create(first);
        }

        return XdmSequence.Create([first, cursor.Current]);
    }
}
