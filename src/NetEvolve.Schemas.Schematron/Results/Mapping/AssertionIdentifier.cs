namespace NetEvolve.Schemas.Schematron.Results.Mapping;

using System.Globalization;

/// <summary>Builds the stable assertion identifier of design #137 AP-13.</summary>
/// <remarks>
/// The hash sign is not an NCName character, so a generated identifier never collides with an author identifier.
/// Limitation: only an author assertion id carries the expansion suffix (design AP-13), so the rule-based form
/// <c>{ruleId}#a{A}</c> repeats for two instances of one expanded rule.
/// </remarks>
internal static class AssertionIdentifier
{
    /// <summary>Creates the identifier of one assertion of the effective model.</summary>
    /// <param name="assertionId">The author id of the assertion, or <see langword="null"/>.</param>
    /// <param name="ruleId">The author id of the rule, or <see langword="null"/>.</param>
    /// <param name="pattern">The one-based ordinal of the pattern or group.</param>
    /// <param name="rule">The one-based ordinal of the rule within its pattern.</param>
    /// <param name="assertion">The one-based ordinal of the assertion within its rule.</param>
    /// <param name="expansion">The one-based ordinal of the expansion that copied an assertion with an author id, 0 for an authored assertion.</param>
    /// <returns>The identifier.</returns>
    internal static string Create(
        string? assertionId,
        string? ruleId,
        int pattern,
        int rule,
        int assertion,
        int expansion = 0
    )
    {
        if (assertionId is { Length: > 0 })
        {
            return Expanded(assertionId, expansion);
        }

        if (ruleId is { Length: > 0 })
        {
            return ruleId + "#a" + Text(assertion);
        }

        // The ordinals already make the identifier unique in the effective model.
        return "#p" + Text(pattern) + ".r" + Text(rule) + ".a" + Text(assertion);
    }

    private static string Expanded(string id, int expansion) => expansion > 0 ? id + "#i" + Text(expansion) : id;

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
