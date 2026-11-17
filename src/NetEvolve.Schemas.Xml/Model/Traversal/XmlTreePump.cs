namespace NetEvolve.Schemas.Xml.Model.Traversal;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Pumps a document or an element subtree of the model into a sink.</summary>
/// <remarks>
/// <para>
/// Events follow the reader order for the same content. The traversal is iterative and keeps no stack, so the depth of a tree
/// is not limited; it allocates nothing per node when the sink shares the name table of the tree.
/// </para>
/// <para>
/// A subtree is self-contained: its root element declares every namespace in scope there, including those declared on
/// ancestors. Below the root, only declarations that change the in-scope bindings are emitted, followed by the undeclarations
/// (an empty URI) of prefixes that the parent has in scope; the source order of the declarations is not kept.
/// </para>
/// <para>
/// Limitations: the model does not keep the XML declaration, the document type declaration, the origin of text (CDATA or
/// reference) or redundant redeclarations of an unchanged binding, so a pumped document carries an empty declaration, no
/// doctype event, text events of kind <see cref="XmlTextKind.Text"/> and none of those redeclarations.
/// </para>
/// </remarks>
public static class XmlTreePump
{
    /// <summary>Pumps a node and its descendants into a class sink.</summary>
    /// <param name="node">
    /// The document, element, text, comment or processing instruction node. An attribute or namespace node emits nothing,
    /// because neither is an event of the sink contract on its own; they are pumped as part of their element.
    /// </param>
    /// <param name="sink">The sink.</param>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="default"/> or <paramref name="sink"/> is <see langword="null"/>.</exception>
    public static void Pump(XmlNode node, IXmlSink sink) => Pump(node, ref sink);

    /// <summary>Pumps a node and its descendants into a class sink or a mutable struct sink.</summary>
    /// <typeparam name="TSink">The sink type.</typeparam>
    /// <param name="node">
    /// The document, element, text, comment or processing instruction node. An attribute or namespace node emits nothing,
    /// because neither is an event of the sink contract on its own; they are pumped as part of their element.
    /// </param>
    /// <param name="sink">The sink, passed by reference so a struct sink keeps its state.</param>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="default"/> or <paramref name="sink"/> is a <see langword="null"/> reference.</exception>
    public static void Pump<TSink>(XmlNode node, ref TSink sink)
        where TSink : IXmlSink
    {
        Argument.ThrowIfNull(node.Tree, nameof(node));

        // A struct sink cannot be null; skipping the check for it keeps the call from boxing.
        if (!typeof(TSink).IsValueType)
        {
            Argument.ThrowIfNull(sink);
        }

        // XmlTree can only be derived inside this assembly, and XmlDocumentTree is its only implementation.
        var tree = (XmlDocumentTree)node.Tree;
        var target = sink.Names;
        var root = node.Index;
        var index = root;
        while (true)
        {
            var kind = Open(tree, index, root, ref sink, target);
            var child = kind is XmlNodeKind.Document or XmlNodeKind.Element
                ? tree.GetFirstChild(index)
                : XmlTree.NoNode;
            if (child != XmlTree.NoNode)
            {
                index = child;
                continue;
            }

            while (true)
            {
                Close(tree, index, ref sink, target);
                if (index == root)
                {
                    return;
                }

                var sibling = tree.GetNextSibling(index);
                if (sibling != XmlTree.NoNode)
                {
                    index = sibling;
                    break;
                }

                index = tree.GetParent(index);
            }
        }
    }

    private static XmlNodeKind Open<TSink>(
        XmlDocumentTree tree,
        int index,
        int root,
        ref TSink sink,
        XmlNameTable target
    )
        where TSink : IXmlSink
    {
        var kind = tree.GetKind(index);
        switch (kind)
        {
            case XmlNodeKind.Document:
                sink.StartDocument(default);
                break;
            case XmlNodeKind.Element:
                OpenElement(tree, index, root, ref sink, target);
                break;
            case XmlNodeKind.Text:
                sink.Text(tree.GetValueSpan(index), XmlTextKind.Text);
                break;
            case XmlNodeKind.Comment:
                sink.Comment(tree.GetValueSpan(index));
                break;
            case XmlNodeKind.ProcessingInstruction:
                sink.ProcessingInstruction(Map(tree, tree.GetName(index).Local, target), tree.GetValueSpan(index));
                break;
            case XmlNodeKind.Attribute:
            case XmlNodeKind.Namespace:
            default:
                break;
        }

        return kind;
    }

    private static void Close<TSink>(XmlDocumentTree tree, int index, ref TSink sink, XmlNameTable target)
        where TSink : IXmlSink
    {
        switch (tree.GetKind(index))
        {
            case XmlNodeKind.Document:
                sink.EndDocument();
                break;
            case XmlNodeKind.Element:
                sink.EndElement(Map(tree, tree.GetName(index), target));
                break;
            case XmlNodeKind.Attribute:
            case XmlNodeKind.Namespace:
            case XmlNodeKind.Text:
            case XmlNodeKind.Comment:
            case XmlNodeKind.ProcessingInstruction:
            default:
                break;
        }
    }

    private static void OpenElement<TSink>(
        XmlDocumentTree tree,
        int index,
        int root,
        ref TSink sink,
        XmlNameTable target
    )
        where TSink : IXmlSink
    {
        sink.StartElement(Map(tree, tree.GetName(index), target), tree.GetPosition(index));

        // The root of a pump declares everything in scope; below it only changes against the parent are declared.
        var parent = index == root ? XmlTree.NoNode : tree.GetParent(index);
        if (parent != XmlTree.NoNode && tree.GetKind(parent) != XmlNodeKind.Element)
        {
            parent = XmlTree.NoNode;
        }

        // ponytail: quadratic in the number of in-scope bindings per element; index them if documents with hundreds of prefixes matter.
        for (var ns = tree.GetFirstNamespace(index); ns != XmlTree.NoNode; ns = tree.GetNextNamespace(ns))
        {
            var prefix = tree.GetName(ns).Local;
            var uri = tree.GetNamespaceUri(ns);
            if (parent == XmlTree.NoNode || !IsBound(tree, parent, prefix, uri))
            {
                sink.NamespaceDeclaration(Map(tree, prefix, target), Map(tree, uri, target));
            }
        }

        if (parent != XmlTree.NoNode)
        {
            // A prefix in scope on the parent but not here was undeclared by an empty URI.
            for (var ns = tree.GetFirstNamespace(parent); ns != XmlTree.NoNode; ns = tree.GetNextNamespace(ns))
            {
                var prefix = tree.GetName(ns).Local;
                if (!IsPrefixBound(tree, index, prefix))
                {
                    sink.NamespaceDeclaration(Map(tree, prefix, target), default);
                }
            }
        }

        for (var at = tree.GetFirstAttribute(index); at != XmlTree.NoNode; at = tree.GetNextAttribute(at))
        {
            sink.Attribute(Map(tree, tree.GetName(at), target), tree.GetValueSpan(at));
        }

        sink.EndAttributes();
    }

    private static bool IsBound(XmlDocumentTree tree, int element, XmlSymbol prefix, XmlSymbol uri)
    {
        for (var ns = tree.GetFirstNamespace(element); ns != XmlTree.NoNode; ns = tree.GetNextNamespace(ns))
        {
            if (tree.GetName(ns).Local == prefix)
            {
                return tree.GetNamespaceUri(ns) == uri;
            }
        }

        return false;
    }

    private static bool IsPrefixBound(XmlDocumentTree tree, int element, XmlSymbol prefix)
    {
        for (var ns = tree.GetFirstNamespace(element); ns != XmlTree.NoNode; ns = tree.GetNextNamespace(ns))
        {
            if (tree.GetName(ns).Local == prefix)
            {
                return true;
            }
        }

        return false;
    }

    // Symbols are only valid for their own table: translate when the sink uses another one.
    private static XmlSymbol Map(XmlDocumentTree tree, XmlSymbol symbol, XmlNameTable target) =>
        symbol.IsEmpty || ReferenceEquals(tree.Names, target) ? symbol : target.Intern(tree.Names.GetString(symbol));

    private static XmlName Map(XmlDocumentTree tree, XmlName name, XmlNameTable target) =>
        ReferenceEquals(tree.Names, target)
            ? name
            : new XmlName(
                Map(tree, name.Prefix, target),
                Map(tree, name.Local, target),
                Map(tree, name.NamespaceUri, target)
            );
}
