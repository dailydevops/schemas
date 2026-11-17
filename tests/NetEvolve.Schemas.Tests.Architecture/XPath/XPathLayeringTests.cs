namespace NetEvolve.Schemas.Tests.Architecture.XPath;

using NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>Namespace layering rules inside the XPath package, each proven against violating fixtures.</summary>
public class XPathLayeringTests
{
    private const string Fixtures = "NetEvolve.Schemas.Tests.Architecture.XPath.Fixtures";
    private const string PackagePrefix = "src/NetEvolve.Schemas.XPath/";

    // The layering list fixed by the XPath contract (#168, design #123 section 9). Removing a rule makes the set test red.
    private static readonly string[] ContractRules =
    [
        "SyntaxDoesNotDependOnEvaluationContextOrFunctions",
        "FunctionLibraryIsReachedOnlyThroughTheRegistry",
        "EveryFunctionGroupFolderHasExactlyOneRegistrationFile",
        "XPathDoesNotDependOnSchematronOrXsd",
    ];

    private static readonly XPathLayerInput Current = new(
        SourceAssemblies.Load(SourceAssemblies.XPath),
        SourceAssemblies.XPath,
        [SourceAssemblies.Schematron, SourceAssemblies.Xsd],
        [
            .. ArchitectureRules
                .ReadSourceFiles()
                .Select(file => file.Path)
                .Where(path => path.StartsWith(PackagePrefix, StringComparison.Ordinal))
                .Select(path => path[PackagePrefix.Length..]),
        ]
    );

    private static readonly XPathLayerInput Violating = new(
        typeof(XPathLayeringTests).Assembly,
        Fixtures,
        [$"{Fixtures}.Schematron", $"{Fixtures}.Xsd"],
        [
            "Functions/NoRegistration/Length.cs",
            "Functions/OneRegistration/Registration.cs",
            "Functions/OneRegistration/Length.cs",
            "Functions/TwoRegistrations/Registration.cs",
            "Functions/TwoRegistrations/Registration.Extra.cs",
            "Functions/Binding/Catalog.cs",
        ]
    );

    private static readonly Dictionary<string, string[]> ExpectedViolations = new(StringComparer.Ordinal)
    {
        [ContractRules[0]] = [$"{Fixtures}.Syntax.SyntaxDependsOnEvaluationFixture"],
        [ContractRules[1]] = [$"{Fixtures}.Evaluation.EvaluationUsesFunctionFixture"],
        [ContractRules[2]] = ["Functions/NoRegistration", "Functions/TwoRegistrations"],
        [ContractRules[3]] = [$"{Fixtures}.Evaluation.EvaluationUsesSchematronFixture"],
    };

    public static IEnumerable<string> RuleNames() => [.. XPathLayering.Rules.Select(rule => rule.Name)];

    private static XPathLayerRule Rule(string name) =>
        XPathLayering.Rules.Single(rule => string.Equals(rule.Name, name, StringComparison.Ordinal));

    [Test]
    public async Task RuleSet_EqualsTheLayeringListOfTheXPathContract() =>
        await Assert.That(RuleNames()).IsEquivalentTo(ContractRules);

    [Test]
    [MethodDataSource(nameof(RuleNames))]
    public async Task Rule_PassesOnTheXPathAssembly(string name) =>
        await Assert.That(Rule(name).Find(Current)).IsEmpty();

    [Test]
    [MethodDataSource(nameof(RuleNames))]
    public async Task Rule_ReportsTheViolatingFixtureByName(string name) =>
        await Assert.That(Rule(name).Find(Violating)).IsEquivalentTo(ExpectedViolations[name]);

    [Test]
    public async Task FunctionGroupFolderWithZeroRegistrationFiles_IsReported()
    {
        var input = Violating with { SourcePaths = ["Functions/Empty/Length.cs"] };

        await Assert.That(Rule(ContractRules[2]).Find(input)).IsEquivalentTo(["Functions/Empty"]);
    }

    [Test]
    public async Task FunctionGroupFolderWithTwoRegistrationFiles_IsReported()
    {
        var input = Violating with
        {
            SourcePaths = ["Functions/Dup/Registration.cs", "Functions/Dup/RegistrationMore.cs"],
        };

        await Assert.That(Rule(ContractRules[2]).Find(input)).IsEquivalentTo(["Functions/Dup"]);
    }

    [Test]
    public async Task FunctionGroupFolderWithOneMisnamedRegistrationFile_IsReported()
    {
        var input = Violating with { SourcePaths = [@"Functions\Odd\RegistrationFile.cs"] };

        await Assert.That(Rule(ContractRules[2]).Find(input)).IsEquivalentTo(["Functions/Odd"]);
    }
}
