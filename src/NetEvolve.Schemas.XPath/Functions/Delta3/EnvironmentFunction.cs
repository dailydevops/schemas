namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// <c>fn:environment-variable</c> and <c>fn:available-environment-variables</c> (Functions and Operators 3.1, 16.4 and 16.5): the
/// first gives the empty sequence for every name, the second gives the empty sequence.
/// </summary>
/// <remarks>
/// Limitation: the environment of the process is never exposed, so a query cannot read a secret from it. The specification allows
/// this: a processor may hide any variable, and then the variable does not exist to the query. A host that wants some variables
/// registers a function of its own, which overrides this one through the layering of the function registry.
/// </remarks>
internal sealed class EnvironmentFunction : Delta3Function
{
    /// <summary>Initializes a new instance of the <see cref="EnvironmentFunction"/> class.</summary>
    /// <param name="all"><see langword="true"/> for <c>fn:available-environment-variables</c>, <see langword="false"/> for <c>fn:environment-variable</c>.</param>
    public EnvironmentFunction(bool all)
        : base(
            all ? "available-environment-variables" : "environment-variable",
            all ? 0 : 1,
            all ? 0 : 1,
            ParametersOf(all)
        ) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => XPathEvaluationStatus.Ok;

    private static XPathParameterType[] ParametersOf(bool all) => all ? [] : [OneString];
}
