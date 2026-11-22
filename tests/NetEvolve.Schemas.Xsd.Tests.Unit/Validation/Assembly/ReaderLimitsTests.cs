namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>One limit governs the reader and the name table of the validator (tracking note on #335 from the review of #607).</summary>
public class ReaderLimitsTests
{
    private const string Document = "<a><b/><c/></a>";

    [Test]
    public async Task Names_TakeMaxDistinctNamesOfTheConfiguredReaderLimits()
    {
        var limits = XmlReaderLimits.Default.WithMaxDistinctNames(2);

        var (status, diagnostic) = Pump(limits);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(diagnostic).IsEqualTo(XmlErrorCodes.XML1111);
    }

    [Test]
    public async Task Names_WithoutConfiguredLimits_UseTheDefaults()
    {
        var (status, diagnostic) = Pump(null);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(diagnostic).IsNull();
    }

    private static (XmlReadStatus Status, XmlErrorCode? Diagnostic) Pump(XmlReaderLimits? limits)
    {
        var schema = new SchemaFixture();
        _ = schema.Global("a", SchemaFixture.AnyType);
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var validator = new StreamingValidator(
            schema.Model(),
            new ScriptedAssessor(),
            new XsdIssueReporter(collector),
            ValidationLimits.Default,
            ValidationHarness.Source,
            [],
            readerLimits: limits
        );
        using var reader = XmlPipelineReader.Create(Document.AsSpan());

        var status = reader.Pump(ref validator);

        return (status, reader.Diagnostic?.Code);
    }
}
