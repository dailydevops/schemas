namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The coded dynamic errors of the construction instructions (XSLT 2.0 sections 5.7 and 11): every one is NES3301 with the code of
/// the rule in the constraint, located at the instruction that found it.
/// </summary>
public class ConstructionErrorTests
{
    private const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    private static ConstructionHarness WithName(string fragment, string name)
    {
        var harness = new ConstructionHarness(fragment, variables: ["n"]);
        harness.Bind("n", XdmSequence.Create(XdmItem.FromString(name)));
        return harness;
    }

    [Test]
    [Arguments("")]
    [Arguments("1a")]
    [Arguments("a b")]
    [Arguments("a:b:c")]
    public async Task Element_WithAnInvalidName_IsXtde0820(string name) =>
        await Assert.That(WithName("<xsl:element name=\"{$n}\"/>", name).Tree()).IsEqualTo("problem:NES3301/XTDE0820");

    [Test]
    public async Task Element_WithAPrefixThatIsNotInScope_IsXtde0830() =>
        await Assert
            .That(new ConstructionHarness("<xsl:element name=\"zz:e\"/>").Tree())
            .IsEqualTo("problem:NES3301/XTDE0830");

    [Test]
    public async Task Element_WithTheXmlnsNamespace_IsXtde0835() =>
        await Assert
            .That(new ConstructionHarness($"<xsl:element name=\"e\" namespace=\"{XmlnsNamespace}\"/>").Tree())
            .IsEqualTo("problem:NES3301/XTDE0835");

    [Test]
    [Arguments("")]
    [Arguments("1a")]
    [Arguments("a b")]
    public async Task Attribute_WithAnInvalidName_IsXtde0850(string name) =>
        await Assert
            .That(WithName("<out><xsl:attribute name=\"{$n}\"/></out>", name).Tree())
            .IsEqualTo("problem:NES3301/XTDE0850");

    [Test]
    public async Task Attribute_NamedXmlnsWithoutANamespace_IsXtde0855() =>
        await Assert
            .That(new ConstructionHarness("<out><xsl:attribute name=\"xmlns\"/></out>").Tree())
            .IsEqualTo("problem:NES3301/XTDE0855");

    [Test]
    [Arguments("zz:a")]
    [Arguments("xmlns:foo")]
    public async Task Attribute_WithAPrefixThatIsNotInScope_IsXtde0860(string name) =>
        await Assert
            .That(WithName("<out><xsl:attribute name=\"{$n}\"/></out>", name).Tree())
            .IsEqualTo("problem:NES3301/XTDE0860");

    [Test]
    public async Task Attribute_WithTheXmlnsNamespace_IsXtde0865() =>
        await Assert
            .That(
                new ConstructionHarness($"<out><xsl:attribute name=\"a\" namespace=\"{XmlnsNamespace}\"/></out>").Tree()
            )
            .IsEqualTo("problem:NES3301/XTDE0865");

    [Test]
    [Arguments("")]
    [Arguments("xml")]
    [Arguments("XML")]
    [Arguments("a:b")]
    [Arguments("1a")]
    public async Task ProcessingInstruction_WithAnInvalidTarget_IsXtde0890(string name) =>
        await Assert
            .That(WithName("<out><xsl:processing-instruction name=\"{$n}\"/></out>", name).Tree())
            .IsEqualTo("problem:NES3301/XTDE0890");

    [Test]
    [Arguments("xmlns")]
    [Arguments("a:b")]
    [Arguments("1a")]
    public async Task Namespace_WithAnInvalidName_IsXtde0920(string name) =>
        await Assert
            .That(WithName("<out><xsl:namespace name=\"{$n}\" select=\"'urn:p'\"/></out>", name).Tree())
            .IsEqualTo("problem:NES3301/XTDE0920");

    [Test]
    [Arguments("p", "''", "XTDE0930")]
    [Arguments("p", "'http://www.w3.org/2000/xmlns/'", "XTDE0905")]
    [Arguments("xml", "'urn:x'", "XTDE0925")]
    [Arguments("p", "'http://www.w3.org/XML/1998/namespace'", "XTDE0925")]
    public async Task Namespace_WithAnInvalidValue_IsACodedError(string name, string select, string code) =>
        await Assert
            .That(new ConstructionHarness($"<out><xsl:namespace name=\"{name}\" select=\"{select}\"/></out>").Tree())
            .IsEqualTo("problem:NES3301/" + code);

    [Test]
    [Arguments("<out>t<xsl:attribute name=\"a\"/></out>")]
    [Arguments("<out><a/><xsl:attribute name=\"a\"/></out>")]
    [Arguments("<out><xsl:comment>c</xsl:comment><xsl:attribute name=\"a\"/></out>")]
    [Arguments("<out><xsl:processing-instruction name=\"p\"/><xsl:attribute name=\"a\"/></out>")]
    [Arguments("<out>t<xsl:namespace name=\"p\" select=\"'urn:p'\"/></out>")]
    [Arguments("<out><xsl:sequence select=\"1\"/><xsl:attribute name=\"a\"/></out>")]
    public async Task Attribute_OrNamespace_AfterAChildNode_IsXtde0410(string fragment) =>
        await Assert.That(new ConstructionHarness(fragment).Tree()).IsEqualTo("problem:NES3301/XTDE0410");

    [Test]
    public async Task Attribute_AfterAChildNode_ReportsTheCodedErrorAtTheInstruction()
    {
        var harness = new ConstructionHarness("<out>\n<xsl:text>t</xsl:text>\n<xsl:attribute name=\"a\"/>\n</out>");
        using var sink = ConstructionHarness.NewTree();

        var status = harness.Execute(sink);
        var problem = harness.Frame.TakeProblem();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problem.Constraint).IsEqualTo("XTDE0410");
        await Assert.That(problem.Location.Line).IsEqualTo(3);
        await Assert.That(problem.Message).Contains("after");
    }

    [Test]
    public async Task CopyOfAttributes_AfterAChildNode_IsXtde0410()
    {
        var source = ConstructionSheet.Source("<s k=\"1\"/>");
        var harness = new ConstructionHarness("<out>t<xsl:copy-of select=\"$a\"/></out>", variables: ["a"]);
        harness.Bind("a", XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Node.FirstAttribute))));

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3301/XTDE0410");
    }

    [Test]
    public async Task CopyOfAnAttribute_AfterAChildNode_IsXtde0410()
    {
        var source = ConstructionSheet.Source("<s k=\"1\"/>");
        var harness = new ConstructionHarness("<out>t<xsl:copy/></out>");
        harness.Focus(new XPathNode(source.Node.FirstAttribute));

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3301/XTDE0410");
    }

    [Test]
    public async Task EmptyText_BeforeAnAttribute_IsNoChildNode()
    {
        var harness = new ConstructionHarness("<out><xsl:text/><xsl:attribute name=\"a\"/></out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out a=''/>");
    }

    [Test]
    [Arguments("<xsl:document><xsl:attribute name=\"a\"/></xsl:document>")]
    [Arguments("<xsl:document><xsl:namespace name=\"p\" select=\"'urn:p'\"/></xsl:document>")]
    [Arguments("<xsl:attribute name=\"a\"/>")]
    [Arguments("<xsl:namespace name=\"p\" select=\"'urn:p'\"/>")]
    public async Task AttributeOrNamespace_InTheContentOfADocument_IsXtde0420(string fragment) =>
        await Assert.That(new ConstructionHarness(fragment).Tree()).IsEqualTo("problem:NES3301/XTDE0420");

    [Test]
    public async Task CopyOfAnAttribute_InTheContentOfADocument_IsXtde0420()
    {
        var source = ConstructionSheet.Source("<s k=\"1\"/>");
        var harness = new ConstructionHarness("<xsl:copy-of select=\"$a\"/>", variables: ["a"]);
        harness.Bind("a", XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Node.FirstAttribute))));

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3301/XTDE0420");
    }

    [Test]
    [Arguments(
        "<out><xsl:namespace name=\"p\" select=\"'urn:a'\"/><xsl:namespace name=\"p\" select=\"'urn:b'\"/></out>"
    )]
    [Arguments("<out xmlns:p=\"urn:a\"><xsl:namespace name=\"p\" select=\"'urn:b'\"/></out>")]
    public async Task Namespace_WithTheSameNameAndAnotherValue_IsXtde0430(string fragment) =>
        await Assert.That(new ConstructionHarness(fragment).Tree()).IsEqualTo("problem:NES3301/XTDE0430");

    [Test]
    public async Task Namespace_WithoutAName_OnAnElementInNoNamespace_IsXtde0440() =>
        await Assert
            .That(
                new ConstructionHarness(
                    "<xsl:element name=\"e\" namespace=\"\"><xsl:namespace name=\"\" select=\"'urn:d'\"/></xsl:element>"
                ).Tree()
            )
            .IsEqualTo("problem:NES3301/XTDE0440");

    [Test]
    public async Task AttributeValueTemplate_WhoseExpressionFails_ReportsTheErrorOfTheExpression() =>
        await Assert
            .That(new ConstructionHarness("<out a=\"{integer('x')}\"/>").Tree())
            .IsEqualTo("problem:NES3301/FORG0001");

    [Test]
    [Arguments("<out><xsl:attribute name=\"a\" select=\"integer('x')\"/></out>")]
    [Arguments("<out><xsl:comment select=\"integer('x')\"/></out>")]
    [Arguments("<out><xsl:value-of select=\"integer('x')\"/></out>")]
    [Arguments("<xsl:element name=\"{integer('x')}\"/>")]
    [Arguments("<xsl:element name=\"e\" namespace=\"{integer('x')}\"/>")]
    [Arguments("<xsl:copy-of select=\"integer('x')\"/>")]
    [Arguments("<xsl:document><xsl:value-of select=\"integer('x')\"/></xsl:document>")]
    public async Task AnExpressionThatFails_StopsTheConstruction_InTreeContent(string fragment) =>
        await Assert.That(new ConstructionHarness(fragment).Tree()).IsEqualTo("problem:NES3301/FORG0001");

    [Test]
    [Arguments("<out a=\"1\"><xsl:value-of select=\"integer('x')\"/></out>")]
    [Arguments("<xsl:document><a/><xsl:value-of select=\"integer('x')\"/></xsl:document>")]
    [Arguments("<xsl:comment select=\"integer('x')\"/>")]
    public async Task AnExpressionThatFails_StopsTheConstruction_InAnItemSequence(string fragment) =>
        await Assert.That(new ConstructionHarness(fragment).Items()).IsEqualTo("problem:NES3301/FORG0001");

    [Test]
    public async Task AttributeValueTemplate_LongerThanTheLimit_IsTheLimitStop()
    {
        var harness = new ConstructionHarness("<out a=\"{'abcdef'}\"/>");
        harness.Frame.MaxMessageLength = 3;

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3503");
    }

    [Test]
    public async Task AttributeValueTemplate_OfAValueWithoutATextForm_IsAnEvaluationFailure()
    {
        var harness = new ConstructionHarness("<out a=\"{$d}\"/>", variables: ["d"]);
        harness.Bind("d", XdmSequence.Create(XdmItem.FromDouble(1.5)));

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3301");
    }

    [Test]
    public async Task ContentValue_WithoutATextForm_IsAnEvaluationFailure()
    {
        var harness = new ConstructionHarness("<out><xsl:sequence select=\"$d\"/></out>", variables: ["d"]);
        harness.Bind("d", XdmSequence.Create(XdmItem.FromDouble(1.5)));

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3301");
    }

    [Test]
    public async Task SimpleContent_WithAValueWithoutATextForm_IsAnEvaluationFailure()
    {
        var harness = new ConstructionHarness("<out><xsl:comment select=\"$d\"/></out>", variables: ["d"]);
        harness.Bind("d", XdmSequence.Create(XdmItem.FromDouble(1.5)));

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3301");
    }

    [Test]
    public async Task Element_IntoAFrozenNameTable_IsAnEvaluationFailure()
    {
        var harness = new ConstructionHarness("<out/>");
        var table = SinkTestSupport.NewTable();
        table.Freeze();
        using var sink = new TreeContentSink(table);

        var status = harness.Execute(sink);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.ProblemText()).IsEqualTo("problem:NES3301");
    }

    [Test]
    public async Task Element_IntoAFullNameTable_IsTheLimitStop()
    {
        var harness = new ConstructionHarness("<out a=\"1\" b=\"2\" c=\"3\"/>");
        using var sink = new TreeContentSink(new SpanInterningNameTable(3));

        var status = harness.Execute(sink);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.ProblemText()).IsEqualTo("problem:NES3503");
    }

    [Test]
    public async Task Namespace_IntoAFullNameTable_IsTheLimitStop()
    {
        var harness = new ConstructionHarness("<out><xsl:namespace name=\"p\" select=\"'urn:p'\"/></out>");
        using var sink = new TreeContentSink(new SpanInterningNameTable(2));

        var status = harness.Execute(sink);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.ProblemText()).IsEqualTo("problem:NES3503");
    }

    [Test]
    public async Task ProcessingInstruction_IntoAFullNameTable_IsTheLimitStop()
    {
        var harness = new ConstructionHarness("<xsl:processing-instruction name=\"target\"/>");
        using var sink = new TreeContentSink(new SpanInterningNameTable(1));

        var status = harness.Execute(sink);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.ProblemText()).IsEqualTo("problem:NES3503");
    }
}
