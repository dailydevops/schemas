namespace NetEvolve.Schemas.Schematron.Tests.Unit.Readme;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Registry;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Testing.Readme;
using NetEvolve.Schemas.XPath.Api;

// The regions between the sample markers are the code blocks of src/NetEvolve.Schemas.Schematron/README.md, character for character.
public class SchematronReadmeTests
{
    private const string Readme = "src/NetEvolve.Schemas.Schematron/README.md";
    private const string Template = "templates/readme-project.md";
    private const string Samples = "tests/NetEvolve.Schemas.Schematron.Tests.Unit/Readme/SchematronReadmeTests.cs";
    private const string LegacySamples =
        "tests/NetEvolve.Schemas.Tests.LegacyCompiler/Readme/SchematronReadmeLegacyTests.cs";

    private const string OrderSchema = """
        <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
          <sch:pattern id="totals">
            <sch:rule id="order" context="order">
              <sch:assert id="total-limit" test="number(@total) le 100">The total <sch:value-of select="@total"/> exceeds 100.</sch:assert>
              <sch:report test="number(@total) = 0">The total is zero.</sch:report>
            </sch:rule>
          </sch:pattern>
        </sch:schema>
        """;

    private const string IncludeSchema = """
        <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron">
          <sch:include href="rules.sch"/>
        </sch:schema>
        """;

    private static readonly string[] SupportedBindings = ["xpath", "xpath2", "xslt", "xslt1", "xslt2", "xslt3"];

    private static readonly string[] OtherBindings =
    [
        "xpath3",
        "xpath31",
        "xpath4",
        "xquery",
        "xquery3",
        "xquery31",
        "xquery4",
        "xslt1.1",
        "xslt4",
        "exslt",
        "stx",
    ];

    [Test]
    public async Task QuickStartSample_ReportsTheFailedAssert(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        const string schema = """
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
              <sch:pattern id="totals">
                <sch:rule id="order" context="order">
                  <sch:assert id="total-limit" test="number(@total) le 100">The total <sch:value-of select="@total"/> exceeds 100.</sch:assert>
                  <sch:report test="number(@total) = 0">The total is zero.</sch:report>
                </sch:rule>
              </sch:pattern>
            </sch:schema>
            """;

        var validator = await SchemaValidator.ForSchematron(schema).BuildAsync(cancellationToken);
        var result = await validator.ValidateAsync(
            """<order total="150"/>""".AsMemory(),
            "file:///orders/1.xml",
            cancellationToken
        );
        // </sample>

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues).HasSingleItem();
        await Assert.That(result.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.AssertionFailed);
        await Assert.That(result.Issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(result.Issues[0].Message).IsEqualTo("The total 150 exceeds 100.");
        await Assert.That(result.Issues[0].Constraint).IsEqualTo("total-limit");
        await Assert.That(result.Issues[0].Location.Source).IsEqualTo("file:///orders/1.xml");
    }

    [Test]
    public async Task Report_IsAWarningThatKeepsTheResultValid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = await SchemaValidator.ForSchematron(OrderSchema).BuildAsync(cancellationToken);

        var result = await validator.ValidateAsync(
            """<order total="0"/>""".AsMemory(),
            cancellationToken: cancellationToken
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Issues).HasSingleItem();
        await Assert.That(result.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.ReportFired);
        await Assert.That(result.Issues[0].Severity).IsEqualTo(IssueSeverity.Warning);
    }

    [Test]
    public async Task PhaseAndParameterSample_SelectsThePhaseAndTheParameterValue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        const string schema = """
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" schematronEdition="2025" defaultPhase="basic">
              <sch:param name="limit" value="100"/>
              <sch:phase id="basic"><sch:active pattern="totals"/></sch:phase>
              <sch:phase id="strict"><sch:active pattern="totals"/><sch:active pattern="notes"/></sch:phase>
              <sch:pattern id="totals">
                <sch:rule context="order"><sch:assert test="number(@total) le number($limit)">Over <sch:value-of select="$limit"/>.</sch:assert></sch:rule>
              </sch:pattern>
              <sch:pattern id="notes">
                <sch:rule context="order"><sch:assert test="note">An order needs a note.</sch:assert></sch:rule>
              </sch:pattern>
            </sch:schema>
            """;

        var validator = await SchemaValidator
            .ForSchematron(schema)
            .WithPhase("strict")
            .WithParameter("limit", "200")
            .BuildAsync(cancellationToken);
        var result = await validator.ValidateAsync(
            """<order total="150"/>""".AsMemory(),
            cancellationToken: cancellationToken
        );
        // </sample>

        await Assert.That(validator.Phase).IsEqualTo("strict");
        await Assert.That(validator.EffectivePhase).IsEqualTo("strict");
        await Assert.That(validator.Parameters["limit"]).IsEqualTo("200");
        await Assert.That(result.Issues).HasSingleItem();
        await Assert.That(result.Issues[0].Message).IsEqualTo("An order needs a note.");
    }

    [Test]
    public async Task PhaseAndParameterSample_DefaultsApplyWithoutBuilderCalls(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = """
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" schematronEdition="2025" defaultPhase="basic">
              <sch:param name="limit" value="100"/>
              <sch:phase id="basic"><sch:active pattern="totals"/></sch:phase>
              <sch:pattern id="totals">
                <sch:rule context="order"><sch:assert test="number(@total) le number($limit)">Over <sch:value-of select="$limit"/>.</sch:assert></sch:rule>
              </sch:pattern>
            </sch:schema>
            """;
        var validator = await SchemaValidator.ForSchematron(schema).BuildAsync(cancellationToken);

        var result = await validator.ValidateAsync(
            """<order total="150"/>""".AsMemory(),
            cancellationToken: cancellationToken
        );

        await Assert.That(validator.Phase).IsEqualTo("#DEFAULT");
        await Assert.That(validator.EffectivePhase).IsEqualTo("basic");
        await Assert.That(result.Issues[0].Message).IsEqualTo("Over 100.");
    }

    [Test]
    public async Task SvrlSample_WritesTheReportOfTheRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = OrderSchema;

        // <sample>
        var output = new ArrayBufferWriter<byte>();
        using var sink = new SvrlReportSink(output);
        var schematron = await SchemaValidator.ForSchematron(schema).BuildAsync(cancellationToken);
        var validator = schematron.WithReportSink(sink);

        var result = await validator.ValidateAsync(
            """<order total="150"/>""".AsMemory(),
            cancellationToken: cancellationToken
        );
        var svrl = Encoding.UTF8.GetString(output.WrittenSpan);
        // </sample>

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(sink.IsCompleted).IsTrue();
        await Assert.That(svrl).Contains("<svrl:schematron-output");
        await Assert.That(svrl).Contains("<svrl:failed-assert id=\"total-limit\"");
        await Assert.That(svrl).Contains("The total 150 exceeds 100.");
    }

    [Test]
    public async Task ResolverSample_LoadsTheIncludeBelowTheRoot(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "schematron-readme-" + Guid.NewGuid().ToString("N"))
        );
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(directory.FullName, "rules.sch"),
                """<sch:pattern xmlns:sch="http://purl.oclc.org/dsdl/schematron"><sch:rule context="order"><sch:assert test="@total">An order needs a total.</sch:assert></sch:rule></sch:pattern>""",
                cancellationToken
            );
            const string schema = IncludeSchema;
            var schemaDirectory = directory.FullName;
            var systemId = new Uri(Path.Combine(schemaDirectory, "main.sch")).AbsoluteUri;

            // <sample>
            var validator = await SchemaValidator
                .ForSchematron(schema, systemId)
                .WithResolver(new SandboxedFileSystemResourceResolver(schemaDirectory))
                .BuildAsync(cancellationToken);
            // </sample>

            var result = await validator.ValidateAsync("<order/>".AsMemory(), cancellationToken: cancellationToken);

            await Assert.That(result.Issues).HasSingleItem();
            await Assert.That(result.Issues[0].Message).IsEqualTo("An order needs a total.");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Include_WithoutAResolver_IsDeniedAtBuild(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = SchemaValidator.ForSchematron(IncludeSchema, "file:///schemas/main.sch");

        var failure = await Assert
            .That(async () => await builder.BuildAsync(cancellationToken))
            .Throws<SchemaCompilationException>();

        await Assert.That(builder.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
        await Assert.That(failure!.Issues).HasSingleItem();
        await Assert.That(failure.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(failure.Issues[0].Code.ToString()).IsEqualTo("NES0100");
    }

    [Test]
    [Arguments("")]
    [Arguments("schematronEdition=\"2016\"")]
    public async Task Edition_AbsentOr2016_SelectsTheEarlierSemantics(
        string attribute,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validator = await SchemaValidator.ForSchematron(Edition(attribute)).BuildAsync(cancellationToken);

        await Assert.That(validator.Edition).IsEqualTo(SchematronEdition.Iso2016);
    }

    [Test]
    public async Task Edition_2025_SelectsTheNewSemantics(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validator = await SchemaValidator
            .ForSchematron(Edition("schematronEdition=\"2025\""))
            .BuildAsync(cancellationToken);

        await Assert.That(validator.Edition).IsEqualTo(SchematronEdition.Iso2025);
    }

    [Test]
    public async Task Edition_UnknownValue_FailsAtBuild(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var failure = await Failure(Edition("schematronEdition=\"2030\""), cancellationToken);

        await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.EditionUnknown);
        await Assert.That(failure.Issues[0].Code.ToString()).IsEqualTo("NES3009");
    }

    [Test]
    public async Task Edition_ElementOfThe2025Edition_NeedsTheAttribute(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var schema = Edition(string.Empty)
            .Replace("<sch:pattern", "<sch:param name=\"p\" value=\"1\"/><sch:pattern", StringComparison.Ordinal);

        var failure = await Failure(schema, cancellationToken);

        await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.EditionMismatch);
        await Assert.That(failure.Issues[0].Code.ToString()).IsEqualTo("NES3008");
    }

    [Test]
    public async Task Bindings_NamesCompareWithoutRegardToCase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(async () => await SchemaValidator.ForSchematron(Binding("XSLT2")).BuildAsync(cancellationToken))
            .ThrowsNothing();
    }

    [Test]
    public async Task Phase_Unknown_FailsAtBuild(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = SchemaValidator.ForSchematron(OrderSchema).WithPhase("missing");

        var failure = await Assert
            .That(async () => await builder.BuildAsync(cancellationToken))
            .Throws<SchemaCompilationException>();

        await Assert.That(failure!.Issues[0].Code.ToString()).IsEqualTo("NES3013");
    }

    [Test]
    public async Task Parameter_Undeclared_FailsAtBuild(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = SchemaValidator
            .ForSchematron(Edition("schematronEdition=\"2025\""))
            .WithParameter("missing", "1");

        var failure = await Assert
            .That(async () => await builder.BuildAsync(cancellationToken))
            .Throws<SchemaCompilationException>();

        await Assert.That(failure!.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.ParamUnknown);
        await Assert.That(failure.Issues[0].Code.ToString()).IsEqualTo("NES3112");
    }

    [Test]
    public async Task Bindings_ReadmeStatusMatchesTheBuild(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var readme = ReadmeSamples.ReadText(Readme);

        foreach (var name in SupportedBindings)
        {
            await Assert
                .That(async () => await SchemaValidator.ForSchematron(Binding(name)).BuildAsync(cancellationToken))
                .ThrowsNothing();
            await Assert.That(Row(readme, "`" + name + "`")![1]).StartsWith("supported");
        }

        foreach (var name in OtherBindings)
        {
            var failure = await Failure(Binding(name), cancellationToken);

            await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.BindingUnsupported);
            await Assert.That(Row(readme, "`" + name + "`")![1]).DoesNotStartWith("supported");
        }
    }

    [Test]
    public async Task Bindings_ReadmeNamesTheWorkPackageOrTheDeferralOfEveryOtherName()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        foreach (var name in OtherBindings)
        {
            var status = Row(readme, "`" + name + "`")![1];
            var hasId =
                status.StartsWith("planned", StringComparison.Ordinal)
                    && status.Contains("SCH-21", StringComparison.Ordinal)
                || status.StartsWith("deferred", StringComparison.Ordinal)
                    && status.Contains("DEF-", StringComparison.Ordinal);

            await Assert.That(hasId).IsTrue().Because(name);
        }
    }

    [Test]
    public async Task Matrix_ReadmeHasARowWithAStageForEveryRegisteredInstruction()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var registry = XsltRegistryCatalog.Shared.Default;
        var rows = 0;

        foreach (var local in XsltElementNames.All())
        {
            var name = new XPathQName(XsltInstructionRegistry.Namespace, local);
            if (!registry.TryGet(in name, out _))
            {
                continue;
            }

            rows++;
            var row = Row(readme, "`xsl:" + local + "`");
            await Assert.That(row).IsNotNull().Because("xsl:" + local);
            await Assert.That(HasStage(row![2])).IsTrue().Because("xsl:" + local);
        }

        await Assert.That(rows).IsEqualTo(24);
    }

    [Test]
    public async Task Matrix_ReadmeHasARowWithAStageForTheTwoConstructsWithoutAName()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var registry = XsltRegistryCatalog.Shared.Default;

        await Assert.That(registry.TryGetSynthetic(XsltSyntheticElement.LiteralResultElement, out _)).IsTrue();
        await Assert.That(registry.TryGetSynthetic(XsltSyntheticElement.TextNode, out _)).IsTrue();
        await Assert.That(HasStage(Row(readme, "literal result element")![2])).IsTrue();
        await Assert.That(HasStage(Row(readme, "text node")![2])).IsTrue();
    }

    [Test]
    public async Task Matrix_EveryOtherXsltElement_IsListedWithItsDeferralOrIssue()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var registry = XsltRegistryCatalog.Shared.Default;
        var lines = readme.Split('\n');

        foreach (var local in XsltElementNames.All())
        {
            var name = new XPathQName(XsltInstructionRegistry.Namespace, local);
            if (registry.TryGet(in name, out _))
            {
                continue;
            }

            var token = "`xsl:" + local + "`";
            var found = false;
            foreach (var line in lines)
            {
                found |=
                    line.Contains(token, StringComparison.Ordinal)
                    && (
                        line.Contains("DEF-", StringComparison.Ordinal) || line.Contains('#', StringComparison.Ordinal)
                    );
            }

            await Assert.That(found).IsTrue().Because("xsl:" + local);
        }
    }

    [Test]
    [Arguments("template")]
    [Arguments("apply-templates")]
    public async Task XsltElement_NotInTheRegistry_InASchema_RaisesTheUnsupportedConstructError(
        string element,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var schema = $"""
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
              <xsl:{element} name="x" match="/"/>
              <sch:pattern><sch:rule context="order"><sch:assert test="true()"/></sch:rule></sch:pattern>
            </sch:schema>
            """;

        var failure = await Failure(schema, cancellationToken);

        await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(failure.Issues[0].Code.ToString()).IsEqualTo("NES3204");
        await Assert.That(failure.Issues[0].Message).Contains("xsl:" + element);
    }

    [Test]
    [Arguments("<sch:phase id=\"p\" from=\"order\"><sch:active pattern=\"a\"/></sch:phase>", "p")]
    [Arguments("<sch:phase id=\"p\" when=\"true()\"><sch:active pattern=\"a\"/></sch:phase>", "p")]
    public async Task UnsupportedSchematronConstruct_IsRejectedAtBuild(
        string fragment,
        string phase,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schema = $"""
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" schematronEdition="2025">
              {fragment}
              <sch:pattern id="a"><sch:rule context="order"><sch:assert test="true()"/></sch:rule></sch:pattern>
            </sch:schema>
            """;
        var builder = SchemaValidator.ForSchematron(schema).WithPhase(phase);

        var failure = await Assert
            .That(async () => await builder.BuildAsync(cancellationToken))
            .Throws<SchemaCompilationException>();

        await Assert.That(failure!.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
        await Assert.That(failure.Issues[0].Code.ToString()).IsEqualTo("NES3007");
    }

    [Test]
    public async Task DocumentTypeDeclaration_InTheInstance_IsRejected(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = await SchemaValidator.ForSchematron(OrderSchema).BuildAsync(cancellationToken);

        var result = await validator.ValidateAsync(
            "<!DOCTYPE order [<!ENTITY e \"x\">]><order total=\"1\"/>".AsMemory(),
            cancellationToken: cancellationToken
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues[0].Code.Number).IsLessThan(3000);
    }

    [Test]
    public async Task DocumentTypeDeclaration_InTheSchema_IsRejected(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var failure = await Failure("<!DOCTYPE sch:schema>" + OrderSchema, cancellationToken);

        await Assert.That(failure.Issues[0].Code.Number).IsLessThan(3000);
    }

    [Test]
    public async Task Functions_TheXsltFamilyAddsCurrentAndGenerateIdAndFormatNumber(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = """
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
              <sch:pattern><sch:rule context="order">
                <sch:assert test="current()/@total = @total and generate-id(.) != '' and format-number(1234.5, '#,##0.00') = '1,234.50' and function-available('current')">The XSLT functions are missing.</sch:assert>
              </sch:rule></sch:pattern>
            </sch:schema>
            """;
        var validator = await SchemaValidator.ForSchematron(schema).BuildAsync(cancellationToken);

        var result = await validator.ValidateAsync(
            """<order total="150"/>""".AsMemory(),
            cancellationToken: cancellationToken
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Functions_TheXPathBindingsDoNotAddThem(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var schema = OrderSchema
            .Replace("xslt2", "xpath2", StringComparison.Ordinal)
            .Replace("number(@total) le 100", "generate-id(.) != ''", StringComparison.Ordinal);

        var failure = await Failure(schema, cancellationToken);

        await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
    }

    [Test]
    public async Task Readme_NamesTheSecurityDefaults()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("deny by default");
        await Assert.That(readme).Contains("DenyAllResourceResolver");
        await Assert.That(readme).Contains("NES0100");
        await Assert.That(readme).Contains("SandboxedFileSystemResourceResolver");
        await Assert.That(readme).Contains("WithResolver");
    }

    [Test]
    public async Task Readme_HasOneParagraphOnTheEditionAttribute()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("### Edition detection");
        await Assert.That(readme).Contains("schematronEdition");
        await Assert.That(readme).Contains("NES3009");
        await Assert.That(readme).Contains("NES3008");
    }

    [Test]
    public async Task Readme_HasLimitationsSectionNamingThePreCSharp14Fallback()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("## Limitations");
        await Assert.That(readme).Contains("C# 14");
        await Assert.That(readme).Contains("SchematronSchemaValidatorExtensions");
    }

    [Test]
    public async Task Readme_NamesTheUnsupportedConstructErrorAndTheConformanceRate()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("NES3204");
        await Assert.That(readme).Contains("## Conformance");
        await Assert.That(readme).Contains("known failures");
    }

    [Test]
    public async Task Conformance_StatedBaselineEntriesEqualTheCommittedBaselines()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var (entries, fileCount) = CountEntries();

        await Assert.That(readme).Contains($"{entries} baseline entries in {fileCount} files");
        await Assert.That(readme).Contains($"{entries} known failures");
    }

    [Test]
    public async Task Readme_FollowsTheSectionOrderOfTheProjectTemplate()
    {
        var template = Headings(ReadmeSamples.ReadText(Template));
        var readme = Headings(ReadmeSamples.ReadText(Readme));
        var position = 0;

        foreach (var heading in template)
        {
            if (heading.StartsWith("# Project Name", StringComparison.Ordinal))
            {
                continue;
            }

            var found = readme.IndexOf(heading, position);
            await Assert.That(found).IsGreaterThanOrEqualTo(0).Because(heading);
            position = found + 1;
        }

        await Assert.That(readme[0]).IsEqualTo("# NetEvolve.Schemas.Schematron");
    }

    [Test]
    public async Task Readme_HasNoTemplatePlaceholdersAndNoComments()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).DoesNotContain("<!--");
        await Assert.That(readme).DoesNotContain("YourPackageName");
        await Assert.That(readme).DoesNotContain("YourRepo");
        await Assert.That(readme).DoesNotContain("x.x.x");
        await Assert.That(readme).Contains("https://www.nuget.org/packages/NetEvolve.Schemas.Schematron/");
    }

    [Test]
    public async Task Readme_LinksToFilesThatExist()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        const string prefix = "https://github.com/dailydevops/schemas/blob/main/";
        var checkedLinks = 0;

        foreach (var line in readme.Split('\n'))
        {
            var from = 0;
            while (true)
            {
                var start = line.IndexOf(prefix, from, StringComparison.Ordinal);
                if (start < 0)
                {
                    break;
                }

                var end = line.IndexOfAny([')', ' ', '`', '>'], start);
                var path = line.Substring(start + prefix.Length, (end < 0 ? line.Length : end) - start - prefix.Length);
                await Assert.That(File.Exists(RepositoryPath(path))).IsTrue().Because(path);
                checkedLinks++;
                from = start + prefix.Length;
            }
        }

        await Assert.That(checkedLinks).IsGreaterThan(0);
    }

    [Test]
    public async Task EveryReadmeCodeBlock_IsACompiledSample()
    {
        var compiled = new List<string>(ReadmeSamples.SamplesIn(Samples));
        compiled.AddRange(ReadmeSamples.SamplesIn(LegacySamples));
        var blocks = ReadmeSamples.CodeBlocks(Readme);

        await Assert.That(blocks).Count().IsEqualTo(compiled.Count);
        foreach (var block in blocks)
        {
            await Assert.That(compiled).Contains(block);
        }
    }

    private static (int Entries, int Files) CountEntries()
    {
        var files = Directory.GetFiles(RepositoryPath("tests/conformance/baselines/schematron-conformance"), "*.txt");
        var entries = 0;
        foreach (var file in files)
        {
            foreach (var line in File.ReadAllLines(file))
            {
                if (line.Trim().Length > 0)
                {
                    entries++;
                }
            }
        }

        return (entries, files.Length);
    }

    private static string RepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Schemas.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new InvalidOperationException("Schemas.slnx was not found."),
            relativePath
        );
    }

    private static List<string> Headings(string markdown)
    {
        var headings = new List<string>();
        var inFence = false;
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
            }
            else if (
                !inFence
                && (line.StartsWith("# ", StringComparison.Ordinal) || line.StartsWith("## ", StringComparison.Ordinal))
            )
            {
                headings.Add(line);
            }
        }

        return headings;
    }

    private static async Task<SchemaCompilationException> Failure(string schema, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = SchemaValidator.ForSchematron(schema);

        var failure = await Assert
            .That(async () => await builder.BuildAsync(cancellationToken))
            .Throws<SchemaCompilationException>();

        return failure!;
    }

    private static string Edition(string attribute) =>
        $"""<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" {attribute}><sch:pattern><sch:rule context="order"><sch:assert test="true()"/></sch:rule></sch:pattern></sch:schema>""";

    private static string Binding(string name) =>
        $"""<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" queryBinding="{name}"><sch:pattern><sch:rule context="order"><sch:assert test="true()"/></sch:rule></sch:pattern></sch:schema>""";

    private static bool HasStage(string cell)
    {
        foreach (var c in cell)
        {
            if (c is >= '1' and <= '5')
            {
                return true;
            }
        }

        return false;
    }

    // The trimmed cells of the first table row whose first cell is the given text, or null.
    private static string[]? Row(string readme, string firstCell)
    {
        var prefix = "| " + firstCell + " |";
        foreach (var line in readme.Split('\n'))
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                var cells = line.Trim().Trim('|').Split('|');
                for (var i = 0; i < cells.Length; i++)
                {
                    cells[i] = cells[i].Trim();
                }

                return cells;
            }
        }

        return null;
    }
}
