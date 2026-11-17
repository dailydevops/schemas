namespace NetEvolve.Schemas.Xsd.Tests.Unit.Substitution;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

/// <summary>A type with an optional base, the method that derives it, and a block.</summary>
internal sealed class FakeType : IBlockedType
{
    public FakeType(string name, FakeType? baseType = null, XsdDerivationMethod method = XsdDerivationMethod.None)
    {
        Name = new XPathQName("urn:test", name);
        Base = baseType;
        Method = method;
    }

    public FakeType? Base { get; }

    public XsdDerivationMethod Method { get; }

    public XsdDerivationMethod Block { get; init; }

    public XsdComponentKind Kind => XsdComponentKind.ComplexType;

    public XPathQName Name { get; }

    public DocumentLocation Location => new("file:///a.xsd", 1, 1);
}
