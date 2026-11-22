namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>The seam between the runner and a Schematron validator; the Engine folder binds the real one, a fake serves the runner tests.</summary>
internal interface ISchematronValidator
{
    /// <summary>Validates the instance of a case with the schema of the case.</summary>
    /// <param name="schematronCase">The case.</param>
    /// <param name="cancellationToken">A token to cancel the validation.</param>
    /// <returns>The observation; <see cref="SchematronOutcome.NotImplemented"/> as outcome when nothing was validated.</returns>
    Task<SchematronObservation> ValidateAsync(SchematronCase schematronCase, CancellationToken cancellationToken);
}
