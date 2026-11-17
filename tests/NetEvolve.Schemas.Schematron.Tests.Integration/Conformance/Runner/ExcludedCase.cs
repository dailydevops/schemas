namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>A case that is left out of the run, with the reason.</summary>
/// <param name="Case">The case.</param>
/// <param name="Reason">Why the case is excluded.</param>
internal sealed record ExcludedCase(SchematronCase Case, string Reason);
