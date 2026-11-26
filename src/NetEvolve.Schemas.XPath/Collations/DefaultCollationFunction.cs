namespace NetEvolve.Schemas.XPath.Collations;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:default-collation() as xs:string</c> (Functions and Operators 1.0, 7.3.2): the default collation of the static context, which is
/// the codepoint collation URI while no other collation is supported.
/// </summary>
/// <remarks>
/// Limitation: a call reaches the evaluation context only, which carries no static context, so the function always gives the codepoint
/// collation URI, as <c>fn:static-base-uri</c> holds its base URI itself. Wiring the static context to a call is #752.
/// </remarks>
internal sealed class DefaultCollationFunction : TextFunction
{
    /// <summary>Initializes a new instance of the <see cref="DefaultCollationFunction"/> class.</summary>
    public DefaultCollationFunction()
        : base("default-collation", 0, 0, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(XdmItem.FromString(CodepointCollation.Instance.Uri));
        return XPathEvaluationStatus.Ok;
    }
}
