namespace NetEvolve.Schemas.Conformance.Tests.Unit.Reporting;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

public class ConformanceReportFileTests
{
    private const string Prefix = "{\"suite\":\"P\",\"targetFramework\":\"net10.0\",";

    private static ConformanceReport Report() =>
        ConformanceReport.Compare(
            [
                new CaseResult("d", "b-set", CaseOutcome.Skipped, "not modelled"),
                new CaseResult("c", "b-set", CaseOutcome.Fail, "boom \"x\"\nsecond line"),
                new CaseResult("b", "b-set", CaseOutcome.Fail, "known"),
                new CaseResult("a", "b-set", CaseOutcome.Pass),
                new CaseResult("z", "a-set", CaseOutcome.Pass),
            ],
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                ["a-set"] = KnownFailureBaseline.Parse("z\n"),
                ["b-set"] = KnownFailureBaseline.Parse("b\n"),
            }
        );

    private static string Text(ReadOnlyMemory<byte> lines) => Encoding.UTF8.GetString(lines.Span);

    [Test]
    public async Task Render_EveryCase_IsOneLineSortedByTestSetAndCaseId()
    {
        var text = Text(ConformanceReportFile.Render("P", "net10.0", Report().TestSets));

        await Assert
            .That(text)
            .IsEqualTo(
                Prefix
                    + "\"testSet\":\"a-set\",\"caseId\":\"z\",\"status\":\"unexpected-pass\"}\n"
                    + Prefix
                    + "\"testSet\":\"b-set\",\"caseId\":\"a\",\"status\":\"passed\"}\n"
                    + Prefix
                    + "\"testSet\":\"b-set\",\"caseId\":\"b\",\"status\":\"pending\",\"reason\":\"known\"}\n"
                    + Prefix
                    + "\"testSet\":\"b-set\",\"caseId\":\"c\",\"status\":\"regression\","
                    + "\"reason\":\"boom \\u0022x\\u0022\\nsecond line\"}\n"
                    + Prefix
                    + "\"testSet\":\"b-set\",\"caseId\":\"d\",\"status\":\"skipped\",\"reason\":\"not modelled\"}\n"
            );
    }

    [Test]
    public async Task Render_ReportWithoutCases_IsEmpty()
    {
        var report = ConformanceReport.Compare(
            [],
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        );

        await Assert.That(ConformanceReportFile.Render("P", "net10.0", report.TestSets).IsEmpty).IsTrue();
    }

    [Test]
    [Arguments("NetEvolve.Schemas.Xsd", "net10.0", "NetEvolve.Schemas.Xsd.net10.0.jsonl")]
    [Arguments("P (EN 16931 goldens)", "net10.0", "P--EN-16931-goldens-.net10.0.jsonl")]
    [Arguments("a/b:c", "t", "a-b-c.t.jsonl")]
    public async Task FileName_ReplacesEveryCharacterThatAFileNameCannotCarry(
        string package,
        string targetFramework,
        string expected
    ) => await Assert.That(ConformanceReportFile.FileName(package, targetFramework)).IsEqualTo(expected);

    [Test]
    public async Task Append_CreatesTheFolderAndAppendsToTheFileOfThePackage(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "report");
        try
        {
            var sets = Report().TestSets;

            ConformanceReportFile.Append("P", "net10.0", sets, directory);
            ConformanceReportFile.Append("P", "net10.0", sets, directory);

            var once = Text(ConformanceReportFile.Render("P", "net10.0", sets));
            await Assert
                .That(await File.ReadAllTextAsync(Path.Combine(directory, "P.net10.0.jsonl"), cancellationToken))
                .IsEqualTo(once + once);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(directory)!, recursive: true);
        }
    }

    [Test]
    public async Task Append_WithoutFolder_IsNoOp()
    {
        var sets = Report().TestSets;

        await Assert.That(() => ConformanceReportFile.Append("P", "net10.0", sets, null)).ThrowsNothing();
        await Assert.That(() => ConformanceReportFile.Append("P", "net10.0", sets, "")).ThrowsNothing();
    }

    [Test]
    public async Task Append_ReportWithoutCases_WritesNoFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        ConformanceReportFile.Append("P", "net10.0", [], directory);

        await Assert.That(Directory.Exists(directory)).IsFalse();
    }

    [Test]
    [NotInParallel]
    public async Task ReporterAppend_WithReportFolder_WritesTheCasesOnceAndNotForARepeatedReport(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var previous = Environment.GetEnvironmentVariable(ConformanceReportFile.DirectoryVariable);
        Environment.SetEnvironmentVariable(ConformanceReportFile.DirectoryVariable, directory);
        try
        {
            var report = Report();

            ConformanceRateReporter.Append("P", "net10.0", report, null);
            ConformanceRateReporter.Append("P part", "net10.0", report, null, listCases: false);
            ConformanceRateReporter.Collect("Q", "net10.0", report);
            ConformanceRateReporter.Flush(null);

            var expected = Text(ConformanceReportFile.Render("Q", "net10.0", report.TestSets));
            await Assert.That(Directory.GetFiles(directory).Length).IsEqualTo(2);
            await Assert.That(File.Exists(Path.Combine(directory, "P.net10.0.jsonl"))).IsTrue();
            await Assert
                .That(await File.ReadAllTextAsync(Path.Combine(directory, "Q.net10.0.jsonl"), cancellationToken))
                .IsEqualTo(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConformanceReportFile.DirectoryVariable, previous);
            Directory.Delete(directory, recursive: true);
        }
    }
}
