namespace NetEvolve.Schemas.XPath.Collations;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Strings;

/// <summary><c>fn:codepoint-equal</c> (Functions and Operators 1.0, 7.3.1).</summary>
internal sealed class CodepointEqualFunction : TextFunction
{
    /// <summary>Initializes a new instance of the <see cref="CodepointEqualFunction"/> class.</summary>
    public CodepointEqualFunction()
        : base("codepoint-equal", 2, 2, [OptionalString, OptionalString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => XPathEvaluationStatus.Ok;
}
