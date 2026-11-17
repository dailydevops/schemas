namespace NetEvolve.Schemas.Xsd.Elements;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// A particle (Structures section 3.9): an occurrence range over a term, which is a local element declaration, a model group,
/// a wildcard, or, once the resolver bound it, a global element declaration or a model group definition that the particle
/// refers to by name.
/// </summary>
/// <remarks>
/// Limitation: an occurrence above <see cref="long.MaxValue"/> is read as <see cref="Unbounded"/> (design #146 DP-7).
/// </remarks>
internal sealed class Particle : IReferencingComponent
{
    /// <summary>The <c>maxOccurs</c> value that stands for <c>unbounded</c>.</summary>
    public const long Unbounded = long.MaxValue;

    private readonly XsdReference[] _references;

    private Particle(
        DocumentLocation location,
        long minOccurs,
        long maxOccurs,
        IXsdComponent? term,
        XsdReference[] references
    )
    {
        Location = location;
        MinOccurs = minOccurs;
        MaxOccurs = maxOccurs;
        Term = term;
        _references = references;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.Particle;

    /// <inheritdoc />
    public XPathQName Name => default;

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <inheritdoc />
    public int ReferenceCount => _references.Length;

    /// <summary>Gets the minimum occurrence.</summary>
    public long MinOccurs { get; }

    /// <summary>Gets the maximum occurrence, <see cref="Unbounded"/> for unbounded.</summary>
    public long MaxOccurs { get; }

    /// <summary>Gets the term; <see langword="null"/> while a by-name reference is unresolved.</summary>
    public IXsdComponent? Term { get; private set; }

    /// <inheritdoc />
    public XsdReference GetReference(int index) => _references[index];

    /// <inheritdoc />
    public void Bind(int index, IXsdComponent target)
    {
        Argument.ThrowIfNull(target);

        Term = target;
    }

    /// <summary>Creates a particle over a term that is part of the particle itself.</summary>
    /// <param name="location">The location of the particle.</param>
    /// <param name="minOccurs">The minimum occurrence.</param>
    /// <param name="maxOccurs">The maximum occurrence.</param>
    /// <param name="term">A local element declaration, a model group, or a wildcard.</param>
    /// <returns>The particle.</returns>
    internal static Particle Create(DocumentLocation location, long minOccurs, long maxOccurs, IXsdComponent term)
    {
        Argument.ThrowIfNull(term);

        return new(location, minOccurs, maxOccurs, term, []);
    }

    /// <summary>Creates a particle that refers to a global element declaration or a model group definition by name.</summary>
    /// <param name="location">The location of the particle.</param>
    /// <param name="minOccurs">The minimum occurrence.</param>
    /// <param name="maxOccurs">The maximum occurrence.</param>
    /// <param name="space">The symbol space, <see cref="XsdSymbolSpace.Elements"/> or <see cref="XsdSymbolSpace.Groups"/>.</param>
    /// <param name="name">The expanded name of the target.</param>
    /// <returns>The particle.</returns>
    internal static Particle CreateReference(
        DocumentLocation location,
        long minOccurs,
        long maxOccurs,
        XsdSymbolSpace space,
        XPathQName name
    ) =>
        new(
            location,
            minOccurs,
            maxOccurs,
            null,
            // Only a group reference is part of the definition: it forms circular definitions, an element reference does not.
            [new XsdReference(space, name, location, space == XsdSymbolSpace.Groups)]
        );

    /// <summary>Appends every unresolved reference below this particle.</summary>
    /// <param name="sites">The list that receives the references.</param>
    internal void CollectReferences(List<RefSite> sites)
    {
        Argument.ThrowIfNull(sites);

        switch (Term)
        {
            case ElementDeclaration element:
                for (var i = 0; i < element.ReferenceCount; i++)
                {
                    sites.Add(new RefSite(element, i));
                }

                break;
            case ModelGroup group:
                group.CollectReferences(sites);
                break;
            default:
                for (var i = 0; i < _references.Length; i++)
                {
                    sites.Add(new RefSite(this, i));
                }

                break;
        }
    }
}
