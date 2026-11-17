namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A located syntax element built in memory.</summary>
internal sealed class FakeSyntaxElement : ISchemaSyntaxElement
{
    private readonly List<XsdSyntaxAttribute> _attributes = [];
    private readonly List<FakeSyntaxElement> _children = [];

    public FakeSyntaxElement(string localName, int line)
    {
        Name = new XPathQName("http://www.w3.org/2001/XMLSchema", localName);
        Location = new DocumentLocation("file:///a.xsd", line, 1);
    }

    public XPathQName Name { get; }

    public DocumentLocation Location { get; }

    public int AttributeCount => _attributes.Count;

    public int ChildCount => _children.Count;

    public FakeSyntaxElement WithAttribute(string localName, string value)
    {
        _attributes.Add(new XsdSyntaxAttribute(new XPathQName(string.Empty, localName), value, Location));
        return this;
    }

    public FakeSyntaxElement WithChild(FakeSyntaxElement child)
    {
        _children.Add(child);
        return this;
    }

    public XsdSyntaxAttribute GetAttribute(int index) => _attributes[index];

    public ISchemaSyntaxElement GetChild(int index) => _children[index];
}
