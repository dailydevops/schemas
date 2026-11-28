namespace NetEvolve.Schemas.Schematron.Xslt.Library.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>snapshot()</c> and <c>copy-of()</c> (XSLT 3.0, 22.1 and 22.2): atomic values pass unchanged and every node is deep copied into a
/// new tree, so the copy has a new identity. <c>copy-of</c> keeps no ancestor (the copy of a node other than a document is the child of
/// a new document node, as for <c>xsl:copy-of</c>); <c>snapshot</c> also copies the ancestors with their attributes and namespaces, but
/// none of their other children, and returns the copy of the argument node. Trees are built only by the tree builder of the Xml package.
/// </summary>
/// <remarks>
/// Limitations: the zero-argument forms are not registered, because a host function cannot read the context item; <c>copy-of</c> of an
/// attribute or namespace node is XTDE0420, as in <c>xsl:copy-of</c> (#814); a namespace node passed to <c>snapshot</c> is copied
/// together with its parent element but not located again, so the result is the copy of that parent.
/// </remarks>
internal sealed class SnapshotFunction : XPathFunction
{
    private readonly bool _ancestors;

    internal SnapshotFunction(string name, bool ancestors)
        : base(
            XsltFunctionScope.Name(name),
            1,
            1,
            [new XPathParameterType(XPathItemType.Item, Occurrence.ZeroOrMore)]
        ) => _ancestors = ancestors;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var input = invocation.Arguments[0];
        var items = new XdmItem[input.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var item = input[i];
            if (item.TryGetNode(out var node))
            {
                if (!_ancestors && node.Node.Kind is XmlNodeKind.Attribute or XmlNodeKind.Namespace)
                {
                    invocation.Diagnostic = new XPathDiagnostic(
                        new XPathErrorCode("XTDE0420"),
                        XPathErrorKind.Dynamic,
                        "An attribute or namespace node cannot be copied without a parent element yet (limitation, #814).",
                        -1,
                        0
                    );
                    return XPathEvaluationStatus.Error;
                }

                item = XdmItem.FromNode(new XPathNode(Copy(node.Node)));
            }

            items[i] = item;
        }

        invocation.Result = XdmSequence.Create(items);
        return XPathEvaluationStatus.Ok;
    }

    private XmlNode Copy(XmlNode node)
    {
        var names = node.Tree!.Names;
        var attribute = node.Kind is XmlNodeKind.Attribute or XmlNodeKind.Namespace;
        var chain = new List<XmlNode>();
        if (_ancestors)
        {
            for (var parent = node.Parent; !parent.IsEmpty; parent = parent.Parent)
            {
                if (parent.Kind == XmlNodeKind.Element)
                {
                    chain.Insert(0, parent);
                }
            }
        }

        using var builder = new XmlTreeBuilder(names, null, false);
        builder.StartDocument(default);
        foreach (var ancestor in chain)
        {
            builder.StartElement(ancestor.Name, default);
            for (var ns = ancestor.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
            {
                // The URI is a name of this table already; the table is the one of the source tree.
                _ = names.TryGet(ns.StringValue.AsSpan(), out var uri);
                builder.NamespaceDeclaration(ns.Name.Local, uri);
            }

            for (var at = ancestor.FirstAttribute; !at.IsEmpty; at = at.NextAttribute)
            {
                builder.Attribute(at.Name, at.StringValue.AsSpan());
            }

            builder.EndAttributes();
        }

        if (!attribute)
        {
            ConstructionOutput.Pump(node, builder);
        }

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            builder.EndElement(chain[i].Name);
        }

        builder.EndDocument();

        var copy = builder.Complete().Document;
        if (node.Kind == XmlNodeKind.Document)
        {
            return copy;
        }

        var found = copy;
        for (var i = 0; i < chain.Count; i++)
        {
            found = found.FirstChild;
        }

        if (!attribute)
        {
            return found.FirstChild;
        }

        // The deepest ancestor is the parent of the attribute; a namespace node is not located again.
        for (var at = found.FirstAttribute; !at.IsEmpty; at = at.NextAttribute)
        {
            if (at.Name.Equals(node.Name))
            {
                return at;
            }
        }

        return found;
    }
}
