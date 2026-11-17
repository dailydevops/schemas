namespace NetEvolve.Schemas.Tests.Architecture.Xml;

using NetEvolve.Schemas.Tests.Architecture.Shared;
using NetEvolve.Schemas.Tests.Architecture.Shared.Fixtures;

public class XmlNamespaceLayeringTests
{
    private const string Fixtures = "NetEvolve.Schemas.Tests.Architecture.Xml.Fixtures";

    private static System.Reflection.Assembly XmlAssembly => SourceAssemblies.Load(SourceAssemblies.Xml);

    // Character classes and the Unicode tables depend on nothing else; the name table only on the contract it implements.
    public static IEnumerable<Func<(string Namespace, string[] Allowed)>> CoreNamespaces() =>
        [
            () => ($"{XmlLayering.Root}.Text.CharClasses", ["Text"]),
            () => ($"{XmlLayering.Root}.Text.Unicode", ["Text"]),
            () => ($"{XmlLayering.Root}.Text.NameTable", ["Text", "Contracts"]),
        ];

    public static IEnumerable<string> DatatypeLayerNamespaces() =>
        [$"{XmlLayering.Root}.Datatypes", $"{XmlLayering.Root}.Regex"];

    private static readonly string[] ParserLayers = ["Reader", "Input", "Model", "Writer", "Dtd"];

    private static string[] ParserLayerNamespaces => [.. ParserLayers.Select(layer => $"{XmlLayering.Root}.{layer}")];

    [Test]
    [MethodDataSource(nameof(CoreNamespaces))]
    public async Task CoreNamespace_DependsOnNothingElseInThePackage(string ns, string[] allowed)
    {
        var forbidden = XmlLayering.NamespacesExcept(XmlAssembly, allowed);

        var violations = XmlLayering.FindDependencies(XmlAssembly, ns, forbidden);

        await Assert.That(forbidden).IsNotEmpty();
        await Assert.That(violations).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(DatatypeLayerNamespaces))]
    public async Task DatatypesAndRegex_DoNotDependOnParserLayers(string ns)
    {
        var violations = XmlLayering.FindDependencies(XmlAssembly, ns, ParserLayerNamespaces);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task InteropNamespace_IsALeaf()
    {
        var violations = ArchitectureRules.FindTypesDependingOn(XmlAssembly, $"{XmlLayering.Root}.Interop", true);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task XmlPackage_UsesSystemXml_OnlyInInteropNamespace()
    {
        var violations = ArchitectureRules.FindTypesDependingOn(XmlAssembly, "System.Xml", true);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task FixtureCoreDependingOnReader_IsReported()
    {
        var violations = XmlLayering.FindDependencies(
            typeof(XmlNamespaceLayeringTests).Assembly,
            $"{Fixtures}.Core",
            [$"{Fixtures}.Reader"]
        );

        await Assert.That(violations).Contains($"{Fixtures}.Core.CoreDependsOnReaderFixture");
    }

    [Test]
    public async Task FixtureDatatypesDependingOnReader_IsReported()
    {
        var violations = XmlLayering.FindDependencies(
            typeof(XmlNamespaceLayeringTests).Assembly,
            $"{Fixtures}.Datatypes",
            [$"{Fixtures}.Reader"]
        );

        await Assert.That(violations).Contains($"{Fixtures}.Datatypes.DatatypesDependsOnReaderFixture");
    }

    [Test]
    public async Task FixtureTableDependingOnContracts_IsOnlyReportedWhenContractsAreForbidden()
    {
        var assembly = typeof(XmlNamespaceLayeringTests).Assembly;
        var table = $"{Fixtures}.Table";

        var allowed = XmlLayering.FindDependencies(assembly, table, [$"{Fixtures}.Reader"]);
        var forbidden = XmlLayering.FindDependencies(assembly, table, [$"{Fixtures}.Contracts"]);

        await Assert.That(allowed).IsEmpty();
        await Assert.That(forbidden).Contains($"{table}.TableImplementsContractFixture");
    }

    [Test]
    public async Task FixtureDependingOnInterop_IsReported()
    {
        var violations = ArchitectureRules.FindTypesDependingOn(
            typeof(XmlNamespaceLayeringTests).Assembly,
            $"{Fixtures}.Interop",
            true
        );

        await Assert.That(violations).Contains($"{Fixtures}.Other.DependsOnInteropFixture");
    }

    [Test]
    public async Task FixtureUsingSystemXmlOutsideInterop_IsReported()
    {
        var violations = ArchitectureRules.FindTypesDependingOn(
            typeof(XmlOutsideInteropFixture).Assembly,
            "System.Xml",
            true
        );

        await Assert.That(violations).Contains(typeof(XmlOutsideInteropFixture).FullName!);
    }

    [Test]
    public async Task NamespacesExcept_ListsPackageNamespacesWithoutTheAllowedOnes()
    {
        var namespaces = XmlLayering.NamespacesExcept(XmlAssembly, "Text");

        await Assert.That(namespaces).Contains($"{XmlLayering.Root}.Reader");
        await Assert.That(namespaces).DoesNotContain($"{XmlLayering.Root}.Text");
    }
}
