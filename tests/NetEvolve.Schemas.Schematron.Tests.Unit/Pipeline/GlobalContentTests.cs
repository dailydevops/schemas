namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System.Linq;
using System.Threading.Tasks;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// A global <c>xsl:variable</c> or <c>xsl:param</c> with a sequence constructor (XSLT 2.0 sections 9.3 and 9.5, #1160): the real XPath
/// evaluator and the public builder. Without <see langword="as"/> the value is a temporary tree with a document node, with
/// <see langword="as"/> it is the constructed sequence after the function conversion rules.
/// </summary>
public class GlobalContentTests
{
    private const string Xsl = "xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";
    private const string Ns =
        "<ns prefix=\"f\" uri=\"urn:f\"/><ns prefix=\"xs\" uri=\"http://www.w3.org/2001/XMLSchema\"/>";

    private static string Rule(string test) =>
        $"<pattern id=\"p\"><rule context=\"a\"><assert test=\"{test}\">Failed.</assert></rule></pattern>";

    private static ISchematronValidator Inline(string declarations, string test) =>
        Build(
            Wrap(
                Ns + declarations + Rule(test),
                $" queryBinding=\"xslt2\" schematronEdition=\"2025\" {Xsl} xmlns:f=\"urn:f\""
            )
        );

    private static string Messages(ValidationResult result) =>
        string.Join("|", result.Issues.Select(issue => issue.Message));

    [Test]
    [Arguments("<a>B</a>", true)]
    [Arguments("<a>Z</a>", false)]
    public async Task Variable_WithElementContent_IsATemporaryTreeNavigatedByPath(string instance, bool expected)
    {
        var validator = Inline(
            "<xsl:variable name=\"catalog\"><entry xmlns=\"\" code=\"A\"/><entry xmlns=\"\" code=\"B\"/></xsl:variable>",
            "string(.) = $catalog/entry/@code"
        );

        await Assert.That(Validate(validator, instance).IsValid).IsEqualTo(expected);
    }

    [Test]
    public async Task Variable_WithElementContent_HasADocumentNode()
    {
        var validator = Inline(
            "<xsl:variable name=\"t\"><e xmlns=\"\"/></xsl:variable>",
            "$t instance of document-node()"
        );

        await Assert.That(Validate(validator, "<a/>").IsValid).IsTrue();
    }

    [Test]
    public async Task Param_WithElementContent_IsTheDefault()
    {
        var validator = Inline(
            "<xsl:param name=\"retired\"><entry xmlns=\"\" code=\"C\"/></xsl:param>",
            "not(string(.) = $retired/entry/@code)"
        );

        await Assert.That(Validate(validator, "<a>A</a>").IsValid).IsTrue();
        await Assert.That(Validate(validator, "<a>C</a>").IsValid).IsFalse();
    }

    [Test]
    public async Task Variable_WithEmptyContent_IsTheEmptyString()
    {
        var validator = Inline("<xsl:variable name=\"t\"></xsl:variable>", "$t = '' and $t instance of xs:string");

        await Assert.That(Validate(validator, "<a/>").IsValid).IsTrue();
    }

    [Test]
    public async Task Variable_WithElementContent_ReadsAnotherGlobalDeclaredAfterIt()
    {
        var validator = Inline(
            "<xsl:variable name=\"copy\"><v xmlns=\"\"><xsl:value-of select=\"$n\"/></v></xsl:variable><xsl:variable name=\"n\" select=\"21 * 2\"/>",
            "$copy/v = '42'"
        );

        await Assert.That(Validate(validator, "<a/>").IsValid).IsTrue();
    }

    [Test]
    public async Task Variable_WithAType_IsTheConstructedSequence()
    {
        var validator = Inline(
            "<xsl:variable name=\"n\" as=\"xs:integer\"><xsl:sequence select=\"40 + 2\"/></xsl:variable>",
            "$n = 42"
        );

        await Assert.That(Validate(validator, "<a/>").IsValid).IsTrue();
    }

    [Test]
    public async Task Variable_WithATypeTheContentDoesNotMatch_ReportsTheTypeError()
    {
        var validator = Inline(
            "<xsl:variable name=\"n\" as=\"xs:integer\"><xsl:sequence select=\"'abc'\"/></xsl:variable>",
            "$n = 1"
        );

        var result = Validate(validator, "<a/>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(Messages(result)).Contains("XTTE0570");
    }

    [Test]
    public async Task Variable_ThatReadsItself_IsXtde0640()
    {
        var validator = Inline(
            "<xsl:variable name=\"t\"><e xmlns=\"\"><xsl:value-of select=\"$t\"/></e></xsl:variable>",
            "$t"
        );

        var result = Validate(validator, "<a/>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Any(issue => issue.Constraint == "XTDE0640")).IsTrue();
    }

    [Test]
    public async Task Variable_WithElementContentThatIsNotUsed_IsNeverEvaluated()
    {
        var validator = Inline(
            "<xsl:variable name=\"n\" as=\"xs:integer\"><xsl:sequence select=\"'abc'\"/></xsl:variable>",
            "true()"
        );

        await Assert.That(Validate(validator, "<a/>").IsValid).IsTrue();
    }
}
