namespace NetEvolve.Schemas.Tests.Architecture.PackageGraph;

using NetEvolve.Schemas.Tests.Architecture.Shared;
using NetEvolve.Schemas.Tests.Architecture.Shared.Fixtures;

public class InteropBoundaryTests
{
    [Test]
    [MethodDataSource(typeof(SourceAssemblies), nameof(SourceAssemblies.Names))]
    public async Task SourceAssembly_UsesSystemXml_OnlyInInteropNamespaces(string name)
    {
        var violations = ArchitectureRules.FindTypesDependingOn(SourceAssemblies.Load(name), "System.Xml", true);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    [MethodDataSource(typeof(SourceAssemblies), nameof(SourceAssemblies.Names))]
    public async Task SourceAssembly_DoesNotUseSystemTextJson(string name)
    {
        var violations = ArchitectureRules.FindTypesDependingOn(SourceAssemblies.Load(name), "System.Text.Json", false);

        await Assert.That(violations).IsEmpty();
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
    public async Task FixtureUsingSystemTextJson_IsReported()
    {
        var violations = ArchitectureRules.FindTypesDependingOn(
            typeof(JsonUsageFixture).Assembly,
            "System.Text.Json",
            false
        );

        await Assert.That(violations).Contains(typeof(JsonUsageFixture).FullName!);
    }

    [Test]
    public async Task SourceFiles_InteropFolderAndNamespace_Match()
    {
        var files = ArchitectureRules.ReadSourceFiles();

        await Assert.That(files).IsNotEmpty();
        await Assert.That(ArchitectureRules.FindInteropMismatches(files)).IsEmpty();
    }

    [Test]
    public async Task FixtureInInteropFolderWithoutInteropNamespace_IsReported()
    {
        var violations = ArchitectureRules.FindInteropMismatches([
            (
                "src/NetEvolve.Schemas.Xml/Interop/Reader.cs",
                "namespace NetEvolve.Schemas.Xml;\npublic class Reader { }"
            ),
        ]);

        await Assert.That(violations).Count().IsEqualTo(1);
    }

    [Test]
    public async Task FixtureInInteropNamespaceOutsideInteropFolder_IsReported()
    {
        var violations = ArchitectureRules.FindInteropMismatches([
            (
                "src/NetEvolve.Schemas.Xml/Reader.cs",
                "namespace NetEvolve.Schemas.Xml.Interop;\npublic class Reader { }"
            ),
        ]);

        await Assert.That(violations).Count().IsEqualTo(1);
    }

    [Test]
    public async Task FixtureMatchingFolderAndNamespace_IsNotReported()
    {
        var violations = ArchitectureRules.FindInteropMismatches([
            (
                "src/NetEvolve.Schemas.Xml/Interop/Reader.cs",
                "namespace NetEvolve.Schemas.Xml.Interop;\npublic class Reader { }"
            ),
            ("src/NetEvolve.Schemas.Xml/Reader.cs", "namespace NetEvolve.Schemas.Xml;\npublic class Reader { }"),
        ]);

        await Assert.That(violations).IsEmpty();
    }
}
