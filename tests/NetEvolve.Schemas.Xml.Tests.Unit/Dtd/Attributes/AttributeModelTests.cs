namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

/// <summary>The declared type and the specified flag of an attribute node in the document model.</summary>
public class AttributeModelTests
{
    private static XmlDocumentTree Load(string declarations, string element) =>
        XmlDocumentLoader.Load($"<!DOCTYPE a [{declarations}]>{element}".AsSpan(), AttributeHarness.DtdOn);

    private static XmlNode Root(XmlDocumentTree tree) => tree.Document.FirstChild;

    [Test]
    public async Task Model_AttributeDeclaredAsId_IsReportedWithTheIdType()
    {
        var tree = Load("<!ATTLIST a i ID #IMPLIED>", "<a i=\"n1\"/>");

        var attribute = Root(tree).FirstAttribute;

        await Assert.That(attribute.AttributeType).IsEqualTo(DtdAttributeType.Id);
        await Assert.That(attribute.IsSpecified).IsTrue();
        await Assert.That(attribute.StringValue).IsEqualTo("n1");
    }

    [Test]
    public async Task Model_DefaultedAttribute_IsAnAttributeNodeThatIsNotSpecified()
    {
        var tree = Load("<!ATTLIST a d NMTOKEN \"v\" f CDATA #FIXED \"w\">", "<a/>");

        var first = Root(tree).FirstAttribute;
        var second = first.NextAttribute;

        await Assert.That(first.StringValue).IsEqualTo("v");
        await Assert.That(first.AttributeType).IsEqualTo(DtdAttributeType.NmToken);
        await Assert.That(first.IsSpecified).IsFalse();
        await Assert.That(second.StringValue).IsEqualTo("w");
        await Assert.That(second.AttributeType).IsEqualTo(DtdAttributeType.Cdata);
        await Assert.That(second.IsSpecified).IsFalse();
        await Assert.That(second.NextAttribute.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Model_SpecifiedAndUndeclaredAttributes()
    {
        var tree = Load("<!ATTLIST a d CDATA \"v\" t NMTOKENS #IMPLIED>", "<a u=\"1\" t=\"  x   y \"/>");

        var undeclared = Root(tree).FirstAttribute;
        var tokens = undeclared.NextAttribute;
        var defaulted = tokens.NextAttribute;

        await Assert.That(undeclared.AttributeType).IsNull();
        await Assert.That(undeclared.IsSpecified).IsTrue();
        await Assert.That(tokens.AttributeType).IsEqualTo(DtdAttributeType.NmTokens);
        await Assert.That(tokens.IsSpecified).IsTrue();
        await Assert.That(tokens.StringValue).IsEqualTo("x y");
        await Assert.That(defaulted.IsSpecified).IsFalse();
    }

    [Test]
    public async Task Model_NodesThatAreNotAttributes_HaveNoTypeAndAreSpecified()
    {
        var tree = Load("<!ATTLIST a d CDATA \"v\">", "<a>t<!--c--><?p d?></a>");

        var element = Root(tree);
        var text = element.FirstChild;
        var comment = text.NextSibling;
        var instruction = comment.NextSibling;

        foreach (var node in new[] { tree.Document, element, text, comment, instruction })
        {
            await Assert.That(node.AttributeType).IsNull();
            await Assert.That(node.IsSpecified).IsTrue();
        }
    }

    [Test]
    public async Task Model_DefaultedNamespaceDeclaration_IsANamespaceNodeAndNoAttributeNode()
    {
        var tree = Load("<!ATTLIST a xmlns:p CDATA \"urn:p\" d CDATA \"v\">", "<a><p:b/></a>");

        var element = Root(tree);
        var child = element.FirstChild;

        await Assert.That(element.FirstNamespace.StringValue).IsEqualTo("urn:p");
        await Assert.That(element.FirstAttribute.StringValue).IsEqualTo("v");
        await Assert.That(element.FirstAttribute.NextAttribute.IsEmpty).IsTrue();
        await Assert.That(child.Name.NamespaceUri.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Model_WithoutDtdProcessing_ReportsNoTypeAndEveryNodeIsSpecified()
    {
        var tree = XmlDocumentLoader.Load("<a i=\"n1\"><b/></a>".AsSpan());

        var attribute = Root(tree).FirstAttribute;

        await Assert.That(attribute.AttributeType).IsNull();
        await Assert.That(attribute.IsSpecified).IsTrue();
    }

    [Test]
    public async Task Model_ADocumentWithoutAnyDeclaredAttribute_ReportsNoType()
    {
        var tree = Load("<!ELEMENT a ANY>", "<a i=\"n1\"/>");

        await Assert.That(Root(tree).FirstAttribute.AttributeType).IsNull();
        await Assert.That(Root(tree).FirstAttribute.IsSpecified).IsTrue();
    }

    [Test]
    public async Task Builder_FactsOfOneTreeDoNotLeakIntoTheNextTree()
    {
        using var builder = new XmlTreeBuilder();
        var sink = builder;
        using (
            var first = XmlPipelineReader.Create(
                "<!DOCTYPE a [<!ATTLIST a i ID \"x\">]><a/>".AsSpan(),
                AttributeHarness.DtdOn
            )
        )
        {
            _ = first.Pump(ref sink);
        }

        var tree = builder.Complete();
        using (var second = XmlPipelineReader.Create("<a i=\"y\"/>".AsSpan()))
        {
            _ = second.Pump(ref sink);
        }

        var next = builder.Complete();

        await Assert.That(tree.Document.FirstChild.FirstAttribute.AttributeType).IsEqualTo(DtdAttributeType.Id);
        await Assert.That(next.Document.FirstChild.FirstAttribute.AttributeType).IsNull();
        await Assert.That(next.Document.FirstChild.FirstAttribute.IsSpecified).IsTrue();
    }

    [Test]
    public async Task Builder_InfoThatDoesNotFollowAnAttribute_IsIgnored()
    {
        using var builder = new XmlTreeBuilder();
        var info = new ValidationAttributeInfo(
            "xmlns:p".AsMemory(),
            "u".AsMemory(),
            DtdAttributeType.Cdata,
            isSpecified: false,
            default
        );
        builder.StartDocument(default);
        builder.StartElement(default, default);
        builder.NamespaceDeclaration(default, default);
        builder.AttributeInfo(in info);
        builder.EndAttributes();
        builder.EndElement(default);
        builder.EndDocument();

        var tree = builder.Complete();

        await Assert.That(tree.Document.FirstChild.FirstAttribute.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Builder_InfoWithoutATypeForAnAttributeTheDocumentDidNotSpecify_IsKept()
    {
        using var builder = new XmlTreeBuilder();
        var info = new ValidationAttributeInfo("d".AsMemory(), "v".AsMemory(), null, isSpecified: false, default);
        builder.StartDocument(default);
        builder.StartElement(default, default);
        builder.Attribute(default, "v".AsSpan());
        builder.AttributeInfo(in info);
        builder.EndAttributes();
        builder.EndElement(default);
        builder.EndDocument();

        var attribute = builder.Complete().Document.FirstChild.FirstAttribute;

        await Assert.That(attribute.AttributeType).IsNull();
        await Assert.That(attribute.IsSpecified).IsFalse();
    }

    [Test]
    public async Task OtherTrees_ReportNoTypeAndEveryNodeIsSpecified()
    {
        var tree = new FakeXmlTree();
        var element = tree.AddElement(0, "a");

        var node = new XmlNode(tree, element);

        await Assert.That(node.AttributeType).IsNull();
        await Assert.That(node.IsSpecified).IsTrue();
    }
}
