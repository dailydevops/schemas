namespace NetEvolve.Schemas.Tests.Architecture.PackageGraph;

using System.Reflection;
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
    [Arguments(SourceAssemblies.Schematron, SourceAssemblies.DependencyInjection)]
    public async Task FixtureReferencingHigherOrSiblingLayer_IsReported(string name, string other)
    {
        var violations = ArchitectureRules.FindDisallowedReferences(name, [new AssemblyName(other)]);

        await Assert.That(violations).Count().IsEqualTo(1);
    }

    [Test]
    public async Task FixtureAssemblyReferencingEveryLayer_IsReported()
    {
        // The test assembly itself references all layers and therefore violates the rules of the lowest one.
        var violations = ArchitectureRules.FindDisallowedReferences(
            SourceAssemblies.Schemas,
            typeof(ReferenceDirectionTests).Assembly.GetReferencedAssemblies()
        );

        await Assert.That(violations).IsNotEmpty();
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
