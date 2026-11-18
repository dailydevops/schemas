namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>Builds small instance trees with the document model builder.</summary>
internal static class PatternTree
{
    internal static PatternElement E(string name, params object[] content) => new(name, content);

    /// <summary>Creates a root element with <paramref name="children"/> empty child elements named <c>item</c>.</summary>
    internal static PatternElement Wide(int children)
    {
        var content = new object[children];
        for (var i = 0; i < children; i++)
        {
            content[i] = E("item");
        }

        return E("r", content);
    }

    internal static PatternAttribute A(string name, string value) => new(name, value);

    /// <summary>Builds the document; <paramref name="namespacePrefix"/> declares one extra namespace on the root element.</summary>
    internal static XPathNode Build(PatternElement root, string? namespacePrefix = null)
    {
        using var builder = new XmlTreeBuilder(trackPositions: false);
        builder.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        Write(builder, root, namespacePrefix);
        builder.EndDocument();
        return new XPathNode(builder.Complete().Document);
    }

    /// <summary>Builds a chain of <paramref name="depth"/> nested elements named <c>n</c>.</summary>
    internal static XPathNode BuildChain(int depth)
    {
        using var builder = new XmlTreeBuilder(trackPositions: false);
        builder.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var name = builder.Names.Intern(string.Empty, "n", string.Empty);
        for (var i = 0; i < depth; i++)
        {
            builder.StartElement(name, default);
            builder.EndAttributes();
        }

        for (var i = 0; i < depth; i++)
        {
            builder.EndElement(name);
        }

        builder.EndDocument();
        return new XPathNode(builder.Complete().Document);
    }

    private static void Write(XmlTreeBuilder builder, PatternElement element, string? namespacePrefix)
    {
        var name = builder.Names.Intern(string.Empty, element.Name, string.Empty);
        builder.StartElement(name, default);
        foreach (var item in element.Content)
        {
            if (item is PatternAttribute attribute)
            {
                builder.Attribute(builder.Names.Intern(string.Empty, attribute.Name, string.Empty), attribute.Value);
            }
        }

        if (namespacePrefix is not null)
        {
            builder.NamespaceDeclaration(builder.Names.Intern(namespacePrefix), builder.Names.Intern("urn:x"));
        }

        builder.EndAttributes();
        foreach (var item in element.Content)
        {
            switch (item)
            {
                case PatternElement child:
                    Write(builder, child, null);
                    break;
                case string text:
                    builder.Text(text, XmlTextKind.Text);
                    break;
                default:
                    break;
            }
        }

        builder.EndElement(name);
    }
}
