namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entry;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Entry;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entry.DtdEntryFixture;

/// <summary>The builder hands its resolver, its limits, its flags, and its mode to the validating pipeline, and builds a reusable validator.</summary>
public class DtdValidatorBuilderTests
{
    private const string Main = LoaderHarness.DocumentUri;

    [Test]
    public async Task Build_ReturnsAValidator_ThatKeepsNoStateBetweenRuns()
    {
        var validator = Build();

        var first = Run(validator, TwoErrors);
        var valid = Run(validator, ValidNote);
        var second = Run(validator, TwoErrors);

        await Assert.That(validator).IsAssignableTo<IDtdValidator>();
        await Assert.That(Describe(first)).IsEquivalentTo(TwoErrorsIssues);
        await Assert.That(valid.IsValid).IsTrue();
        await Assert.That(Describe(second)).IsEquivalentTo(TwoErrorsIssues);
    }

    [Test]
    public async Task Build_SnapshotsTheBuilder_ALaterChangeOfTheBuilderDoesNotReachTheValidator()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT r EMPTY>");
        var builder = SchemaValidator.ForDtd();
        var validator = Build(_ => builder);

        _ = builder.WithResolver(resolver).WithAllowExternalSubset(true);
        var result = Run(validator, ExternalSubsetDocument, Main);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(resolver.Requests).IsEmpty();
        await Assert.That(Build(_ => builder)).IsNotSameReferenceAs(validator);
    }

    [Test]
    public async Task BuildAsync_ReturnsTheValidator(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validator = await SchemaValidator.ForDtd().BuildAsync(cancellationToken);

        await Assert.That(validator).IsAssignableTo<IDtdValidator>();
        await Assert.That(Run(validator, ValidNote).IsValid).IsTrue();
    }

    [Test]
    public async Task BuildAsync_WithACanceledToken_IsCanceled()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert
            .That(async () => await SchemaValidator.ForDtd().BuildAsync(source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Validate_WithACanceledToken_ThrowsBeforeTheRun()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var validator = Build();

        await Assert
            .That(() => validator.Validate(ValidNote.AsSpan(), null, source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Resolver_WithTheFlagOfTheSubset_IsAskedForTheExternalSubset_AndResolvesAgainstTheSystemId()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT r EMPTY>");

        var result = Run(ExternalSubsetDocument, b => b.WithResolver(resolver).WithAllowExternalSubset(true), Main);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
        await Assert.That(resolver.Requests[0].Kind).IsEqualTo(ResourceKind.ExternalSubset);
        await Assert.That(resolver.Requests[0].ResolvedUri).IsEqualTo(SubsetHarness.SubsetUri);
    }

    [Test]
    public async Task Resolver_WithoutAFlag_IsNeverCalled_AndTheRequestIsDeniedWithXml2002()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT r EMPTY>");

        var result = Run(ExternalSubsetDocument, b => b.WithResolver(resolver), Main);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues[0].Code).IsEqualTo(XmlErrorCodes.XML2002.Code);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Flag_OfTheGeneralEntities_EnablesOnlyExternalGeneralEntities()
    {
        var xml = GeneralHarness.Doc(GeneralHarness.ExternalE + "<!ELEMENT doc ANY><!ELEMENT a EMPTY>", "&e;");
        var resolver = GeneralHarness.Serving("<a/>");

        var withoutFlag = Run(xml, b => b.WithResolver(resolver).WithAllowExternalSubset(true), Main);
        var requestsWithoutFlag = resolver.Requests.Count;
        var withFlag = Run(xml, b => b.WithResolver(resolver).WithAllowExternalGeneralEntities(true), Main);

        await Assert.That(withoutFlag.Issues[0].Code).IsEqualTo(XmlErrorCodes.XML2002.Code);
        await Assert.That(requestsWithoutFlag).IsEqualTo(0);
        await Assert.That(withFlag.IsValid).IsTrue();
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
        await Assert.That(resolver.Requests[0].Kind).IsEqualTo(ResourceKind.ExternalEntity);
    }

    [Test]
    public async Task Flag_OfTheParameterEntities_EnablesOnlyExternalParameterEntities()
    {
        const string Xml = "<!DOCTYPE r [<!ENTITY % p SYSTEM \"p.dtd\">%p;]>\n<r/>";
        var resolver = new RecordingResolver().Add(
            "http://example.org/doc/p.dtd",
            LoaderHarness.Utf8("<!ELEMENT r EMPTY>")
        );

        var withoutFlag = Run(Xml, b => b.WithResolver(resolver).WithAllowExternalSubset(true), Main);
        var requestsWithoutFlag = resolver.Requests.Count;
        var withFlag = Run(Xml, b => b.WithResolver(resolver).WithAllowExternalParameterEntities(true), Main);

        await Assert.That(withoutFlag.Issues[0].Code).IsEqualTo(XmlErrorCodes.XML2002.Code);
        await Assert.That(requestsWithoutFlag).IsEqualTo(0);
        await Assert.That(withFlag.IsValid).IsTrue();
        await Assert.That(resolver.Requests[0].Kind).IsEqualTo(ResourceKind.ExternalParameterEntity);
    }

    [Test]
    public async Task Limits_MaxIssues_TruncatesTheResult()
    {
        var result = Run(TwoErrors, b => b.WithLimits(ValidationLimits.Default.WithMaxIssues(1)));

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.HasDroppedErrors).IsTrue();
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Limits_MaxInputSize_RefusesAnInputThatIsTooLong()
    {
        var result = Run(ValidNote, b => b.WithLimits(ValidationLimits.Default.WithMaxInputSize(10)));

        await Assert.That(Describe(result)).IsEquivalentTo(["NES0001|Error|http://example.org/doc/main.xml|0|0|"]);
    }

    [Test]
    public async Task Limits_MaxDepthAndMaxAttributes_AreAppliedToTheReader_AndReportedWithTheCoreCode()
    {
        var depth = Run("<!DOCTYPE r [<!ELEMENT r ANY>]><r><r><r/></r></r>", b =>
            b.WithLimits(ValidationLimits.Default.WithMaxDepth(2))
        );
        var attributes = Run("<!DOCTYPE r [<!ELEMENT r ANY>]><r a=\"1\" b=\"2\"/>", b =>
            b.WithLimits(ValidationLimits.Default.WithMaxAttributesPerElement(1))
        );

        var depthIssue = depth.Issues[depth.Issues.Count - 1];
        var attributeIssue = attributes.Issues[attributes.Issues.Count - 1];

        await Assert.That(depthIssue.Code.ToString()).IsEqualTo("NES0002");
        await Assert.That(depthIssue.Constraint).IsEqualTo("Limit: MaxElementDepth");
        await Assert.That(depthIssue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(attributeIssue.Code.ToString()).IsEqualTo("NES0003");
        await Assert.That(depth.IsValid).IsFalse();
        await Assert.That(attributes.IsValid).IsFalse();
    }

    [Test]
    public async Task Limits_MaxEntityExpansionAndDepth_AreReportedWithTheCoreCode()
    {
        const string Expansion = "<!DOCTYPE r [<!ENTITY e \"aaaaaaaaaa\"><!ELEMENT r ANY>]><r>&e;</r>";
        const string Nested = "<!DOCTYPE r [<!ENTITY a \"x\"><!ENTITY b \"&a;\"><!ELEMENT r ANY>]><r>&b;</r>";

        var expansion = Run(Expansion, b => b.WithLimits(ValidationLimits.Default.WithMaxEntityExpansion(5)));
        var depth = Run(Nested, b => b.WithLimits(ValidationLimits.Default.WithMaxEntityDepth(1)));

        await Assert.That(expansion.Issues[expansion.Issues.Count - 1].Code.ToString()).IsEqualTo("NES0004");
        await Assert.That(depth.Issues[depth.Issues.Count - 1].Code.ToString()).IsEqualTo("NES0005");
    }

    [Test]
    public async Task Limits_AReaderErrorWithoutACoreCode_KeepsItsCodeOfTheXmlRange()
    {
        var result = Run("<!DOCTYPE r [<!ELEMENT r ANY>]><r></x>");

        await Assert.That(result.Issues[result.Issues.Count - 1].Code).IsEqualTo(XmlErrorCodes.XML0005.Code);
    }

    [Test]
    public async Task Limits_AFatalStatusOfTheReader_AfterAStepStoppedTheRun_IsNotRepeated()
    {
        // The content model of r needs more than one position, so the content step stops the run (XML1118); the depth
        // limit of the reader is reached afterwards, and the issue of the stop stays the last one.
        const string Xml = "<!DOCTYPE r [<!ELEMENT r (a,b,c)><!ELEMENT a ANY>]><r><a><a><a/></a></a></r>";

        var result = Run(Xml, b =>
            b.WithLimits(ValidationLimits.Default.WithMaxDepth(3))
                .WithDtdLimits(DtdValidationLimits.Default.WithMaxContentModelStates(1))
        );

        await Assert.That(result.Issues[result.Issues.Count - 1].Code).IsEqualTo(DtdLimitCodes.XML1118.Code);
    }

    [Test]
    public async Task DtdLimits_MaxExternalEntityRequests_BoundsTheResolverCalls()
    {
        var resolver = SubsetHarness.Serving("<!ENTITY % p SYSTEM \"p.dtd\">%p;");
        _ = resolver.Add("http://example.org/doc/p.dtd", LoaderHarness.Utf8("<!ELEMENT r EMPTY>"));

        var result = Run(
            ExternalSubsetDocument,
            b =>
                b.WithResolver(resolver)
                    .WithAllowExternalSubset(true)
                    .WithAllowExternalParameterEntities(true)
                    .WithDtdLimits(DtdValidationLimits.Default.WithMaxExternalEntityRequests(1)),
            Main
        );

        await Assert.That(result.Issues[result.Issues.Count - 1].Code).IsEqualTo(DtdLimitCodes.XML1117.Code);
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DtdLimits_MaxDeclarations_BoundsTheDeclarations()
    {
        var result = Run(TwoErrors, b => b.WithDtdLimits(DtdValidationLimits.Default.WithMaxDeclarations(1)));

        await Assert.That(result.Issues[result.Issues.Count - 1].Code).IsEqualTo(DtdLimitCodes.XML1119.Code);
    }

    [Test]
    [Arguments(DtdNonDeterminism.Accept, 0, true)]
    [Arguments(DtdNonDeterminism.Warn, 1, true)]
    [Arguments(DtdNonDeterminism.Reject, 1, false)]
    public async Task Mode_OfTheNonDeterministicContentModels_ReachesTheContentStep(
        DtdNonDeterminism mode,
        int issues,
        bool isValid
    )
    {
        const string Xml =
            "<!DOCTYPE r [<!ELEMENT r ((a,b)|(a,c))><!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY>]><r><a/><b/></r>";

        var result = Run(Xml, b => b.WithNonDeterministicContentModels(mode));

        await Assert.That(result.Issues.Count).IsEqualTo(issues);
        await Assert.That(result.IsValid).IsEqualTo(isValid);
        if (issues > 0)
        {
            await Assert.That(result.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5911.Code);
        }
    }

    [Test]
    public async Task Constructor_RejectsNullOptions() =>
        await Assert.That(() => new DtdValidator(null!)).Throws<ArgumentNullException>();
}
