namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

internal sealed class FakeSchematronValidator(Func<SchematronCase, SchematronOutcome> verdict) : ISchematronValidator
{
    public List<string> Calls { get; } = [];

    public Task<SchematronObservation> ValidateAsync(SchematronCase schematronCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(schematronCase.Id);
        return Task.FromResult(new SchematronObservation(verdict(schematronCase)));
    }
}
