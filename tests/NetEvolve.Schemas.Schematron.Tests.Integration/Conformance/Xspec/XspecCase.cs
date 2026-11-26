namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>One expectation with everything needed to check it.</summary>
/// <param name="Id">The case id: the test set, the position in the file, the scenario path, and the expectation.</param>
/// <param name="TestSet">The test set: the <c>.xspec</c> file.</param>
/// <param name="SchemaPath">The full path of the Schematron schema.</param>
/// <param name="Phase">The phase to validate with, if any.</param>
/// <param name="Context">The instance document.</param>
/// <param name="Expectation">The expectation.</param>
internal sealed record XspecCase(
    string Id,
    string TestSet,
    string SchemaPath,
    string? Phase,
    XspecContext Context,
    XspecExpectation Expectation
);
