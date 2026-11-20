namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>xsl:element</c> and <c>xsl:attribute</c> (XSLT 2.0 sections 11.2 and 11.3): computed names and namespaces, the name resolved
/// through the namespaces in scope of the instruction, the separator of the attribute, and the namespace fixup of the constructed
/// element (section 5.7.3): a missing declaration is added, a prefix that conflicts is replaced by a generated one.
/// </summary>
public class ElementAttributeTests
{
    [Test]
    public async Task Element_WithAComputedName_BuildsAnElementWithNameAndStringValue()
    {
        var harness = new ConstructionHarness("<xsl:element name=\"e{$n}\">v</xsl:element>", variables: ["n"]);
        harness.Bind("n", XdmSequence.Create(XdmItem.FromInteger(1)));
        using var sink = ConstructionHarness.NewTree();

        var status = harness.Execute(sink);
        var found = PathQuery.Select(sink.Document, "/e1");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(found.Count).IsEqualTo(1);
        await Assert.That(PathQuery.Describe(found[0])).IsEqualTo("Element:e1=v");
    }

    [Test]
    public async Task Element_WithTheNamespaceAttributeAndNoPrefix_UsesTheDefaultNamespace()
    {
        var harness = new ConstructionHarness("<xsl:element name=\"e\" namespace=\"urn:x\"/>");

        await Assert.That(harness.Tree()).IsEqualTo("<e xmlns='urn:x'/>");
    }

    [Test]
    public async Task Element_WithAPrefixAndTheNamespaceAttribute_DeclaresThePrefix()
    {
        var harness = new ConstructionHarness("<xsl:element name=\"p:e\" namespace=\"{'urn:x'}\"/>");

        await Assert.That(harness.Tree()).IsEqualTo("<p:e xmlns:p='urn:x'/>");
    }

    [Test]
    public async Task Element_WithAnEmptyNamespaceAttribute_IsInNoNamespaceAndDropsThePrefix()
    {
        var harness = new ConstructionHarness("<xsl:element name=\"p:e\" namespace=\"\"/>");

        await Assert.That(harness.Tree()).IsEqualTo("<e/>");
    }

    [Test]
    public async Task Element_WithAPrefixInScopeOfTheInstruction_ResolvesItThere()
    {
        var harness = new ConstructionHarness("<xsl:element name=\"f:e\"/>");

        await Assert.That(harness.Tree()).IsEqualTo("<f:e xmlns:f='urn:f'/>");
    }

    [Test]
    public async Task Element_WithoutAPrefix_UsesTheDefaultNamespaceInScopeOfTheInstruction()
    {
        var harness = new ConstructionHarness("<xsl:element name=\"e\" xmlns=\"urn:d\"/>");

        await Assert.That(harness.Tree()).IsEqualTo("<e xmlns='urn:d'/>");
    }

    [Test]
    public async Task Element_WithoutAPrefixAndWithoutADefaultNamespace_IsInNoNamespace()
    {
        var harness = new ConstructionHarness("<xsl:element name=\"e\"/>");

        await Assert.That(harness.Tree()).IsEqualTo("<e/>");
    }

    [Test]
    public async Task Fixup_PrefixConflictWithANamespaceNode_IsResolvedByAGeneratedPrefix()
    {
        var harness = new ConstructionHarness(
            "<xsl:element name=\"p:e\" namespace=\"urn:one\"><xsl:namespace name=\"p\" select=\"'urn:two'\"/></xsl:element>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<ns1:e xmlns:p='urn:two' xmlns:ns1='urn:one'/>");
    }

    [Test]
    public async Task Fixup_AttributeWithAPrefixOfAnotherNamespace_GetsAGeneratedPrefix()
    {
        var harness = new ConstructionHarness(
            "<xsl:element name=\"p:e\" namespace=\"urn:a\"><xsl:attribute name=\"p:x\" namespace=\"urn:b\">v</xsl:attribute></xsl:element>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<p:e xmlns:p='urn:a' xmlns:ns1='urn:b' ns1:x='v'/>");
    }

    [Test]
    public async Task Fixup_AttributeInANamespaceWithoutAPrefix_GetsAGeneratedPrefix()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:attribute name=\"x\" namespace=\"urn:b\">v</xsl:attribute></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:ns1='urn:b' ns1:x='v'/>");
    }

    [Test]
    public async Task Fixup_AttributeInANamespaceThatHasAPrefix_UsesThatPrefix()
    {
        var harness = new ConstructionHarness(
            "<out xmlns:q=\"urn:q\"><xsl:attribute name=\"x\" namespace=\"urn:q\">v</xsl:attribute></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:q='urn:q' q:x='v'/>");
    }

    [Test]
    public async Task Fixup_GeneratedPrefix_SkipsThePrefixesInScope()
    {
        var harness = new ConstructionHarness(
            "<out xmlns:ns1=\"urn:z\"><xsl:attribute name=\"x\" namespace=\"urn:b\">v</xsl:attribute></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:ns1='urn:z' xmlns:ns2='urn:b' ns2:x='v'/>");
    }

    [Test]
    public async Task Fixup_AttributeNamedWithTheReservedPrefixXmlns_GetsAGeneratedPrefix()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:attribute name=\"xmlns:xsl\" namespace=\"urn:n\">v</xsl:attribute></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:ns1='urn:n' ns1:xsl='v'/>");
    }

    [Test]
    public async Task Fixup_ElementPrefixBoundByTheParentToAnotherNamespace_IsRedeclaredOnTheElement()
    {
        var harness = new ConstructionHarness(
            "<p:out xmlns:p=\"urn:a\"><xsl:element name=\"p:e\" namespace=\"urn:b\"/></p:out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<p:out xmlns:p='urn:a'><p:e xmlns:p='urn:b'/></p:out>");
    }

    [Test]
    public async Task Fixup_AttributePrefixBoundByTheParentToAnotherNamespace_GetsAGeneratedPrefix()
    {
        var harness = new ConstructionHarness(
            "<p:out xmlns:p=\"urn:a\"><e><xsl:attribute name=\"p:x\" namespace=\"urn:b\">v</xsl:attribute></e></p:out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<p:out xmlns:p='urn:a'><e xmlns:ns1='urn:b' ns1:x='v'/></p:out>");
    }

    [Test]
    public async Task Attribute_BuildsAnAttributeWithNameAndStringValue()
    {
        var harness = new ConstructionHarness("<out><xsl:attribute name=\"a\">v</xsl:attribute></out>");
        using var sink = ConstructionHarness.NewTree();

        var status = harness.Execute(sink);
        var found = PathQuery.Select(sink.Document, "/out/@a");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(PathQuery.Describe(found[0])).IsEqualTo("Attribute:a=v");
    }

    [Test]
    [Arguments(null, "1 2 3")]
    [Arguments("-", "1-2-3")]
    [Arguments("", "123")]
    [Arguments("{'+'}", "1+2+3")]
    public async Task Attribute_WithASelect_JoinsTheItemsWithTheSeparator(string? separator, string expected)
    {
        var attribute = separator is null ? string.Empty : $" separator=\"{separator}\"";
        var harness = new ConstructionHarness($"<out><xsl:attribute name=\"a\" select=\"(1,2,3)\"{attribute}/></out>");

        await Assert.That(harness.Tree()).IsEqualTo($"<out a='{expected}'/>");
    }

    [Test]
    public async Task Attribute_WithContent_ConcatenatesTextNodesWithoutASeparator()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:attribute name=\"a\">x<xsl:value-of select=\"1\"/><xsl:value-of select=\"2\"/>y</xsl:attribute></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out a='x12y'/>");
    }

    [Test]
    public async Task Attribute_WithAPrefixInScopeOfTheInstruction_ResolvesItThere()
    {
        var harness = new ConstructionHarness("<out><xsl:attribute name=\"f:a\">v</xsl:attribute></out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:f='urn:f' f:a='v'/>");
    }

    [Test]
    public async Task Attribute_WithAnEmptyNamespaceAttribute_IsInNoNamespace()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:attribute name=\"f:a\" namespace=\"\">v</xsl:attribute></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out a='v'/>");
    }

    [Test]
    public async Task Attribute_RepeatedWithTheSameName_KeepsTheLastOne()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:attribute name=\"a\">1</xsl:attribute><xsl:attribute name=\"b\">x</xsl:attribute><xsl:attribute name=\"a\">2</xsl:attribute></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out b='x' a='2'/>");
    }

    [Test]
    public async Task Attribute_TwoPrefixesOfOneNamespace_AreOneAttributeAndTheLastOneWins()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:attribute name=\"p:a\" namespace=\"urn:n\">1</xsl:attribute><xsl:attribute name=\"q:a\" namespace=\"urn:n\">2</xsl:attribute></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:q='urn:n' q:a='2'/>");
    }

    [Test]
    public async Task Attribute_BeforeTheChildren_IsAcceptedInAnyOrderWithNamespaces()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:attribute name=\"a\">1</xsl:attribute><xsl:namespace name=\"p\" select=\"'urn:p'\"/><xsl:attribute name=\"b\">2</xsl:attribute><c/></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:p='urn:p' a='1' b='2'><c/></out>");
    }
}
