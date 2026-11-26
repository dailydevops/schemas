namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.XRechnung;

/// <summary>A Schematron step of a scenario, named by the rule layer (<c>cen</c> or <c>xrechnung</c>) and the syntax (<c>ubl</c> or <c>cii</c>).</summary>
/// <param name="Layer">The rule layer.</param>
/// <param name="Syntax">The syntax.</param>
internal sealed record KositRuleSet(string Layer, string Syntax);
