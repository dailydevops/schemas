namespace NetEvolve.Schemas.Xsd.Validation.Tree;

using System;
using System.Collections.Generic;

/// <summary>
/// The element ordinal at which each issue of a tree run was reported, in the order of the issues of the result: the bridge from an
/// issue to its node (XSD-79, #361). Ordinal zero stands for the document, an ordinal of one or more for the n-th element start event.
/// </summary>
/// <remarks>Not thread-safe; one instance serves one run.</remarks>
internal sealed class TreeIssueLog
{
    private readonly List<int> _ordinals = [];

    /// <summary>Gets the number of issues logged.</summary>
    internal int Count => _ordinals.Count;

    /// <summary>Gets the ordinal of an issue.</summary>
    /// <param name="index">The index of the issue in the result.</param>
    /// <returns>The ordinal.</returns>
    internal int this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _ordinals.Count);

            return _ordinals[index];
        }
    }

    /// <summary>Logs the ordinal of the next issue.</summary>
    /// <param name="ordinal">The ordinal, zero for the document.</param>
    internal void Add(int ordinal)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ordinal, 0);

        _ordinals.Add(ordinal);
    }
}
