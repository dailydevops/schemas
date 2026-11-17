namespace NetEvolve.Schemas.XPath.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>A fixed tree of <c>&lt;root a="1"&gt;hello&lt;/root&gt;</c>: document 0, element 1, attribute 2, text 3. No annotation exists.</summary>
internal sealed class FakeNodeSource : XmlTree
{
    private static readonly XmlNodeKind[] Kinds =
    [
        XmlNodeKind.Document,
        XmlNodeKind.Element,
        XmlNodeKind.Attribute,
        XmlNodeKind.Text,
    ];
    private static readonly int[] Parents = [NoNode, 0, 1, 1];
    private static readonly int[] FirstChildren = [1, 3, NoNode, NoNode];
    private static readonly string[] Values = ["hello", "hello", "1", "hello"];
    private readonly SpanInterningNameTable _names = new(1000);
    private readonly XmlName[] _nameOf;

    public FakeNodeSource() =>
        _nameOf = [default, _names.Intern("", "root", ""), _names.Intern("", "a", ""), default];

    public XmlNode Element => new(this, 1);

    public XmlNode Attribute => new(this, 2);

    public XmlNode Text => new(this, 3);

    public override XmlNameTable Names => _names;

    public override IReadOnlyList<XmlUnparsedEntity> UnparsedEntities => [];

    public override IReadOnlyList<XmlNotation> Notations => [];

    protected internal override XmlNodeKind GetKind(int index) => Kinds[index];

    protected internal override XmlName GetName(int index) => _nameOf[index];

    protected internal override int GetParent(int index) => Parents[index];

    protected internal override int GetFirstChild(int index) => FirstChildren[index];

    protected internal override int GetNextSibling(int index) => NoNode;

    protected internal override int GetFirstAttribute(int index) => index == 1 ? 2 : NoNode;

    protected internal override int GetNextAttribute(int index) => NoNode;

    protected internal override int GetFirstNamespace(int index) => NoNode;

    protected internal override int GetNextNamespace(int index) => NoNode;

    protected internal override string GetStringValue(int index) => Values[index];

    protected internal override Uri? GetBaseUri(int index) => null;

    protected internal override Uri? GetDocumentUri(int index) => null;

    protected internal override int CompareDocumentOrder(int first, int second) => first.CompareTo(second);
}
