namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The construction instructions in every kind of sink (design #150 EP-8): a tree sink builds in place, an item sink builds a
/// temporary tree and returns its node, and a text sink, which has no tree, gets a coded problem and never an exception.
/// </summary>
public class SinkKindTests
{
    private static async Task AssertUnsupportedInTextAsync(string fragment)
    {
        var harness = new ConstructionHarness(fragment);
        using var text = new TextSink(100);

        var status = harness.Execute(text);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.ProblemText()).IsEqualTo("problem:NES3204");
    }

    [Test]
    [Arguments("<out/>")]
    [Arguments("<xsl:element name=\"e\"/>")]
    [Arguments("<xsl:comment>c</xsl:comment>")]
    [Arguments("<xsl:processing-instruction name=\"p\"/>")]
    [Arguments("<xsl:document><a/></xsl:document>")]
    [Arguments("<xsl:attribute name=\"a\"/>")]
    [Arguments("<xsl:namespace name=\"p\" select=\"'urn:p'\"/>")]
    public async Task Construction_IntoATextSink_IsTheUnsupportedConstructError(string fragment) =>
        await AssertUnsupportedInTextAsync(fragment);

    [Test]
    [Arguments("<out/>")]
    [Arguments("<xsl:comment>c</xsl:comment>")]
    [Arguments("<xsl:document><a/></xsl:document>")]
    [Arguments("<xsl:copy-of select=\"$n\"/>")]
    public async Task Construction_IntoASinkWhoseBeginTreeFails_IsTheUnsupportedConstructError_NotAnException(
        string fragment
    )
    {
        var source = ConstructionSheet.Source("<s/>");
        var harness = new ConstructionHarness(fragment, variables: ["n"]);
        harness.Bind("n", XdmSequence.Create(XdmItem.FromNode(source)));
        var sink = new RecordingSink();

        var status = harness.Execute(sink);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.ProblemText()).IsEqualTo("problem:NES3204");
    }

    [Test]
    public async Task CopyOf_IntoATextSink_AppendsTheStringValueOfTheNodes()
    {
        var source = ConstructionSheet.Source("<s>t<u>v</u></s>");
        var harness = new ConstructionHarness("<xsl:copy-of select=\"($n, 1)\"/>", variables: ["n"]);
        harness.Bind("n", XdmSequence.Create(XdmItem.FromNode(source)));
        using var text = new TextSink(100);

        var status = harness.Execute(text);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(text.ToString()).IsEqualTo("tv1");
    }

    [Test]
    public async Task Construction_InTheContentOfAMessage_IsTheUnsupportedConstructError()
    {
        var harness = new ConstructionHarness("<xsl:message><out/></xsl:message>");

        var status = harness.Execute(new RecordingSink());

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.ProblemText()).IsEqualTo("problem:NES3204");
    }

    [Test]
    public async Task Construction_InTheContentOfAValueOfInAMessage_IsTheUnsupportedConstructError()
    {
        var harness = new ConstructionHarness("<xsl:message><xsl:value-of><out>t</out></xsl:value-of></xsl:message>");

        var status = harness.Execute(new RecordingSink());

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.ProblemText()).IsEqualTo("problem:NES3204");
    }

    [Test]
    public async Task Construction_InTheContentOfAValueOf_IsTheStringValueOfTheNode()
    {
        var harness = new ConstructionHarness("<xsl:value-of><out>t<in>u</in></out></xsl:value-of>");

        await Assert.That(harness.Tree()).IsEqualTo("tu");
    }

    [Test]
    public async Task Construction_InAnItemSequence_BuildsNestedElements()
    {
        var harness = new ConstructionHarness("<out><in><xsl:comment>c</xsl:comment></in></out>");

        await Assert.That(harness.Items()).IsEqualTo("<out><in><!--c--></in></out>");
    }

    [Test]
    [Arguments("<xsl:attribute name=\"a\">v</xsl:attribute>")]
    [Arguments("<xsl:namespace name=\"p\" select=\"'urn:p'\"/>")]
    [Arguments("<xsl:copy-of select=\"$n\"/>")]
    public async Task AttributeOrNamespace_InAnItemSequence_IsTheUnsupportedConstructError(string fragment)
    {
        var source = ConstructionSheet.Source("<s k=\"1\" xmlns:q=\"urn:q\"/>");
        var harness = new ConstructionHarness(fragment, variables: ["n"]);
        harness.Bind(
            "n",
            XdmSequence.Create([
                XdmItem.FromNode(new XPathNode(source.Node.FirstAttribute)),
                XdmItem.FromNode(new XPathNode(source.Node.FirstNamespace)),
            ])
        );

        await Assert.That(harness.Items()).IsEqualTo("problem:NES3204");
    }

    [Test]
    public async Task Element_InAnItemSequenceInsideAnElement_KeepsTheNamespacesOfTheParent()
    {
        var harness = new ConstructionHarness("<p:out xmlns:p=\"urn:p\"><p:in p:a=\"1\"/></p:out>");

        await Assert.That(harness.Items()).IsEqualTo("<p:out xmlns:p='urn:p'><p:in p:a='1'/></p:out>");
    }
}
