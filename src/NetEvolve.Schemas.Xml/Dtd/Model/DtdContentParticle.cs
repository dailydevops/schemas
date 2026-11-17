namespace NetEvolve.Schemas.Xml.Dtd.Model;

using System;
using System.Collections.Generic;

/// <summary>A node of an element content model: a name, or a sequence or choice of particles.</summary>
internal sealed class DtdContentParticle
{
    internal DtdContentParticle(
        DtdParticleKind kind,
        string? name,
        DtdOccurrence occurrence,
        IReadOnlyList<DtdContentParticle>? items
    )
    {
        Kind = kind;
        Name = name;
        Occurrence = occurrence;
        Items = items ?? Array.Empty<DtdContentParticle>();
    }

    /// <summary>Gets the kind.</summary>
    internal DtdParticleKind Kind { get; }

    /// <summary>Gets the element type name of a <see cref="DtdParticleKind.Name"/> particle, otherwise <see langword="null"/>.</summary>
    internal string? Name { get; }

    /// <summary>Gets the occurrence indicator.</summary>
    internal DtdOccurrence Occurrence { get; }

    /// <summary>Gets the members of a sequence or choice, empty for a name.</summary>
    internal IReadOnlyList<DtdContentParticle> Items { get; }
}
