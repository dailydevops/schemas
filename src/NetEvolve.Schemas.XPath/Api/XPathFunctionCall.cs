#pragma warning disable CA1815 // Approved shape (#123): a call is a transient by-reference carrier and defines no value equality.

namespace NetEvolve.Schemas.XPath.Api;

using System;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>One call of a host function: the arguments in, the result and a diagnostic out.</summary>
public ref struct XPathFunctionCall
{
    /// <summary>Initializes a new instance of the <see cref="XPathFunctionCall"/> struct.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="arguments">The arguments, one sequence each.</param>
    public XPathFunctionCall(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments)
    {
        Context = context;
        Arguments = arguments;
        Result = XdmSequence.Empty;
        Diagnostic = default;
    }

    /// <summary>Gets the evaluation context.</summary>
    public XPathEvaluationContext Context { get; }

    /// <summary>Gets the arguments.</summary>
    public ReadOnlySpan<XdmSequence> Arguments { get; }

    /// <summary>Gets or sets the result.</summary>
    public XdmSequence Result { get; set; }

    /// <summary>Gets or sets the diagnostic when the function reports <see cref="XPathEvaluationStatus.Error"/>.</summary>
    public XPathDiagnostic Diagnostic { get; set; }
}
