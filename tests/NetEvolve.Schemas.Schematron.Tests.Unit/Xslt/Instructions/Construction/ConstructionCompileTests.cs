namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System.Threading.Tasks;

/// <summary>
/// The static errors and the unsupported constructs of the construction instructions: every problem is located and coded, and a
/// construct of the schema-aware, attribute-set, and namespace-alias features is the unsupported XSLT construct error.
/// </summary>
public class ConstructionCompileTests
{
    [Test]
    [Arguments("<xsl:element/>", "NES3005")]
    [Arguments("<xsl:attribute/>", "NES3005")]
    [Arguments("<xsl:processing-instruction/>", "NES3005")]
    [Arguments("<xsl:namespace/>", "NES3005")]
    [Arguments("<xsl:copy-of/>", "NES3005")]
    public async Task MissingAttribute_IsReported(string fragment, string expected)
    {
        var harness = new ConstructionHarness(fragment);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(expected);
    }

    [Test]
    public async Task Namespace_WithoutSelectAndWithoutContent_IsXtse0910()
    {
        var harness = new ConstructionHarness("<xsl:namespace name=\"p\"/>");

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3005/XTSE0910");
    }

    [Test]
    [Arguments("<xsl:attribute name=\"a\" select=\"1\">x</xsl:attribute>", "NES3003/XTSE0840")]
    [Arguments(
        "<xsl:processing-instruction name=\"p\" select=\"1\">x</xsl:processing-instruction>",
        "NES3003/XTSE0880"
    )]
    [Arguments("<xsl:namespace name=\"p\" select=\"'urn:p'\">x</xsl:namespace>", "NES3003/XTSE0910")]
    [Arguments("<xsl:comment select=\"1\">x</xsl:comment>", "NES3003/XTSE0940")]
    public async Task SelectAndContent_AtOnce_IsAStaticError(string fragment, string expected)
    {
        var harness = new ConstructionHarness(fragment);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<xsl:element name=\"e\" use-attribute-sets=\"s\"/>")]
    [Arguments("<xsl:element name=\"e\" validation=\"strip\"/>")]
    [Arguments("<xsl:element name=\"e\" type=\"xs:string\"/>")]
    [Arguments("<xsl:element name=\"e\" inherit-namespaces=\"no\"/>")]
    [Arguments("<xsl:attribute name=\"a\" validation=\"lax\"/>")]
    [Arguments("<xsl:attribute name=\"a\" type=\"xs:string\"/>")]
    [Arguments("<xsl:copy use-attribute-sets=\"s\"/>")]
    [Arguments("<xsl:copy validation=\"strict\"/>")]
    [Arguments("<xsl:copy type=\"xs:string\"/>")]
    [Arguments("<xsl:copy copy-namespaces=\"no\"/>")]
    [Arguments("<xsl:copy inherit-namespaces=\"no\"/>")]
    [Arguments("<xsl:copy-of select=\"1\" validation=\"strip\"/>")]
    [Arguments("<xsl:copy-of select=\"1\" type=\"xs:string\"/>")]
    [Arguments("<xsl:copy-of select=\"1\" copy-namespaces=\"no\"/>")]
    [Arguments("<xsl:document validation=\"strip\"/>")]
    [Arguments("<xsl:document type=\"xs:string\"/>")]
    [Arguments("<out xsl:use-attribute-sets=\"s\"/>")]
    [Arguments("<out xsl:validation=\"strip\"/>")]
    [Arguments("<out xsl:type=\"xs:string\"/>")]
    [Arguments("<out xsl:inherit-namespaces=\"no\"/>")]
    [Arguments("<out xsl:extension-element-prefixes=\"f\"/>")]
    [Arguments("<out xsl:default-collation=\"urn:c\"/>")]
    [Arguments("<out xsl:xpath-default-namespace=\"urn:x\"/>")]
    public async Task UnsupportedConstruct_IsTheUnsupportedXsltConstructError(string fragment)
    {
        var harness = new ConstructionHarness(fragment);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3204");
    }

    [Test]
    [Arguments("<xsl:copy copy-namespaces=\"maybe\"/>", "NES3006/XTSE0020")]
    [Arguments("<xsl:copy inherit-namespaces=\"maybe\"/>", "NES3006/XTSE0020")]
    [Arguments("<xsl:copy-of select=\"1\" copy-namespaces=\"maybe\"/>", "NES3006/XTSE0020")]
    [Arguments("<xsl:element name=\"e\" inherit-namespaces=\"maybe\"/>", "NES3006/XTSE0020")]
    [Arguments("<out xsl:inherit-namespaces=\"maybe\"/>", "NES3006/XTSE0020")]
    [Arguments("<out xsl:bogus=\"1\"/>", "NES3006/XTSE0805")]
    [Arguments("<out xsl:exclude-result-prefixes=\"zz\"/>", "NES3006/XTSE0808")]
    [Arguments("<out xsl:exclude-result-prefixes=\"#default\"/>", "NES3006/XTSE0809")]
    [Arguments("<out a=\"{\"/>", "NES3006/XTSE0350")]
    [Arguments("<out a=\"}\"/>", "NES3006/XTSE0370")]
    [Arguments("<out a=\"{'x\"/>", "NES3006/XTSE0350")]
    [Arguments("<xsl:element name=\"{\"/>", "NES3006/XTSE0350")]
    [Arguments("<out a=\"{$zz}\"/>", "NES3300/XPST0008")]
    [Arguments("<out a=\"{1 +}\"/>", "NES3202/XPST0003")]
    public async Task InvalidAttribute_IsAStaticError(string fragment, string expected)
    {
        var harness = new ConstructionHarness(fragment);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(expected);
    }

    [Test]
    public async Task EveryProblemOfAnInstruction_IsReported_WithoutStoppingTheCompile()
    {
        var harness = new ConstructionHarness(
            "<out a=\"{\" b=\"}\" xsl:bogus=\"1\"><xsl:element/><xsl:attribute name=\"a\" select=\"1\">x</xsl:attribute></out>"
        );

        await Assert.That(harness.Executable).IsNull();
        await Assert
            .That(harness.CompileCodes)
            .IsEqualTo("NES3006/XTSE0350,NES3006/XTSE0370,NES3006/XTSE0805,NES3005,NES3003/XTSE0840");
    }

    [Test]
    public async Task CopyOf_WithTheCopyNamespacesYes_IsAccepted()
    {
        var harness = new ConstructionHarness("<xsl:copy-of select=\"1\" copy-namespaces=\"yes\"/>");

        await Assert.That(harness.Executable).IsNotNull();
        await Assert.That(harness.CompileCodes).IsEmpty();
    }

    [Test]
    public async Task Compile_LocatesTheProblemAtTheElement()
    {
        var harness = new ConstructionHarness("<out>\n<xsl:element/>\n</out>");

        await Assert.That(harness.Problems.Problems[0].Location.Line).IsEqualTo(2);
    }
}
