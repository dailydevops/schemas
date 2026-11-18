namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Arguments;

internal sealed class FakeGoldenValidator : IGoldenValidator
{
    private readonly Func<GoldenDocument, GoldenObservation> _verdict;

    public FakeGoldenValidator(Func<GoldenDocument, GoldenObservation> verdict)
    {
        Argument.ThrowIfNull(verdict);
        _verdict = verdict;
    }

    public List<string> Calls { get; } = [];

    public Task<GoldenObservation> ValidateAsync(GoldenDocument document, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(document);
        Calls.Add(document.Id);
        return Task.FromResult(_verdict(document));
    }
}
