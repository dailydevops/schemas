namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>xsl:copy</c> and <c>xsl:copy-of</c> (XSLT 2.0 section 11.9): the copy goes through the Xml pump, so it has a new identity, keeps
/// the document order, and carries the namespace nodes of the source.
/// </summary>
public class CopyTests
{
    private const string Source = "<s xmlns:q=\"urn:q\" k=\"1\" j=\"2\"><a/><b>t</b><!--c--><?p d?><c/></s>";

    private static XdmSequence One(XPathNode node) => XdmSequence.Create(XdmItem.FromNode(node));

    [Test]
    public async Task CopyOf_OfASourceSubtree_IsADeepCopyWithANewIdentityAndTheSameDocumentOrder()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<r><xsl:copy-of select=\"$src\"/></r>", variables: ["src"]);
        harness.Bind("src", One(source));
        using var sink = ConstructionHarness.NewTree();

        var status = harness.Execute(sink);
        var copy = PathQuery.Select(sink.Document, "/r/s");
        _ = copy[0].TryGetNode(out var copied);
        var children = PathQuery.Select(sink.Document, "/r/s/*");
        _ = children[0].TryGetNode(out var copiedA);
        _ = children[1].TryGetNode(out var copiedB);
        _ = children[2].TryGetNode(out var copiedC);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(PathQuery.Describe(copy[0])).IsEqualTo("Element:s=t");
        await Assert
            .That(NodeDump.Of(sink.Document))
            .IsEqualTo("<r><s xmlns:q='urn:q' k='1' j='2'><a/><b>t</b><!--c--><?p d?><c/></s></r>");
        await Assert.That(copied.IsSameNode(in source)).IsFalse();
        await Assert.That(copied.Tree).IsNotSameReferenceAs(source.Tree);
        await Assert.That(children.Count).IsEqualTo(3);
        await Assert.That(copiedA.CompareDocumentOrder(in copiedB)).IsLessThan(0);
        await Assert.That(copiedB.CompareDocumentOrder(in copiedC)).IsLessThan(0);
        await Assert.That(copiedC.CompareDocumentOrder(in copiedA)).IsGreaterThan(0);
    }

    [Test]
    public async Task CopyOf_IntoTheContentOfADocument_CopiesTheSubtree()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<xsl:copy-of select=\"$src\"/>", variables: ["src"]);
        harness.Bind("src", One(source));

        await Assert
            .That(harness.Tree())
            .IsEqualTo("<s xmlns:q='urn:q' k='1' j='2'><a/><b>t</b><!--c--><?p d?><c/></s>");
    }

    [Test]
    public async Task CopyOf_OfADocumentNode_CopiesItsChildren()
    {
        var document = ConstructionSheet.Parse("<d><x/></d>").Document;
        var harness = new ConstructionHarness("<r><xsl:copy-of select=\"$d\"/></r>", variables: ["d"]);
        harness.Bind("d", One(new XPathNode(document)));

        await Assert.That(harness.Tree()).IsEqualTo("<r><d><x/></d></r>");
    }

    [Test]
    public async Task CopyOf_OfAttributeNodes_AddsThemToTheElement()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<r><xsl:copy-of select=\"$a\"/></r>", variables: ["a"]);
        var first = source.Node.FirstAttribute;
        harness.Bind(
            "a",
            XdmSequence.Create([
                XdmItem.FromNode(new XPathNode(first)),
                XdmItem.FromNode(new XPathNode(first.NextAttribute)),
            ])
        );

        await Assert.That(harness.Tree()).IsEqualTo("<r k='1' j='2'/>");
    }

    [Test]
    public async Task CopyOf_OfNamespaceNodes_AddsThemToTheElement()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<r><xsl:copy-of select=\"$n\"/></r>", variables: ["n"]);
        harness.Bind("n", One(new XPathNode(source.Node.FirstNamespace)));

        await Assert.That(harness.Tree()).IsEqualTo("<r xmlns:q='urn:q'/>");
    }

    [Test]
    public async Task CopyOf_OfAtomicValues_AreSpaceSeparatedLikeSequence()
    {
        var harness = new ConstructionHarness("<r><xsl:copy-of select=\"(1,2)\"/></r>");

        await Assert.That(harness.Tree()).IsEqualTo("<r>1 2</r>");
    }

    [Test]
    public async Task CopyOf_IntoAnItemSequence_ReturnsCopiesWithANewIdentity()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<xsl:copy-of select=\"($src, 1)\"/>", variables: ["src"]);
        harness.Bind("src", One(source));
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        var status = harness.Execute(sink);
        var copy = sink.Result[0];
        _ = copy.TryGetNode(out var copied);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(sink.Result.Count).IsEqualTo(2);
        await Assert
            .That(NodeDump.Of(in copy))
            .IsEqualTo("<s xmlns:q='urn:q' k='1' j='2'><a/><b>t</b><!--c--><?p d?><c/></s>");
        await Assert.That(copied.IsSameNode(in source)).IsFalse();
        await Assert.That(sink.Result[1].Kind).IsEqualTo(XdmItemKind.XsInteger);
    }

    [Test]
    public async Task CopyOf_OfADocumentNodeIntoAnItemSequence_ReturnsANewDocumentNode()
    {
        var document = ConstructionSheet.Parse("<d><x/></d>").Document;
        var harness = new ConstructionHarness("<xsl:copy-of select=\"$d\"/>", variables: ["d"]);
        harness.Bind("d", One(new XPathNode(document)));
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        _ = harness.Execute(sink);
        _ = sink.Result[0].TryGetNode(out var copied);

        await Assert.That(copied.Node.Kind).IsEqualTo(XmlNodeKind.Document);
        await Assert.That(NodeDump.Of(copied.Node)).IsEqualTo("<d><x/></d>");
        await Assert.That(copied.Node == document).IsFalse();
    }

    [Test]
    public async Task CopyOf_OfLeafNodesIntoAnItemSequence_ReturnsNewLeafNodes()
    {
        var source = ConstructionSheet.Source(Source);
        var comment = source.Node.FirstChild.NextSibling.NextSibling;
        var instruction = source.Node.FirstChild.NextSibling.NextSibling.NextSibling;
        var text = source.Node.FirstChild.NextSibling.FirstChild;
        var harness = new ConstructionHarness("<xsl:copy-of select=\"$n\"/>", variables: ["n"]);
        harness.Bind(
            "n",
            XdmSequence.Create([
                XdmItem.FromNode(new XPathNode(comment)),
                XdmItem.FromNode(new XPathNode(instruction)),
                XdmItem.FromNode(new XPathNode(text)),
            ])
        );

        await Assert.That(harness.Items()).IsEqualTo("<!--c-->|<?p d?>|t");
    }

    [Test]
    public async Task Copy_OfAnElement_IsShallowWithTheNamespaceNodesAndTheConstructedContent()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<xsl:copy><xsl:attribute name=\"n\">3</xsl:attribute><x/></xsl:copy>");
        harness.Focus(source);

        await Assert.That(harness.Tree()).IsEqualTo("<s xmlns:q='urn:q' n='3'><x/></s>");
    }

    [Test]
    public async Task Copy_OfAnElement_BuildsAnElementWithNameAndStringValue()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<xsl:copy>v</xsl:copy>");
        harness.Focus(source);
        using var sink = ConstructionHarness.NewTree();

        _ = harness.Execute(sink);
        var found = PathQuery.Select(sink.Document, "/s");

        await Assert.That(PathQuery.Describe(found[0])).IsEqualTo("Element:s=v");
    }

    [Test]
    public async Task Copy_OfADocumentNode_BuildsADocumentWithTheConstructedContent()
    {
        var document = ConstructionSheet.Parse("<d/>").Document;
        var harness = new ConstructionHarness("<xsl:copy><x/></xsl:copy>");
        harness.Focus(new XPathNode(document));

        await Assert.That(harness.Tree()).IsEqualTo("<x/>");
        await Assert.That(harness.Items()).IsEqualTo("<x/>");
    }

    [Test]
    public async Task Copy_OfADocumentNodeIntoAnItemSequence_IsADocumentNode()
    {
        var document = ConstructionSheet.Parse("<d/>").Document;
        var harness = new ConstructionHarness("<xsl:copy><x/></xsl:copy>");
        harness.Focus(new XPathNode(document));
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        _ = harness.Execute(sink);
        _ = sink.Result[0].TryGetNode(out var copied);

        await Assert.That(copied.Node.Kind).IsEqualTo(XmlNodeKind.Document);
    }

    [Test]
    public async Task Copy_OfAnAttributeNode_AddsANewAttribute()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<r><xsl:copy>ignored</xsl:copy></r>");
        harness.Focus(new XPathNode(source.Node.FirstAttribute));

        await Assert.That(harness.Tree()).IsEqualTo("<r k='1'/>");
    }

    [Test]
    public async Task Copy_OfANamespaceNode_AddsANewNamespace()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<r><xsl:copy/></r>");
        harness.Focus(new XPathNode(source.Node.FirstNamespace));

        await Assert.That(harness.Tree()).IsEqualTo("<r xmlns:q='urn:q'/>");
    }

    [Test]
    public async Task Copy_OfATextCommentAndProcessingInstructionNode_AddsANewNode()
    {
        var source = ConstructionSheet.Source(Source);
        var b = source.Node.FirstChild.NextSibling;
        var harness = new ConstructionHarness("<r><xsl:copy/></r>");

        harness.Focus(new XPathNode(b.FirstChild));
        var text = harness.Tree();
        harness.Focus(new XPathNode(b.NextSibling));
        var comment = harness.Tree();
        harness.Focus(new XPathNode(b.NextSibling.NextSibling));
        var instruction = harness.Tree();

        await Assert.That(text).IsEqualTo("<r>t</r>");
        await Assert.That(comment).IsEqualTo("<r><!--c--></r>");
        await Assert.That(instruction).IsEqualTo("<r><?p d?></r>");
    }

    [Test]
    public async Task Copy_OfAnAtomicValue_ReturnsTheValueAndDoesNotRunTheContent()
    {
        var harness = new ConstructionHarness("<r><xsl:copy><xsl:value-of select=\"integer('x')\"/></xsl:copy></r>");
        harness.Focus(XdmItem.FromInteger(5));

        await Assert.That(harness.Tree()).IsEqualTo("<r>5</r>");
    }

    [Test]
    public async Task Copy_WithoutAContextItem_IsTheDynamicErrorXpdy0002()
    {
        var harness = new ConstructionHarness("<xsl:copy/>");

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3301/XPDY0002");
    }

    [Test]
    public async Task Copy_OfAnElementWithTheCopyNamespacesYes_IsAccepted()
    {
        var source = ConstructionSheet.Source(Source);
        var harness = new ConstructionHarness("<xsl:copy copy-namespaces=\"yes\" inherit-namespaces=\"yes\"/>");
        harness.Focus(source);

        await Assert.That(harness.Tree()).IsEqualTo("<s xmlns:q='urn:q'/>");
    }
}
