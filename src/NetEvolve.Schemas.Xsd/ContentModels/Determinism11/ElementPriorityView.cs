namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism11;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// A wildcard that allows no element: <see cref="ParticleOverlap"/> then finds no element that the wildcard competes with,
/// which is the priority of a declared element over a wildcard in XSD 1.1. Everything else is that of the wildcard.
/// </summary>
internal sealed class ElementPriorityView : IWildcard
{
    private readonly IWildcard _wildcard;

    internal ElementPriorityView(IWildcard wildcard)
    {
        ArgumentNullException.ThrowIfNull(wildcard);

        _wildcard = wildcard;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => _wildcard.Kind;

    /// <inheritdoc />
    public XPathQName Name => _wildcard.Name;

    /// <inheritdoc />
    public DocumentLocation Location => _wildcard.Location;

    /// <inheritdoc />
    public XsdProcessContents ProcessContents => _wildcard.ProcessContents;

    /// <inheritdoc />
    public bool Allows(XPathQName name) => false;

    /// <inheritdoc />
    public bool IsSubsetOf(IWildcard other) => _wildcard.IsSubsetOf(other);

    /// <inheritdoc />
    public IWildcard? Union(IWildcard other) => _wildcard.Union(other);

    /// <inheritdoc />
    public IWildcard? Intersect(IWildcard other) => _wildcard.Intersect(other);
}
