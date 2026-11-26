namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The public switch <c>WithGlobalPatternLets</c> (#615) on the real engines: a pattern <see langword="let"/> read from a later pattern
/// and from a function body, the collision error, the interaction with a phase, and the unchanged default.
/// </summary>
public class GlobalPatternLetBuildTests
{
    private const string Attributes =
        " queryBinding=\"xslt2\" schematronEdition=\"2025\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns:f=\"urn:f\"";

    private const string Ns = "<ns prefix=\"f\" uri=\"urn:f\"/>";

    private const string Function =
        "<xsl:function name=\"f:over\"><xsl:param name=\"x\"/><xsl:sequence select=\"$x &gt; $limit\"/></xsl:function>";

    private static string Source(string body) => Wrap(Ns + body, Attributes);

    private static string Declares(string name, string value) =>
        $"<pattern id=\"decl\"><let name=\"{name}\" value=\"{value}\"/><rule context=\"never\"><assert test=\"true()\">x</assert></rule></pattern>";

    private static string Uses(string test) =>
        $"<pattern id=\"use\"><rule context=\"a\"><assert test=\"{test}\">Failed.</assert></rule></pattern>";

    private static SchemaCompilationException Failure(string schema, bool enabled) =>
        Capture(() => _ = Builder(schema).WithGlobalPatternLets(enabled).BuildNow());

    [Test]
    public async Task On_PatternLet_IsReadFromALaterPattern()
    {
        var schema = Source(Declares("limit", "5") + Uses("number(.) &lt;= $limit"));
        var validator = Builder(schema).WithGlobalPatternLets(true).BuildNow();

        await Assert.That(Validate(validator, "<a>4</a>").IsValid).IsTrue();
        await Assert.That(Validate(validator, "<a>6</a>").IsValid).IsFalse();
    }

    [Test]
    public async Task On_PatternLet_IsReadFromAFunctionBody()
    {
        var schema = Source(Function + Declares("limit", "5") + Uses("not(f:over(number(.)))"));
        var validator = Builder(schema).WithGlobalPatternLets(true).BuildNow();

        await Assert.That(Validate(validator, "<a>4</a>").IsValid).IsTrue();
        await Assert.That(Validate(validator, "<a>6</a>").IsValid).IsFalse();
    }

    [Test]
    [Arguments(false)]
    public async Task Off_PatternLet_ReadFromALaterPattern_IsVariableUndefined(bool enabled)
    {
        var schema = Source(Declares("limit", "5") + Uses("number(.) &lt;= $limit"));

        var failure = Failure(schema, enabled);

        await Assert.That(failure.Issues.Select(i => i.Code.ToString()).Distinct()).IsEquivalentTo(["NES3300"]);
    }

    [Test]
    public async Task Default_IsOff_SoAPatternLetIsReadFromALaterPatternOnlyAfterTheSwitch()
    {
        var schema = Source(Declares("limit", "5") + Uses("number(.) &lt;= $limit"));

        var failure = Capture(() => _ = Builder(schema).BuildNow());

        await Assert.That(failure.Issues.Select(i => i.Code.ToString()).Distinct()).IsEquivalentTo(["NES3300"]);
    }

    [Test]
    public async Task Off_PatternLet_ReadFromAFunctionBody_IsVariableUndefined()
    {
        var schema = Source(Function + Declares("limit", "5") + Uses("not(f:over(number(.)))"));

        var failure = Failure(schema, enabled: false);

        await Assert.That(failure.Issues.Select(i => i.Code.ToString()).Distinct()).IsEquivalentTo(["NES3300"]);
    }

    [Test]
    public async Task On_PatternLet_WithTheNameOfASchemaLet_IsLetDuplicate()
    {
        var schema = Source("<let name=\"limit\" value=\"1\"/>" + Declares("limit", "5") + Uses("true()"));

        var failure = Failure(schema, enabled: true);

        await Assert.That(failure.Issues.Select(i => i.Code.ToString()).Distinct()).IsEquivalentTo(["NES3200"]);
    }

    [Test]
    public async Task Off_PatternLet_WithTheNameOfASchemaLet_ShadowsWithoutAnError()
    {
        var schema = Source(
            "<let name=\"limit\" value=\"1\"/><pattern id=\"p\"><let name=\"limit\" value=\"5\"/>"
                + "<rule context=\"a\"><assert test=\"number(.) &lt;= $limit\">Failed.</assert></rule></pattern>"
        );
        var validator = Builder(schema).WithGlobalPatternLets(false).BuildNow();

        await Assert.That(Validate(validator, "<a>4</a>").IsValid).IsTrue();
    }

    [Test]
    public async Task On_PhaseLet_ShadowsTheHoistedPatternLet()
    {
        var schema = Source(
            "<phase id=\"ph\"><let name=\"limit\" value=\"2\"/><active pattern=\"decl\"/><active pattern=\"use\"/></phase>"
                + Declares("limit", "5")
                + Uses("number(.) &lt;= $limit")
        );
        var validator = Builder(schema).WithPhase("ph").WithGlobalPatternLets(true).BuildNow();

        await Assert.That(Validate(validator, "<a>2</a>").IsValid).IsTrue();
        await Assert.That(Validate(validator, "<a>4</a>").IsValid).IsFalse();
    }

    [Test]
    public async Task On_PatternLet_OfAnInactivePattern_IsNotVisible()
    {
        var schema = Source(
            "<phase id=\"ph\"><active pattern=\"use\"/></phase>"
                + Declares("limit", "5")
                + Uses("number(.) &lt;= $limit")
        );

        var failure = Capture(() => _ = Builder(schema).WithPhase("ph").WithGlobalPatternLets(true).BuildNow());

        await Assert.That(failure.Issues.Select(i => i.Code.ToString()).Distinct()).IsEquivalentTo(["NES3300"]);
    }
}
