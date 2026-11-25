namespace NetEvolve.Schemas.Schematron.Tests.Unit.Results.Mapping;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

public class IssueMapperTests
{
    private static readonly DocumentLocation Where = new("file:///doc.xml", 7, 3);

    private static ValidationLimits Limits(int maxIssues) => ValidationLimits.Default.WithMaxIssues(maxIssues);

    [Test]
    public async Task Map_FailedAssert_IsErrorWithAssertionFailedCode()
    {
        var collector = new ValidationIssueCollector(Limits(10));
        var mapper = new IssueMapper(collector, null);

        var issue = mapper.Map(false, "a1", "Order needs an id", Where);

        await Assert.That(issue).IsNotNull();
        await Assert.That(issue!.Code).IsEqualTo(SchematronDiagnosticCodes.AssertionFailed);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("a1");
        await Assert.That(issue.Message).IsEqualTo("Order needs an id");
        await Assert.That(issue.Location.Source).IsEqualTo("file:///doc.xml");
        await Assert.That(issue.Location.Line).IsEqualTo(7);
        await Assert.That(issue.Location.Column).IsEqualTo(3);
        await Assert.That(collector.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Map_SuccessfulReport_IsWarningWithReportFiredCode()
    {
        var mapper = new IssueMapper(new ValidationIssueCollector(Limits(10)), null);

        var issue = mapper.Map(true, "r1#a2", "Seen", Where);

        await Assert.That(issue!.Code).IsEqualTo(SchematronDiagnosticCodes.ReportFired);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Warning);
    }

    [Test]
    public async Task Map_WhenCollectorIsFull_ReturnsNullAndRemembersDroppedError()
    {
        var collector = new ValidationIssueCollector(Limits(1));
        var mapper = new IssueMapper(collector, null);

        var kept = mapper.Map(true, "r1", "first", Where);
        var droppedWarning = mapper.Map(true, "r2", "second", Where);
        var hasDroppedAfterWarning = mapper.HasDroppedError;
        var droppedError = mapper.Map(false, "a3", "third", Where);

        await Assert.That(kept).IsNotNull();
        await Assert.That(droppedWarning).IsNull();
        await Assert.That(hasDroppedAfterWarning).IsFalse();
        await Assert.That(droppedError).IsNull();
        await Assert.That(mapper.HasDroppedError).IsTrue();
        await Assert.That(collector.ToResult().IsValid).IsFalse();
        await Assert.That(collector.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Finding_CarriesFlagRoleSeverityPathAndIssue_ToTheSink()
    {
        var collector = new ValidationIssueCollector(Limits(10));
        var sink = new CapturingSink();
        var mapper = new IssueMapper(collector, sink);

        Forward(mapper, "warning", "f1 f2", "fatal", "/a/b[1]", "a1", Where);

        var finding = sink.Last;
        await Assert.That(mapper.WantsLocationPath).IsTrue();
        await Assert.That(finding).IsNotNull();
        await Assert.That(finding!.Flags).IsEqualTo("f1 f2");
        await Assert.That(finding.Role).IsEqualTo("warning");
        await Assert.That(finding.Severity).IsEqualTo("fatal");
        await Assert.That(finding.Location).IsEqualTo("/a/b[1]");
        await Assert.That(finding.RuleId).IsEqualTo("r1");
        await Assert.That(finding.Issue).IsNotNull();
        await Assert.That(finding.Issue!.Constraint).IsEqualTo("a1");
        await Assert.That(finding.Issue.Location.Line).IsEqualTo(7);
        await Assert.That(collector.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Finding_SeverityToken_NeverChangesIssueSeverity()
    {
        var collector = new ValidationIssueCollector(Limits(10));
        var mapper = new IssueMapper(collector, null);

        foreach (var token in new[] { "fatal", "warning", "info", "bogus", "$s", "" })
        {
            Forward(mapper, "role", "", token, "/", "a1", Where);
        }

        var result = collector.ToResult();

        await Assert.That(result.Issues.Count).IsEqualTo(6);
        foreach (var issue in result.Issues)
        {
            await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        }
    }

    [Test]
    public async Task Finding_WhenDropped_IsStillDeliveredWithNullIssue()
    {
        var collector = new ValidationIssueCollector(Limits(1));
        var sink = new CapturingSink();
        var mapper = new IssueMapper(collector, sink);

        Forward(mapper, "", "", "", "/", "a1", Where);
        Forward(mapper, "", "", "", "/", "a2", Where);

        await Assert.That(sink.Count).IsEqualTo(2);
        await Assert.That(sink.Last!.Constraint).IsEqualTo("a2");
        await Assert.That(sink.Last.Issue).IsNull();
        await Assert.That(mapper.HasDroppedError).IsTrue();
    }

    [Test]
    public async Task Events_WithoutSink_AreAcceptedAndNoLocationPathIsWanted()
    {
        var mapper = new IssueMapper(new ValidationIssueCollector(Limits(10)), null);

        RunWithoutFindings(mapper);

        await Assert.That(mapper.WantsLocationPath).IsFalse();
    }

    [Test]
    public async Task Events_AreForwardedToTheSinkInOrder()
    {
        var sink = new FakeReportSink();
        var mapper = new IssueMapper(new ValidationIssueCollector(Limits(10)), sink);

        RunWithoutFindings(mapper);

        await Assert
            .That(sink.Events)
            .IsEquivalentTo(
                new List<string>
                {
                    "begin:#ALL:file:///doc.xml",
                    "pattern:p1",
                    "rule:r1",
                    "error:NES3301:/",
                    "end:False:NES0000",
                }
            );
    }

    [Test]
    public async Task Fanout_Mapper_RunsBeforeSink_IssueAvailable()
    {
        var collector = new ValidationIssueCollector(Limits(10));
        var sink = new OrderSink(collector);
        IEvaluationEvents events = new IssueMapper(collector, sink);

        MapThenDeliver(events);

        await Assert.That(sink.CountSeenAtFinding).IsEqualTo(1);
        await Assert.That(sink.IssueSeen).IsNotNull();
        await Assert.That(sink.IssueSeen).IsSameReferenceAs(collector.ToResult().Issues[0]);
    }

    [Test]
    public async Task Constructor_NullCollector_Throws() =>
        await Assert.That(() => new IssueMapper(null!, null)).Throws<ArgumentNullException>();

    // The event structs are ref structs: they live in synchronous helpers, never in async test methods.
    private static void MapThenDeliver(IEvaluationEvents events)
    {
        var issue = events.Map(false, "a1", "message", Where);
        var finding = new SchematronFinding(
            false,
            "a1",
            "r1",
            "a1",
            "@id",
            "/",
            "",
            "",
            "",
            "message",
            default,
            default,
            issue
        );
        events.Finding(in finding);
    }

    private sealed class OrderSink(ValidationIssueCollector collector) : ISchematronReportSink
    {
        internal int CountSeenAtFinding { get; private set; }

        internal ValidationIssue? IssueSeen { get; private set; }

        public void Begin(in SchematronRunInfo info) { }

        public void ActivePattern(in SchematronPatternEvent pattern) { }

        public void FiredRule(in SchematronRuleEvent rule) { }

        public void Finding(in SchematronFinding finding)
        {
            CountSeenAtFinding = collector.Count;
            IssueSeen = finding.Issue;
        }

        public void ErrorOccurred(in SchematronErrorEvent errorEvent) { }

        public void Finish(in SchematronRunSummary summary) { }
    }

    // The event structs are ref structs: they live in synchronous helpers, never in async test methods.
    private static void Forward(
        IssueMapper mapper,
        string role,
        string flags,
        string severity,
        string path,
        string constraint,
        DocumentLocation location
    )
    {
        var issue = mapper.Map(false, constraint, "message", location);
        var finding = new SchematronFinding(
            false,
            constraint,
            "r1",
            constraint,
            "@id",
            path,
            role,
            flags,
            severity,
            "message",
            default,
            default,
            issue
        );
        mapper.Finding(in finding);
    }

    private static void RunWithoutFindings(IssueMapper mapper)
    {
        var info = new SchematronRunInfo("t", "#ALL", "", default, [], "file:///doc.xml");
        mapper.Begin(in info);
        var pattern = new SchematronPatternEvent("p1", "", "", "", false);
        mapper.ActivePattern(in pattern);
        var rule = new SchematronRuleEvent("r1", "a", "", "", "");
        mapper.FiredRule(in rule);
        var error = new SchematronErrorEvent("file:///doc.xml", "/", "NES3301", "bad", null);
        mapper.ErrorOccurred(in error);
        var summary = new SchematronRunSummary(false, default);
        mapper.Finish(in summary);
    }

    private sealed class CapturingSink : ISchematronReportSink
    {
        internal int Count { get; private set; }

        internal Captured? Last { get; private set; }

        public void Begin(in SchematronRunInfo info) { }

        public void ActivePattern(in SchematronPatternEvent pattern) { }

        public void FiredRule(in SchematronRuleEvent rule) { }

        public void Finding(in SchematronFinding finding)
        {
            Count++;
            Last = new Captured(
                finding.Role.ToString(),
                finding.Flags.ToString(),
                finding.Severity.ToString(),
                finding.Location.ToString(),
                finding.RuleId.ToString(),
                finding.Constraint.ToString(),
                finding.Issue
            );
        }

        public void ErrorOccurred(in SchematronErrorEvent errorEvent) { }

        public void Finish(in SchematronRunSummary summary) { }
    }

    private sealed record Captured(
        string Role,
        string Flags,
        string Severity,
        string Location,
        string RuleId,
        string Constraint,
        ValidationIssue? Issue
    );
}
