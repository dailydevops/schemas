namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>current-group()</c> (XSLT 2.0 section 14.1, XSLT 3.0 section 14.2.1): the items of the group that <c>xsl:for-each-group</c> is
/// processing, read from the current group slot of the evaluation context. Where the slot is unset, which is outside every grouping
/// instruction and in a function body, the function returns the empty sequence under XSLT 2.0 and raises XTDE1061 under XSLT 3.0
/// (design #150 EP-13, rows G1 and G2), so the version decision is a constructor argument and the function holds no other state.
/// </summary>
/// <remarks>
/// The function declares no host requirement: the slot policy is its own, so an unset slot must not raise XPDY0002 and the function
/// must be found by a host that sets no slot flag. Limitation: the function is not in <see cref="XsltFunctionScope"/> yet, which is
/// pinned to the functions of its own folder, and the XPath binding does not pass the host flags to the registry (#297, #628); the
/// registration of <see cref="XsltGroupingFunctions.Create"/> belongs to the run that composes the scope.
/// </remarks>
internal sealed class CurrentGroupFunction : XPathHostFunction
{
    private readonly bool _raiseWhenAbsent;

    /// <summary>Initializes a new instance of the <see cref="CurrentGroupFunction"/> class.</summary>
    /// <param name="raiseWhenAbsent"><see langword="true"/> for XSLT 3.0, where an absent group is XTDE1061; <see langword="false"/> for XSLT 2.0, where it is the empty sequence.</param>
    internal CurrentGroupFunction(bool raiseWhenAbsent)
        : base(XsltFunctionScope.Name("current-group"), 0, 0) => _raiseWhenAbsent = raiseWhenAbsent;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (invocation.Context.Host.CurrentGroup is { } group)
        {
            invocation.Result = group;
            return XPathEvaluationStatus.Ok;
        }

        if (!_raiseWhenAbsent)
        {
            invocation.Result = XdmSequence.Empty;
            return XPathEvaluationStatus.Ok;
        }

        invocation.Diagnostic = new XPathDiagnostic(
            new XPathErrorCode("XTDE1061"),
            XPathErrorKind.Dynamic,
            "The current group is absent: current-group() is used outside xsl:for-each-group.",
            -1,
            0
        );
        return XPathEvaluationStatus.Error;
    }
}
