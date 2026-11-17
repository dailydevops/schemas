namespace NetEvolve.Schemas.Schematron.Results.Mapping;

using NetEvolve.Schemas.Schematron.Diagnostics;

/// <summary>Builds the stable assertion identifier of design #137 AP-13.</summary>
internal static class AssertionIdentifier
{
    /// <summary>Creates the identifier of one assertion of the effective model.</summary>
    /// <param name="assertionId">The author id of the assertion, or <see langword="null"/>.</param>
    /// <param name="ruleId">The author id of the rule, or <see langword="null"/>.</param>
    /// <param name="pattern">The one-based ordinal of the pattern or group.</param>
    /// <param name="rule">The one-based ordinal of the rule within its pattern.</param>
    /// <param name="assertion">The one-based ordinal of the assertion within its rule.</param>
    /// <param name="expansion">The one-based ordinal of the expansion the assertion was copied by, 0 for an authored assertion.</param>
    /// <returns>The identifier.</returns>
    internal static string Create(
        string? assertionId,
        string? ruleId,
        int pattern,
        int rule,
        int assertion,
        int expansion = 0
    ) => throw NotImplementedFailure.For("assertion identifier");
}
