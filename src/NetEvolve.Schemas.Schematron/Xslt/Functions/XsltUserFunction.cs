namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The signature-only host function of one stylesheet function name and arity (design #150 EP-13): it makes a call compile, and holds no
/// body. At invoke time it reads the calls of the run from the host state slot, resolves the function that won the import precedence,
/// and runs it through <see cref="XsltFunctionCaller"/>, which converts the arguments and the result by the declared types. Immutable
/// and thread-safe.
/// </summary>
/// <remarks>
/// The function declares no parameter types, so the argument reaches the caller as evaluated and the conversion of the stylesheet
/// function applies once. A failed call hands the problem of the callee to the run (<see cref="IXsltFunctionRun.FailWith"/>), so the
/// outermost evaluation reports it.
/// </remarks>
internal sealed class XsltUserFunction : XPathFunction
{
    internal XsltUserFunction(XPathQName name, int arity)
        : base(name, arity, arity, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (
            invocation.Context.Host.HostState is not XsltFunctionHost host
            || !host.Table.TryGet(Name, MinArity, out var function)
        )
        {
            // The public code type, not the internal table of the XPath package, which the seam rule keeps out of reach.
            invocation.Diagnostic = new XPathDiagnostic(
                new XPathErrorCode("XPST0017"),
                XPathErrorKind.Dynamic,
                $"The stylesheet function '{Name.LocalName}' with {MinArity} arguments is not available in this run.",
                -1,
                0
            );
            return XPathEvaluationStatus.Error;
        }

        if (host.Caller.TryCall(function, invocation.Arguments, out var result) == QueryStatus.Ok)
        {
            invocation.Result = result;
            return XPathEvaluationStatus.Ok;
        }

        var problem = host.Caller.GetProblem();
        host.Run.FailWith(in problem);
        invocation.Diagnostic = new XPathDiagnostic(
            new XPathErrorCode("FOER0000"),
            XPathErrorKind.Dynamic,
            problem.Message,
            -1,
            0
        );
        return XPathEvaluationStatus.Error;
    }
}
