namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A tree that answers from a document tree and is none itself, so that a walk over it takes the path of every other tree.</summary>
internal sealed class ForwardingTree(XmlDocumentTree inner) : XmlTree
{
    public override XmlNameTable Names => inner.Names;

    public override IReadOnlyList<XmlUnparsedEntity> UnparsedEntities => inner.UnparsedEntities;

    public override IReadOnlyList<XmlNotation> Notations => inner.Notations;

    /// <summary>Gets the node of this tree that stands for a node of the document tree.</summary>
    /// <param name="node">The node of the document tree.</param>
    /// <returns>The node at the same row.</returns>
    public XPathNode Map(XPathNode node) => new(new XmlNode(this, node.Node.Index));

    protected internal override XmlNodeKind GetKind(int index) => inner.GetKind(index);

    protected internal override XmlName GetName(int index) => inner.GetName(index);

    protected internal override int GetParent(int index) => inner.GetParent(index);

    protected internal override int GetFirstChild(int index) => inner.GetFirstChild(index);

    protected internal override int GetNextSibling(int index) => inner.GetNextSibling(index);

    protected internal override int GetFirstAttribute(int index) => inner.GetFirstAttribute(index);

    protected internal override int GetNextAttribute(int index) => inner.GetNextAttribute(index);

    protected internal override int GetFirstNamespace(int index) => inner.GetFirstNamespace(index);

    protected internal override int GetNextNamespace(int index) => inner.GetNextNamespace(index);

    protected internal override string GetStringValue(int index) => inner.GetStringValue(index);

    protected internal override Uri? GetBaseUri(int index) => inner.GetBaseUri(index);

    protected internal override Uri? GetDocumentUri(int index) => inner.GetDocumentUri(index);

    protected internal override int CompareDocumentOrder(int first, int second) =>
        inner.CompareDocumentOrder(first, second);
}
