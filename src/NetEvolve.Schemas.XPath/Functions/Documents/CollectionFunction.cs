namespace NetEvolve.Schemas.XPath.Functions.Documents;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// <c>fn:collection($arg as xs:string?) as node()*</c> (Functions and Operators 1.0, 15.5.6): raises FODC0002 for every call, with and
/// without an argument. The library holds no collection and the resolver gives access to single resources only, so no collection can
/// be retrieved.
/// </summary>
/// <remarks>
/// Limitation: working <c>fn:collection</c> and <c>fn:uri-collection</c> are DEF-49 (#61). The error is FODC0002 also for an argument
/// that is no URI reference, where the specification allows FODC0004, because no URI is looked at.
/// </remarks>
internal sealed class CollectionFunction : DocumentFunction
{
    /// <summary>Initializes a new instance of the <see cref="CollectionFunction"/> class.</summary>
    public CollectionFunction()
        : base("collection", 0, 1, [OptionalString], null, null) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">Always: FODC0002.</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) =>
        throw XPathErrors.Exception(XPathErrorCodes.FODC0002);
}
