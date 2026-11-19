namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Substitution;

/// <summary>An element declaration with any type component, for substitution groups over real types.</summary>
internal sealed class TypeElement : ISubstitutionElement
{
    public TypeElement(string name, IXsdComponent type, TypeElement? head = null)
    {
        Name = new XPathQName(TypeFixture.Tns, name);
        Type = type;
        SubstitutionGroup = head;
    }

    public ISubstitutionElement? SubstitutionGroup { get; }

    public IXsdComponent Type { get; }

    public bool IsAbstract { get; init; }

    public XsdDerivationMethod Block { get; init; }

    public XsdDerivationMethod Final { get; init; }

    public XsdComponentKind Kind => XsdComponentKind.ElementDeclaration;

    public XPathQName Name { get; }

    public DocumentLocation Location => TypeFixture.Where;
}
