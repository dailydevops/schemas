namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>Namespace layering rules inside the Schematron package, each proven against violating fixtures.</summary>
public class SchematronLayeringTests
{
    private const string Fixtures = "NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures";

    // The layering list fixed by the Schematron contract (#137 sections 2 and 9, SCH-50 #180). Removing a rule makes the set test red.
    private static readonly string[] ContractRules =
    [
        "ModelDependsOnNothingElseInThePackage",
        "SeamsDependOnlyOnTheModelAndDiagnostics",
        "ParsingDependsOnlyOnTheModelAndSeams",
        "LoadingDependsOnlyOnParsingTheModelAndSeams",
        "ExpansionDependsOnlyOnTheModelAndSeams",
        "EvaluationDoesNotReachIntoTheOtherStages",
        "MapperDependsOnlyOnTheModelAndSeams",
        "WriterDependsOnlyOnTheModelAndSeams",
        "BindingsDependOnlyOnTheModelAndSeams",
    ];

    private static readonly LayerInput Current = new(
        SourceAssemblies.Load(SourceAssemblies.Schematron),
        SourceAssemblies.Schematron
    );

    private static readonly LayerInput Violating = new(typeof(SchematronLayeringTests).Assembly, Fixtures);

    private static readonly Dictionary<string, string[]> ExpectedViolations = new(StringComparer.Ordinal)
    {
        [ContractRules[0]] = [$"{Fixtures}.Model.ModelUsesParsingFixture"],
        [ContractRules[1]] = [$"{Fixtures}.Seams.SeamsUseNewcomerFixture"],
        [ContractRules[2]] = [$"{Fixtures}.Parsing.ParsingUsesLoadingFixture"],
        [ContractRules[3]] = [$"{Fixtures}.Loading.LoadingUsesExpansionFixture"],
        [ContractRules[4]] = [$"{Fixtures}.Expansion.ExpansionUsesParsingFixture"],
        [ContractRules[5]] = [$"{Fixtures}.Evaluation.EvaluationUsesSvrlFixture"],
        [ContractRules[6]] = [$"{Fixtures}.Results.MapperUsesEvaluationFixture"],
        [ContractRules[7]] = [$"{Fixtures}.Svrl.WriterUsesResultsFixture"],
        [ContractRules[8]] = [$"{Fixtures}.Bindings.BindingsUseXsltFixture"],
    };

    public static IEnumerable<string> RuleNames() => [.. SchematronLayering.Rules.Select(rule => rule.Name)];

    private static LayerRule Rule(string name) =>
        SchematronLayering.Rules.Single(rule => string.Equals(rule.Name, name, StringComparison.Ordinal));

    [Test]
    public async Task RuleSet_EqualsTheLayeringListOfTheSchematronContract() =>
        await Assert.That(RuleNames()).IsEquivalentTo(ContractRules);

    [Test]
    [MethodDataSource(nameof(RuleNames))]
    public async Task Rule_PassesOnTheSchematronAssembly(string name) =>
        await Assert.That(Rule(name).Find(Current)).IsEmpty();

    [Test]
    [MethodDataSource(nameof(RuleNames))]
    public async Task Rule_ReportsTheViolatingFixtureByName(string name) =>
        await Assert.That(Rule(name).Find(Violating)).IsEquivalentTo(ExpectedViolations[name]);
}
