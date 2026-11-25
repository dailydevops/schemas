namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.Xml.Model.Traversal;

/// <summary>
/// <c>fn:parse-xml-fragment</c> (Functions and Operators 3.1, 14.9.2): the string parsed as the content of an element, so it may hold
/// several top-level elements, text, comments, and processing instructions, or nothing; the result is a document node that has these as
/// children and the static base URI. A leading XML declaration is accepted and ignored.
/// </summary>
/// <remarks>
/// The string is wrapped in a dummy element, parsed, and its content is copied below a new document node, so the XML package needs no
/// fragment mode. A string that closes the dummy element early leaves a document with more than one root, which does not parse.
/// </remarks>
internal sealed class ParseXmlFragmentFunction : XmlTextFunction
{
    /// <summary>Initializes a new instance of the <see cref="ParseXmlFragmentFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    /// <param name="options">The reader options, <see langword="null"/> for the secure defaults.</param>
    public ParseXmlFragmentFunction(Uri? staticBaseUri = null, XmlReaderOptions? options = null)
        : base("parse-xml-fragment", staticBaseUri, options) { }

    /// <inheritdoc />
    protected override XmlDocumentTree Parse(string text)
    {
        var wrapped = "<w>" + WithoutDeclaration(text) + "</w>";
        var parsed = XmlDocumentLoader.LoadFromMemory(wrapped.AsMemory(), Options, BaseUri);

        using var builder = new XmlTreeBuilder(documentUri: BaseUri, trackPositions: false);
        builder.StartDocument(default);
        for (var child = parsed.Document.FirstChild.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            XmlTreePump.Pump(child, builder);
        }

        builder.EndDocument();
        return builder.Complete();
    }

    private static string WithoutDeclaration(string text)
    {
        if (
            text.StartsWith("<?xml", StringComparison.Ordinal)
            && text.Length > 5
            && char.IsWhiteSpace(text[5])
            && text.IndexOf("?>", StringComparison.Ordinal) is var end
            && end > 0
        )
        {
            return text.Substring(end + 2);
        }

        return text;
    }
}
