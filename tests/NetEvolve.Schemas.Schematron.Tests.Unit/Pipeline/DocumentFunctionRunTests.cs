namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The functions <c>document</c>, <c>doc</c>, <c>doc-available</c>, and <c>unparsed-text</c> in the rules of a schema (#312): a run
/// binds them to the base URI of the schema and to the resolver and the limits of the validator.
/// </summary>
public class DocumentFunctionRunTests
{
    private const string Xsl = "xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";
    private const string Schema = "file:///schemas/rules.sch";
    private const string Codes = "<codes><c>A</c><c>B</c></codes>";
    private static readonly string Large = "<codes>" + new string('x', 3000) + "</codes>";
    private const string Instance = "<doc><sub/></doc>";

    private static SchematronValidatorBuilder Builder(string test, string attributes = "", string binding = "xslt2") =>
        SchemaValidator.ForSchematron(
            Wrap(
                $"<pattern id=\"p\"><rule context=\"doc\"><assert test=\"{test}\">Failed.</assert></rule></pattern>",
                $" queryBinding=\"{binding}\" schematronEdition=\"2025\" {Xsl}{attributes}"
            ),
            Schema
        );

    private static FakeResources Files() => new FakeResources().Add("file:///schemas/codes.xml", Codes);

    private static ISchematronValidator Validator(
        string test,
        FakeResources? files = null,
        string attributes = "",
        ValidationLimits? limits = null,
        string binding = "xslt2"
    )
    {
        var builder = Builder(test, attributes, binding);
        if (files is not null)
        {
            _ = builder.WithResolver(files);
        }

        if (limits is not null)
        {
            _ = builder.WithLimits(limits);
        }

        return builder.Build();
    }

    private static ValidationResult Run(
        string test,
        FakeResources? files = null,
        string attributes = "",
        ValidationLimits? limits = null,
        string binding = "xslt2"
    ) => Validate(Validator(test, files, attributes, limits, binding), Instance);

    private static string Code(ValidationResult result) => result.Issues.Single().Constraint!;

    [Test]
    public async Task Doc_RelativeReference_ResolvesAgainstTheSchemaBaseUri()
    {
        var files = Files();

        var result = Run("count(doc('codes.xml')//c) = 2", files);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(files.Requests.Single()).IsEqualTo("file:///schemas/codes.xml");
    }

    [Test]
    [Arguments("xslt")]
    [Arguments("xslt1")]
    [Arguments("xslt3")]
    public async Task Document_UnderTheOtherXsltBindings_LoadsTheDocument(string binding)
    {
        var result = Run(
            "count(document('codes.xml')//c) = 2 and count(doc('codes.xml')//c) = 2",
            Files(),
            binding: binding
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Document_OneArgument_LoadsTheDocument()
    {
        var result = Run("count(document('codes.xml')//c) = 2", Files());

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Document_BaseNodeAsSecondArgument_ResolvesAgainstTheBaseOfThatNode()
    {
        var files = Files().Add("file:///codes.xml", "<codes><c>X</c></codes>");

        var result = Run("document('codes.xml', .)//c = 'X'", files);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(files.Requests.Single()).IsEqualTo("file:///codes.xml");
    }

    [Test]
    public async Task Document_AndDoc_WithTheSameUri_ReturnTheSameNode()
    {
        var files = Files();

        var result = Run("doc('codes.xml') is document('codes.xml') and doc('codes.xml') is doc('./codes.xml')", files);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(files.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Doc_WithTheDefaultResolver_IsFODC0002()
    {
        var result = Run("doc('codes.xml')");

        await Assert.That(Code(result)).IsEqualTo("FODC0002");
    }

    [Test]
    public async Task Document_WithTheDefaultResolver_IsXTRE1160()
    {
        var result = Run("document('codes.xml')");

        await Assert.That(Code(result)).IsEqualTo("XTRE1160");
    }

    [Test]
    public async Task DocAvailable_WithTheDefaultResolver_IsFalse()
    {
        var result = Run("not(doc-available('codes.xml'))");

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task DocAvailable_OfAFoundAndOfAMissingDocument_AnswersEach()
    {
        var result = Run("doc-available('codes.xml') and not(doc-available('missing.xml'))", Files());

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Doc_OfAMissingDocument_IsFODC0002()
    {
        var result = Run("doc('missing.xml')", Files());

        await Assert.That(Code(result)).IsEqualTo("FODC0002");
    }

    [Test]
    public async Task Doc_OfADocumentThatIsNotWellFormed_IsFODC0002()
    {
        var result = Run("doc('bad.xml')", Files().Add("file:///schemas/bad.xml", "<a>"));

        await Assert.That(Code(result)).IsEqualTo("FODC0002");
    }

    [Test]
    public async Task Doc_WithAFragmentIdentifier_IsFODC0005()
    {
        var result = Run("doc('codes.xml#a')", Files());

        await Assert.That(Code(result)).IsEqualTo("FODC0005");
    }

    [Test]
    public async Task Document_WithAFragmentIdentifier_IsXTRE1160()
    {
        var result = Run("document('codes.xml#a')", Files());

        await Assert.That(Code(result)).IsEqualTo("XTRE1160");
    }

    [Test]
    public async Task Doc_OfADocumentAboveTheSizeLimit_IsNES0007()
    {
        var issue = Run(
            "doc('large.xml')",
            Files().Add("file:///schemas/large.xml", Large),
            limits: ValidationLimits.Default.WithMaxResourceSize(2000)
        )
            .Issues.Single();

        await Assert.That(issue.Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(issue.Constraint).IsEqualTo("FODC0002");
    }

    [Test]
    public async Task Document_OfADocumentAboveTheSizeLimit_IsNES0007()
    {
        var issue = Run(
            "document('large.xml')",
            Files().Add("file:///schemas/large.xml", Large),
            limits: ValidationLimits.Default.WithMaxResourceSize(2000)
        )
            .Issues.Single();

        await Assert.That(issue.Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task Doc_OfMoreDocumentsThanTheLimit_IsNES0006()
    {
        var files = Files().Add("file:///schemas/two.xml", "<two/>");
        var issue = Run(
            "doc('codes.xml') and doc('two.xml')",
            files,
            limits: ValidationLimits.Default.WithMaxExternalResources(1)
        )
            .Issues.Single();

        await Assert.That(issue.Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(files.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DocAvailable_AboveTheSizeLimit_IsFalse()
    {
        var result = Run(
            "not(doc-available('large.xml'))",
            Files().Add("file:///schemas/large.xml", Large),
            limits: ValidationLimits.Default.WithMaxResourceSize(2000)
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task UnparsedText_RelativeReference_ResolvesAgainstTheSchemaBaseUri()
    {
        var files = new FakeResources().Add("file:///schemas/list.txt", "one two");

        var result = Run("unparsed-text('list.txt') = 'one two' and unparsed-text-available('list.txt')", files);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(files.Requests.Single()).IsEqualTo("file:///schemas/list.txt");
    }

    [Test]
    public async Task UnparsedText_OfAMissingResource_IsFOUT1170()
    {
        var result = Run("unparsed-text('missing.txt')", Files());

        await Assert.That(Code(result)).IsEqualTo("FOUT1170");
    }

    [Test]
    public async Task UnparsedTextAvailable_WithTheDefaultResolver_IsFalse()
    {
        var result = Run("not(unparsed-text-available('list.txt'))");

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task FunctionAvailable_OfTheDocumentFunctions_IsTrue()
    {
        var result = Run(
            "function-available('document') and function-available('doc') and function-available('unparsed-text')"
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task ElementAvailable_OfARegisteredAndAnUnknownInstruction_AnswersFromTheRegistry()
    {
        var validator = ElementAvailableValidator();
        var result = Validate(validator, Instance);

        await Assert.That(result.IsValid).IsTrue();
    }

    private static ValidationResult RunAsserts(string[] tests, FakeResources files, ValidationLimits? limits = null)
    {
        var asserts = string.Concat(tests.Select(t => $"<assert test=\"{t}\">Failed.</assert>"));
        var builder = SchemaValidator
            .ForSchematron(
                Wrap(
                    $"<pattern id=\"p\"><rule context=\"doc\">{asserts}</rule></pattern>",
                    $" queryBinding=\"xslt2\" schematronEdition=\"2025\" {Xsl}"
                ),
                Schema
            )
            .WithResolver(files);
        if (limits is not null)
        {
            _ = builder.WithLimits(limits);
        }

        return Validate(builder.Build(), Instance);
    }

    [Test]
    public async Task Doc_AfterABreachThatDidNotFail_ReportsTheOwnFailure()
    {
        var result = RunAsserts(
            ["not(doc-available('large.xml'))", "doc('missing.xml')"],
            Files().Add("file:///schemas/large.xml", Large),
            ValidationLimits.Default.WithMaxResourceSize(2000)
        );

        var issue = result.Issues.Single();
        await Assert.That(issue.Constraint).IsEqualTo("FODC0002");
        await Assert.That(issue.Code).IsNotEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task Doc_AfterAReportedBreach_ReportsTheOwnFailureOfTheNextCall()
    {
        var result = RunAsserts(
            ["doc('large.xml')", "doc('missing.xml')"],
            Files().Add("file:///schemas/large.xml", Large),
            ValidationLimits.Default.WithMaxResourceSize(2000)
        );

        await Assert.That(result.Issues.Count).IsEqualTo(2);
        await Assert.That(result.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(result.Issues[1].Code).IsNotEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert.That(result.Issues[1].Constraint).IsEqualTo("FODC0002");
    }

    [Test]
    public async Task Document_BaseNodeFromALoadedDocument_ResolvesAgainstTheSystemIdOfThatDocument()
    {
        var files = Files()
            .Add("file:///schemas/sub/inner.xml", "<codes><c>Y</c></codes>")
            .Add("file:///schemas/sub/outer.xml", "<o/>");

        var result = Run("document('inner.xml', doc('sub/outer.xml'))//c = 'Y'", files);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(files.Requests.Last()).IsEqualTo("file:///schemas/sub/inner.xml");
    }

    private static ISchematronValidator ElementAvailableValidator() =>
        SchemaValidator
            .ForSchematron(
                Wrap(
                    "<ns prefix=\"xsl\" uri=\"http://www.w3.org/1999/XSL/Transform\"/>"
                        + "<pattern id=\"p\"><rule context=\"doc\"><assert test=\"element-available('xsl:copy-of') and not(element-available('xsl:no-such-instruction'))\">Failed.</assert></rule></pattern>",
                    $" queryBinding=\"xslt2\" schematronEdition=\"2025\" {Xsl}"
                ),
                Schema
            )
            .Build();

    private static ISchematronValidator FunctionBodyValidator(FakeResources files) =>
        SchemaValidator
            .ForSchematron(
                Wrap(
                    "<ns prefix=\"f\" uri=\"urn:f\"/>"
                        + "<xsl:function name=\"f:d\"><xsl:sequence select=\"doc('codes.xml')\"/></xsl:function>"
                        + "<pattern id=\"p\"><rule context=\"doc\"><assert test=\"not(doc('codes.xml') is f:d())\">Failed.</assert></rule></pattern>",
                    $" queryBinding=\"xslt2\" schematronEdition=\"2025\" {Xsl} xmlns:f=\"urn:f\""
                ),
                Schema
            )
            .WithResolver(files)
            .Build();

    [Test]
    public async Task Doc_InAStylesheetFunctionBody_LoadsAgainOnTheContextOfTheCall()
    {
        // Documented limit: the pool of loaded documents belongs to one evaluation context, so the body of a function does not see
        // the node of the rule that called it.
        var files = Files();
        var validator = FunctionBodyValidator(files);

        var result = Validate(validator, Instance);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(files.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Doc_WithACanceledToken_Cancels()
    {
        using var source = new CancellationTokenSource();
        var validator = Validator("doc-available('codes.xml')", Files());
        await source.CancelAsync();

        await Assert
            .That(() => validator.Validate(Instance.AsSpan(), "file:///invoice.xml", source.Token))
            .Throws<OperationCanceledException>();
    }
}
