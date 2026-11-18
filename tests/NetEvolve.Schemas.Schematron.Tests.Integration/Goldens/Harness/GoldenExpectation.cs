namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

/// <summary>The expected result of validating one document with one rule set.</summary>
/// <param name="Source">The document.</param>
/// <param name="ExpectedRuleIds">
/// The ids of the rules that must fire. Compared as a set: a rule that fires once per context node is expected once.
/// </param>
/// <param name="ExpectedValid">Whether the document is expected to be valid.</param>
internal sealed record GoldenExpectation(
    GoldenDocumentSource Source,
    IReadOnlyList<string> ExpectedRuleIds,
    bool ExpectedValid
);
