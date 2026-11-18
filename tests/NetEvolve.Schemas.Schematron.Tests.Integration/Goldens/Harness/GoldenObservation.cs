namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

/// <summary>What a validator observed for one document.</summary>
/// <param name="IsValid">Whether the document is valid.</param>
/// <param name="FiredRuleIds">
/// The ids of the fired rules, in any order and with repeats. A rule without an id cannot be expected, so the
/// harness ignores an empty id.
/// </param>
internal sealed record GoldenObservation(bool IsValid, IReadOnlyList<string> FiredRuleIds);
