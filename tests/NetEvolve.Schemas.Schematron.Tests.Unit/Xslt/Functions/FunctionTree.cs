namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Builds a schema element with XSLT declarations as children, the way an <c>xslt</c> binding keeps them, over the Xml contract
/// node model. Every element gets its own line, so a problem can be told apart; the prefixes <c>xsl</c>, <c>f</c>, <c>xs</c>, and
/// <c>sch</c> are declared on the root.
/// </summary>
internal static class FunctionTree
{
    internal const string Xslt = "http://www.w3.org/1999/XSL/Transform";
    internal const string Functions = "urn:test:functions";
    internal const string XmlSchema = "http://www.w3.org/2001/XMLSchema";
    internal const string Schematron = "http://purl.oclc.org/dsdl/schematron";

    /// <summary>Creates an element in the XSLT namespace; the content mixes <see cref="XmlAttr"/>, elements, and text.</summary>
    internal static XmlElem X(string local, int line, params object[] content) => new(local, Xslt, line, 3, content);

    /// <summary>Creates an attribute.</summary>
    internal static XmlAttr A(string name, string value) => new(name, value);

    /// <summary>Creates an element in a namespace other than XSLT.</summary>
    internal static XmlElem Other(string local, string namespaceUri, int line, params object[] content) =>
        new(local, namespaceUri, line, 3, content);

    /// <summary>Builds the schema and returns the nth child element of the root, which is the declaration under test.</summary>
    internal static XPathNode Declaration(XmlElem declaration, int index = 0)
    {
        var tree = Build(new XmlElem("schema", Schematron, 1, 1, [declaration]));
        return Child(tree, index);
    }

    /// <summary>Builds the schema and returns the nth child element of the declaration, such as a parameter in the head of a function.</summary>
    internal static XPathNode ChildOfDeclaration(XmlElem declaration, int index = 0)
    {
        var parent = Declaration(declaration);
        var child = parent.Node.FirstChild;
        while (index > 0 || child.Kind != XmlNodeKind.Element)
        {
            if (child.Kind == XmlNodeKind.Element)
            {
                index--;
            }

            child = child.NextSibling;
        }

        return new XPathNode(child);
    }

    /// <summary>Builds a schema with several declarations; the declarations are the children of the root in order.</summary>
    internal static IReadOnlyList<XPathNode> Declarations(params XmlElem[] declarations) =>
        DeclarationsUnder("schema", Schematron, declarations);

    /// <summary>Builds a document whose root is the given element; the declarations are the children of the root in order.</summary>
    internal static IReadOnlyList<XPathNode> DeclarationsUnder(
        string rootLocal,
        string rootNamespace,
        params XmlElem[] declarations
    )
    {
        var tree = Build(new XmlElem(rootLocal, rootNamespace, 1, 1, [.. declarations]));
        var nodes = new List<XPathNode>();
        for (var i = 0; i < declarations.Length; i++)
        {
            nodes.Add(Child(tree, i));
        }

        return nodes;
    }

    private static XmlDocumentTree Build(XmlElem root)
    {
        using var builder = new XmlTreeBuilder(trackPositions: true);
        builder.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        Write(builder, root, true);
        builder.EndDocument();
        return builder.Complete();
    }

    private static XPathNode Child(XmlDocumentTree tree, int index)
    {
        var child = tree.Document.FirstChild.FirstChild;
        while (index > 0 || child.Kind != XmlNodeKind.Element)
        {
            if (child.Kind == XmlNodeKind.Element)
            {
                index--;
            }

            child = child.NextSibling;
        }

        return new XPathNode(child);
    }

    private static void Write(XmlTreeBuilder builder, XmlElem element, bool root)
    {
        var name = builder.Names.Intern(string.Empty, element.Local, element.NamespaceUri);
        builder.StartElement(name, new XmlPosition(element.Line, element.Column, 0));
        if (root)
        {
            builder.NamespaceDeclaration(builder.Names.Intern("xsl"), builder.Names.Intern(Xslt));
            builder.NamespaceDeclaration(builder.Names.Intern("f"), builder.Names.Intern(Functions));
            builder.NamespaceDeclaration(builder.Names.Intern("xs"), builder.Names.Intern(XmlSchema));
        }

        foreach (var attribute in element.Attributes)
        {
            builder.Attribute(builder.Names.Intern(string.Empty, attribute.Name, string.Empty), attribute.Value);
        }

        builder.EndAttributes();
        foreach (var item in element.Content)
        {
            if (item is XmlElem child)
            {
                Write(builder, child, false);
            }
            else
            {
                builder.Text((string)item, XmlTextKind.Text);
            }
        }

        builder.EndElement(name);
    }
}
