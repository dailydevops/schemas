namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;

/// <summary>
/// <c>fn:parse-xml</c> (Functions and Operators 3.1, 14.9.1): the string parsed as a well-formed document with one document element;
/// the result is a document node that has the static base URI. Anything else, a fragment of several elements included, is FODC0006.
/// </summary>
internal sealed class ParseXmlFunction : XmlTextFunction
{
    /// <summary>Initializes a new instance of the <see cref="ParseXmlFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    /// <param name="options">The reader options, <see langword="null"/> for the secure defaults.</param>
    public ParseXmlFunction(Uri? staticBaseUri = null, XmlReaderOptions? options = null)
        : base("parse-xml", staticBaseUri, options) { }

    /// <inheritdoc />
    protected override XmlDocumentTree Parse(string text) =>
        XmlDocumentLoader.LoadFromMemory(text.AsMemory(), Options, BaseUri);
}
