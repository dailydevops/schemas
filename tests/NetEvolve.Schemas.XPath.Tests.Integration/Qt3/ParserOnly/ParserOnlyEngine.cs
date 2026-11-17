namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.ParserOnly;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>The engine of the parser-only runs: no evaluator, so it reports a syntax error or nothing at all.</summary>
internal sealed class ParserOnlyEngine : IQt3Engine
{
    public EngineResult Evaluate(Qt3Case testCase) => EngineResult.NotBound;
}
