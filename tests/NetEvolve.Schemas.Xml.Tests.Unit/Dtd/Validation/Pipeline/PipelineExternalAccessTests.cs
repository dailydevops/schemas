namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline.PipelineHarness;

/// <summary>The external subset and the external entities reach the validation through the resolver of the options (design #122, D6 and D7).</summary>
public class PipelineExternalAccessTests
{
    private const string ExternalSubsetDocument = "<!DOCTYPE r SYSTEM \"ext.dtd\">\n<r>x</r>";

    [Test]
    public async Task Options_Default_DeniesTheExternalSubset_AsAFatalStatus_WithoutAResolverCall()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT r EMPTY>");

        var result = Validate(ExternalSubsetDocument, DtdValidationOptions.Default.WithResolver(resolver));

        await Assert.That(result.IsWellFormed).IsFalse();
        await Assert.That(result.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(result.Issues.Issues[^1].Code).IsEqualTo(XmlErrorCodes.XML2002.Code);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Options_FlagWithoutAResolver_IsDeniedByTheDefaultResolver()
    {
        var result = Validate(ExternalSubsetDocument, DtdValidationOptions.Default.WithAllowExternalSubset(true));

        await Assert.That(result.IsWellFormed).IsFalse();
        await Assert.That(result.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
    }

    [Test]
    public async Task ExternalSubset_LoadedThroughTheResolver_DeclaresTheElementsTheStepsCheck()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT r EMPTY>");

        var result = Validate(ExternalSubsetDocument, LoaderHarness.Open(resolver));

        await Assert.That(result.IsWellFormed).IsTrue();
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
        await Assert.That(resolver.Requests[0].Kind).IsEqualTo(NetEvolve.Schemas.Resources.ResourceKind.ExternalSubset);
        await Assert.That(result.Issues.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
    }

    [Test]
    public async Task ExternalSubset_ResolvesAgainstTheDocumentUri()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT r (#PCDATA)>");

        _ = Validate(ExternalSubsetDocument, LoaderHarness.Open(resolver));

        await Assert.That(resolver.Requests[0].ResolvedUri).IsEqualTo(SubsetHarness.SubsetUri);
    }

    [Test]
    public async Task ExternalGeneralEntity_LoadedThroughTheResolver_ReachesTheSteps()
    {
        var resolver = GeneralHarness.Serving("<a>t</a>");
        var xml = GeneralHarness.Doc(GeneralHarness.ExternalE + "<!ELEMENT doc ANY><!ELEMENT a EMPTY>", "&e;");
        var options = DtdValidatingPipeline.ReaderOptions(LoaderHarness.Open(resolver), null, DocumentUri);
        var sink = new RecordingSink();

        var result = DtdValidatingPipeline.Validate(
            xml.AsSpan(),
            ref sink,
            LoaderHarness.Open(resolver),
            documentUri: GeneralHarness.DocumentUri
        );

        await Assert.That(options.DtdProcessing).IsTrue();
        await Assert.That(result.IsWellFormed).IsTrue();
        await Assert.That(sink.Events.Exists(e => e.StartsWith("<a@", StringComparison.Ordinal))).IsTrue();
        await Assert.That(result.Issues.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
    }

    [Test]
    public async Task ExternalGeneralEntity_WithTheFlagOff_IsAFatalStatus()
    {
        var resolver = GeneralHarness.Serving("<a>t</a>");
        var xml = GeneralHarness.Doc(GeneralHarness.ExternalE + "<!ELEMENT doc ANY>", "&e;");
        var options = DtdValidationOptions.Default.WithResolver(resolver).WithAllowExternalSubset(true);

        var result = Validate(xml, options);

        await Assert.That(result.IsWellFormed).IsFalse();
        await Assert.That(result.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(resolver.Requests).IsEmpty();
    }
}
