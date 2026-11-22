namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.XPath.Api;

/// <summary><c>fn:unparsed-text-available</c>; the body follows with the implementation.</summary>
internal class UnparsedTextAvailableFunction : DocumentFunction
{
    /// <summary>Initializes a new instance of the <see cref="UnparsedTextAvailableFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    public UnparsedTextAvailableFunction(Uri? staticBaseUri = null)
        : base("unparsed-text-available", 1, 2, [], staticBaseUri, null) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => XPathEvaluationStatus.Ok;
}
