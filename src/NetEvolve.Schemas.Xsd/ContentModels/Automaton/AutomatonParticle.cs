namespace NetEvolve.Schemas.Xsd.ContentModels.Automaton;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The input tree of the counting automaton: element particles, wildcard particles, sequences, and choices with their
/// occurrence ranges (Structures 3.8 and 3.9).
/// </summary>
/// <remarks>
/// Limitation: the automaton owns this input tree (design #146 DP-7 gap 1). The <see cref="IContentModelCompiler"/>
/// adapter that builds it from resolved <see cref="IXsdComponent"/> particles is delivered with the component package
/// (XSD-23/XSD-24); until then the automaton is driven by hand-built particles. Name ids are the bound schema name ids
/// of design #146 DP-6. An occurrence of <see cref="Unbounded"/> or more is treated as unbounded; exact larger values
/// stay with the components (DP-7).
/// </remarks>
internal sealed class AutomatonParticle
{
    /// <summary>The <c>maxOccurs</c> value that stands for <c>unbounded</c> (the clamp of DP-7).</summary>
    public const long Unbounded = long.MaxValue;

    private static readonly AutomatonParticle[] NoChildren = [];

    private AutomatonParticle(
        AutomatonParticleKind kind,
        long minOccurs,
        long maxOccurs,
        int[]? nameIds,
        IWildcard? wildcard,
        AutomatonParticle[] children
    )
    {
        Argument.ThrowIfLessThan(minOccurs, 0L);
        Argument.ThrowIfGreaterThan(minOccurs, maxOccurs);

        Kind = kind;
        MinOccurs = minOccurs;
        MaxOccurs = maxOccurs;
        NameIds = nameIds;
        WildcardComponent = wildcard;
        Children = children;
    }

    /// <summary>Gets the kind.</summary>
    public AutomatonParticleKind Kind { get; }

    /// <summary>Gets the minimum occurrence.</summary>
    public long MinOccurs { get; }

    /// <summary>Gets the maximum occurrence, <see cref="Unbounded"/> for unbounded.</summary>
    public long MaxOccurs { get; }

    /// <summary>Gets the schema name ids an element particle accepts (a substitution group closure has several).</summary>
    public int[]? NameIds { get; }

    /// <summary>Gets the wildcard of a wildcard particle.</summary>
    public IWildcard? WildcardComponent { get; }

    /// <summary>Gets the children of a sequence or choice.</summary>
    public AutomatonParticle[] Children { get; }

    /// <summary>Creates an element particle.</summary>
    /// <param name="minOccurs">The minimum occurrence.</param>
    /// <param name="maxOccurs">The maximum occurrence.</param>
    /// <param name="nameIds">The accepted schema name ids, at least one.</param>
    /// <returns>The particle.</returns>
    public static AutomatonParticle Element(long minOccurs, long maxOccurs, params int[] nameIds)
    {
        Argument.ThrowIfNull(nameIds);
        Argument.ThrowIfLessThan(nameIds.Length, 1);

        var sorted = (int[])nameIds.Clone();
        foreach (var id in sorted)
        {
            Argument.ThrowIfLessThan(id, 0);
        }

        System.Array.Sort(sorted);

        return new AutomatonParticle(AutomatonParticleKind.Element, minOccurs, maxOccurs, sorted, null, NoChildren);
    }

    /// <summary>Creates a wildcard particle.</summary>
    /// <param name="minOccurs">The minimum occurrence.</param>
    /// <param name="maxOccurs">The maximum occurrence.</param>
    /// <param name="wildcard">The wildcard seam.</param>
    /// <returns>The particle.</returns>
    public static AutomatonParticle Wildcard(long minOccurs, long maxOccurs, IWildcard wildcard)
    {
        Argument.ThrowIfNull(wildcard);

        return new AutomatonParticle(AutomatonParticleKind.Wildcard, minOccurs, maxOccurs, null, wildcard, NoChildren);
    }

    /// <summary>Creates a sequence.</summary>
    /// <param name="minOccurs">The minimum occurrence.</param>
    /// <param name="maxOccurs">The maximum occurrence.</param>
    /// <param name="children">The particles.</param>
    /// <returns>The particle.</returns>
    public static AutomatonParticle Sequence(long minOccurs, long maxOccurs, params AutomatonParticle[] children)
    {
        ThrowIfAnyNull(children);

        return new AutomatonParticle(AutomatonParticleKind.Sequence, minOccurs, maxOccurs, null, null, children);
    }

    /// <summary>Creates a choice.</summary>
    /// <param name="minOccurs">The minimum occurrence.</param>
    /// <param name="maxOccurs">The maximum occurrence.</param>
    /// <param name="children">The alternatives.</param>
    /// <returns>The particle.</returns>
    public static AutomatonParticle Choice(long minOccurs, long maxOccurs, params AutomatonParticle[] children)
    {
        ThrowIfAnyNull(children);

        return new AutomatonParticle(AutomatonParticleKind.Choice, minOccurs, maxOccurs, null, null, children);
    }

    private static void ThrowIfAnyNull(AutomatonParticle[] children)
    {
        Argument.ThrowIfNull(children);
        foreach (var child in children)
        {
            Argument.ThrowIfNull(child);
        }
    }
}
