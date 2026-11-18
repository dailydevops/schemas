namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Arguments;

/// <summary>The expected result of validating one document with one rule set.</summary>
internal sealed record GoldenExpectation
{
    /// <summary>Initializes a new instance of the <see cref="GoldenExpectation"/> class.</summary>
    /// <param name="source">The document.</param>
    /// <param name="expectedRuleIds">
    /// The ids of the rules that must fire. Compared as a set: a rule that fires once per context node is expected once.
    /// </param>
    /// <param name="expectedValid">Whether the document is expected to be valid.</param>
    public GoldenExpectation(GoldenDocumentSource source, IReadOnlyList<string> expectedRuleIds, bool expectedValid)
    {
        Argument.ThrowIfNull(source);
        Argument.ThrowIfNull(expectedRuleIds);
        foreach (var id in expectedRuleIds)
        {
            Argument.ThrowIfNullOrWhiteSpace(id);
        }

        Source = source;
        ExpectedRuleIds = expectedRuleIds;
        ExpectedValid = expectedValid;
    }

    /// <summary>Gets the document.</summary>
    public GoldenDocumentSource Source { get; init; }

    /// <summary>Gets the ids of the rules that must fire, compared as a set.</summary>
    public IReadOnlyList<string> ExpectedRuleIds { get; init; }

    /// <summary>Gets a value indicating whether the document is expected to be valid.</summary>
    public bool ExpectedValid { get; init; }
}
