namespace NetEvolve.Schemas.Xsd.Elements;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// A model group (Structures section 3.8): a compositor over particles. It is never registered; the references of everything
/// nested in it are reached through the component that owns it, which the resolver binds exactly once.
/// </summary>
internal sealed class ModelGroup : IReferencingComponent
{
    private readonly RefSite[] _sites;

    private ModelGroup(
        ModelGroupCompositor compositor,
        DocumentLocation location,
        Particle[] particles,
        RefSite[] sites
    )
    {
        Compositor = compositor;
        Location = location;
        Particles = particles;
        _sites = sites;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.ModelGroup;

    /// <inheritdoc />
    public XPathQName Name => default;

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <summary>Gets the compositor.</summary>
    public ModelGroupCompositor Compositor { get; }

    /// <summary>Gets the particles in document order.</summary>
    public IReadOnlyList<Particle> Particles { get; }

    /// <inheritdoc />
    public int ReferenceCount => _sites.Length;

    /// <inheritdoc />
    public XsdReference GetReference(int index) => _sites[index].Owner.GetReference(_sites[index].Index);

    /// <inheritdoc />
    public void Bind(int index, IXsdComponent target)
    {
        ArgumentNullException.ThrowIfNull(target);

        _sites[index].Owner.Bind(_sites[index].Index, target);
    }

    /// <summary>Creates a model group.</summary>
    /// <param name="compositor">The compositor.</param>
    /// <param name="location">The location of the compositor element.</param>
    /// <param name="particles">The particles in document order.</param>
    /// <returns>The model group.</returns>
    internal static ModelGroup Create(
        ModelGroupCompositor compositor,
        DocumentLocation location,
        IReadOnlyList<Particle> particles
    )
    {
        ArgumentNullException.ThrowIfNull(particles);

        var copy = new Particle[particles.Count];
        var sites = new List<RefSite>();
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = particles[i];
            copy[i].CollectReferences(sites);
        }

        return new(compositor, location, copy, sites.ToArray());
    }

    /// <summary>Appends every unresolved reference below this group.</summary>
    /// <param name="sites">The list that receives the references.</param>
    internal void CollectReferences(List<RefSite> sites)
    {
        ArgumentNullException.ThrowIfNull(sites);

        sites.AddRange(_sites);
    }
}
