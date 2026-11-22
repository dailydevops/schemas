namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

/// <summary>The seam between the runner and an XPath engine; <c>Qt3Engine</c> binds the library to it.</summary>
internal interface IQt3Engine
{
    /// <summary>Evaluates the query of a case; an engine failure is returned as an error result, never thrown.</summary>
    /// <param name="testCase">The case.</param>
    /// <returns>The sequence, the error, or <see cref="EngineResult.NotBound"/>.</returns>
    EngineResult Evaluate(Qt3Case testCase);
}
