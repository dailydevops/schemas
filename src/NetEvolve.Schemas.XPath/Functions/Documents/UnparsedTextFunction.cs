namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.XPath.Api;

/// <summary><c>fn:unparsed-text</c>; the body follows with the implementation.</summary>
internal class UnparsedTextFunction : DocumentFunction
{
    /// <summary>Initializes a new instance of the <see cref="UnparsedTextFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    public UnparsedTextFunction(Uri? staticBaseUri = null)
        : base("unparsed-text", 1, 2, [], staticBaseUri, null) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => XPathEvaluationStatus.Ok;
}
