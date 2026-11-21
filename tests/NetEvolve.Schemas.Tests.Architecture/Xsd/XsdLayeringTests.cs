namespace NetEvolve.Schemas.Tests.Architecture.Xsd;

using NetEvolve.Schemas.Tests.Architecture.Schematron;
using NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>Namespace layering rules inside the Xsd package, each proven against violating fixtures.</summary>
public class XsdLayeringTests
{
    private const string Fixtures = "NetEvolve.Schemas.Tests.Architecture.Xsd.Fixtures";

    // The layering list fixed by the Xsd contract (#146 DP-1, XSD-50 #192). Removing a rule makes the set test red.
    private static readonly string[] ContractRules =
    [
        "SchemaDocumentsDependOnlyOnContractsAndDiagnostics",
        "ContractsDependOnlyOnDiagnostics",
        "CompositionDependsOnlyOnTheSchemaDocumentSyntax",
        "ComponentRegistryDependsOnlyOnTheSchemaDocumentSyntax",
        "SchemaModelDoesNotDependOnTheRuntimeValidator",
    ];

    private static readonly LayerInput Current = new(SourceAssemblies.Load(SourceAssemblies.Xsd), SourceAssemblies.Xsd);

    private static readonly LayerInput Violating = new(typeof(XsdLayeringTests).Assembly, Fixtures);

    // The simple type fixture also depends on the validator and is not listed: that namespace is exempt from the last rule.
    private static readonly Dictionary<string, string[]> ExpectedViolations = new(StringComparer.Ordinal)
    {
        [ContractRules[0]] = [$"{Fixtures}.SchemaDocuments.SchemaDocumentsUseComponentsFixture"],
        [ContractRules[1]] = [$"{Fixtures}.Contracts.ContractsUseCompositionFixture"],
        [ContractRules[2]] = [$"{Fixtures}.Composition.CompositionUsesComponentsFixture"],
        [ContractRules[3]] = [$"{Fixtures}.Components.ComponentsUseCompositionFixture"],
        [ContractRules[4]] = [$"{Fixtures}.Elements.ElementsUseValidationFixture"],
    };

    public static IEnumerable<string> RuleNames() => [.. XsdLayering.Rules.Select(rule => rule.Name)];

    private static LayerRule Rule(string name) =>
        XsdLayering.Rules.Single(rule => string.Equals(rule.Name, name, StringComparison.Ordinal));

    [Test]
    public async Task RuleSet_EqualsTheLayeringListOfTheXsdContract() =>
        await Assert.That(RuleNames()).IsEquivalentTo(ContractRules);

    [Test]
    [MethodDataSource(nameof(RuleNames))]
    public async Task Rule_PassesOnTheXsdAssembly(string name) => await Assert.That(Rule(name).Find(Current)).IsEmpty();

    [Test]
    [MethodDataSource(nameof(RuleNames))]
    public async Task Rule_ReportsTheViolatingFixtureByName(string name) =>
        await Assert.That(Rule(name).Find(Violating)).IsEquivalentTo(ExpectedViolations[name]);
}
