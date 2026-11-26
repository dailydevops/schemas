namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

internal sealed class FakeSchematronValidator(Func<SchematronCase, SchematronOutcome> verdict) : ISchematronValidator
{
    public List<string> Calls { get; } = [];

    public Task<SchematronObservation> ValidateAsync(
        SchematronCase schematronCase,
        string binding,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(schematronCase.Id + "@" + binding);
        return Task.FromResult(new SchematronObservation(verdict(schematronCase)));
    }
}
