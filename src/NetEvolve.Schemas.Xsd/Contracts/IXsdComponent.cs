namespace NetEvolve.Schemas.Xsd.Contracts;

using NetEvolve.Schemas.XPath.Api;

/// <summary>A schema component. Immutable once the component registry is frozen.</summary>
internal interface IXsdComponent
{
    /// <summary>Gets the kind of the component.</summary>
    XsdComponentKind Kind { get; }

    /// <summary>Gets the expanded name, <see langword="default"/> for an anonymous component.</summary>
    XPathQName Name { get; }

    /// <summary>Gets the location of the declaring syntax.</summary>
    DocumentLocation Location { get; }
}
