namespace NetEvolve.Schemas.Xml.Interop;

using System;
using System.Xml;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Copies the nodes an <see cref="XmlReader"/> delivers into the document model.</summary>
internal static class XmlReaderImporter
{
    internal static XmlTree Import(XmlReader reader, XmlInteropOptions? options)
    {
        Argument.ThrowIfNull(reader);

        using var emitter = new TreeEmitter(options);
        var lineInfo = reader as IXmlLineInfo;
        var more = reader.ReadState == ReadState.Initial ? reader.Read() : !reader.EOF;
        if (more && reader.NodeType == XmlNodeType.XmlDeclaration)
        {
            emitter.StartDocument(
                reader.GetAttribute("version"),
                reader.GetAttribute("encoding"),
                TreeEmitter.ParseStandalone(reader.GetAttribute("standalone"))
            );
            more = reader.Read();
        }
        else
        {
            emitter.StartDocument(null, null, null);
        }

        while (more)
        {
            Node(reader, lineInfo, emitter);
            more = reader.Read();
        }

        return emitter.Complete();
    }

    private static void Node(XmlReader reader, IXmlLineInfo? lineInfo, TreeEmitter emitter)
    {
        switch (reader.NodeType)
        {
            case XmlNodeType.Element:
                Element(reader, lineInfo, emitter);
                break;
            case XmlNodeType.EndElement:
                emitter.EndElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                break;
            case XmlNodeType.Text:
            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
                emitter.Text(reader.Value, XmlTextKind.Text);
                break;
            case XmlNodeType.CDATA:
                emitter.Text(reader.Value, XmlTextKind.CData);
                break;
            case XmlNodeType.Comment:
                emitter.Comment(reader.Value);
                break;
            case XmlNodeType.ProcessingInstruction:
                emitter.ProcessingInstruction(reader.Name, reader.Value);
                break;
            case XmlNodeType.DocumentType:
                emitter.Doctype(
                    reader.Name,
                    reader.GetAttribute("PUBLIC"),
                    reader.GetAttribute("SYSTEM"),
                    reader.Value,
                    TreeEmitter.Position(lineInfo, 0)
                );
                break;
            default:
                // Entity references and their ends are not mapped (limitation of XML-18).
                break;
        }
    }

    private static void Element(XmlReader reader, IXmlLineInfo? lineInfo, TreeEmitter emitter)
    {
        var prefix = reader.Prefix;
        var local = reader.LocalName;
        var uri = reader.NamespaceURI;
        var isEmpty = reader.IsEmptyElement;

        // The reader points at the element name, one column after the angle bracket of the start tag.
        emitter.StartElement(prefix, local, uri, TreeEmitter.Position(lineInfo, -1));
        if (reader.HasAttributes)
        {
            // The sink contract wants every namespace declaration before the first attribute.
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    if (IsDeclaration(reader))
                    {
                        emitter.NamespaceDeclaration(
                            reader.Prefix.Length == 0 ? string.Empty : reader.LocalName,
                            reader.Value
                        );
                    }
                } while (reader.MoveToNextAttribute());
            }

            _ = reader.MoveToElement();
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    if (!IsDeclaration(reader))
                    {
                        emitter.Attribute(reader.Prefix, reader.LocalName, reader.NamespaceURI, reader.Value);
                    }
                } while (reader.MoveToNextAttribute());
            }

            _ = reader.MoveToElement();
        }

        emitter.EndAttributes();
        if (isEmpty)
        {
            emitter.EndElement(prefix, local, uri);
        }
    }

    private static bool IsDeclaration(XmlReader reader) =>
        string.Equals(reader.NamespaceURI, TreeEmitter.XmlnsUri, StringComparison.Ordinal);
}
