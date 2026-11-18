namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

internal sealed class FakeGoldenValidator(Func<GoldenDocument, GoldenObservation> verdict) : IGoldenValidator
{
    public List<string> Calls { get; } = [];

    public Task<GoldenObservation> ValidateAsync(GoldenDocument document, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(document.Id);
        return Task.FromResult(verdict(document));
    }
}
