namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Registry;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>Builds one-element trees with a recorded position, so the tests run over the Xml contract node model.</summary>
internal static class RegistryTree
{
    internal const string Xslt = "http://www.w3.org/1999/XSL/Transform";

    internal static XPathQName Name(string local) => new(Xslt, local);

    /// <summary>Creates the only element of a new tree, at the given one-based position.</summary>
    internal static XPathNode Element(string prefix, string local, string namespaceUri, int line, int column)
    {
        using var builder = new XmlTreeBuilder(trackPositions: true);
        builder.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var name = builder.Names.Intern(prefix, local, namespaceUri);
        builder.StartElement(name, new XmlPosition(line, column, 0));
        builder.EndAttributes();
        builder.EndElement(name);
        builder.EndDocument();
        return new XPathNode(builder.Complete().Document.FirstChild);
    }
}
