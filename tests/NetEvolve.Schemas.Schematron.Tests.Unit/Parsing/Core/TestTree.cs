namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;

/// <summary>
/// Builds node trees with the document model builder, so the parser tests run over the Xml contract node model and never
/// touch the real reader.
/// </summary>
internal static class TestTree
{
    internal const string SchematronNamespace = "http://purl.oclc.org/dsdl/schematron";

    internal const string ForeignNamespace = "urn:test:foreign";

    /// <summary>Creates an element in the Schematron namespace; the content mixes attributes, elements, and text.</summary>
    internal static XmlElem E(string local, int line, int column, params object[] content) =>
        new(local, SchematronNamespace, line, column, content);

    /// <summary>Creates an element in a foreign namespace.</summary>
    internal static XmlElem Foreign(string local, int line, int column, params object[] content) =>
        new(local, ForeignNamespace, line, column, content);

    /// <summary>Creates an attribute without namespace.</summary>
    internal static XmlAttr A(string name, string value) => new(name, value);

    /// <summary>Builds a document that holds only a comment, so it has no root element.</summary>
    internal static XmlNode BuildWithoutRoot()
    {
        using var builder = new XmlTreeBuilder();
        builder.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        builder.Comment("empty");
        builder.EndDocument();
        return builder.Complete().Document;
    }

    /// <summary>Builds the tree and returns its document node.</summary>
    internal static XmlNode Build(XmlElem root, bool trackPositions = true)
    {
        using var builder = new XmlTreeBuilder(trackPositions: trackPositions);
        builder.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        Write(builder, root);
        builder.EndDocument();
        return builder.Complete().Document;
    }

    private static void Write(XmlTreeBuilder builder, XmlElem element)
    {
        var name = builder.Names.Intern(string.Empty, element.Local, element.NamespaceUri);
        builder.StartElement(name, new XmlPosition(element.Line, element.Column, 0));
        foreach (var attribute in element.Attributes)
        {
            builder.Attribute(builder.Names.Intern(string.Empty, attribute.Name, string.Empty), attribute.Value);
        }

        builder.EndAttributes();
        foreach (var item in element.Content)
        {
            if (item is XmlElem child)
            {
                Write(builder, child);
            }
            else
            {
                builder.Text((string)item, XmlTextKind.Text);
            }
        }

        builder.EndElement(name);
    }
}
