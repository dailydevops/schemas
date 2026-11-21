namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Declarations11;
using NetEvolve.Schemas.Xsd.Substitution;

/// <summary>An element declaration with any type component and a list of heads that the test may extend.</summary>
internal sealed class MultiHeadElement : IMultiHeadElement
{
    private readonly List<IMultiHeadElement> _heads = [];

    public MultiHeadElement(string name, IXsdComponent type, int line = 1, params MultiHeadElement[] heads)
    {
        Name = new XPathQName("urn:test", name);
        Type = type;
        Location = new DocumentLocation("file:///a.xsd", line, 1);
        _heads.AddRange(heads);
    }

    public IReadOnlyList<IMultiHeadElement> Heads => _heads;

    // The single-head property of the 1.0 seam is not read by the 1.1 groups.
    public ISubstitutionElement? SubstitutionGroup => null;

    public IXsdComponent Type { get; }

    public bool IsAbstract { get; init; }

    public XsdDerivationMethod Block { get; init; }

    public XsdDerivationMethod Final { get; init; }

    public XsdComponentKind Kind => XsdComponentKind.ElementDeclaration;

    public XPathQName Name { get; }

    public DocumentLocation Location { get; }

    public MultiHeadElement AddHead(MultiHeadElement head)
    {
        _heads.Add(head);
        return this;
    }
}
