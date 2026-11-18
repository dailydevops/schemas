namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

/// <summary>A rule set with its validator and its expectations; the name is the test set of the results.</summary>
/// <param name="Name">The rule set name.</param>
/// <param name="Validator">The validator built for the rule set.</param>
/// <param name="Expectations">The expectations, one per document.</param>
internal sealed record GoldenRuleSet(
    string Name,
    IGoldenValidator Validator,
    IReadOnlyList<GoldenExpectation> Expectations
);
