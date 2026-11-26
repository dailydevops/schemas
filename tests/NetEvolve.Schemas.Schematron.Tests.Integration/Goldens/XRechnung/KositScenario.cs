namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.XRechnung;

/// <summary>One scenario of the KoSIT validator configuration: the match expression, the schema, and the rule sets in order.</summary>
/// <param name="Name">The scenario name.</param>
/// <param name="Namespaces">The prefix bindings of the match expression.</param>
/// <param name="Match">The XPath expression that selects the instances of the scenario.</param>
/// <param name="XsdLocation">The schema location, relative to the configuration root.</param>
/// <param name="RuleSets">The Schematron steps in the order of the scenario.</param>
/// <param name="CustomLevels">The severity overrides by rule id.</param>
internal sealed record KositScenario(
    string Name,
    IReadOnlyDictionary<string, string> Namespaces,
    string Match,
    string XsdLocation,
    IReadOnlyList<KositRuleSet> RuleSets,
    IReadOnlyDictionary<string, string> CustomLevels
);
