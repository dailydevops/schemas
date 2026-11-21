namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Parses the stylesheet fragment of a test from text with the Xml reader, so a test reads like the stylesheet it stands for. The
/// fragment sits under a stylesheet root that declares <c>xsl</c>, <c>f</c> (<c>urn:f</c>), and <c>ex</c> (<c>urn:ex</c>).
/// </summary>
internal static class ConstructionSheet
{
    internal const string Xslt = "http://www.w3.org/1999/XSL/Transform";

    /// <summary>Parses a document.</summary>
    /// <param name="xml">The document text.</param>
    /// <returns>The tree, with positions.</returns>
    internal static XmlDocumentTree Parse(string xml)
    {
        var builder = new XmlTreeBuilder(null, null, true);
        try
        {
            using var reader = XmlPipelineReader.Create(xml.AsSpan());
            var status = reader.Pump(ref builder);
            return status == XmlReadStatus.Completed
                ? builder.Complete()
                : throw new InvalidOperationException(
                    "The test document is not well formed: " + reader.Diagnostic?.Message
                );
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Parses a fragment under the stylesheet root and returns its first element, the instruction under test.</summary>
    /// <param name="fragment">The instruction as text.</param>
    /// <param name="rootAttributes">More attributes of the stylesheet root, such as <c>exclude-result-prefixes="f"</c>.</param>
    /// <param name="index">Which child element of the root to return.</param>
    /// <returns>The element.</returns>
    internal static XPathNode Fragment(string fragment, string rootAttributes = "", int index = 0)
    {
        var tree = Parse(
            $"<xsl:stylesheet xmlns:xsl=\"{Xslt}\" xmlns:f=\"urn:f\" xmlns:ex=\"urn:ex\" version=\"2.0\" {rootAttributes}>{fragment}</xsl:stylesheet>"
        );
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

    /// <summary>Parses a source document and returns its root element.</summary>
    /// <param name="xml">The document text.</param>
    /// <returns>The root element.</returns>
    internal static XPathNode Source(string xml) => new(Parse(xml).Document.FirstChild);
}
