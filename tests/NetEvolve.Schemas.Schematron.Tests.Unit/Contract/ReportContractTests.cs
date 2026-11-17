namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System;
using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

/// <summary>
/// The event structs are ref structs, which cannot live in an async method; every scenario runs in a synchronous helper
/// that returns plain data, and the asynchronous tests only assert it.
/// </summary>
public class ReportContractTests
{
    private static readonly string[] OneId = ["d1"];
    private static readonly string[] OneText = ["text one"];
    private static readonly string[] TwoIds = ["d1", "d2"];
    private static readonly string[] TwoTexts = ["one", "two"];

    private static readonly ValidationIssue Issue = new(
        SchematronDiagnosticCodes.AssertionFailed,
        IssueSeverity.Error,
        "msg",
        new DocumentLocation("doc.xml", 1, 1),
        "a1"
    );

    private static SchematronFinding Finding(
        ReadOnlySpan<char> id,
        ReadOnlySpan<string> ids,
        ReadOnlySpan<string> texts,
        ValidationIssue? issue
    ) =>
        new(
            false,
            id,
            "r1".AsSpan(),
            "a1#i2".AsSpan(),
            "x > 1".AsSpan(),
            "/*[1]".AsSpan(),
            "warn".AsSpan(),
            "f g".AsSpan(),
            "high".AsSpan(),
            "message".AsSpan(),
            new SchematronReferenceList(ids, texts),
            new SchematronReferenceList(default, default),
            issue
        );

    private static List<string> DeliverAllEvents()
    {
        var sink = new FakeReportSink();
        var info = new SchematronRunInfo(
            "t".AsSpan(),
            "basic".AsSpan(),
            "1".AsSpan(),
            SchematronEdition.Iso2025,
            [],
            "doc.xml".AsSpan()
        );
        var pattern = new SchematronPatternEvent("p1".AsSpan(), default, "name".AsSpan(), "role".AsSpan(), true);
        var rule = new SchematronRuleEvent(
            "r1".AsSpan(),
            "invoice".AsSpan(),
            default,
            "f".AsSpan(),
            "doc.xml".AsSpan()
        );
        var error = new SchematronErrorEvent(
            "doc.xml".AsSpan(),
            "/".AsSpan(),
            "NES3301".AsSpan(),
            "boom".AsSpan(),
            null
        );
        var summary = new SchematronRunSummary(true, SchematronDiagnosticCodes.IssueLimitStop);
        var finding = Finding("a1".AsSpan(), default, default, null);

        sink.Begin(in info);
        sink.ActivePattern(in pattern);
        sink.FiredRule(in rule);
        sink.Finding(in finding);
        sink.ErrorOccurred(in error);
        sink.Finish(in summary);

        return sink.Events;
    }

    private static (FakeReportSink Sink, string Reused) DeliverFindingThenReuseBuffer()
    {
        var buffer = new char[] { 'm', 's', 'g' };
        var sink = new FakeReportSink();
        var finding = new SchematronFinding(
            true,
            default,
            default,
            "a1".AsSpan(),
            default,
            default,
            default,
            default,
            default,
            buffer,
            new SchematronReferenceList(TwoIds, TwoTexts),
            default,
            Issue
        );

        sink.Finding(in finding);
        Array.Clear(buffer, 0, buffer.Length);

        return (sink, finding.Message.ToString());
    }

    private static List<string> ReadFinding()
    {
        var finding = Finding("i".AsSpan(), OneId, OneText, Issue);

        return
        [
            finding.IsReport.ToString(),
            finding.Id.ToString(),
            finding.RuleId.ToString(),
            finding.Constraint.ToString(),
            finding.Test.ToString(),
            finding.Location.ToString(),
            finding.Role.ToString(),
            finding.Flags.ToString(),
            finding.Severity.ToString(),
            finding.Message.ToString(),
            finding.Diagnostics.Count.ToString(CultureInfo.InvariantCulture),
            finding.Diagnostics.IdAt(0),
            finding.Diagnostics.TextAt(0),
            finding.Properties.Count.ToString(CultureInfo.InvariantCulture),
            ReferenceEquals(finding.Issue, Issue).ToString(),
        ];
    }

    private static List<string> ReadEvents()
    {
        var namespaces = new[] { new SchematronNamespace(FakeModel.Origin, "x", "urn:x") };
        var info = new SchematronRunInfo(
            "title".AsSpan(),
            "ph".AsSpan(),
            "2".AsSpan(),
            SchematronEdition.Iso2025,
            namespaces,
            "d.xml".AsSpan()
        );
        var pattern = new SchematronPatternEvent("p".AsSpan(), "doc".AsSpan(), "n".AsSpan(), "r".AsSpan(), false);
        var rule = new SchematronRuleEvent(
            "id".AsSpan(),
            "ctx".AsSpan(),
            "role".AsSpan(),
            "f".AsSpan(),
            "d.xml".AsSpan()
        );
        var error = new SchematronErrorEvent("d.xml".AsSpan(), "/".AsSpan(), "NES3500".AsSpan(), "m".AsSpan(), null);
        var summary = new SchematronRunSummary(false, default);

        return
        [
            info.Title.ToString(),
            info.Phase.ToString(),
            info.SchemaVersion.ToString(),
            info.Edition.ToString(),
            info.Namespaces[0].Prefix,
            info.InstanceSystemId.ToString(),
            pattern.Id.ToString(),
            pattern.Documents.ToString(),
            pattern.Name.ToString(),
            pattern.Role.ToString(),
            pattern.IsGroup.ToString(),
            rule.Id.ToString(),
            rule.Context.ToString(),
            rule.Role.ToString(),
            rule.Flags.ToString(),
            rule.Document.ToString(),
            error.Document.ToString(),
            error.Location.ToString(),
            error.Code.ToString(),
            error.Message.ToString(),
            (error.Issue is null).ToString(),
            summary.IsTruncated.ToString(),
            summary.StopCode.ToString(),
        ];
    }

    private static string FinishThroughEvents(bool wantsLocationPath, out bool reported)
    {
        var events = new FakeEvaluationEvents(wantsLocationPath);
        var summary = new SchematronRunSummary(false, default);

        events.Finish(in summary);
        reported = events.WantsLocationPath;

        return events.Events[0];
    }

    [Test]
    public async Task FakeSink_ReceivesEventsInOrder() =>
        await Assert
            .That(DeliverAllEvents())
            .IsEquivalentTo([
                "begin:basic:doc.xml",
                "pattern:p1:group",
                "rule:r1",
                "assert:a1",
                "error:NES3301:/",
                "end:True:NES3502",
            ]);

    [Test]
    public async Task Finding_SpanMembers_AreValidOnlyDuringCall()
    {
        var (sink, reused) = DeliverFindingThenReuseBuffer();

        await Assert.That(sink.FindingMessage).IsEqualTo("msg");
        await Assert.That(sink.FindingConstraint).IsEqualTo("a1");
        await Assert.That(sink.FindingIssue).IsSameReferenceAs(Issue);
        await Assert.That(sink.DiagnosticCount).IsEqualTo(2);
        await Assert.That(sink.Events[0]).IsEqualTo("report:");
        await Assert.That(reused).IsNotEqualTo("msg");
    }

    [Test]
    public async Task Finding_CarriesEveryDataMember() =>
        await Assert
            .That(ReadFinding())
            .IsEquivalentTo([
                "False",
                "i",
                "r1",
                "a1#i2",
                "x > 1",
                "/*[1]",
                "warn",
                "f g",
                "high",
                "message",
                "1",
                "d1",
                "text one",
                "0",
                "True",
            ]);

    [Test]
    public async Task EventStructs_CarryTheirMembers() =>
        await Assert
            .That(ReadEvents())
            .IsEquivalentTo([
                "title",
                "ph",
                "2",
                "Iso2025",
                "x",
                "d.xml",
                "p",
                "doc",
                "n",
                "r",
                "False",
                "id",
                "ctx",
                "role",
                "f",
                "d.xml",
                "d.xml",
                "/",
                "NES3500",
                "m",
                "True",
                "False",
                "NES0000",
            ]);

    [Test]
    public async Task FakeEvaluationEvents_ExposeWantsLocationPath_AndForwardEvents()
    {
        var forwarded = FinishThroughEvents(true, out var withPath);
        _ = FinishThroughEvents(false, out var withoutPath);

        await Assert.That(withPath).IsTrue();
        await Assert.That(withoutPath).IsFalse();
        await Assert.That(forwarded).IsEqualTo("end:False:NES0000");
    }
}
