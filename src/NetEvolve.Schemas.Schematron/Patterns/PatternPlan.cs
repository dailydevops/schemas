namespace NetEvolve.Schemas.Schematron.Patterns;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>A compiled pattern: the prefilter data of the seam plus the alternatives the matcher runs.</summary>
internal sealed class PatternPlan : CompiledPattern
{
    private readonly int[] _referencedSlots;

    internal PatternPlan(
        QueryNodeKinds nodeKinds,
        XPathQName[] candidateNames,
        PatternAlternative[] alternatives,
        decimal[] priorities,
        int[] referencedSlots
    )
        : base(nodeKinds, candidateNames)
    {
        Alternatives = alternatives;
        Priorities = priorities;
        _referencedSlots = referencedSlots;
    }

    internal PatternAlternative[] Alternatives { get; }

    /// <summary>Gets the default priority of every alternative, in pattern order.</summary>
    internal decimal[] Priorities { get; }

    /// <inheritdoc />
    internal override ReadOnlySpan<int> ReferencedSlots => _referencedSlots;
}
