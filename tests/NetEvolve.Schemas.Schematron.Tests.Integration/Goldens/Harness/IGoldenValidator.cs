namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

/// <summary>The seam between the harness and the validator of one rule set; a fake fills it until SCH-55 binds the real one.</summary>
internal interface IGoldenValidator
{
    /// <summary>Validates a document with the rule set this validator was built for.</summary>
    /// <param name="document">The document.</param>
    /// <param name="cancellationToken">A token to cancel the validation.</param>
    /// <returns>The validity and the fired rules.</returns>
    Task<GoldenObservation> ValidateAsync(GoldenDocument document, CancellationToken cancellationToken);
}
