namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System.Linq;
using System.Threading.Tasks;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// A rule context and an <c>xsl:key</c> match pattern that start with <c>key()</c> (#780): the pattern matches right to left
/// against the key index of the run.
/// </summary>
public class KeyContextRunTests
{
    private const string Attributes =
        " queryBinding=\"xslt2\" schematronEdition=\"2025\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";
    private const string Key = "<xsl:key name=\"k\" match=\"item\" use=\"@id\"/>";

    private const string Document =
        "<doc><item id=\"x\"><c n=\"1\"/><g><c n=\"2\"/></g></item><item id=\"y\"><c n=\"3\"/></item>"
        + "<item id=\"x\"><c n=\"4\"/></item><other><c n=\"5\"/></other></doc>";

    private static ISchematronValidator Context(string context, string declarations = Key) =>
        Build(
            Wrap(
                declarations
                    + $"<pattern id=\"p\"><rule context=\"{context}\"><assert test=\"false()\">hit</assert></rule></pattern>",
                Attributes
            )
        );

    private static int Hits(string context) => Validate(Context(context), Document).Issues.Count;

    [Test]
    [Arguments("key('k', 'x')", 2)]
    [Arguments("key('k', 'y')", 1)]
    [Arguments("key('k', 'none')", 0)]
    [Arguments("key('k', 'x')/c", 2)]
    [Arguments("key('k', 'x')/*", 3)]
    [Arguments("key('k', 'x')/c/@n", 2)]
    [Arguments("key('k', 'x')//c", 3)]
    [Arguments("key('k', 'x')//@n", 3)]
    [Arguments("key('k', 'x')/g/c", 1)]
    [Arguments("key('k', 'x')/c[@n = '4']", 1)]
    [Arguments("key('k', 'x')/c | other/c", 3)]
    [Arguments("key('k', 'y') | key('k', 'x')/c", 3)]
    [Arguments("key('k', 'y') | item", 3)]
    public async Task RuleContext_StartsWithKey_MatchesTheNodesBelowTheIndexedOnes(string context, int expected) =>
        await Assert.That(Hits(context)).IsEqualTo(expected);

    [Test]
    public async Task RuleContext_KeyWithAVariableArgument_ReadsTheVariable()
    {
        var validator = Context("key('k', $v)/c", "<let name=\"v\" value=\"'y'\"/>" + Key);

        await Assert.That(Validate(validator, Document).Issues.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("key('unknown', 'x')/c")]
    [Arguments("key('unknown', 'x')")]
    public async Task RuleContext_KeyOfAnUndeclaredName_IsXTDE1260(string context)
    {
        var issue = Validate(Context(context), Document).Issues[0];

        await Assert.That(issue.Constraint).IsEqualTo("XTDE1260");
    }

    [Test]
    public async Task XslKeyMatch_StartsWithKey_IndexesTheNodesBelowTheIndexedOnes()
    {
        const string declarations = Key + "<xsl:key name=\"c\" match=\"key('k', 'x')//c\" use=\"@n\"/>";
        var validator = Build(
            Wrap(
                declarations
                    + "<pattern id=\"p\"><rule context=\"doc\"><assert test=\"count(key('c', ('1', '2', '4'))) = 3 and empty(key('c', ('3', '5')))\">Failed.</assert></rule></pattern>",
                Attributes
            )
        );

        await Assert.That(Validate(validator, Document).IsValid).IsTrue();
    }

    [Test]
    public async Task XslKeyMatch_ReadingItsOwnKey_IsXTDE0640()
    {
        var validator = Build(
            Wrap(
                "<xsl:key name=\"k\" match=\"key('k', 'x')/c\" use=\"@n\"/>"
                    + "<pattern id=\"p\"><rule context=\"doc\"><assert test=\"empty(key('k', 'x'))\">m</assert></rule></pattern>",
                Attributes
            )
        );
        var issue = Validate(validator, Document).Issues[0];

        await Assert.That(issue.Constraint).IsEqualTo("XTDE0640");
    }

    [Test]
    [Arguments("key('k')")]
    [Arguments("key('k', @id)")]
    [Arguments("key('k', 'x', .)")]
    [Arguments("key('k', 'x')[1]")]
    public async Task RuleContext_KeyOutsideTheIdKeyPattern_FailsTheBuildAsUnsupported(string context)
    {
        var failure = BuildFailure(
            Wrap(
                $"{Key}<pattern><rule context=\"{context}\"><assert test=\"true()\">m</assert></rule></pattern>",
                Attributes
            )
        );

        await Assert.That(failure.Message).Contains("NES");
    }
}
