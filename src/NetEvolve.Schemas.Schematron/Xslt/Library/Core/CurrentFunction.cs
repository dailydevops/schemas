namespace NetEvolve.Schemas.Schematron.Xslt.Library.Core;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>current()</c> (XSLT 2.0, 16.6.1): the item that was the context item where the expression started. The evaluator writes it into
/// the current item slot (design #150 EP-13) once per rule firing, and a predicate or a path step changes the focus but not the slot.
/// The slot is cleared on entering a function body, so there the function raises XPDY0002 (design #150, G6).
/// </summary>
/// <remarks>
/// Limitation: XSLT 2.0 names XTDE1360 for an undefined context item; the approved design pins XPDY0002 and this function follows
/// the design. Nothing writes the slot yet: the run of the XPath binding copies the focus into it (#297, #628).
/// </remarks>
internal sealed class CurrentFunction : XPathFunction
{
    internal CurrentFunction()
        : base(XsltFunctionScope.Name("current"), 0, 0, []) { }

    /// <inheritdoc />
    public override XPathHostRequirements HostRequirements => XPathHostRequirements.CurrentItem;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (invocation.Context.Host.CurrentItem is not { } item)
        {
            // The public code type, not the internal table of the XPath package, which the seam rule keeps out of reach.
            invocation.Diagnostic = new XPathDiagnostic(
                new XPathErrorCode("XPDY0002"),
                XPathErrorKind.Dynamic,
                "The current item has no value: the focus is absent, as in the body of a stylesheet function.",
                -1,
                0
            );
            return XPathEvaluationStatus.Error;
        }

        invocation.Result = XdmSequence.Create(in item);
        return XPathEvaluationStatus.Ok;
    }
}
