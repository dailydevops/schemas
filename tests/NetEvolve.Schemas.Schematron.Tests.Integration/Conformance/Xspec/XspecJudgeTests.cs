namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using System.Text;
using System.Xml.Linq;

public class XspecJudgeTests
{
    private const string Svrl = "http://purl.oclc.org/dsdl/svrl";

    private static readonly IReadOnlyDictionary<string, string> NoNamespaces = new Dictionary<string, string>();

    private static readonly XspecNodeLocator Locator = XspecNodeLocator.Load(Encoding.UTF8.GetBytes("<a><b/><b/></a>"));

    private static XDocument Output(string body) =>
        XDocument.Parse($"""<svrl:schematron-output xmlns:svrl="{Svrl}">{body}</svrl:schematron-output>""");

    private static XspecExpectation Expect(
        XspecExpectKind kind,
        string? id = null,
        string? role = null,
        string? location = null,
        int? count = null,
        string? text = null
    ) => new(kind, id, role, location, count, text, NoNamespaces);

    private const string Rule = """<svrl:active-pattern id="p1" role="info"/><svrl:fired-rule id="r1" role="warn"/>""";

    [Test]
    public async Task Check_ExpectAssert_MatchesFailedAssertById()
    {
        var svrl = Output(
            Rule + """<svrl:failed-assert id="a1" test="x"><svrl:text>m</svrl:text></svrl:failed-assert>"""
        );

        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Assert, id: "a1"), svrl, Locator)).IsNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Assert, id: "a2"), svrl, Locator)).IsNotNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Report, id: "a1"), svrl, Locator)).IsNotNull();
    }

    [Test]
    public async Task Check_IdAndRole_FallBackToTheRuleAndThePattern()
    {
        var svrl = Output(Rule + """<svrl:failed-assert test="x"/>""");

        await Assert
            .That(XspecJudge.Check(Expect(XspecExpectKind.Assert, id: "r1", role: "warn"), svrl, Locator))
            .IsNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Assert, id: "p1"), svrl, Locator)).IsNotNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Assert, role: "error"), svrl, Locator)).IsNotNull();
    }

    [Test]
    public async Task Check_RoleOfTheAssertion_WinsOverTheRule()
    {
        var svrl = Output(Rule + """<svrl:successful-report id="x" role="error" test="y"/>""");

        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Report, role: "error"), svrl, Locator)).IsNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Report, role: "warn"), svrl, Locator)).IsNotNull();
    }

    [Test]
    public async Task Check_Count_RequiresTheExactNumberOfMatches()
    {
        var svrl = Output(
            Rule + """<svrl:successful-report id="x" test="y"/><svrl:successful-report id="x" test="z"/>"""
        );

        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Report, id: "x", count: 2), svrl, Locator)).IsNull();
        await Assert
            .That(XspecJudge.Check(Expect(XspecExpectKind.Report, id: "x", count: 1), svrl, Locator))
            .IsNotNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Assert, count: 0), svrl, Locator)).IsNull();
    }

    [Test]
    public async Task Check_NotAssert_NeedsAFiredRuleAndNoMatch()
    {
        var failed = Output(Rule + """<svrl:failed-assert id="a1" test="x"/>""");
        var silent = Output(Rule);
        var nothingFired = Output("");

        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.NotAssert, id: "a1"), failed, Locator)).IsNotNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.NotAssert, id: "a2"), failed, Locator)).IsNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.NotAssert, id: "a1"), silent, Locator)).IsNull();
        await Assert
            .That(XspecJudge.Check(Expect(XspecExpectKind.NotAssert, id: "a1"), nothingFired, Locator))
            .IsNotNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.NotReport), nothingFired, Locator)).IsNotNull();
    }

    [Test]
    public async Task Check_Valid_IgnoresReportsBelowErrorSeverity()
    {
        var warned = Output(Rule + """<svrl:failed-assert role="warning" test="x"/>""");
        var errored = Output(Rule + """<svrl:failed-assert role="Error" test="x"/>""");
        var unroled = Output(Rule + """<svrl:successful-report test="x"/>""");

        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Valid), warned, Locator)).IsNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Valid), errored, Locator)).IsNotNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Valid), unroled, Locator)).IsNotNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Valid), Output(""), Locator)).IsNotNull();
    }

    [Test]
    public async Task Check_Text_ComparesTheNormalizedMessageParts()
    {
        var svrl = Output(
            Rule
                + """
                <svrl:failed-assert test="x">
                  <svrl:text>the   message</svrl:text>
                  <svrl:diagnostic-reference diagnostic="d">the diagnostic</svrl:diagnostic-reference>
                </svrl:failed-assert>
                """
        );

        await Assert
            .That(XspecJudge.Check(Expect(XspecExpectKind.Assert, text: "the message"), svrl, Locator))
            .IsNull();
        await Assert
            .That(XspecJudge.Check(Expect(XspecExpectKind.Assert, text: "the diagnostic"), svrl, Locator))
            .IsNull();
        await Assert.That(XspecJudge.Check(Expect(XspecExpectKind.Assert, text: "other"), svrl, Locator)).IsNotNull();
    }

    [Test]
    public async Task Check_Location_ComparesTheSelectedNodes()
    {
        var svrl = Output(Rule + """<svrl:failed-assert id="a" test="x" location="/a/b[2]"/>""");

        await Assert
            .That(XspecJudge.Check(Expect(XspecExpectKind.Assert, location: "/a[1]/b[2]"), svrl, Locator))
            .IsNull();
        await Assert
            .That(XspecJudge.Check(Expect(XspecExpectKind.Assert, location: "/a/b[1]"), svrl, Locator))
            .IsNotNull();
    }

    [Test]
    public async Task Same_ExpressionsThatSelectNoSingleNode_ReportTheProblem()
    {
        var same = Locator.Same("/a/b", NoNamespaces, "/a/b[1]", NoNamespaces, out var problem);

        await Assert.That(same).IsFalse();
        await Assert.That(problem).IsNotNull();

        await Assert.That(Locator.Same("1", NoNamespaces, "/a", NoNamespaces, out var atomic)).IsFalse();
        await Assert.That(atomic).IsNotNull();
        await Assert.That(Locator.Same("/a/(", NoNamespaces, "/a", NoNamespaces, out var broken)).IsFalse();
        await Assert.That(broken).IsNotNull();
        await Assert.That(Locator.Same("/a", NoNamespaces, "/a", NoNamespaces, out var fine)).IsTrue();
        await Assert.That(fine).IsNull();
    }

    [Test]
    public async Task Same_PrefixedNames_UseTheBindingsOfTheirOwnSide()
    {
        var locator = XspecNodeLocator.Load(Encoding.UTF8.GetBytes("""<n:a xmlns:n="urn:n"><n:b/></n:a>"""));
        var left = new Dictionary<string, string> { ["x"] = "urn:n" };
        var right = new Dictionary<string, string> { ["y"] = "urn:n" };

        await Assert.That(locator.Same("/x:a/x:b", left, "/y:a/y:b[1]", right, out var problem)).IsTrue();
        await Assert.That(problem).IsNull();
    }
}
