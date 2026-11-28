namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System.Linq;
using System.Threading.Tasks;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The diagnostic of an unknown function (#1506): NES3202 with the XPST0017 constraint names the function and its arity and points
/// at the element that holds the expression, not at the offset inside the expression text.
/// </summary>
public class UnknownFunctionDiagnosticTests
{
    // Line 1 is the schema element, so the lines below are the ones the author sees.
    private static string Schema(string body) =>
        "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\">\n"
        + "  <ns prefix=\"j\" uri=\"urn:j\"/>\n"
        + body
        + "</schema>";

    [Test]
    public async Task Assertion_NamesTheFunctionWithItsArity_AtTheAssertion()
    {
        var issue = BuildFailure(
            Schema(
                "  <pattern>\n"
                    + "    <rule context=\"a\">\n"
                    + "      <assert test=\"true() and j:isEqualOrSuperset(@x, @y)\">m</assert>\n"
                    + "    </rule>\n"
                    + "  </pattern>\n"
            )
        )
            .Issues.Single();

        await Assert.That(issue.Constraint).IsEqualTo("XPST0017");
        await Assert.That(issue.Message).Contains("j:isEqualOrSuperset#2");
        await Assert.That(issue.Location.Source).IsEqualTo(SchemaSystemId);
        await Assert.That(issue.Location.Line).IsEqualTo(5);
    }

    [Test]
    public async Task Let_NamesTheFunctionWithItsArity_AtTheLet()
    {
        var issue = BuildFailure(
            Schema(
                "  <pattern>\n"
                    + "    <rule context=\"a\">\n"
                    + "      <let name=\"v\" value=\"j:nope()\"/>\n"
                    + "      <assert test=\"true()\">m</assert>\n"
                    + "    </rule>\n"
                    + "  </pattern>\n"
            )
        )
            .Issues.Single();

        await Assert.That(issue.Constraint).IsEqualTo("XPST0017");
        await Assert.That(issue.Message).Contains("j:nope#0");
        await Assert.That(issue.Location.Line).IsEqualTo(5);
    }

    [Test]
    public async Task RuleContext_NamesTheFunctionWithItsArity_AtTheRule()
    {
        var issue = BuildFailure(
            Schema(
                "  <pattern>\n"
                    + "    <rule context=\"a[j:nope(1)]\">\n"
                    + "      <assert test=\"true()\">m</assert>\n"
                    + "    </rule>\n"
                    + "  </pattern>\n"
            )
        )
            .Issues.Single();

        await Assert.That(issue.Constraint).IsEqualTo("XPST0017");
        await Assert.That(issue.Message).Contains("j:nope#1");
        await Assert.That(issue.Location.Line).IsEqualTo(4);
    }
}
