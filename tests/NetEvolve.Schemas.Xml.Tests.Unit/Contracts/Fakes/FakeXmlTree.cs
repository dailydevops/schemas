namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>
/// A list-backed tree for tests: rows are added through the <c>Add*</c> methods, document order is computed on demand,
/// and every accessor call is recorded so tests can prove that <see cref="XmlNode"/> delegates to it.
/// </summary>
internal sealed class FakeXmlTree : XmlTree
{
    private readonly List<Row> _rows = [new Row(XmlNodeKind.Document, default, string.Empty, XmlTree.NoNode)];
    private readonly FakeNameTable _names = new();
    private int[]? _order;

    public List<string> Calls { get; } = [];

    public Uri? DocumentUriValue { get; set; }

    public Uri? BaseUriValue { get; set; }

    public List<XmlUnparsedEntity> UnparsedEntityList { get; } = [];

    public List<XmlNotation> NotationList { get; } = [];

    public override XmlNameTable Names => _names;

    public override IReadOnlyList<XmlUnparsedEntity> UnparsedEntities => UnparsedEntityList;

    public override IReadOnlyList<XmlNotation> Notations => NotationList;

    public int AddElement(int parent, string local, string prefix = "", string uri = "") =>
        AddChild(parent, new Row(XmlNodeKind.Element, _names.Intern(prefix, local, uri), string.Empty, parent));

    public int AddText(int parent, string text) => AddChild(parent, new Row(XmlNodeKind.Text, default, text, parent));

    public int AddComment(int parent, string text) =>
        AddChild(parent, new Row(XmlNodeKind.Comment, default, text, parent));

    public int AddProcessingInstruction(int parent, string target, string data) =>
        AddChild(
            parent,
            new Row(
                XmlNodeKind.ProcessingInstruction,
                new XmlName(default, _names.Intern(target), default),
                data,
                parent
            )
        );

    public int AddAttribute(int element, string local, string value, string prefix = "", string uri = "")
    {
        var index = Add(new Row(XmlNodeKind.Attribute, _names.Intern(prefix, local, uri), value, element));
        var owner = _rows[element];
        if (owner.FirstAttribute == XmlTree.NoNode)
        {
            owner.FirstAttribute = index;
            return index;
        }

        var last = owner.FirstAttribute;
        while (_rows[last].NextAttribute != XmlTree.NoNode)
        {
            last = _rows[last].NextAttribute;
        }

        _rows[last].NextAttribute = index;
        return index;
    }

    public int AddNamespace(int element, string prefix, string uri)
    {
        var index = Add(
            new Row(XmlNodeKind.Namespace, new XmlName(default, _names.Intern(prefix), default), uri, element)
        );
        var owner = _rows[element];
        if (owner.FirstNamespace == XmlTree.NoNode)
        {
            owner.FirstNamespace = index;
            return index;
        }

        var last = owner.FirstNamespace;
        while (_rows[last].NextNamespace != XmlTree.NoNode)
        {
            last = _rows[last].NextNamespace;
        }

        _rows[last].NextNamespace = index;
        return index;
    }

    protected internal override XmlNodeKind GetKind(int index) => Record(nameof(GetKind), index).Kind;

    protected internal override XmlName GetName(int index) => Record(nameof(GetName), index).Name;

    protected internal override int GetParent(int index) => Record(nameof(GetParent), index).Parent;

    protected internal override int GetFirstChild(int index) => Record(nameof(GetFirstChild), index).FirstChild;

    protected internal override int GetNextSibling(int index) => Record(nameof(GetNextSibling), index).NextSibling;

    protected internal override int GetFirstAttribute(int index) =>
        Record(nameof(GetFirstAttribute), index).FirstAttribute;

    protected internal override int GetNextAttribute(int index) =>
        Record(nameof(GetNextAttribute), index).NextAttribute;

    protected internal override int GetFirstNamespace(int index) =>
        Record(nameof(GetFirstNamespace), index).FirstNamespace;

    protected internal override int GetNextNamespace(int index) =>
        Record(nameof(GetNextNamespace), index).NextNamespace;

    protected internal override string GetStringValue(int index)
    {
        var row = Record(nameof(GetStringValue), index);
        if (row.Kind is not (XmlNodeKind.Document or XmlNodeKind.Element))
        {
            return row.Value;
        }

        var builder = new StringBuilder();
        AppendText(builder, index);
        return builder.ToString();
    }

    protected internal override DtdAttributeType? GetAttributeType(int index)
    {
        _ = Record(nameof(GetAttributeType), index);
        return null;
    }

    protected internal override bool GetIsSpecified(int index)
    {
        _ = Record(nameof(GetIsSpecified), index);
        return true;
    }

    protected internal override Uri? GetBaseUri(int index)
    {
        _ = Record(nameof(GetBaseUri), index);
        return BaseUriValue;
    }

    protected internal override Uri? GetDocumentUri(int index)
    {
        _ = Record(nameof(GetDocumentUri), index);
        return DocumentUriValue;
    }

    protected internal override int CompareDocumentOrder(int first, int second)
    {
        Calls.Add($"{nameof(CompareDocumentOrder)}({first},{second})");
        var order = _order ??= ComputeOrder();
        return order[first].CompareTo(order[second]);
    }

    private int Add(Row row)
    {
        _order = null;
        _rows.Add(row);
        return _rows.Count - 1;
    }

    private int AddChild(int parent, Row row)
    {
        var index = Add(row);
        var last = _rows[parent].FirstChild;
        if (last == XmlTree.NoNode)
        {
            _rows[parent].FirstChild = index;
            return index;
        }

        while (_rows[last].NextSibling != XmlTree.NoNode)
        {
            last = _rows[last].NextSibling;
        }

        _rows[last].NextSibling = index;
        return index;
    }

    private Row Record(string accessor, int index)
    {
        Calls.Add($"{accessor}({index})");
        return _rows[index];
    }

    private void AppendText(StringBuilder builder, int index)
    {
        for (var child = _rows[index].FirstChild; child != XmlTree.NoNode; child = _rows[child].NextSibling)
        {
            if (_rows[child].Kind == XmlNodeKind.Text)
            {
                builder.Append(_rows[child].Value);
            }
            else if (_rows[child].Kind == XmlNodeKind.Element)
            {
                AppendText(builder, child);
            }
        }
    }

    // Pre-order: a node, its namespace nodes, its attributes, then its children.
    private int[] ComputeOrder()
    {
        var order = new int[_rows.Count];
        var counter = 0;
        Visit(0);
        return order;

        void Visit(int index)
        {
            order[index] = counter++;
            for (var ns = _rows[index].FirstNamespace; ns != XmlTree.NoNode; ns = _rows[ns].NextNamespace)
            {
                order[ns] = counter++;
            }

            for (
                var attribute = _rows[index].FirstAttribute;
                attribute != XmlTree.NoNode;
                attribute = _rows[attribute].NextAttribute
            )
            {
                order[attribute] = counter++;
            }

            for (var child = _rows[index].FirstChild; child != XmlTree.NoNode; child = _rows[child].NextSibling)
            {
                Visit(child);
            }
        }
    }

    private sealed class Row(XmlNodeKind kind, XmlName name, string value, int parent)
    {
        public XmlNodeKind Kind { get; } = kind;

        public XmlName Name { get; } = name;

        public string Value { get; } = value;

        public int Parent { get; } = parent;

        public int FirstChild { get; set; } = XmlTree.NoNode;

        public int NextSibling { get; set; } = XmlTree.NoNode;

        public int FirstAttribute { get; set; } = XmlTree.NoNode;

        public int NextAttribute { get; set; } = XmlTree.NoNode;

        public int FirstNamespace { get; set; } = XmlTree.NoNode;

        public int NextNamespace { get; set; } = XmlTree.NoNode;
    }
}
