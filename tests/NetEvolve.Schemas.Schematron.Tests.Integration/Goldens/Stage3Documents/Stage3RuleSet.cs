namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

/// <summary>One rule set that looks up a code database through <c>document()</c>, with its sample invoices.</summary>
/// <param name="Name">The short name of the rule set; it is the test set of the results.</param>
/// <param name="Schema">The path of the schema relative to the suite root.</param>
/// <param name="Documents">The sample invoices.</param>
public sealed record Stage3RuleSet(string Name, string Schema, IReadOnlyList<Stage3Document> Documents);
