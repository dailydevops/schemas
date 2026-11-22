namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Nodes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of the document functions <c>fn:doc</c>, <c>fn:doc-available</c>, and <c>fn:collection</c>, and of the text functions: every
/// one is a built-in function of the <c>fn</c> namespace that holds the static base URI itself, like <see cref="Functions.QNames.StaticBaseUriFunction"/>,
/// and reads the argument as <c>xs:string?</c>.
/// </summary>
/// <remarks>
/// Limitation: a call reaches the evaluation context only, which carries no static context, so the function holds the static base URI and
/// the reader options; the ones in the default catalog hold none and use the secure reader defaults, so a relative reference is
/// unresolvable and a document type declaration is refused. A host that wants either registers a function built over them (the layering
/// of the function registry lets it override the one in the catalog). Wiring the static context to a call is #752 and, for the base URI,
/// #806; binding these functions to the schema base URI is SCH-63.
/// </remarks>
internal abstract class DocumentFunction : NodeFunction
{
    private readonly string? _staticBaseUri;
    private readonly XmlReaderOptions _options;

    /// <summary>Initializes a new instance of the <see cref="DocumentFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none; a relative URI counts as none.</param>
    /// <param name="options">The reader options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="localName"/> or <paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected DocumentFunction(
        string localName,
        int minArity,
        int maxArity,
        IReadOnlyList<XPathParameterType> parameters,
        Uri? staticBaseUri,
        XmlReaderOptions? options
    )
        : base(localName, minArity, maxArity, parameters)
    {
        _staticBaseUri = ResourceUris.BaseOf(staticBaseUri);
        _options = options ?? XmlReaderOptions.Default;
    }

    /// <summary>Gets <c>xs:string?</c>: the argument is atomized, an <c>xs:untypedAtomic</c> is cast, and an <c>xs:anyURI</c> is promoted.</summary>
    protected static XPathParameterType OptionalString => new(XPathItemType.String, Occurrence.ZeroOrOne);

    /// <summary>Gets the static base URI that the function holds.</summary>
    protected string? StaticBaseUri => _staticBaseUri;

    /// <summary>Reads an argument that the function conversion has matched against <c>xs:string?</c>.</summary>
    /// <param name="argument">The argument: empty or one string.</param>
    /// <returns>The string; the zero-length string for the empty sequence.</returns>
    protected static string Text(in XdmSequence argument)
    {
        if (argument.IsEmpty)
        {
            return string.Empty;
        }

        _ = argument[0].TryGetString(out var text);
        return text!;
    }

    /// <summary>Gets the document of a reference from the pool of the context, loading it through the resolver of the context on first use.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="reference">The reference as written.</param>
    /// <param name="tree">The document when the result is <see cref="DocumentStatus.Loaded"/>.</param>
    /// <returns>The status.</returns>
    protected DocumentStatus TryLoad(XPathEvaluationContext context, string reference, out XmlDocumentTree? tree)
    {
        tree = null;
        var status = ResourceUris.Resolve(reference, _staticBaseUri, out var absolute);
        if (status == ResourceUriStatus.Invalid)
        {
            return DocumentStatus.InvalidUri;
        }

        if (status == ResourceUriStatus.NoBase || context.ResourceResolver is not { } resolver)
        {
            return DocumentStatus.Unavailable;
        }

        tree = DocumentPool.For(context).GetDocument(resolver, reference, _staticBaseUri, absolute!, _options);
        return tree is null ? DocumentStatus.Unavailable : DocumentStatus.Loaded;
    }
}
