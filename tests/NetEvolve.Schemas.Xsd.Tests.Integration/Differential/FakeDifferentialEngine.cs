namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

internal sealed class FakeDifferentialEngine(Func<DifferentialCase, EngineResult> result) : IDifferentialEngine
{
    public List<string> Calls { get; } = [];

    public Task<EngineResult> RunAsync(DifferentialCase differentialCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(differentialCase.Id);
        return Task.FromResult(result(differentialCase));
    }
}
