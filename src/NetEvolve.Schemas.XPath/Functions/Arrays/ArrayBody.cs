namespace NetEvolve.Schemas.XPath.Functions.Arrays;

using NetEvolve.Schemas.XPath.Api;

/// <summary>The body of a function of the <c>array</c> namespace: it reads the converted arguments and sets the result.</summary>
/// <param name="invocation">The call.</param>
/// <returns>The status.</returns>
internal delegate XPathEvaluationStatus ArrayBody(ref XPathFunctionCall invocation);
