namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>The seam between the runner and a Schematron validator; a fake fills it until SCH-56 binds the real one.</summary>
internal interface ISchematronValidator
{
    /// <summary>Validates the instance of a case with the schema of the case.</summary>
    /// <param name="schematronCase">The case.</param>
    /// <param name="cancellationToken">A token to cancel the validation.</param>
    /// <returns>The observed outcome; <see cref="SchematronOutcome.NotImplemented"/> when nothing was validated.</returns>
    Task<SchematronOutcome> ValidateAsync(SchematronCase schematronCase, CancellationToken cancellationToken);
}
