namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline.PipelineHarness;

/// <summary>The inputs of the validating reader mode, the sink that keeps its state, the default steps, and the guards of the boundaries.</summary>
public class PipelineInputAndGuardTests
{
    private const string Invalid = "<!DOCTYPE r [<!ELEMENT r EMPTY>]>\n<r>x</r>";

    [Test]
    public async Task Bytes_AreDecodedWithTheirEncoding_AndValidatedLikeCharacters()
    {
        var fromBytes = DtdValidatingPipeline.Validate(Utf16(Invalid), documentUri: DocumentUri);
        var fromChars = DtdValidatingPipeline.Validate(Invalid.AsSpan(), documentUri: DocumentUri);

        await Assert.That(fromBytes.IsWellFormed).IsTrue();
        await Assert.That(fromBytes.Issues.Issues.Count).IsEqualTo(1);
        await Assert.That(fromBytes.Issues.Issues[0].Code).IsEqualTo(fromChars.Issues.Issues[0].Code);
        await Assert.That(fromBytes.Issues.Issues[0].Location).IsEqualTo(fromChars.Issues.Issues[0].Location);
    }

    [Test]
    public async Task Bytes_WithASink_ForwardEveryEvent()
    {
        var sink = new RecordingSink();

        var result = DtdValidatingPipeline.Validate(Utf16(Invalid), ref sink);

        await Assert.That(result.IsWellFormed).IsTrue();
        await Assert.That(sink.Events).Contains("doctype:r");
        await Assert.That(sink.Events[^1]).IsEqualTo("EndDocument");
        await Assert.That(result.Issues.Issues[0].Location.Source).IsNull();
    }

    [Test]
    public async Task Bytes_WithOptionsReaderOptionsAndSteps_UseThem()
    {
        var step = new ScriptedDtdValidationStep();
        var sink = new RecordingSink();
        var options = DtdValidationOptions.Default.WithNonDeterministicContentModels(DtdNonDeterminism.Accept);
        var readerOptions = XmlReaderOptions.Default.WithLimits(
            XmlReaderLimits.Default.WithMaxDocumentCharacters(4096)
        );

        var withSteps = DtdValidatingPipeline.Validate(
            Utf16(Invalid),
            ref sink,
            options,
            readerOptions,
            DocumentUri,
            [step]
        );
        var sinkless = DtdValidatingPipeline.Validate(Utf16(Invalid), options, readerOptions, DocumentUri);

        await Assert.That(withSteps.IsWellFormed).IsTrue();
        await Assert.That(step.Context!.Options).IsSameReferenceAs(options);
        await Assert.That(withSteps.Issues.Issues).IsEmpty();
        await Assert.That(sinkless.Issues.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Sinkless_ConsumesEveryKindOfEvent()
    {
        const string Xml =
            "<?xml version=\"1.0\"?><?pre x?><!DOCTYPE r [<!ELEMENT r ANY><!ATTLIST r xmlns:p CDATA #IMPLIED>]>"
            + "<r xmlns:p=\"urn:p\" j=\"1\"><!--c-->t<?i d?></r>";

        var chars = DtdValidatingPipeline.Validate(Xml.AsSpan());
        var bytes = DtdValidatingPipeline.Validate(Utf16(Xml));

        await Assert.That(chars.IsWellFormed).IsTrue();
        await Assert.That(bytes.IsWellFormed).IsTrue();
        await Assert.That(chars.Issues.Issues.Count).IsEqualTo(bytes.Issues.Issues.Count);
    }

    [Test]
    public async Task Sink_OfAStructType_KeepsItsState()
    {
        var sink = new CountingSink();

        var chars = DtdValidatingPipeline.Validate("<r><a/><a/></r>".AsSpan(), ref sink);
        var bytes = DtdValidatingPipeline.Validate(Utf16("<r><a/></r>"), ref sink);

        await Assert.That(chars.IsWellFormed).IsTrue();
        await Assert.That(bytes.IsWellFormed).IsTrue();
        await Assert.That(sink.Elements).IsEqualTo(5);
    }

    [Test]
    public async Task Sink_OfAClassType_ReceivesTheAttributeFacts_ThroughTheDecorator()
    {
        var sink = new RecordingSink();

        _ = DtdValidatingPipeline.Validate("<!DOCTYPE r [<!ATTLIST r a ID #IMPLIED>]><r a=\"x\"/>".AsSpan(), ref sink);

        await Assert.That(sink.Events).Contains("info:a:Id:True");
    }

    [Test]
    public async Task ReaderOptions_SwitchOnDtdProcessing_ExternalAccess_AndTheBaseUri_AndKeepTheLimits()
    {
        var limits = XmlReaderLimits.Default.WithMaxDocumentCharacters(4096);
        var access = DtdValidationOptions.Default.WithAllowExternalSubset(true);

        var options = DtdValidatingPipeline.ReaderOptions(
            access,
            XmlReaderOptions.Default.WithLimits(limits),
            DocumentUri
        );

        await Assert.That(options.DtdProcessing).IsTrue();
        await Assert.That(options.ExternalAccess).IsSameReferenceAs(access);
        await Assert.That(options.BaseUri).IsEqualTo(DocumentUri);
        await Assert.That(options.Limits).IsSameReferenceAs(limits);
    }

    [Test]
    public async Task CreateSteps_ReturnsFiveNewSteps_InTheRegistrationOrderOfTheDesign()
    {
        var first = DtdValidatingPipeline.CreateSteps(XmlReaderLimits.Default);
        var second = DtdValidatingPipeline.CreateSteps(XmlReaderLimits.Default);

        await Assert
            .That(first.Select(s => s.GetType().Name))
            .IsEquivalentTo([
                "DtdContentModelStep",
                "DtdAttributeStep",
                "DtdIdentityStep",
                "DtdNotationStep",
                "DtdDocumentStep",
            ]);
        await Assert.That(first[0]).IsNotSameReferenceAs(second[0]);
    }

    [Test]
    public async Task Pipeline_ReaderLimits_ReachTheReader()
    {
        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxDocumentCharacters(8));

        var result = DtdValidatingPipeline.Validate("<r>0123456789</r>".AsSpan(), null, options);

        await Assert.That(result.IsWellFormed).IsFalse();
        await Assert.That(result.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML1101);
    }

    [Test]
    public async Task Pipeline_MaxDocumentCharactersOfLongMaxValue_ReadsTheBytes()
    {
        var options = XmlReaderOptions.Default.WithLimits(
            XmlReaderLimits.Default.WithMaxDocumentCharacters(long.MaxValue)
        );

        using var reader = XmlPipelineReader.Create(Utf16("<r>0123456789</r>"), options);
        var result = DtdValidatingPipeline.Validate(Utf16("<r>0123456789</r>"), null, options);

        await Assert.That(result.IsWellFormed).IsTrue();
    }

    [Test]
    public async Task Guards_NullArguments_Throw()
    {
        await Assert.That(() => DtdValidatingPipeline.CreateSteps(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => DtdValidatingPipeline.ReaderOptions(null!, null, null)).Throws<ArgumentNullException>();
        await Assert.That(() => new DtdDiscardSink(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new DtdPipelineResult(XmlReadStatus.Completed, null, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
            {
                var sink = new RecordingSink();
                return DtdValidatingPipeline.Run(null!, ref sink, DtdValidationOptions.Default, [], null);
            })
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
            {
                using var reader = XmlPipelineReader.Create("<r/>".AsSpan());
                var sink = new RecordingSink();
                return DtdValidatingPipeline.Run(reader, ref sink, null!, [], null);
            })
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Guards_TheSink_RejectsNullArguments()
    {
        using var reader = XmlPipelineReader.Create("<r/>".AsSpan());
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var inner = new RecordingSink();
        IReadOnlyList<IDtdValidationStep> steps = [];

        await Assert
            .That(() =>
                new DtdValidatingSink<RecordingSink>(null!, inner, steps, DtdValidationOptions.Default, issues, null)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new DtdValidatingSink<RecordingSink>(reader, null!, steps, DtdValidationOptions.Default, issues, null)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new DtdValidatingSink<RecordingSink>(reader, inner, null!, DtdValidationOptions.Default, issues, null)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new DtdValidatingSink<RecordingSink>(reader, inner, steps, null!, issues, null))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new DtdValidatingSink<RecordingSink>(reader, inner, steps, DtdValidationOptions.Default, null!, null)
            )
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Result_IsWellFormed_FollowsTheStatus()
    {
        var ok = new DtdPipelineResult(XmlReadStatus.Completed, null, ValidationResult.Valid);
        var failed = new DtdPipelineResult(XmlReadStatus.Error, null, ValidationResult.Valid);

        await Assert.That(ok.IsWellFormed).IsTrue();
        await Assert.That(failed.IsWellFormed).IsFalse();
    }

    [Test]
    public async Task Pipeline_WithoutOptions_UsesTheDefaults_AndAScriptedStepSeesThem()
    {
        var step = new ScriptedDtdValidationStep();

        _ = Run("<r/>", step);

        await Assert.That(step.Context!.Options).IsSameReferenceAs(DtdValidationOptions.Default);
    }
}
