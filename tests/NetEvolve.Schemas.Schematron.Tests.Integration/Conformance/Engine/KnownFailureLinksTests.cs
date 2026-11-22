namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

public class KnownFailureLinksTests
{
    [Test]
    public async Task Parse_Line_ReadsCaseIssueAndReason()
    {
        var links = KnownFailureLinks.Parse("set/one.xml\t#832\tXSLT declarations are reported NES3204.\n");

        await Assert.That(links).HasSingleItem();
        await Assert
            .That(links[0])
            .IsEqualTo(new KnownFailureLink("set/one.xml", 832, "XSLT declarations are reported NES3204."));
    }

    [Test]
    public async Task Parse_BlankLinesAndCommentLines_AreIgnored()
    {
        var links = KnownFailureLinks.Parse("# header\n\nset/one.xml\t#1\tr\n");

        await Assert.That(links.Select(l => l.CaseId)).IsEquivalentTo(["set/one.xml"]);
    }

    [Test]
    [Arguments("set/one.xml\t\treason")]
    [Arguments("set/one.xml\t832\treason")]
    [Arguments("set/one.xml\t#0\treason")]
    [Arguments("set/one.xml\t#abc\treason")]
    [Arguments("set/one.xml\t#832")]
    [Arguments("set/one.xml\t#832\t ")]
    [Arguments("\t#832\treason")]
    public async Task Parse_LineWithoutAValidIssueOrReason_Throws(string line) =>
        await Assert.That(() => KnownFailureLinks.Parse(line + "\n")).Throws<InvalidDataException>();

    [Test]
    public async Task Parse_CaseListedTwice_Throws() =>
        await Assert
            .That(() => KnownFailureLinks.Parse("set/one.xml\t#1\tr\nset/one.xml\t#2\tr\n"))
            .Throws<InvalidDataException>();

    [Test]
    public async Task Verify_EveryBaselineEntryHasALink_ReportsNothing()
    {
        var problems = KnownFailureLinks.Verify(["set/one.xml"], [new KnownFailureLink("set/one.xml", 1, "r")]);

        await Assert.That(problems).IsEmpty();
    }

    [Test]
    public async Task Verify_BaselineEntryWithoutLink_IsReported()
    {
        var problems = KnownFailureLinks.Verify(
            ["set/one.xml", "set/two.xml"],
            [new KnownFailureLink("set/one.xml", 1, "r")]
        );

        await Assert.That(problems).HasSingleItem();
        await Assert.That(problems[0]).Contains("set/two.xml");
    }

    [Test]
    public async Task Verify_LinkWithoutBaselineEntry_IsReported()
    {
        var problems = KnownFailureLinks.Verify(
            ["set/one.xml"],
            [new KnownFailureLink("set/one.xml", 1, "r"), new KnownFailureLink("set/gone.xml", 2, "r")]
        );

        await Assert.That(problems).HasSingleItem();
        await Assert.That(problems[0]).Contains("set/gone.xml");
    }
}
