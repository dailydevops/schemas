namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>The Schematron validator behind the runner seam.</summary>
internal sealed class SchematronEngine : ISchematronValidator
{
    public static SchematronEngine Instance { get; } = new();

    public Task<SchematronObservation> ValidateAsync(
        SchematronCase schematronCase,
        CancellationToken cancellationToken
    ) => Task.FromResult(new SchematronObservation(SchematronOutcome.NotImplemented));
}
