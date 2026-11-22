namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entry;

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Testing.Readme;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

// The regions between the sample markers are code blocks of src/NetEvolve.Schemas.Xml/README.md, character for character.
// A sample is a synchronous method, because the analyzers ask an async test to await BuildAsync, which the sample does not show.
public class DtdEntryReadmeTests
{
    private const string Readme = "src/NetEvolve.Schemas.Xml/README.md";
    private const string Samples = "tests/NetEvolve.Schemas.Xml.Tests.Unit/Dtd/Entry/DtdEntryReadmeTests.cs";
    private const string LegacySamples = "tests/NetEvolve.Schemas.Tests.LegacyCompiler/Dtd/DtdLegacyTests.cs";

    [Test]
    public async Task ValiditySample_ReportsTheIssueWithCodeConstraintAndLocation()
    {
        var (isValid, code, constraint, line) = Validity();

        await Assert.That(isValid).IsFalse();
        await Assert.That(code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(constraint).IsEqualTo("Element Valid");
        await Assert.That(line).IsEqualTo(2);
    }

    [Test]
    public async Task DenyByDefaultSample_RefusesTheExternalSubset()
    {
        var code = DenyByDefault();

        await Assert.That(code).IsEqualTo(XmlErrorCodes.XML2002.Code);
        await Assert.That(code.ToString()).IsEqualTo("NES1202");
    }

    [Test]
    public async Task ResolverSample_LoadsTheExternalSubsetFromTheDirectory(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.Combine(Path.GetTempPath(), "dtd-readme-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(directory, "note.dtd"),
                "<!ELEMENT note EMPTY>",
                cancellationToken
            );

            var result = AllowTheExternalSubset(directory);

            await Assert.That(result.IsValid).IsTrue();
            await Assert.That(result.Issues).IsEmpty();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public async Task Readme_DocumentsTheEntryPoint_TheFallbackAndTheDenyByDefaultResolver()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("## DTD validation");
        await Assert.That(readme).Contains("SchemaValidator.ForDtd()");
        await Assert.That(readme).Contains("DtdSchemaValidatorExtensions.ForDtd()");
        await Assert.That(readme).Contains("C# 14");
        await Assert.That(readme).Contains("DenyAllResourceResolver");
        await Assert.That(readme).Contains("WithAllowExternalSubset");
        await Assert.That(readme).Contains("XML2002");
        await Assert.That(readme).Contains("XML5004");
        await Assert.That(readme).DoesNotContain("NotSupportedException");
        await Assert.That(readme).DoesNotContain("until DTD validation is available");
    }

    [Test]
    public async Task Readme_StatesTheLimitationsOfTheEntryPoint()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("**External access is off by default.**");
        await Assert.That(readme).Contains("**A stream and a sequence are buffered.**");
        await Assert.That(readme).Contains("**Two core limits are not applied to external resources.**");
        await Assert.That(readme).Contains("MaxExternalResources");
        await Assert.That(readme).Contains("MaxResourceSize");
        await Assert.That(readme).Contains("[#926](https://github.com/dailydevops/schemas/issues/926)");
        await Assert.That(readme).Contains("[#698](https://github.com/dailydevops/schemas/issues/698)");
    }

    [Test]
    public async Task Readme_NamesTheDefaultsOfTheBuilder()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var limits = DtdValidationLimits.Default;
        var builder = SchemaValidator.ForDtd();

        await Assert
            .That(readme)
            .Contains(
                $"`MaxExternalEntityRequests` ({limits.MaxExternalEntityRequests.ToString("N0", CultureInfo.InvariantCulture)}, error `{DtdLimitCodes.XML1117.Alias}`)"
            );
        await Assert
            .That(readme)
            .Contains(
                $"`MaxContentModelStates` ({limits.MaxContentModelStates.ToString("N0", CultureInfo.InvariantCulture)}, error `{DtdLimitCodes.XML1118.Alias}`)"
            );
        await Assert
            .That(readme)
            .Contains(
                $"`MaxDeclarations` ({limits.MaxDeclarations.ToString("N0", CultureInfo.InvariantCulture)}, error `{DtdLimitCodes.XML1119.Alias}`)"
            );
        await Assert
            .That(readme)
            .Contains($"| `WithNonDeterministicContentModels` | `{builder.NonDeterministicContentModels}` |");
        await Assert.That(readme).Contains("| `WithAllowExternalSubset` | `false` |");
        await Assert.That(builder.AllowExternalSubset).IsFalse();
    }

    [Test]
    public async Task Samples_AreInTheReadme()
    {
        var compiled = ReadmeSamples.SamplesIn(Samples);
        var legacy = ReadmeSamples.SamplesIn(LegacySamples);
        var blocks = ReadmeSamples.CodeBlocks(Readme);

        await Assert.That(compiled).Count().IsEqualTo(3);
        await Assert.That(legacy).Count().IsEqualTo(1);
        foreach (var sample in compiled)
        {
            await Assert.That(blocks).Contains(sample);
        }

        await Assert.That(blocks).Contains(legacy[0]);
    }

    private static (bool IsValid, DiagnosticCode Code, string? Constraint, int Line) Validity()
    {
        // <sample>
        var validator = SchemaValidator.ForDtd().Build();

        var result = validator.Validate("<!DOCTYPE note [<!ELEMENT note EMPTY>]>\n<note>text</note>");

        var issue = result.Issues[0];
        var isValid = result.IsValid;
        var code = issue.Code;
        var constraint = issue.Constraint;
        var line = issue.Location.Line;
        // </sample>

        return (isValid, code, constraint, line);
    }

    private static DiagnosticCode DenyByDefault()
    {
        // <sample>
        var validator = SchemaValidator.ForDtd().Build();

        var result = validator.Validate("<!DOCTYPE note SYSTEM \"note.dtd\"><note/>");

        var code = result.Issues[0].Code;
        // </sample>

        return code;
    }

    private static ValidationResult AllowTheExternalSubset(string directory)
    {
        // <sample>
        var validator = SchemaValidator
            .ForDtd()
            .WithResolver(new SandboxedFileSystemResourceResolver(directory))
            .WithAllowExternalSubset(true)
            .Build();

        var systemId = new Uri(Path.Combine(directory, "note.xml")).AbsoluteUri;
        var result = validator.Validate("<!DOCTYPE note SYSTEM \"note.dtd\"><note/>", systemId);
        // </sample>

        return result;
    }
}
