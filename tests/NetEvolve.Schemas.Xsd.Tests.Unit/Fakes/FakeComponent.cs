namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A component with a kind, a name, and a location.</summary>
internal sealed class FakeComponent : IXsdComponent
{
    public FakeComponent(XsdComponentKind kind, string localName, int line = 1)
    {
        Kind = kind;
        Name = localName.Length == 0 ? default : new XPathQName("urn:test", localName);
        Location = new DocumentLocation("file:///a.xsd", line, 1);
    }

    public XsdComponentKind Kind { get; }

    public XPathQName Name { get; }

    public DocumentLocation Location { get; }
}
