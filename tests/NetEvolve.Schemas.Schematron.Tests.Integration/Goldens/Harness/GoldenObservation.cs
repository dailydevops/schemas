namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Arguments;

/// <summary>What a validator observed for one document.</summary>
internal sealed record GoldenObservation
{
    /// <summary>Initializes a new instance of the <see cref="GoldenObservation"/> class.</summary>
    /// <param name="isValid">Whether the document is valid.</param>
    /// <param name="firedRuleIds">
    /// The ids of the fired rules, in any order and with repeats. A rule without an id cannot be expected, so the
    /// harness ignores an empty or whitespace-only id.
    /// </param>
    public GoldenObservation(bool isValid, IReadOnlyList<string> firedRuleIds)
    {
        Argument.ThrowIfNull(firedRuleIds);
        IsValid = isValid;
        FiredRuleIds = firedRuleIds;
    }

    /// <summary>Gets a value indicating whether the document is valid.</summary>
    public bool IsValid { get; init; }

    /// <summary>Gets the ids of the fired rules, in any order and with repeats.</summary>
    public IReadOnlyList<string> FiredRuleIds { get; init; }
}
