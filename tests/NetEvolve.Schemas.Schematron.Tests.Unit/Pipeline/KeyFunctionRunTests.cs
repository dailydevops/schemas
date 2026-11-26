namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System.Linq;
using System.Threading.Tasks;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The XPath function <c>key</c> in the rules of a schema with <c>xsl:key</c> declarations (#776): the run assigns the key provider of
/// the schema to the keys host slot, bound to the documents of the run.
/// </summary>
public class KeyFunctionRunTests
{
    private const string Xsl = "xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";
    private const string Key = "<xsl:key name=\"k\" match=\"item\" use=\"@id\"/>";

    private static ISchematronValidator Inline(string rule, string declarations = Key) =>
        Build(
            Wrap(
                declarations + $"<pattern id=\"p\"><rule context=\"doc\">{rule}</rule></pattern>",
                $" queryBinding=\"xslt2\" schematronEdition=\"2025\" {Xsl}"
            )
        );

    private static ISchematronValidator Assert2(string test) => Inline($"<assert test=\"{test}\">Failed.</assert>");

    private const string Document =
        "<doc><item id=\"x\"/><item id=\"y\"/><item id=\"x\"/><sub><item id=\"x\"/></sub></doc>";

    [Test]
    public async Task Key_TwoArguments_FindsEveryMatchOfTheDocument()
    {
        var result = Validate(Assert2("count(key('k', 'x')) = 3 and count(key('k', ('x', 'y'))) = 4"), Document);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Key_ThreeArguments_RestrictsTheMatchesToTheSubtree()
    {
        var result = Validate(Assert2("count(key('k', 'x', sub)) = 1"), Document);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Key_InALetVariable_IsEvaluated()
    {
        var validator = Inline(
            "<let name=\"n\" value=\"count(key('k', 'x'))\"/><assert test=\"$n = 3\">Failed.</assert>"
        );

        await Assert.That(Validate(validator, Document).IsValid).IsTrue();
    }

    [Test]
    public async Task Key_InTwoRunsOfOneValidator_UsesTheDocumentOfEachRun()
    {
        var validator = Assert2("count(key('k', 'x')) = 1");

        var first = Validate(validator, "<doc><item id=\"x\"/></doc>");
        var second = Validate(validator, "<doc><item id=\"x\"/><item id=\"x\"/></doc>");
        var third = Validate(validator, "<doc><item id=\"x\"/></doc>");

        await Assert.That(first.IsValid).IsTrue();
        await Assert.That(second.IsValid).IsFalse();
        await Assert.That(third.IsValid).IsTrue();
    }

    [Test]
    public async Task Key_WithAnUnknownName_IsXTDE1260()
    {
        var issue = Validate(Assert2("count(key('unknown', 'x')) = 0"), Document).Issues.Single();

        await Assert.That(issue.Constraint).IsEqualTo("XTDE1260");
    }
}
