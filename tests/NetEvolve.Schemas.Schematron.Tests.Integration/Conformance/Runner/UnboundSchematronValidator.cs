namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>The validator of this package: none is bound, so every case is a known failure.</summary>
internal sealed class UnboundSchematronValidator : ISchematronValidator
{
    public static UnboundSchematronValidator Instance { get; } = new();

    public Task<SchematronOutcome> ValidateAsync(SchematronCase schematronCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SchematronOutcome.NotImplemented);
    }
}
