namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

/// <summary>A notation declaration without namespace; no builder creates the component yet.</summary>
internal sealed class NotationFake(string name) : IXsdComponent
{
    public XsdComponentKind Kind => XsdComponentKind.Notation;

    public XPathQName Name { get; } = new(string.Empty, name);

    public DocumentLocation Location => SchemaFixture.Where;
}
