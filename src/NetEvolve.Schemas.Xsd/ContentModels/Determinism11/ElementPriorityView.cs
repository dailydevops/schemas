namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism11;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A wildcard that allows no element, so that a declared element wins over it.</summary>
internal sealed class ElementPriorityView : IWildcard
{
    internal ElementPriorityView(IWildcard wildcard) => throw new NotSupportedException();

    /// <inheritdoc />
    public XsdComponentKind Kind => throw new NotSupportedException();

    /// <inheritdoc />
    public XPathQName Name => throw new NotSupportedException();

    /// <inheritdoc />
    public DocumentLocation Location => throw new NotSupportedException();

    /// <inheritdoc />
    public XsdProcessContents ProcessContents => throw new NotSupportedException();

    /// <inheritdoc />
    public bool Allows(XPathQName name) => throw new NotSupportedException();

    /// <inheritdoc />
    public bool IsSubsetOf(IWildcard other) => throw new NotSupportedException();

    /// <inheritdoc />
    public IWildcard? Union(IWildcard other) => throw new NotSupportedException();

    /// <inheritdoc />
    public IWildcard? Intersect(IWildcard other) => throw new NotSupportedException();
}
