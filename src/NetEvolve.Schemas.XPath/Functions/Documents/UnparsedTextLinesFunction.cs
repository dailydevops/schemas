namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.XPath.Api;

/// <summary><c>fn:unparsed-text-lines</c>; the body follows with the implementation.</summary>
internal sealed class UnparsedTextLinesFunction : DocumentFunction
{
    /// <summary>Initializes a new instance of the <see cref="UnparsedTextLinesFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    public UnparsedTextLinesFunction(Uri? staticBaseUri = null)
        : base("unparsed-text-lines", 1, 2, [], staticBaseUri, null) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => XPathEvaluationStatus.Ok;
}
