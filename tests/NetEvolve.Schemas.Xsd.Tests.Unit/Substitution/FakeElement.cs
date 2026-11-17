namespace NetEvolve.Schemas.Xsd.Tests.Unit.Substitution;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

internal sealed class FakeElement : ISubstitutionElement
{
    public FakeElement(string name, FakeType type, FakeElement? head = null, int line = 1)
    {
        Name = new XPathQName("urn:test", name);
        Type = type;
        SubstitutionGroup = head;
        Location = new DocumentLocation("file:///a.xsd", line, 1);
    }

    public ISubstitutionElement? SubstitutionGroup { get; set; }

    public IXsdComponent Type { get; }

    public bool IsAbstract { get; init; }

    public XsdDerivationMethod Block { get; init; }

    public XsdDerivationMethod Final { get; init; }

    public XsdComponentKind Kind => XsdComponentKind.ElementDeclaration;

    public XPathQName Name { get; }

    public DocumentLocation Location { get; }
}
