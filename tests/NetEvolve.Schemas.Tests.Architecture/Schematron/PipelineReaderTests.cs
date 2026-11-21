namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>The pipeline folder reads instances only through the Xml package readers (SCH-55, #311).</summary>
public class PipelineReaderTests
{
    private const string Fixtures = "NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Pipeline";

    private static System.Reflection.Assembly SchematronAssembly => SourceAssemblies.Load(SourceAssemblies.Schematron);

    [Test]
    public async Task Pipeline_DoesNotDecodeOrParseTextOnItsOwn() =>
        await Assert.That(PipelineReaderRules.Violations(SchematronAssembly, PipelineReaderRules.Pipeline)).IsEmpty();

    [Test]
    public async Task Pipeline_ReadsInstancesThroughTheLoaderOfTheXmlPackage() =>
        await Assert.That(PipelineReaderRules.Loaders(SchematronAssembly, PipelineReaderRules.Pipeline)).IsNotEmpty();

    [Test]
    public async Task FixtureThatDecodesOnItsOwn_IsReported() =>
        await Assert
            .That(PipelineReaderRules.Violations(typeof(PipelineReaderTests).Assembly, $"{Fixtures}.Violating"))
            .IsEquivalentTo([
                $"{Fixtures}.Violating.DecodesWithEncodingFixture",
                $"{Fixtures}.Violating.ReadsWithTextReaderFixture",
            ]);

    [Test]
    public async Task FixtureThatUsesTheLoader_IsNotReportedAndIsFound()
    {
        var assembly = typeof(PipelineReaderTests).Assembly;

        await Assert.That(PipelineReaderRules.Violations(assembly, $"{Fixtures}.Conforming")).IsEmpty();
        await Assert
            .That(PipelineReaderRules.Loaders(assembly, $"{Fixtures}.Conforming"))
            .IsEquivalentTo([$"{Fixtures}.Conforming.ReadsWithTheLoaderFixture"]);
    }
}
