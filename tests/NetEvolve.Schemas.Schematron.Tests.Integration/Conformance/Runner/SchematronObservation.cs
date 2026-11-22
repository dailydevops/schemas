namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>What a validator observed for a case: the outcome and, when the case does not pass, why.</summary>
/// <param name="Outcome">The observed outcome.</param>
/// <param name="Reason">
/// Why the case fails although the outcome may match, for example an assertion on the SVRL output that does not hold;
/// <see langword="null"/> when nothing is wrong.
/// </param>
internal sealed record SchematronObservation(SchematronOutcome Outcome, string? Reason = null);
