namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>The real engine behind the runner seam; the binding is not written yet.</summary>
internal sealed class Qt3Engine(string suiteRoot) : IQt3Engine
{
    public string SuiteRoot { get; } = suiteRoot;

    public EngineResult Evaluate(Qt3Case testCase) => EngineResult.NotBound;
}
