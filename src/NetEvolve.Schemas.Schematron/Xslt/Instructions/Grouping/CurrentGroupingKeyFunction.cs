namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>current-grouping-key()</c> (XSLT 2.0 section 14.2, XSLT 3.0 section 14.2.2): the grouping key of the group that
/// <c>xsl:for-each-group</c> is processing, read from the grouping key slot of the evaluation context. The slot is unset outside every
/// grouping instruction, in a function body, and for a grouping by pattern, which has no key. There the function returns the empty
/// sequence under XSLT 2.0 and raises XTDE1071 under XSLT 3.0 (design #150 EP-13, rows G1 and G5). A slot that holds the empty sequence
/// is set and gives the empty sequence under both.
/// </summary>
/// <remarks>See <see cref="CurrentGroupFunction"/> for the host requirement and for the registration.</remarks>
internal sealed class CurrentGroupingKeyFunction : XPathHostFunction
{
    private readonly bool _raiseWhenAbsent;

    /// <summary>Initializes a new instance of the <see cref="CurrentGroupingKeyFunction"/> class.</summary>
    /// <param name="raiseWhenAbsent"><see langword="true"/> for XSLT 3.0, where an absent key is XTDE1071; <see langword="false"/> for XSLT 2.0, where it is the empty sequence.</param>
    internal CurrentGroupingKeyFunction(bool raiseWhenAbsent)
        : base(XsltFunctionScope.Name("current-grouping-key"), 0, 0) => _raiseWhenAbsent = raiseWhenAbsent;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (invocation.Context.Host.CurrentGroupingKey is { } key)
        {
            invocation.Result = key;
            return XPathEvaluationStatus.Ok;
        }

        if (!_raiseWhenAbsent)
        {
            invocation.Result = XdmSequence.Empty;
            return XPathEvaluationStatus.Ok;
        }

        invocation.Diagnostic = new XPathDiagnostic(
            new XPathErrorCode("XTDE1071"),
            XPathErrorKind.Dynamic,
            "The current grouping key is absent: current-grouping-key() is used outside xsl:for-each-group, or in a grouping by pattern.",
            -1,
            0
        );
        return XPathEvaluationStatus.Error;
    }
}
