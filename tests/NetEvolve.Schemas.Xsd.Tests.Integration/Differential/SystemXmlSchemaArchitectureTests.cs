namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>System.Xml.Schema is a comparison baseline: the test project may use it, no source project may.</summary>
public class SystemXmlSchemaArchitectureTests
{
    [Test]
    public async Task Src_NeverReferencesSystemXmlSchema() =>
        await Assert.That(SourceScan.FindSystemXmlSchemaUsage(Path.Combine(RepositoryFiles.Root, "src"))).IsEmpty();

    [Test]
    public async Task TestProject_ReferencesSystemXmlSchemaInTheBaselineAdapter() =>
        await Assert
            .That(
                SourceScan.FindSystemXmlSchemaUsage(
                    Path.Combine(
                        RepositoryFiles.Root,
                        "tests",
                        "NetEvolve.Schemas.Xsd.Tests.Integration",
                        "Differential"
                    )
                )
            )
            .Contains("SystemXmlSchemaEngine.cs");

    [Test]
    public async Task Scan_ReportsCodeAndProjectUsage_AndIgnoresCommentsAndBuildOutput()
    {
        using var directory = new TempDirectory();
        _ = directory.Write("Using/Bad.cs", "using System.Xml.Schema;\nclass Bad;\n");
        _ = directory.Write("Qualified/Bad.cs", "class Bad { System.Xml.Schema.XmlSchemaSet Set; }\n");
        _ = directory.Write(
            "Project/Bad.csproj",
            "<Project><ItemGroup><Reference Include=\"System.Xml.Schema\"/></ItemGroup></Project>"
        );
        _ = directory.Write(
            "Comment/Ok.cs",
            "// unlike System.Xml.Schema\n/// <see cref=\"System.Xml.Schema\"/>\nclass Ok;\n"
        );
        _ = directory.Write("bin/Bad.cs", "using System.Xml.Schema;\n");
        _ = directory.Write("obj/Bad.cs", "using System.Xml.Schema;\n");

        var found = SourceScan.FindSystemXmlSchemaUsage(directory.Path);

        await Assert.That(found).IsEquivalentTo(["Project/Bad.csproj", "Qualified/Bad.cs", "Using/Bad.cs"]);
    }
}
