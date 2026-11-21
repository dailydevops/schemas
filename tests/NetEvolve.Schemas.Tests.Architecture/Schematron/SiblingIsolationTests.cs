namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using System.Reflection;
using NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>The Schematron and Xsd assemblies do not reference each other, by assembly reference and by type dependency.</summary>
public class SiblingIsolationTests
{
    private const string Fixtures = "NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Isolation";

    [Test]
    [Arguments(SourceAssemblies.Schematron, SourceAssemblies.Xsd)]
    [Arguments(SourceAssemblies.Xsd, SourceAssemblies.Schematron)]
    public async Task Assembly_DoesNotReference_ItsSibling(string name, string sibling) =>
        await Assert
            .That(SiblingReferences(name, sibling, SourceAssemblies.Load(name).GetReferencedAssemblies()))
            .IsEmpty();

    [Test]
    [Arguments(SourceAssemblies.Schematron, SourceAssemblies.Xsd)]
    [Arguments(SourceAssemblies.Xsd, SourceAssemblies.Schematron)]
    public async Task Assembly_HasNoTypeDependingOn_ItsSibling(string name, string sibling) =>
        await Assert
            .That(ArchitectureRules.FindTypesDependingOn(SourceAssemblies.Load(name), sibling, false))
            .IsEmpty();

    [Test]
    [Arguments(SourceAssemblies.Schematron, SourceAssemblies.Xsd)]
    [Arguments(SourceAssemblies.Xsd, SourceAssemblies.Schematron)]
    public async Task FixtureAssemblyReferencingTheSibling_IsReported(string name, string sibling) =>
        await Assert
            .That(SiblingReferences(name, sibling, [new AssemblyName(sibling)]))
            .IsEquivalentTo([$"{name} -> {sibling}"]);

    [Test]
    public async Task FixtureReferencingXPathAndXsd_IsReportedOnlyForTheSibling() =>
        await Assert
            .That(
                SiblingReferences(
                    SourceAssemblies.Schematron,
                    SourceAssemblies.Xsd,
                    [new AssemblyName(SourceAssemblies.XPath), new AssemblyName(SourceAssemblies.Xsd)]
                )
            )
            .Count()
            .IsEqualTo(1);

    [Test]
    public async Task FixtureTypeDependingOnTheSiblingNamespace_IsReported()
    {
        var violations = ArchitectureRules.FindTypesDependingOn(
            typeof(SiblingIsolationTests).Assembly,
            $"{Fixtures}.Xsd",
            false
        );

        await Assert.That(violations).IsEquivalentTo([$"{Fixtures}.Schematron.SchematronUsesXsdFixture"]);
    }

    // The reference direction rule of the package graph already reports a sibling as a disallowed reference.
    private static IReadOnlyList<string> SiblingReferences(
        string name,
        string sibling,
        IEnumerable<AssemblyName> referenced
    ) =>
        [
            .. ArchitectureRules
                .FindDisallowedReferences(name, referenced)
                .Where(violation => violation.EndsWith(" -> " + sibling, StringComparison.Ordinal)),
        ];
}
