namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>The split of the catalog into cases that run and cases that are excluded.</summary>
/// <param name="Selected">The cases to run.</param>
/// <param name="Excluded">The cases left out, each with a reason.</param>
internal sealed record CaseSelection(IReadOnlyList<SchematronCase> Selected, IReadOnlyList<ExcludedCase> Excluded);
