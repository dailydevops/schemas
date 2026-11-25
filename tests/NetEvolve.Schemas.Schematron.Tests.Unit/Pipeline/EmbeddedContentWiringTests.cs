namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System.Linq;
using System.Threading.Tasks;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The default pipeline hands an embedded content to the slot binder and to the assertion evaluator (#914): a <see langword="let"/>
/// with element content and a property with <c>xsl:copy-of</c> are built instead of reported as NES3204.
/// </summary>
public class EmbeddedContentWiringTests
{
    private const string Xsl = "xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";

    [Test]
    public async Task LetWithElementContent_IsBound_AndTheAssertionSeesIt()
    {
        var schema = Wrap(
            "<let name=\"v\"><item>hello</item></let>"
                + "<pattern><rule context=\"a\"><assert test=\"contains($v, 'hello')\">Missing.</assert></rule></pattern>"
        );

        var result = Validate(Build(schema), "<a/>");

        await Assert.That(result.Issues.Select(i => i.Message)).IsEmpty();
    }

    [Test]
    public async Task PropertyWithCopyOf_IsBuilt_AndReportedWithoutAProblem()
    {
        var schema = Wrap(
            "<properties><property id=\"p\"><xsl:copy-of select=\"@id\"/></property></properties>"
                + "<pattern><rule context=\"a\"><assert test=\"false()\" properties=\"p\">Failed.</assert></rule></pattern>",
            " " + Xsl
        );

        var result = Validate(Build(schema), "<a id=\"7\"/>");

        await Assert.That(result.Issues.Select(i => i.Message)).IsEquivalentTo(["Failed."]);
    }
}
