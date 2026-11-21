namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;
using ConstructionRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction.Registration;

/// <summary>
/// Literal result elements (XSLT 2.0 section 11.1) and the character data of a sequence constructor: the element, its attribute
/// value templates, its namespace nodes with the excluded namespaces, and the text node entry.
/// </summary>
public class LiteralResultTests
{
    [Test]
    public async Task LiteralResultElement_BuildsAnElementWithNameAndStringValue()
    {
        var harness = new ConstructionHarness("<out a=\"1\">text</out>");
        using var sink = ConstructionHarness.NewTree();

        var status = harness.Execute(sink);
        var element = PathQuery.Select(sink.Document, "/out");
        var attribute = PathQuery.Select(sink.Document, "/out/@a");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(element.Count).IsEqualTo(1);
        await Assert.That(PathQuery.Describe(element[0])).IsEqualTo("Element:out=text");
        await Assert.That(PathQuery.Describe(attribute[0])).IsEqualTo("Attribute:a=1");
    }

    [Test]
    public async Task LiteralResultElement_NestedContent_KeepsTheOrder()
    {
        var harness = new ConstructionHarness("<out><a/>t<b x=\"1\"/><!--c--></out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out><a/>t<b x='1'/></out>");
    }

    [Test]
    public async Task LiteralResultElement_AttributeValueTemplate_EvaluatesTheExpressionsAndTheEscapes()
    {
        var harness = new ConstructionHarness(
            "<out a=\"p{$x}q{$y}r\" b=\"{{x}}\" c=\"{'}'}\" d=\"{(1,2)}\" e=\"{()}\"/>",
            variables: ["x", "y"]
        );
        harness.Bind("x", XdmSequence.Create(XdmItem.FromString("1")));
        harness.Bind("y", XdmSequence.Create(XdmItem.FromInteger(2)));

        await Assert.That(harness.Tree()).IsEqualTo("<out a='p1q2r' b='{x}' c='}' d='1 2' e=''/>");
    }

    [Test]
    public async Task LiteralResultElement_NamespacePrefixesAndDefaultNamespace_AreKept()
    {
        var harness = new ConstructionHarness("<p:out xmlns:p=\"urn:p\" p:a=\"1\" b=\"2\"><p:in/></p:out>");

        await Assert.That(harness.Tree()).IsEqualTo("<p:out xmlns:p='urn:p' p:a='1' b='2'><p:in/></p:out>");
    }

    [Test]
    public async Task LiteralResultElement_DefaultNamespace_IsNotDeclaredAgainOnTheChild()
    {
        var harness = new ConstructionHarness("<out xmlns=\"urn:d\"><in/></out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns='urn:d'><in/></out>");
    }

    [Test]
    public async Task LiteralResultElement_InNoNamespaceUnderADefaultNamespace_UndeclaresIt()
    {
        var harness = new ConstructionHarness("<out xmlns=\"urn:d\"><xsl:element name=\"e\" namespace=\"\"/></out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns='urn:d'><e xmlns=''/></out>");
    }

    [Test]
    public async Task LiteralResultElement_CopiesTheInScopeNamespaces_ExceptTheXsltNamespace()
    {
        var harness = new ConstructionHarness("<out/>", rootAttributes: string.Empty);

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:f='urn:f' xmlns:ex='urn:ex'/>");
    }

    [Test]
    public async Task LiteralResultElement_ExcludeResultPrefixes_OnTheElement_LeavesTheNamespaceOut()
    {
        var harness = new ConstructionHarness("<out xsl:exclude-result-prefixes=\"f\"/>", rootAttributes: string.Empty);

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:ex='urn:ex'/>");
    }

    [Test]
    public async Task LiteralResultElement_ExcludeResultPrefixes_OnTheStylesheetRoot_AppliesToTheDescendants()
    {
        var harness = new ConstructionHarness("<out><in/></out>", rootAttributes: "exclude-result-prefixes=\"ex\"");

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:f='urn:f'><in/></out>");
    }

    [Test]
    public async Task LiteralResultElement_ExcludeResultPrefixes_Default_ExcludesTheDefaultNamespace()
    {
        var harness = new ConstructionHarness(
            "<out xmlns=\"urn:d\" xmlns:q=\"urn:q\" xsl:exclude-result-prefixes=\"#default\"><in xmlns=\"urn:e\"/></out>",
            rootAttributes: "exclude-result-prefixes=\"#all\""
        );

        // The element is in urn:d, so the namespace fixup declares it although the copy of the declaration is excluded.
        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:q='urn:q' xmlns='urn:d'><in xmlns='urn:e'/></out>");
    }

    [Test]
    public async Task LiteralResultElement_AttributeInAnExcludedNamespace_GetsItsNamespaceFromTheFixup()
    {
        var harness = new ConstructionHarness("<out ex:a=\"1\"/>");

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:ex='urn:ex' ex:a='1'/>");
    }

    [Test]
    public async Task LiteralResultElement_XmlAttribute_NeedsNoDeclaration()
    {
        var harness = new ConstructionHarness("<out xml:lang=\"en\"/>");

        await Assert.That(harness.Tree()).IsEqualTo("<out xml:lang='en'/>");
    }

    [Test]
    public async Task LiteralResultElement_VersionAndInheritNamespacesYes_AreAcceptedAndNotCopied()
    {
        var harness = new ConstructionHarness("<out xsl:version=\"2.0\" xsl:inherit-namespaces=\"yes\" a=\"1\"/>");

        await Assert.That(harness.Tree()).IsEqualTo("<out a='1'/>");
    }

    [Test]
    public async Task LiteralResultElement_BodyAttribute_WinsOverTheLiteralAttributeOfTheSameName()
    {
        var harness = new ConstructionHarness("<out a=\"1\"><xsl:attribute name=\"a\">2</xsl:attribute></out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out a='2'/>");
    }

    [Test]
    public async Task LiteralResultElement_InAnItemSequence_IsAnElementWithADocumentParent()
    {
        var harness = new ConstructionHarness("<out a=\"1\"/>");
        var sink = new Schematron.Xslt.Construction.ItemSequenceSink(SinkTestSupport.NewTable());

        var status = harness.Execute(sink);
        var item = sink.Result[0];
        _ = item.TryGetNode(out var node);

        // Limitation: the Xml tree builder starts every temporary tree with a document node, so the element has a parent.
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(sink.Result.Count).IsEqualTo(1);
        await Assert.That(NodeDump.Of(in item)).IsEqualTo("<out a='1'/>");
        await Assert.That(node.Node.Kind).IsEqualTo(XmlNodeKind.Element);
        await Assert.That(node.Node.Parent.Kind).IsEqualTo(XmlNodeKind.Document);
    }

    [Test]
    public async Task LiteralResultElement_ThroughTheSyntheticEntry_IsRegisteredOnce()
    {
        var builder = new Schematron.Xslt.Registry.XsltRegistryBuilder();
        ConstructionRegistration.Register(builder);
        var registry = builder.Build();

        var found = registry.TryGetSynthetic(XsltSyntheticElement.LiteralResultElement, out var literal);

        await Assert.That(found).IsTrue();
        await Assert.That(literal.Position).IsEqualTo(XsltPosition.Instruction);
        await Assert.That(literal.Name).IsEqualTo(default(XPathQName));
    }

    [Test]
    public async Task TextNode_ThroughTheSyntheticEntry_IsRegisteredAndPushesItsText()
    {
        var builder = new Schematron.Xslt.Registry.XsltRegistryBuilder();
        ConstructionRegistration.Register(builder);
        var registry = builder.Build();

        var found = registry.TryGetSynthetic(XsltSyntheticElement.TextNode, out var text);

        await Assert.That(found).IsTrue();
        await Assert.That(text.Position).IsEqualTo(XsltPosition.Instruction);
    }

    [Test]
    public async Task TextNode_InASequenceConstructor_BecomesATextNode_AndWhitespaceOnlyTextIsStripped()
    {
        var harness = new ConstructionHarness("<out>\n  a<xsl:text> </xsl:text>b\n  <x/>\n</out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out>\n  a b\n  <x/></out>");
    }

    [Test]
    public async Task TextNode_InAnXslIf_IsPushedIntoTheSink()
    {
        var harness = new ConstructionHarness("<xsl:if test=\"true()\">yes</xsl:if>");

        await Assert.That(harness.Tree()).IsEqualTo("yes");
    }

    [Test]
    public async Task LiteralResultElement_WithAnAtomicValueInTheContent_SeparatesAdjacentValuesBySpace()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:sequence select=\"(1,2)\"/>t<xsl:sequence select=\"3\"/><xsl:sequence select=\"4\"/></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out>1 2t3 4</out>");
    }
}
