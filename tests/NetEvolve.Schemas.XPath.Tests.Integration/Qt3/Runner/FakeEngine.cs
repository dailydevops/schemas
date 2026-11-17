namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

internal sealed class FakeEngine(Func<Qt3Case, EngineResult> evaluate) : IQt3Engine
{
    public int Calls { get; private set; }

    public EngineResult Evaluate(Qt3Case testCase)
    {
        Calls++;
        return evaluate(testCase);
    }
}
