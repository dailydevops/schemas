namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of <c>fn:parse-xml</c> and <c>fn:parse-xml-fragment</c> (Functions and Operators 3.1, 14.9): the string is parsed with the
/// reader options of the function, which are the secure defaults of the XML package, so a document type declaration is refused. A
/// string that does not parse is FODC0006; the empty sequence gives the empty sequence.
/// </summary>
/// <remarks>
/// Limitation: a call reaches the evaluation context only, which carries no static context, so the function holds the static base URI
/// itself, like <see cref="Documents.DocFunction"/>; the one in the default catalog holds none, and the nodes it returns have no base
/// URI. A host that wants one registers a function built over it (#752, #806).
/// </remarks>
internal abstract class XmlTextFunction : Delta3Function
{
    private readonly Uri? _baseUri;
    private readonly XmlReaderOptions _options;

    /// <summary>Initializes a new instance of the <see cref="XmlTextFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none; a relative URI counts as none.</param>
    /// <param name="options">The reader options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="localName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected XmlTextFunction(string localName, Uri? staticBaseUri, XmlReaderOptions? options)
        : base(localName, 1, 1, [OptionalString])
    {
        _baseUri = staticBaseUri is { IsAbsoluteUri: true } ? staticBaseUri : null;
        _options = options ?? XmlReaderOptions.Default;
    }

    /// <summary>Gets the static base URI that the function holds, <see langword="null"/> for none.</summary>
    protected Uri? BaseUri => _baseUri;

    /// <summary>Gets the reader options.</summary>
    protected XmlReaderOptions Options => _options;

    /// <inheritdoc />
    /// <exception cref="XPathException">The string is not well formed (FODC0006).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (!argument.IsEmpty)
        {
            _ = argument[0].TryGetString(out var text);
            XmlDocumentTree tree;
            try
            {
                tree = Parse(text!);
            }
            catch (XmlParseException)
            {
                throw Delta3Errors.InvalidXml();
            }

            invocation.Result = XdmSequence.Create(XdmItem.FromNode(new XPathNode(tree.Document)));
        }

        return XPathEvaluationStatus.Ok;
    }

    /// <summary>Parses the string into a tree whose first node is a document node.</summary>
    /// <param name="text">The string.</param>
    /// <returns>The tree.</returns>
    /// <exception cref="XmlParseException">The string is not well formed or breaks a limit.</exception>
    protected abstract XmlDocumentTree Parse(string text);
}
