namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>A scenario, expectation, or file that is left out of the run, with the reason.</summary>
/// <param name="Id">The id; it has the form of a case id.</param>
/// <param name="TestSet">The test set: the <c>.xspec</c> file.</param>
/// <param name="Reason">Why it is left out.</param>
internal sealed record XspecExclusion(string Id, string TestSet, string Reason);
