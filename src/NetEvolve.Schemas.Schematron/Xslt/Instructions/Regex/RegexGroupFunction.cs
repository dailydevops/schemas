namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Regex;

using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>regex-group()</c> (XSLT 2.0 section 15.2): the text of the capture group with the given number in the current matching
/// substring of <c>xsl:analyze-string</c>, read from the regex groups slot of the evaluation context. Group zero is the whole match.
/// The result is the zero-length string where the slot is unset (outside a matching substring and inside a non-matching one), for a
/// number that is negative or above the number of groups, and for a group that took no part in the match.
/// </summary>
/// <remarks>
/// The function declares no host requirement, because an unset slot is not an error. Limitation: the function is not in
/// <see cref="XsltFunctionScope"/> yet, as <c>current-group</c> is not (#297, #628, #803); the run that composes the scope adds it.
/// </remarks>
internal sealed class RegexGroupFunction : XPathFunction
{
    internal RegexGroupFunction()
        : base(
            XsltFunctionScope.Name("regex-group"),
            1,
            1,
            [new XPathParameterType(XPathItemType.Integer, Occurrence.ExactlyOne)]
        ) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        _ = invocation.Arguments[0][0].TryGetInteger(out var number);
        var text = string.Empty;
        if (invocation.Context.Host.RegexGroups is { } groups && number >= 0 && number < groups.Count)
        {
            text = groups.GetGroup((int)number).ToString();
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromString(text));
        return XPathEvaluationStatus.Ok;
    }
}
