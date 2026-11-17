namespace NetEvolve.Schemas.Xml.Interop;

using System;
using System.Xml;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using XmlNode = System.Xml.XmlNode;

/// <summary>Copies an <see cref="XmlDocument"/> into the document model; its nodes carry no line information.</summary>
internal static class XmlDocumentImporter
{
    internal static XmlTree Import(XmlDocument document, XmlInteropOptions? options)
    {
        Argument.ThrowIfNull(document);

        using var emitter = new TreeEmitter(options);
        var declaration = document.FirstChild as System.Xml.XmlDeclaration;
        emitter.StartDocument(
            declaration?.Version,
            declaration?.Encoding,
            TreeEmitter.ParseStandalone(declaration?.Standalone)
        );

        // Iterative, so the depth of the source tree cannot overflow the stack.
        var node = document.FirstChild;
        while (node is not null)
        {
            if (Enter(node, emitter))
            {
                node = node.FirstChild;
                continue;
            }

            while (true)
            {
                Exit(node, emitter);
                var sibling = node.NextSibling;
                if (sibling is not null)
                {
                    node = sibling;
                    break;
                }

                node = node.ParentNode;
                if (node is null || ReferenceEquals(node, document))
                {
                    node = null;
                    break;
                }
            }
        }

        return emitter.Complete();
    }

    // Returns whether the node has children to visit. An entity reference is transparent: its expansion is visited in place.
    private static bool Enter(XmlNode node, TreeEmitter emitter)
    {
        switch (node.NodeType)
        {
            case XmlNodeType.Element:
                StartElement((XmlElement)node, emitter);
                return node.HasChildNodes;
            case XmlNodeType.EntityReference:
                return node.HasChildNodes;
            case XmlNodeType.Text:
            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
                emitter.Text(node.Value!, XmlTextKind.Text);
                return false;
            case XmlNodeType.CDATA:
                emitter.Text(node.Value!, XmlTextKind.CData);
                return false;
            case XmlNodeType.Comment:
                emitter.Comment(node.Value!);
                return false;
            case XmlNodeType.ProcessingInstruction:
                var instruction = (XmlProcessingInstruction)node;
                emitter.ProcessingInstruction(instruction.Target, instruction.Data);
                return false;
            case XmlNodeType.DocumentType:
                var type = (XmlDocumentType)node;
                emitter.Doctype(type.Name, type.PublicId, type.SystemId, type.InternalSubset, default);
                return false;
            default:
                return false;
        }
    }

    private static void Exit(XmlNode node, TreeEmitter emitter)
    {
        if (node is XmlElement element)
        {
            emitter.EndElement(element.Prefix, element.LocalName, element.NamespaceURI);
        }
    }

    private static void StartElement(XmlElement element, TreeEmitter emitter)
    {
        emitter.StartElement(element.Prefix, element.LocalName, element.NamespaceURI, default);

        // The sink contract wants every namespace declaration before the first attribute.
        var attributes = element.Attributes;
        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            if (IsDeclaration(attribute))
            {
                emitter.NamespaceDeclaration(
                    attribute.Prefix.Length == 0 ? string.Empty : attribute.LocalName,
                    attribute.Value
                );
            }
        }

        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            if (!IsDeclaration(attribute))
            {
                emitter.Attribute(attribute.Prefix, attribute.LocalName, attribute.NamespaceURI, attribute.Value);
            }
        }

        emitter.EndAttributes();
    }

    private static bool IsDeclaration(XmlAttribute attribute) =>
        string.Equals(attribute.NamespaceURI, TreeEmitter.XmlnsUri, StringComparison.Ordinal);
}
