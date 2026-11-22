namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Nodes;

/// <summary><c>fn:doc-available</c>; the body follows with the implementation.</summary>
internal sealed class DocAvailableFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="DocAvailableFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    /// <param name="options">The reader options, <see langword="null"/> for the secure defaults.</param>
    public DocAvailableFunction(Uri? staticBaseUri = null, XmlReaderOptions? options = null)
        : base("doc-available", 1, 1, [])
    {
        _ = staticBaseUri;
        _ = options;
    }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => XPathEvaluationStatus.Ok;
}
