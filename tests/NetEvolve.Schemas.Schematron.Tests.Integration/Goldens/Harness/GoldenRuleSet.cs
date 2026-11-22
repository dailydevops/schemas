namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Arguments;

/// <summary>A rule set with its validator and its expectations; the name is the test set of the results.</summary>
/// <remarks>
/// Two expectations for the same document in one rule set produce two <c>CaseResult</c> values with the same case id.
/// </remarks>
internal sealed record GoldenRuleSet
{
    /// <summary>Initializes a new instance of the <see cref="GoldenRuleSet"/> class.</summary>
    /// <param name="name">The rule set name.</param>
    /// <param name="validator">The validator built for the rule set.</param>
    /// <param name="expectations">The expectations, one per document.</param>
    public GoldenRuleSet(string name, IGoldenValidator validator, IReadOnlyList<GoldenExpectation> expectations)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(expectations);
        foreach (var expectation in expectations)
        {
            ArgumentNullException.ThrowIfNull(expectation);
        }

        Name = name;
        Validator = validator;
        Expectations = expectations;
    }

    /// <summary>Gets the rule set name.</summary>
    public string Name { get; init; }

    /// <summary>Gets the validator built for the rule set.</summary>
    public IGoldenValidator Validator { get; init; }

    /// <summary>Gets the expectations, one per document.</summary>
    public IReadOnlyList<GoldenExpectation> Expectations { get; init; }
}
