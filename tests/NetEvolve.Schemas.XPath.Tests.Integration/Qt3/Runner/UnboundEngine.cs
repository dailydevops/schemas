namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

/// <summary>The engine used until the evaluator is bound: every active assertion fails.</summary>
internal sealed class UnboundEngine : IQt3Engine
{
    public EngineResult Evaluate(Qt3Case testCase) => EngineResult.NotBound;
}
