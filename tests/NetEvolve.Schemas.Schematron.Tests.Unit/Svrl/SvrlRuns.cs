namespace NetEvolve.Schemas.Schematron.Tests.Unit.Svrl;

using System;
using System.Text;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>Event lists driven into a sink. The event structs are ref structs, so every run is a synchronous helper.</summary>
internal static class SvrlRuns
{
    private static readonly DocumentLocation Origin = new("fake.sch", 1, 1);

    internal const string Document = "file:///doc.xml";

    /// <summary>One pattern with a failed assert and a successful report, then a group with an error and a rule.</summary>
    internal static void Fixed(ISchematronReportSink sink)
    {
        Begin(sink, "main", SchematronEdition.Iso2025);

        var pattern = new SchematronPatternEvent("p1", "", "Order", "", false);
        sink.ActivePattern(in pattern);
        FiredRule(sink, "r1", "inv:order");
        string[] ids = ["d1"];
        string[] texts = ["Diag text"];
        string[] propertyIds = ["pr1"];
        string[] propertyTexts = ["Prop & text"];
        var assert = new SchematronFinding(
            false,
            "a1",
            "r1",
            "a1",
            "@id",
            "/inv:order[1]",
            "error",
            "f1 f2",
            "fatal",
            "Order needs an id",
            new SchematronReferenceList(ids, texts),
            new SchematronReferenceList(propertyIds, propertyTexts),
            null
        );
        sink.Finding(in assert);
        var report = new SchematronFinding(
            true,
            "",
            "r1",
            "#p1.r1.a2",
            "count(item) > 1",
            "/inv:order[1]",
            "",
            "",
            "",
            "Many <items>",
            default,
            default,
            null
        );
        sink.Finding(in report);

        var group = new SchematronPatternEvent("g1", "", "", "", true);
        sink.ActivePattern(in group);
        var rule = new SchematronRuleEvent("", "inv:item", "warn", "x", "");
        sink.FiredRule(in rule);
        Error(sink, "NES3301", "bad");
        Finish(sink, false);
    }

    internal static void Begin(ISchematronReportSink sink, string phase, SchematronEdition edition)
    {
        var info = new SchematronRunInfo(
            "Order checks",
            phase,
            "1.0",
            edition,
            [new SchematronNamespace(Origin, "inv", "urn:inv")],
            Document
        );
        sink.Begin(in info);
    }

    internal static void Pattern(ISchematronReportSink sink, string id)
    {
        var pattern = new SchematronPatternEvent(id, "", "", "", false);
        sink.ActivePattern(in pattern);
    }

    internal static void FiredRule(ISchematronReportSink sink, string id, string context)
    {
        var rule = new SchematronRuleEvent(id, context, "", "", "");
        sink.FiredRule(in rule);
    }

    internal static void Assert(ISchematronReportSink sink, string id, string message)
    {
        var finding = new SchematronFinding(
            false,
            id,
            "",
            id,
            "true()",
            "/",
            "",
            "",
            "",
            message,
            default,
            default,
            null
        );
        sink.Finding(in finding);
    }

    internal static void Error(ISchematronReportSink sink, string code, string message)
    {
        var error = new SchematronErrorEvent(Document, "/", code, message, null);
        sink.ErrorOccurred(in error);
    }

    internal static void Finish(ISchematronReportSink sink, bool truncated)
    {
        var summary = new SchematronRunSummary(
            truncated,
            truncated ? SchematronDiagnosticCodes.IssueLimitStop : default
        );
        sink.Finish(in summary);
    }

    /// <summary>The run of a malformed instance: no pattern, one top-level error.</summary>
    internal static void TopLevelError(ISchematronReportSink sink, string code)
    {
        Begin(sink, "#ALL", SchematronEdition.Iso2016);
        Error(sink, code, "no evaluation");
        Finish(sink, false);
    }

    /// <summary>Many passing rules in one pattern, followed by a small second pattern.</summary>
    internal static void ManyFiredRules(ISchematronReportSink sink, int count)
    {
        Begin(sink, "#ALL", SchematronEdition.Iso2016);
        Pattern(sink, "big");
        for (var i = 0; i < count; i++)
        {
            FiredRule(sink, "r", "context-" + i);
        }

        Pattern(sink, "small");
        FiredRule(sink, "r2", "ctx");
        Assert(sink, "a9", "still here");
        Finish(sink, false);
    }

    internal static string Text(byte[] bytes) => new UTF8Encoding(false).GetString(bytes);
}
