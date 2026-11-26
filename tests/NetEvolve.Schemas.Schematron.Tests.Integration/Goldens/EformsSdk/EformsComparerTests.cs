namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.EformsSdk;

using NetEvolve.Schemas.Conformance.Results;

public class EformsComparerTests
{
    private const string Head =
        """<svrl:schematron-output xmlns:svrl="http://purl.oclc.org/dsdl/svrl" phase="eforms-16"><svrl:ns-prefix-in-attribute-values prefix="cbc" uri="urn:cbc"/>""";

    private static EformsReport Report(params string[] body) =>
        EformsReport.Parse(Head + string.Concat(body) + "</svrl:schematron-output>");

    private static string Pattern(string id) => $"""<svrl:active-pattern id="{id}"/>""";

    private static string Failed(string id, string location = "/N/cbc:ID", string text = "t") =>
        $"""<svrl:failed-assert test="x" id="{id}" role="ERROR" location="{location}"><svrl:text>{text}</svrl:text></svrl:failed-assert>""";

    [Test]
    public async Task Parse_ReadsPhasePatternsStagesAndFindings()
    {
        var report = Report(
            Pattern("EFORMS-validation-stage-2a-1"),
            Failed("A", text: "  two\n   words "),
            Pattern("EFORMS-validation-stage-1m"),
            """<svrl:successful-report id="B" location="/N"><svrl:text>r</svrl:text></svrl:successful-report>""",
            Pattern("other"),
            """<svrl:fired-rule context="/*"/>""",
            Failed("C")
        );

        await Assert.That(report.Phase).IsEqualTo("eforms-16");
        await Assert
            .That(report.ActivePatterns)
            .IsEquivalentTo(["EFORMS-validation-stage-2a-1", "EFORMS-validation-stage-1m", "other"]);
        await Assert.That(report.Findings.Select(f => f.Stage)).IsEquivalentTo([2, 1, 0]);
        await Assert.That(report.Findings[0].Text).IsEqualTo("two words");
        await Assert.That(report.Findings[1].Role).IsEmpty();
        await Assert.That(report.Findings[0].Key).IsEqualTo("A\tERROR\t/N/cbc:ID");
    }

    [Test]
    public async Task Parse_KeepsErrorsApartAndPhaseNullWithoutAttribute()
    {
        var report = EformsReport.Parse(
            """<svrl:schematron-output xmlns:svrl="http://purl.oclc.org/dsdl/svrl"><svrl:error id="e"/></svrl:schematron-output>"""
        );

        await Assert.That(report.Phase).IsNull();
        await Assert.That(report.Errors).HasSingleItem();
        await Assert.That(report.Findings).IsEmpty();
    }

    [Test]
    public async Task RestrictTo_KeepsTheFindingsOfTheGivenPatterns()
    {
        var report = Report(Pattern("p1"), Failed("A"), Pattern("p2"), Failed("B"));

        var restricted = report.RestrictTo(["p2"]);

        await Assert.That(restricted.ActivePatterns).IsEquivalentTo(["p2"]);
        await Assert.That(restricted.Findings.Select(f => f.Id)).IsEquivalentTo(["B"]);
    }

    [Test]
    public async Task Compare_NoFindingsOnBothSides_IsOneValidCase()
    {
        var results = EformsComparer.Compare("n.xml", Report(), Report());

        await Assert.That(results).HasSingleItem();
        await Assert.That(results[0].CaseId).IsEqualTo("valid/n.xml");
        await Assert.That(results[0].Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Compare_SameFindingsInAnyOrder_PassesEveryStageAndTheText()
    {
        var expected = Report(Pattern("stage-1a"), Failed("A"), Pattern("stage-3a"), Failed("B"), Failed("B"));
        var actual = Report(Pattern("stage-3a"), Failed("B"), Failed("B"), Pattern("stage-1a"), Failed("A"));

        var results = EformsComparer.Compare("n.xml", expected, actual);

        await Assert
            .That(results.Select(r => r.CaseId))
            .IsEquivalentTo(["stage-1/n.xml", "stage-3/n.xml", "text/n.xml"]);
        await Assert.That(results.All(r => r.Outcome == CaseOutcome.Pass)).IsTrue();
    }

    [Test]
    public async Task Compare_MissingAndUnexpectedFindings_FailOnlyTheirStage()
    {
        var expected = Report(Pattern("stage-1a"), Failed("A"), Pattern("stage-3a"), Failed("B"));
        var actual = Report(
            Pattern("stage-1a"),
            Failed("A"),
            Pattern("stage-3a"),
            Failed("C"),
            Failed("D"),
            Failed("E"),
            Failed("F")
        );

        var results = EformsComparer.Compare("n.xml", expected, actual).ToDictionary(r => r.CaseId);

        await Assert.That(results["stage-1/n.xml"].Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(results["stage-3/n.xml"].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(results["stage-3/n.xml"].Reason)
            .IsEqualTo(
                "Missing 1: B ERROR /N/cbc:ID. Unexpected 4: C ERROR /N/cbc:ID; D ERROR /N/cbc:ID; E ERROR /N/cbc:ID; ...."
            );
        await Assert.That(results["text/n.xml"].Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Compare_SameKeyWithOtherText_FailsOnlyTheTextVerdict()
    {
        var expected = Report(Pattern("stage-1a"), Failed("A", text: "one"));
        var actual = Report(Pattern("stage-1a"), Failed("A", text: "two"));

        var results = EformsComparer.Compare("n.xml", expected, actual).ToDictionary(r => r.CaseId);

        await Assert.That(results["stage-1/n.xml"].Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(results["text/n.xml"].Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Compare_NullArguments_Throw()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Task.FromResult(EformsComparer.Compare(null!, Report(), Report()))
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Task.FromResult(EformsComparer.Compare("n", null!, Report()))
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Task.FromResult(EformsComparer.Compare("n", Report(), null!))
        );
    }
}
