namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>xsl:comment</c>, <c>xsl:processing-instruction</c>, <c>xsl:namespace</c>, and <c>xsl:document</c> (XSLT 2.0 sections 11.4 to
/// 11.7 and 11.2): the node kind, the name, and the string value of the node in the constructed tree.
/// </summary>
public class SimpleNodeTests
{
    [Test]
    public async Task Comment_BuildsACommentNodeWithTheStringValue()
    {
        var harness = new ConstructionHarness("<out><xsl:comment>x<xsl:text>y</xsl:text></xsl:comment></out>");
        using var sink = ConstructionHarness.NewTree();

        var status = harness.Execute(sink);
        var found = PathQuery.Select(sink.Document, "/out/comment()");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(found.Count).IsEqualTo(1);
        await Assert.That(PathQuery.Describe(found[0])).IsEqualTo("Comment:=xy");
    }

    [Test]
    public async Task Comment_WithASelect_JoinsTheItemsWithASpace()
    {
        var harness = new ConstructionHarness("<out><xsl:comment select=\"(1,2)\"/></out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out><!--1 2--></out>");
    }

    [Test]
    public async Task Comment_InAnItemSequence_IsACommentNode()
    {
        var harness = new ConstructionHarness("<xsl:comment>c</xsl:comment>");

        await Assert.That(harness.Items()).IsEqualTo("<!--c-->");
    }

    [Test]
    public async Task ProcessingInstruction_BuildsANodeWithTargetAndStringValue_WithoutTheLeadingWhiteSpace()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:processing-instruction name=\"t{$n}\">  d <xsl:text>e</xsl:text></xsl:processing-instruction></out>",
            variables: ["n"]
        );
        harness.Bind("n", XdmSequence.Create(XdmItem.FromInteger(1)));
        using var sink = ConstructionHarness.NewTree();

        var status = harness.Execute(sink);
        var found = PathQuery.Select(sink.Document, "/out/processing-instruction()");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(PathQuery.Describe(found[0])).IsEqualTo("ProcessingInstruction:t1=d e");
    }

    [Test]
    public async Task ProcessingInstruction_WithASelect_JoinsTheItemsWithASpace()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:processing-instruction name=\"t\" select=\"' ', 1, 2\"/></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out><?t 1 2?></out>");
    }

    [Test]
    public async Task ProcessingInstruction_InAnItemSequence_IsAProcessingInstructionNode()
    {
        var harness = new ConstructionHarness("<xsl:processing-instruction name=\"t\">d</xsl:processing-instruction>");

        await Assert.That(harness.Items()).IsEqualTo("<?t d?>");
    }

    [Test]
    public async Task Namespace_BuildsANamespaceNodeWithNameAndStringValue()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:namespace name=\"p\" select=\"'urn:p'\"/><xsl:namespace name=\"q\">urn:<xsl:text>q</xsl:text></xsl:namespace></out>"
        );
        using var sink = ConstructionHarness.NewTree();

        var status = harness.Execute(sink);
        var element = sink.Document.TryGetNode(out var document) ? document.Node.FirstChild : default;
        var first = element.FirstNamespace;
        var second = first.NextNamespace;

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(first.Kind).IsEqualTo(XmlNodeKind.Namespace);
        await Assert.That(PathQuery.Describe(XdmItem.FromNode(new(first)))).IsEqualTo("Namespace:p=urn:p");
        await Assert.That(PathQuery.Describe(XdmItem.FromNode(new(second)))).IsEqualTo("Namespace:q=urn:q");
        await Assert.That(NodeDump.Of(document.Node)).IsEqualTo("<out xmlns:p='urn:p' xmlns:q='urn:q'/>");
    }

    [Test]
    public async Task Namespace_WithAnEmptyName_IsTheDefaultNamespaceOfTheElement()
    {
        var harness = new ConstructionHarness(
            "<xsl:element name=\"e\" namespace=\"urn:d\"><xsl:namespace name=\"\" select=\"'urn:d'\"/></xsl:element>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<e xmlns='urn:d'/>");
    }

    [Test]
    public async Task Namespace_DeclaredTwiceWithTheSameValue_IsOneNamespaceNode()
    {
        var harness = new ConstructionHarness(
            "<out xmlns:p=\"urn:p\"><xsl:namespace name=\"p\" select=\"'urn:p'\"/><xsl:namespace name=\"p\" select=\"'urn:p'\"/></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:p='urn:p'/>");
    }

    [Test]
    public async Task Namespace_XmlPrefixWithTheXmlNamespace_IsAcceptedAndAddsNothing()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:namespace name=\"xml\" select=\"'http://www.w3.org/XML/1998/namespace'\"/></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out/>");
    }

    [Test]
    public async Task Document_InAnItemSequence_IsADocumentNodeWithTheConstructedChildren()
    {
        var harness = new ConstructionHarness("<xsl:document><a/>t<b/></xsl:document>");
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        var status = harness.Execute(sink);
        _ = sink.Result[0].TryGetNode(out var document);
        var found = PathQuery.Select(sink.Result[0], "/a");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(sink.Result.Count).IsEqualTo(1);
        await Assert.That(document.Node.Kind).IsEqualTo(XmlNodeKind.Document);
        await Assert.That(NodeDump.Of(document.Node)).IsEqualTo("<a/>t<b/>");
        await Assert.That(PathQuery.Describe(found[0])).IsEqualTo("Element:a=");
        await Assert.That(document.Node.StringValue).IsEqualTo("t");
    }

    [Test]
    public async Task Document_InTheContentOfAnElement_ContributesItsChildren()
    {
        var harness = new ConstructionHarness("<out><xsl:document><a/>t</xsl:document></out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out><a/>t</out>");
    }

    [Test]
    public async Task Document_InTheContentOfADocument_ContributesItsChildren()
    {
        var harness = new ConstructionHarness("<xsl:document>t<xsl:document><a/></xsl:document></xsl:document>");

        await Assert.That(harness.Tree()).IsEqualTo("t<a/>");
    }

    [Test]
    public async Task Document_WithAtomicValues_SeparatesAdjacentValuesBySpace()
    {
        var harness = new ConstructionHarness("<xsl:document><xsl:sequence select=\"(1,2)\"/></xsl:document>");

        await Assert.That(harness.Tree()).IsEqualTo("1 2");
    }
}
