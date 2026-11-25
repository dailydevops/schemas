namespace NetEvolve.Schemas.Tests.Architecture.PackageGraph;

using System.Reflection;
using NetEvolve.Schemas.Architecture.Fixtures;
using NetEvolve.Schemas.Tests.Architecture.Shared;

public class ReferenceDirectionTests
{
    [Test]
    [MethodDataSource(typeof(SourceAssemblies), nameof(SourceAssemblies.Names))]
    public async Task Assembly_References_OnlyAllowedLayers(string name)
    {
        var violations = ArchitectureRules.FindDisallowedReferences(
            name,
            SourceAssemblies.Load(name).GetReferencedAssemblies()
        );

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    [Arguments(SourceAssemblies.Schemas, SourceAssemblies.Xml)]
    [Arguments(SourceAssemblies.Xml, SourceAssemblies.XPath)]
    [Arguments(SourceAssemblies.XPath, SourceAssemblies.Schematron)]
    [Arguments(SourceAssemblies.XPath, SourceAssemblies.Xsd)]
    [Arguments(SourceAssemblies.Xsd, SourceAssemblies.Schematron)]
    [Arguments(SourceAssemblies.Schematron, SourceAssemblies.Xsd)]
    [Arguments(SourceAssemblies.DependencyInjection, SourceAssemblies.Xml)]
    [Arguments(SourceAssemblies.DependencyInjection, SourceAssemblies.XPath)]
    [Arguments(SourceAssemblies.DependencyInjection, SourceAssemblies.Schematron)]
    [Arguments(SourceAssemblies.DependencyInjection, SourceAssemblies.Xsd)]
    [Arguments(SourceAssemblies.XPath, SourceAssemblies.DependencyInjection)]
    public async Task FixtureReferencingHigherOrSiblingLayer_IsReported(string name, string other)
    {
        var violations = ArchitectureRules.FindDisallowedReferences(name, [new AssemblyName(other)]);

        await Assert.That(violations).Count().IsEqualTo(1);
    }

    [Test]
    public async Task FixtureAssemblyReferencingXml_IsReportedWhenCheckedAsCore()
    {
        var violations = ArchitectureRules.FindDisallowedReferences(
            SourceAssemblies.Schemas,
            typeof(HigherLayerReferenceFixture).Assembly.GetReferencedAssemblies()
        );

        await Assert.That(violations).Contains("NetEvolve.Schemas -> NetEvolve.Schemas.Xml");
    }

    [Test]
    public async Task Fixture_ReadsNameFromXmlSymbol() =>
        await Assert.That(HigherLayerReferenceFixture.Name).IsEqualTo("XmlSymbol");

    [Test]
    [MethodDataSource(typeof(SourceAssemblies), nameof(SourceAssemblies.Names))]
    public async Task ArgumentsReference_IsNotReported(string name)
    {
        var violations = ArchitectureRules.FindDisallowedReferences(name, [new AssemblyName("NetEvolve.Arguments")]);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task UnlistedPolyfillPackage_IsReported()
    {
        var violations = ArchitectureRules.FindDisallowedReferences(
            SourceAssemblies.Xml,
            [new AssemblyName("Microsoft.Bcl.Memory")]
        );

        await Assert.That(violations).Count().IsEqualTo(1);
    }

    [Test]
    public async Task SystemTextJsonReference_IsReported()
    {
        var violations = ArchitectureRules.FindDisallowedReferences(
            SourceAssemblies.Xml,
            [new AssemblyName("System.Text.Json")]
        );

        await Assert.That(violations).Count().IsEqualTo(1);
    }

    [Test]
    public async Task LowerLayerAndFrameworkReferences_AreNotReported()
    {
        var violations = ArchitectureRules.FindDisallowedReferences(
            SourceAssemblies.Xsd,
            [
                new AssemblyName(SourceAssemblies.Schemas),
                new AssemblyName(SourceAssemblies.Xml),
                new AssemblyName(SourceAssemblies.XPath),
                new AssemblyName("netstandard"),
                new AssemblyName("System.Xml"),
                new AssemblyName("System.Memory"),
                new AssemblyName("Microsoft.Bcl.AsyncInterfaces"),
            ]
        );

        await Assert.That(violations).IsEmpty();
    }
}
