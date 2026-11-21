namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The remaining paths of the construction instructions: sinks that forward, failures of the parts of an instruction, and the fixup against inherited prefixes.</summary>
public class ConstructionBranchTests
{
    private static CopySink NewCopySink(ItemSequenceSink inner) =>
        new(new ConstructionHarness("<out/>").Frame, inner, default);

    [Test]
    public async Task CopySink_ForwardsTextAndTrees_AndHasNoContent()
    {
        var inner = new ItemSequenceSink(SinkTestSupport.NewTable());
        var copies = NewCopySink(inner);

        copies.Text("ab".AsSpan());
        var tree = copies.BeginTree();
        var document = copies.EndTree(tree);
        copies.Item(in document);

        await Assert.That(copies.Kind).IsEqualTo(SequenceSinkKind.Items);
        await Assert.That(() => copies.Content).Throws<InvalidOperationException>();
        await Assert.That(inner.Result.Count).IsEqualTo(2);
        await Assert.That(copies.Failed).IsFalse();
    }

    [Test]
    public async Task CopySink_AfterAFailure_IgnoresTheLaterItems()
    {
        var source = ConstructionSheet.Source("<s k=\"1\"/>");
        var inner = new ItemSequenceSink(SinkTestSupport.NewTable());
        var copies = NewCopySink(inner);
        var attribute = XdmItem.FromNode(new XPathNode(source.Node.FirstAttribute));
        var number = XdmItem.FromInteger(1);

        copies.Item(in attribute);
        copies.Item(in number);

        await Assert.That(copies.Failed).IsTrue();
        await Assert.That(inner.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixup_AttributeInANamespaceOfAnInheritedPrefix_UsesThatPrefix()
    {
        var harness = new ConstructionHarness(
            "<p:out xmlns:p=\"urn:p\"><xsl:element name=\"e\"><xsl:attribute name=\"x\" namespace=\"urn:p\">v</xsl:attribute></xsl:element></p:out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<p:out xmlns:p='urn:p'><e p:x='v'/></p:out>");
    }

    [Test]
    public async Task Fixup_XmlPrefixOfAnotherNamespace_GetsAGeneratedPrefix()
    {
        var harness = new ConstructionHarness(
            "<out><xsl:attribute name=\"xml:a\" namespace=\"urn:x\">v</xsl:attribute></out>"
        );

        await Assert.That(harness.Tree()).IsEqualTo("<out xmlns:ns1='urn:x' ns1:a='v'/>");
    }

    [Test]
    public async Task Fixup_XmlAttributeFromTheXmlNamespace_NeedsNoDeclaration()
    {
        var harness = new ConstructionHarness("<out><xsl:attribute name=\"xml:space\">preserve</xsl:attribute></out>");

        await Assert.That(harness.Tree()).IsEqualTo("<out xml:space='preserve'/>");
    }

    [Test]
    public async Task Document_WithAValueWithoutATextForm_IsAnEvaluationFailure()
    {
        var harness = new ConstructionHarness(
            "<xsl:document><xsl:sequence select=\"$d\"/></xsl:document>",
            variables: ["d"]
        );
        harness.Bind("d", XdmSequence.Create(XdmItem.FromDouble(1.5)));

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3301");
    }

    [Test]
    [Arguments("<out><xsl:comment>abcdef</xsl:comment></out>")]
    [Arguments("<out><xsl:processing-instruction name=\"p\">abcdef</xsl:processing-instruction></out>")]
    public async Task SimpleContent_LongerThanTheLimit_IsTheLimitStop(string fragment)
    {
        var harness = new ConstructionHarness(fragment);
        harness.Frame.MaxMessageLength = 3;

        await Assert.That(harness.Tree()).IsEqualTo("problem:NES3503");
    }

    [Test]
    [Arguments("<out><xsl:attribute name=\"a\" separator=\"{integer('x')}\" select=\"(1,2)\"/></out>")]
    [Arguments("<out><xsl:namespace name=\"{integer('x')}\" select=\"'urn:p'\"/></out>")]
    [Arguments("<out><xsl:namespace name=\"p\" select=\"integer('x')\"/></out>")]
    [Arguments("<out><xsl:processing-instruction name=\"{integer('x')}\"/></out>")]
    [Arguments("<out><xsl:processing-instruction name=\"p\" select=\"integer('x')\"/></out>")]
    [Arguments("<out><xsl:attribute name=\"{integer('x')}\"/></out>")]
    public async Task APartThatFails_StopsTheInstruction(string fragment) =>
        await Assert.That(new ConstructionHarness(fragment).Tree()).IsEqualTo("problem:NES3301/FORG0001");

    [Test]
    public async Task TextNode_ThroughTheSyntheticEntry_HasNoName()
    {
        var builder = new Schematron.Xslt.Registry.XsltRegistryBuilder();
        Schematron.Xslt.Instructions.Construction.Registration.Register(builder);
        var registry = builder.Build();

        _ = registry.TryGetSynthetic(XsltSyntheticElement.TextNode, out var text);

        await Assert.That(text.Name).IsEqualTo(default(XPathQName));
    }

    [Test]
    [Arguments("<xsl:element name=\"e\" namespace=\"{\"/>", "NES3006/XTSE0350")]
    [Arguments("<xsl:comment><xsl:element/></xsl:comment>", "NES3005")]
    public async Task CompileProblem_InAPartOfAnInstruction_IsReported(string fragment, string expected)
    {
        var harness = new ConstructionHarness(fragment);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(expected);
    }

    [Test]
    public async Task CopyOfANamespaceNode_IntoAnItemSequence_IsTheUnsupportedConstructError()
    {
        var source = ConstructionSheet.Source("<s xmlns:q=\"urn:q\"/>");
        var harness = new ConstructionHarness("<xsl:copy-of select=\"$n\"/>", variables: ["n"]);
        harness.Bind("n", XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Node.FirstNamespace))));

        await Assert.That(harness.Items()).IsEqualTo("problem:NES3204");
    }
}
