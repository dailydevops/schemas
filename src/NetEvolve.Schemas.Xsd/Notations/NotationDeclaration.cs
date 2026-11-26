namespace NetEvolve.Schemas.Xsd.Notations;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// A notation declaration (Structures section 3.12): a name in the target namespace with a public identifier, a system identifier,
/// or both. Notation Declaration Correct (<c>n-props-correct</c>) has no clause of its own, so the component needs no check.
/// </summary>
internal sealed class NotationDeclaration : IXsdComponent
{
    internal NotationDeclaration(XPathQName name, DocumentLocation location, string? publicId, string? systemId)
    {
        Name = name;
        Location = location;
        PublicId = publicId;
        SystemId = systemId;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.Notation;

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <summary>Gets the public identifier, <see langword="null"/> when the declaration has none.</summary>
    public string? PublicId { get; }

    /// <summary>Gets the system identifier, <see langword="null"/> when the declaration has none.</summary>
    public string? SystemId { get; }
}
