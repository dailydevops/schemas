namespace NetEvolve.Schemas.XPath.Collations;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:codepoint-equal</c> (Functions and Operators 1.0, 7.3.1): whether two strings are equal code point by code point, and the empty
/// sequence when an argument is the empty sequence. It always uses the codepoint collation and takes no collation argument.
/// </summary>
internal sealed class CodepointEqualFunction : TextFunction
{
    /// <summary>Initializes a new instance of the <see cref="CodepointEqualFunction"/> class.</summary>
    public CodepointEqualFunction()
        : base("codepoint-equal", 2, 2, [OptionalString, OptionalString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        if (!arguments[0].IsEmpty && !arguments[1].IsEmpty)
        {
            var equal = CodepointCollation.Instance.AreEqual(Text(arguments[0]), Text(arguments[1]));
            invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(equal));
        }

        return XPathEvaluationStatus.Ok;
    }
}
