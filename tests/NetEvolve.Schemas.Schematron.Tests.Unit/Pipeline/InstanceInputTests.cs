namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// One instance through every input kind gives identical results (SCH-55, #311): the instance is read through the readers of the Xml
/// package, spans and sequences without a second parse path, and a stream as a thin adapter.
/// </summary>
public class InstanceInputTests
{
    [Test]
    public async Task EveryInputKind_GivesIdenticalResults(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = Build(Schema);
        var results = ValidateSyncKinds(validator, Instance);
        results.AddRange(await ValidateAsyncKinds(validator, Instance, cancellationToken));
        var expected = Describe(results[0].Result);

        await Assert.That(results.Count).IsEqualTo(11);
        foreach (var (kind, result) in results)
        {
            await Assert.That(Describe(result)).IsEqualTo(expected).Because(kind);
        }
    }

    [Test]
    public async Task EveryInputKind_ReportsTheAssertAndTheReport()
    {
        var validator = Build(Schema);

        foreach (var (kind, result) in ValidateSyncKinds(validator, Instance))
        {
            await Assert.That(result.IsValid).IsFalse().Because(kind);
            await Assert.That(result.Issues.Count).IsEqualTo(2).Because(kind);

            var assertion = result.Issues[0];
            await Assert.That(assertion.Code).IsEqualTo(SchematronDiagnosticCodes.AssertionFailed).Because(kind);
            await Assert.That(assertion.Severity).IsEqualTo(IssueSeverity.Error).Because(kind);
            await Assert.That(assertion.Message).IsEqualTo("An invoice needs a number, Müller.").Because(kind);
            await Assert.That(assertion.Constraint).IsEqualTo("a1").Because(kind);
            await Assert.That(assertion.Location).IsEqualTo(new DocumentLocation(InstanceSystemId, 1, 1)).Because(kind);

            var report = result.Issues[1];
            await Assert.That(report.Code).IsEqualTo(SchematronDiagnosticCodes.ReportFired).Because(kind);
            await Assert.That(report.Severity).IsEqualTo(IssueSeverity.Warning).Because(kind);
            await Assert.That(report.Message).IsEqualTo("Large invoice: 2500 for Müller").Because(kind);
            await Assert.That(report.Constraint).IsEqualTo("rp1").Because(kind);
        }
    }

    [Test]
    public async Task ValidInstance_HasNoIssues()
    {
        var validator = Build(Schema);

        var result = Validate(validator, "<invoice xmlns=\"urn:invoice\"><number>1</number><total>5</total></invoice>");

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task MalformedInstance_ReportsTheXmlIssueAndRunsNoRule()
    {
        var validator = Build(Schema);

        foreach (var (kind, result) in ValidateSyncKinds(validator, "<invoice xmlns=\"urn:invoice\"><total>"))
        {
            await Assert.That(result.IsValid).IsFalse().Because(kind);
            await Assert.That(result.Issues.Count).IsEqualTo(1).Because(kind);
            await Assert
                .That(result.Issues[0].Code)
                .IsNotEqualTo(SchematronDiagnosticCodes.AssertionFailed)
                .Because(kind);
            await Assert.That(result.Issues[0].Location.Source).IsEqualTo(InstanceSystemId).Because(kind);
            await Assert.That(result.Issues[0].Location.Line).IsGreaterThan(0).Because(kind);
        }
    }

    [Test]
    public async Task InstanceDeeperThanMaxDepth_ReportsTheDepthLimit()
    {
        var validator = SchemaValidator
            .ForSchematron(Schema.AsSpan(), SchemaSystemId)
            .WithLimits(ValidationLimits.Default.WithMaxDepth(6))
            .BuildNow();

        var result = Validate(validator, "<a><b><c><d><e><f><g/></f></e></d></c></b></a>");

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.DepthLimitExceeded);
    }

    [Test]
    public async Task InstanceLargerThanMaxInputSize_ReportsTheInputSizeLimit()
    {
        var validator = SchemaValidator
            .ForSchematron(Schema.AsSpan(), SchemaSystemId)
            .WithLimits(ValidationLimits.Default.WithMaxInputSize(10))
            .BuildNow();

        var result = Validate(validator, Instance);

        await Assert.That(result.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.InputSizeLimitExceeded);
    }

    [Test]
    public async Task InstanceWithMoreAttributesThanTheLimit_ReportsTheAttributeLimit()
    {
        var validator = SchemaValidator
            .ForSchematron(Schema.AsSpan(), SchemaSystemId)
            .WithLimits(ValidationLimits.Default.WithMaxAttributesPerElement(1))
            .BuildNow();

        var result = Validate(validator, "<a x=\"1\" y=\"2\"/>");

        await Assert.That(result.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.AttributeLimitExceeded);
    }

    [Test]
    public async Task Validate_Cancelled_Cancels()
    {
        var validator = Build(Schema);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert
            .That(async () => await validator.ValidateAsync(Utf8(Instance).AsMemory(), InstanceSystemId, source.Token))
            .Throws<OperationCanceledException>();
    }

    private static async Task<(string Kind, ValidationResult Result)[]> ValidateAsyncKinds(
        ISchematronValidator validator,
        string instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = Utf8(instance);
        using var stream = new MemoryStream(bytes);
        return
        [
            ("async stream", await validator.ValidateAsync(stream, InstanceSystemId, cancellationToken)),
            ("async byte memory", await validator.ValidateAsync(bytes.AsMemory(), InstanceSystemId, cancellationToken)),
            (
                "async char memory",
                await validator.ValidateAsync(instance.AsMemory(), InstanceSystemId, cancellationToken)
            ),
            (
                "async char sequence",
                await validator.ValidateAsync(Segments(instance), InstanceSystemId, cancellationToken)
            ),
        ];
    }
}
