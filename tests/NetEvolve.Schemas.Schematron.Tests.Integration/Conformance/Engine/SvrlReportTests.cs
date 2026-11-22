namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

using System.Text;

/// <summary>
/// The comparison of SVRL output with the expectations of the suite. The suite states its expectations as XPath assertions on the SVRL
/// document, so the comparison ignores exactly what XPath does not see: the order of attributes, white space between elements, and the
/// prefix of the SVRL namespace. Everything else, text and structure included, is significant.
/// </summary>
public class SvrlReportTests
{
    private const string Ns = "http://purl.oclc.org/dsdl/svrl";

    private static SvrlReport Svrl(string body, string prefix = "svrl") =>
        SvrlReport.Load(
            Encoding.UTF8.GetBytes(
                $"<{prefix}:schematron-output xmlns:{prefix}=\"{Ns}\">{body}</{prefix}:schematron-output>"
            )
        );

    [Test]
    public async Task Check_AssertionHolds_ReportsNothing()
    {
        var report = Svrl("""<svrl:failed-assert test="false()"/>""");

        await Assert.That(report.Check(["//svrl:failed-assert/@test = 'false()'"])).IsEmpty();
    }

    [Test]
    public async Task Check_FiredAssertionIsMissing_FailsAndNamesTheAssertion()
    {
        var report = Svrl("""<svrl:active-pattern/>""");

        var failures = report.Check(["count(//svrl:failed-assert) = 1"]);

        await Assert.That(failures).HasSingleItem();
        await Assert.That(failures[0]).Contains("count(//svrl:failed-assert) = 1");
    }

    [Test]
    public async Task Check_EveryAssertionIsEvaluated_AndEachFailureIsReported()
    {
        var report = Svrl("""<svrl:failed-assert test="a"/>""");

        var failures = report.Check([
            "//svrl:failed-assert/@test = 'b'",
            "//svrl:failed-assert/@test = 'a'",
            "false()",
        ]);

        await Assert.That(failures.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Check_AttributeOrderDiffers_GivesTheSameResult()
    {
        const string assertion = "//svrl:failed-assert[@id = 'a'][@test = 't'][@flag = 'f']";
        var one = Svrl("""<svrl:failed-assert id="a" test="t" flag="f"/>""");
        var other = Svrl("""<svrl:failed-assert flag="f" test="t" id="a"/>""");

        await Assert.That(one.Check([assertion])).IsEmpty();
        await Assert.That(other.Check([assertion])).IsEmpty();
    }

    [Test]
    public async Task Check_WhiteSpaceBetweenElementsDiffers_GivesTheSameResult()
    {
        const string assertion = "count(/svrl:schematron-output/svrl:active-pattern/svrl:fired-rule) = 2";
        var compact = Svrl("<svrl:active-pattern><svrl:fired-rule/><svrl:fired-rule/></svrl:active-pattern>");
        var indented = Svrl(
            "\n  <svrl:active-pattern>\n    <svrl:fired-rule/>\n    <svrl:fired-rule/>\n  </svrl:active-pattern>\n"
        );

        await Assert.That(compact.Check([assertion])).IsEmpty();
        await Assert.That(indented.Check([assertion])).IsEmpty();
    }

    [Test]
    public async Task Check_PrefixOfTheSvrlNamespaceDiffers_GivesTheSameResult()
    {
        var report = Svrl("""<s:failed-assert test="false()"/>""", prefix: "s");

        await Assert.That(report.Check(["//svrl:failed-assert/@test = 'false()'"])).IsEmpty();
    }

    [Test]
    public async Task Check_TextOfAMessageDiffers_IsSignificant()
    {
        var report = Svrl("<svrl:failed-assert><svrl:text>one</svrl:text></svrl:failed-assert>");

        await Assert.That(report.Check(["//svrl:failed-assert/svrl:text = 'two'"])).HasSingleItem();
    }

    [Test]
    public async Task Check_ElementInAnotherNamespace_IsNoSvrlElement()
    {
        var report = SvrlReport.Load(
            Encoding.UTF8.GetBytes(
                """<svrl:schematron-output xmlns:svrl="http://purl.oclc.org/dsdl/svrl"><failed-assert/></svrl:schematron-output>"""
            )
        );

        await Assert.That(report.Check(["count(//svrl:failed-assert) = 1"])).HasSingleItem();
    }

    [Test]
    public async Task Check_NodeSequenceAssertion_HoldsWhenItIsNotEmpty()
    {
        var report = Svrl("""<svrl:active-pattern id="pattern"/>""");

        await Assert.That(report.Check(["//svrl:active-pattern[@id = 'pattern']"])).IsEmpty();
        await Assert.That(report.Check(["//svrl:active-pattern[@id = 'other']"])).HasSingleItem();
    }

    [Test]
    public async Task Check_AssertionThatDoesNotCompile_FailsAndSaysSo()
    {
        var report = Svrl("");

        var failures = report.Check(["//svrl:failed-assert["]);

        await Assert.That(failures).HasSingleItem();
        await Assert.That(failures[0]).Contains("does not compile");
    }

    [Test]
    public async Task HasError_ErrorElementPresent_IsTrue()
    {
        await Assert.That(Svrl("""<svrl:error code="NES3204"/>""").HasError).IsTrue();
        await Assert.That(Svrl("""<svrl:failed-assert test="false()"/>""").HasError).IsFalse();
    }

    [Test]
    public async Task Load_DocumentIsNotWellFormed_Throws() =>
        await Assert
            .That(() => SvrlReport.Load(Encoding.UTF8.GetBytes("<svrl:schematron-output")))
            .Throws<InvalidDataException>();
}
