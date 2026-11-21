namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Writes a node of a constructed tree as text, with the namespace declarations that the element adds to its parent, so a test can
/// read the namespace fixup: <c>&lt;p:e xmlns:p='urn:p' q:a='1'&gt;text&lt;/p:e&gt;</c>. A document is written as its children, an
/// undeclared prefix of the parent as <c>xmlns:p=''</c>.
/// </summary>
internal static class NodeDump
{
    internal static string Of(in XdmItem item)
    {
        _ = item.TryGetNode(out var node);
        return Of(node.Node);
    }

    internal static string Of(XmlNode node)
    {
        var text = new StringBuilder();
        Write(node, text);
        return text.ToString();
    }

    private static void Write(XmlNode node, StringBuilder text)
    {
        var names = node.Tree!.Names;
        switch (node.Kind)
        {
            case XmlNodeKind.Document:
                for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
                {
                    Write(child, text);
                }

                break;
            case XmlNodeKind.Element:
                WriteElement(node, text);
                break;
            case XmlNodeKind.Text:
                _ = text.Append(node.StringValue);
                break;
            case XmlNodeKind.Comment:
                _ = text.Append("<!--").Append(node.StringValue).Append("-->");
                break;
            case XmlNodeKind.ProcessingInstruction:
                _ = text.Append("<?")
                    .Append(names.GetString(node.Name.Local))
                    .Append(' ')
                    .Append(node.StringValue)
                    .Append("?>");
                break;
            case XmlNodeKind.Attribute:
                _ = text.Append(Qualified(node)).Append("='").Append(node.StringValue).Append('\'');
                break;
            case XmlNodeKind.Namespace:
                _ = text.Append(Declaration(node));
                break;
            default:
                break;
        }
    }

    private static void WriteElement(XmlNode element, StringBuilder text)
    {
        var name = Qualified(element);
        _ = text.Append('<').Append(name);
        var parent = element.Parent;
        var inherited = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!parent.IsEmpty && parent.Kind == XmlNodeKind.Element)
        {
            for (var ns = parent.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
            {
                inherited[Prefix(ns)] = ns.StringValue;
            }
        }

        var own = new HashSet<string>(StringComparer.Ordinal);
        for (var ns = element.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
        {
            _ = own.Add(Prefix(ns));
            if (
                !inherited.TryGetValue(Prefix(ns), out var bound)
                || !string.Equals(bound, ns.StringValue, StringComparison.Ordinal)
            )
            {
                _ = text.Append(' ').Append(Declaration(ns));
            }
        }

        foreach (var pair in inherited)
        {
            if (!own.Contains(pair.Key))
            {
                _ = text.Append(' ').Append(pair.Key.Length == 0 ? "xmlns=''" : "xmlns:" + pair.Key + "=''");
            }
        }

        for (var attribute = element.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            _ = text.Append(' ');
            Write(attribute, text);
        }

        if (element.FirstChild.IsEmpty)
        {
            _ = text.Append("/>");
            return;
        }

        _ = text.Append('>');
        for (var child = element.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            Write(child, text);
        }

        _ = text.Append("</").Append(name).Append('>');
    }

    private static string Prefix(XmlNode ns) => ns.Tree!.Names.GetString(ns.Name.Local);

    private static string Declaration(XmlNode ns) =>
        (Prefix(ns).Length == 0 ? "xmlns" : "xmlns:" + Prefix(ns)) + "='" + ns.StringValue + "'";

    private static string Qualified(XmlNode node)
    {
        var names = node.Tree!.Names;
        var name = node.Name;
        var prefix = names.GetString(name.Prefix);
        var local = names.GetString(name.Local);
        return prefix.Length == 0 ? local : prefix + ":" + local;
    }
}
