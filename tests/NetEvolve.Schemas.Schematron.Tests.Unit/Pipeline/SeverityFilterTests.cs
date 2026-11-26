namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Buffers;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Unit.Svrl;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The public option <c>WithSeverityFilter</c> (#384): an allow-list of <c>severity</c> values. A failed assert or a successful report
/// whose severity is not listed is dropped from the result and from the SVRL output alike; a finding without a severity is kept.
/// </summary>
public class SeverityFilterTests
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";

    private const string Schema =
        "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" schematronEdition=\"2025\"><pattern><rule context=\"a\">"
        + "<assert id=\"fatal-1\" test=\"false()\" severity=\"fatal\">fatal assert</assert>"
        + "<assert id=\"minor-1\" test=\"false()\" severity=\"Minor\">minor assert</assert>"
        + "<assert id=\"none-1\" test=\"false()\">plain assert</assert>"
        + "<report id=\"fatal-2\" test=\"true()\" severity=\"fatal\">fatal report</report>"
        + "<report id=\"minor-2\" test=\"true()\" severity=\"minor\">minor report</report>"
        + "</rule></pattern></schema>";

    private static string[] Constraints(ValidationResult result) =>
        [.. result.Issues.Select(i => i.Constraint ?? string.Empty).OrderBy(c => c, StringComparer.Ordinal)];

    [Test]
    public async Task NoFilter_KeepsEveryFinding()
    {
        var result = Validate(Build(Schema), "<a/>");

        await Assert
            .That(Constraints(result))
            .IsEquivalentTo(new[] { "fatal-1", "fatal-2", "minor-1", "minor-2", "none-1" });
    }

    [Test]
    public async Task Filter_DropsUnlistedFailedAssertsAndReports_AndKeepsFindingsWithoutSeverity()
    {
        var validator = Builder(Schema).WithSeverityFilter(["fatal"]).Build();
        var result = Validate(validator, "<a/>");

        await Assert.That(Constraints(result)).IsEquivalentTo(new[] { "fatal-1", "fatal-2", "none-1" });
    }

    [Test]
    public async Task Filter_ThatDropsEveryErrorFinding_LeavesTheResultValid()
    {
        var schema = Wrap(
            "<pattern><rule context=\"a\"><assert test=\"false()\" severity=\"minor\">m</assert></rule></pattern>",
            " schematronEdition=\"2025\""
        );
        var result = Validate(Builder(schema).WithSeverityFilter(["fatal"]).Build(), "<a/>");

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Filter_DefaultComparison_IgnoresCase()
    {
        var result = Validate(Builder(Schema).WithSeverityFilter(["MINOR"]).Build(), "<a/>");

        await Assert.That(Constraints(result)).IsEquivalentTo(new[] { "minor-1", "minor-2", "none-1" });
    }

    [Test]
    public async Task Filter_OrdinalComparison_IsCaseSensitive()
    {
        var validator = Builder(Schema).WithSeverityFilter(["minor"], StringComparison.Ordinal).Build();
        var result = Validate(validator, "<a/>");

        await Assert.That(Constraints(result)).IsEquivalentTo(new[] { "minor-2", "none-1" });
    }

    [Test]
    public async Task Filter_LastCallWins()
    {
        var validator = Builder(Schema).WithSeverityFilter(["fatal"]).WithSeverityFilter(["minor"]).Build();
        var result = Validate(validator, "<a/>");

        await Assert.That(Constraints(result)).IsEquivalentTo(new[] { "minor-1", "minor-2", "none-1" });
    }

    [Test]
    public async Task Filter_CopiesTheList()
    {
        var severities = new[] { "fatal" };
        var builder = Builder(Schema).WithSeverityFilter(severities);
        severities[0] = "minor";
        var result = Validate(builder.Build(), "<a/>");

        await Assert.That(Constraints(result)).IsEquivalentTo(new[] { "fatal-1", "fatal-2", "none-1" });
    }

    [Test]
    public async Task Filter_AppliesToTheSvrlOutputToo()
    {
        var output = new ArrayBufferWriter<byte>();
        ValidationResult result;
        using (var sink = new SvrlReportSink(output))
        {
            result = Validate(Builder(Schema).WithSeverityFilter(["fatal"]).Build().WithReportSink(sink), "<a/>");
        }

        var svrl = XDocument.Parse(SvrlRuns.Text(output.WrittenSpan.ToArray()));
        var ids = svrl.Descendants()
            .Where(e => e.Name == Svrl + "failed-assert" || e.Name == Svrl + "successful-report")
            .Select(e => (string?)e.Attribute("id"))
            .OrderBy(i => i, StringComparer.Ordinal)
            .ToArray();

        await Assert.That(ids).IsEquivalentTo(new[] { "fatal-1", "fatal-2", "none-1" });
        await Assert.That(Constraints(result)).IsEquivalentTo(ids!);
    }

    [Test]
    public async Task Filter_RejectsNullEmptyAndBlankEntries()
    {
        var builder = Builder(Schema);

        await Assert.That(() => builder.WithSeverityFilter(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.WithSeverityFilter([])).Throws<ArgumentException>();
        await Assert.That(() => builder.WithSeverityFilter([null!])).Throws<ArgumentNullException>();
        await Assert.That(() => builder.WithSeverityFilter([string.Empty])).Throws<ArgumentException>();
    }
}
