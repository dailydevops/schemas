namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>A test that applies to a version but is left out of the run, with the reason.</summary>
/// <param name="Id">The case id as <see cref="XstsSelectedCase.Id"/>.</param>
/// <param name="TestSet">The test set key.</param>
/// <param name="Reason">Why the test is excluded.</param>
internal sealed record XstsExcludedCase(string Id, string TestSet, string Reason);
