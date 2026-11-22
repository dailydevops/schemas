namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The content model of one complex type as the validator runs it: the matcher program and, per position, the term that a
/// matched position stands for.
/// </summary>
/// <remarks>
/// <see cref="MatchedParticle.ParticleIndex"/> indexes <see cref="Terms"/>. A program with neither an automaton nor an all
/// group is unusable: its content model exceeded a ceiling when it was compiled, which was reported then.
/// </remarks>
internal sealed class ContentProgram
{
    internal ContentProgram(
        CountingAutomaton? automaton,
        AllGroup? allGroup,
        IReadOnlyList<IXsdComponent> terms,
        int poolIndex
    )
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentOutOfRangeException.ThrowIfLessThan(poolIndex, 0);

        Automaton = automaton;
        AllGroup = allGroup;
        Terms = terms;
        PoolIndex = poolIndex;
    }

    /// <summary>Gets the counting automaton, <see langword="null"/> for an all group or an unusable program.</summary>
    public CountingAutomaton? Automaton { get; }

    /// <summary>Gets the all group matcher, <see langword="null"/> for an automaton or an unusable program.</summary>
    public AllGroup? AllGroup { get; }

    /// <summary>Gets the terms by position: element declarations and wildcards.</summary>
    public IReadOnlyList<IXsdComponent> Terms { get; }

    /// <summary>Gets a value indicating whether the program can be run.</summary>
    public bool IsUsable => Automaton is not null || AllGroup is not null;

    /// <summary>Gets the dense index that a session uses to pool the runs of this program.</summary>
    internal int PoolIndex { get; }
}
