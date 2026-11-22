namespace NetEvolve.Schemas.Xml.Interop;

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Copies an <see cref="XDocument"/> into the document model.</summary>
/// <remarks>
/// An <see cref="XName"/> carries no prefix, so the prefix of a name is the first one bound to its namespace by a declaration
/// on the element or an ancestor. A namespace used without any declaration gets one: the default namespace for an element,
/// a generated prefix for an attribute.
/// </remarks>
internal static class XDocumentImporter
{
    internal static XmlTree Import(XDocument document, XmlInteropOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var emitter = new TreeEmitter(options);
        var declaration = document.Declaration;
        emitter.StartDocument(
            declaration?.Version,
            declaration?.Encoding,
            TreeEmitter.ParseStandalone(declaration?.Standalone)
        );

        // Iterative, so the depth of the source tree cannot overflow the stack.
        var node = document.FirstNode;
        while (node is not null)
        {
            if (node is XElement start)
            {
                StartElement(start, emitter);
                if (start.FirstNode is { } child)
                {
                    node = child;
                    continue;
                }
            }
            else
            {
                Leaf(node, emitter);
            }

            while (true)
            {
                if (node is XElement end)
                {
                    emitter.EndElement(
                        PrefixOf(end, end.Name.NamespaceName, false) ?? string.Empty,
                        end.Name.LocalName,
                        end.Name.NamespaceName
                    );
                }

                var sibling = node.NextNode;
                if (sibling is not null)
                {
                    node = sibling;
                    break;
                }

                node = node.Parent;
                if (node is null)
                {
                    break;
                }
            }
        }

        return emitter.Complete();
    }

    private static void Leaf(XNode node, TreeEmitter emitter)
    {
        switch (node)
        {
            case XCData cdata:
                emitter.Text(cdata.Value, XmlTextKind.CData);
                break;
            case XText text:
                emitter.Text(text.Value, XmlTextKind.Text);
                break;
            case XComment comment:
                emitter.Comment(comment.Value);
                break;
            case XProcessingInstruction instruction:
                emitter.ProcessingInstruction(instruction.Target, instruction.Data);
                break;
            case XDocumentType type:
                emitter.Doctype(
                    type.Name,
                    type.PublicId,
                    type.SystemId,
                    type.InternalSubset,
                    TreeEmitter.Position(type, 0)
                );
                break;
            default:
                break;
        }
    }

    private static void StartElement(XElement element, TreeEmitter emitter)
    {
        var uri = element.Name.NamespaceName;
        var prefix = PrefixOf(element, uri, false);

        // The element position points at its name, one column after the angle bracket of the start tag.
        emitter.StartElement(prefix ?? string.Empty, element.Name.LocalName, uri, TreeEmitter.Position(element, -1));
        if (prefix is null && uri.Length != 0)
        {
            emitter.NamespaceDeclaration(string.Empty, uri);
        }

        // The sink contract wants every namespace declaration before the first attribute.
        for (var a = element.FirstAttribute; a is not null; a = a.NextAttribute)
        {
            if (a.IsNamespaceDeclaration)
            {
                emitter.NamespaceDeclaration(
                    a.Name.NamespaceName.Length == 0 ? string.Empty : a.Name.LocalName,
                    a.Value
                );
            }
        }

        List<(string Uri, string Prefix)>? generated = null;
        for (var a = element.FirstAttribute; a is not null; a = a.NextAttribute)
        {
            var attributeUri = a.Name.NamespaceName;
            if (!a.IsNamespaceDeclaration && attributeUri.Length != 0 && PrefixOf(element, attributeUri, true) is null)
            {
                generated ??= [];
                if (!Generated(generated, attributeUri, out _))
                {
                    var name = "p" + (generated.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    generated.Add((attributeUri, name));
                    emitter.NamespaceDeclaration(name, attributeUri);
                }
            }
        }

        for (var a = element.FirstAttribute; a is not null; a = a.NextAttribute)
        {
            if (a.IsNamespaceDeclaration)
            {
                continue;
            }

            var attributeUri = a.Name.NamespaceName;
            var attributePrefix = attributeUri.Length == 0 ? string.Empty : PrefixOf(element, attributeUri, true);
            if (attributePrefix is null && generated is not null)
            {
                _ = Generated(generated, attributeUri, out attributePrefix);
            }

            emitter.Attribute(attributePrefix ?? string.Empty, a.Name.LocalName, attributeUri, a.Value);
        }

        emitter.EndAttributes();
    }

    private static bool Generated(List<(string Uri, string Prefix)> generated, string uri, out string? prefix)
    {
        foreach (var (generatedUri, generatedPrefix) in generated)
        {
            if (string.Equals(generatedUri, uri, StringComparison.Ordinal))
            {
                prefix = generatedPrefix;
                return true;
            }
        }

        prefix = null;
        return false;
    }

    // The first prefix bound to the namespace by an xmlns attribute of the element or an ancestor; an attribute needs a
    // non-empty prefix, because an unprefixed attribute is in no namespace.
    private static string? PrefixOf(XElement element, string uri, bool forAttribute)
    {
        if (uri.Length == 0)
        {
            return string.Empty;
        }

        for (var e = element; e is not null; e = e.Parent)
        {
            for (var a = e.FirstAttribute; a is not null; a = a.NextAttribute)
            {
                if (a.IsNamespaceDeclaration && string.Equals(a.Value, uri, StringComparison.Ordinal))
                {
                    var prefix = a.Name.NamespaceName.Length == 0 ? string.Empty : a.Name.LocalName;
                    if (!forAttribute || prefix.Length != 0)
                    {
                        return prefix;
                    }
                }
            }
        }

        return null;
    }
}
