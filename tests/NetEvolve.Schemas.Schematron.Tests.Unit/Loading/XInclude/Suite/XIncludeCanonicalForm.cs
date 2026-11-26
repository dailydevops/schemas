namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

using System.Text;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Writes a tree as text for comparison: attributes in ascending order of their expanded names, no namespace declarations, no
/// document type, and every name as <c>{namespace}local</c>, so the prefix does not matter.
/// </summary>
internal static class XIncludeCanonicalForm
{
    /// <summary>Writes the children of a document node.</summary>
    /// <param name="document">The document node.</param>
    /// <returns>The canonical text.</returns>
    public static string Of(XmlNode document)
    {
        var builder = new StringBuilder();
        Write(builder, document);
        return builder.ToString();
    }

    private static void Write(StringBuilder builder, XmlNode node)
    {
        switch (node.Kind)
        {
            case XmlNodeKind.Element:
                var name = Expanded(node);
                _ = builder.Append('<').Append(name);
                var attributes = new List<(string Name, string Value)>();
                for (var a = node.FirstAttribute; !a.IsEmpty; a = a.NextAttribute)
                {
                    attributes.Add((Expanded(a), a.StringValue));
                }

                foreach (var (attribute, value) in attributes.OrderBy(a => a.Name, StringComparer.Ordinal))
                {
                    _ = builder.Append(' ').Append(attribute).Append("=\"").Append(value).Append('"');
                }

                _ = builder.Append('>');
                WriteChildren(builder, node);
                _ = builder.Append("</").Append(name).Append('>');
                break;
            case XmlNodeKind.Text:
                _ = builder.Append(node.StringValue);
                break;
            case XmlNodeKind.Comment:
                _ = builder.Append("<!--").Append(node.StringValue).Append("-->");
                break;
            case XmlNodeKind.ProcessingInstruction:
                _ = builder.Append("<?").Append(Expanded(node)).Append(' ').Append(node.StringValue).Append("?>");
                break;
            default:
                WriteChildren(builder, node);
                break;
        }
    }

    private static void WriteChildren(StringBuilder builder, XmlNode node)
    {
        for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            Write(builder, child);
        }
    }

    private static string Expanded(XmlNode node)
    {
        var name = node.Name;
        var names = node.Tree!.Names;
        var uri = names.GetString(name.NamespaceUri);
        return uri.Length == 0 ? names.GetString(name.Local) : $"{{{uri}}}{names.GetString(name.Local)}";
    }
}
