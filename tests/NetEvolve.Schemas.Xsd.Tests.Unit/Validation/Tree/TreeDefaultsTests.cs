namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Tree;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The annotated copy holds the defaulted attributes and the defaulted element values as nodes (design #146 DP-19, option A; #361).</summary>
public class TreeDefaultsTests
{
    private const string Schema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:element name="root">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="size" type="xs:int" default="5" minOccurs="0"/>
                <xs:element name="tag" type="xs:string" fixed="t" minOccurs="0"/>
                <xs:element name="item" type="xs:int" minOccurs="0" maxOccurs="unbounded"/>
              </xs:sequence>
              <xs:attribute name="a" type="xs:int" default="7"/>
              <xs:attribute name="b" type="xs:string" fixed="f"/>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    private const string QualifiedSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:p" xmlns:p="urn:p" elementFormDefault="qualified" attributeFormDefault="qualified">
          <xs:element name="root">
            <xs:complexType>
              <xs:attribute name="a" type="xs:int" default="7"/>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    public static IEnumerable<Func<string>> Documents()
    {
        foreach (
            var instance in new[]
            {
                "<root/>",
                "<root><size/></root>",
                "<root a=\"x\"><size>y</size></root>",
                "<root b=\"g\"><tag>u</tag></root>",
                "<root b=\"f\"><size>3</size><tag/><item>1</item><item>z</item></root>",
                "<root><item>1</item><bad/></root>",
            }
        )
        {
            var captured = instance;
            yield return () => captured;
        }
    }

    [Test]
    public async Task Validate_DefaultedAttribute_IsANodeOfTheAnnotatedTreeWithItsAnnotation(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = TreeFixture.Load("<root><size>1</size></root>");

        var result = TreeFixture.Validator(Schema).Validate(input.Document, cancellationToken);

        var root = TreeFixture.Element(result.Document, "root");
        var a = root.FirstAttribute;
        var b = a.NextAttribute;
        await Assert.That(TreeFixture.Element(input, "root").FirstAttribute.IsEmpty).IsTrue();
        await Assert.That(result.Document.Names.GetString(a.Name.Local)).IsEqualTo("a");
        await Assert.That(a.StringValue).IsEqualTo("7");
        await Assert.That(a.IsSpecified).IsFalse();
        await Assert.That(result.TryGetAnnotation(a, out var annotationOfA)).IsTrue();
        await Assert.That(annotationOfA.TypeName.LocalName).IsEqualTo("int");
        await Assert.That(annotationOfA.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(result.Document.Names.GetString(b.Name.Local)).IsEqualTo("b");
        await Assert.That(b.StringValue).IsEqualTo("f");
        await Assert.That(result.TryGetAnnotation(b, out var annotationOfB)).IsTrue();
        await Assert.That(annotationOfB.TypeName.LocalName).IsEqualTo("string");
        await Assert.That(b.NextAttribute.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Validate_SpecifiedAttribute_PrecedesTheDefaultedOnesWithEachOwnAnnotation(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = TreeFixture
            .Validator(Schema)
            .Validate(TreeFixture.Load("<root b=\"f\"/>").Document, cancellationToken);

        var first = TreeFixture.Element(result.Document, "root").FirstAttribute;
        var second = first.NextAttribute;
        await Assert.That(first.StringValue).IsEqualTo("f");
        await Assert.That(first.IsSpecified).IsTrue();
        await Assert.That(result.TryGetAnnotation(first, out var specified)).IsTrue();
        await Assert.That(specified.TypeName.LocalName).IsEqualTo("string");
        await Assert.That(second.StringValue).IsEqualTo("7");
        await Assert.That(second.IsSpecified).IsFalse();
        await Assert.That(result.TryGetAnnotation(second, out var defaulted)).IsTrue();
        await Assert.That(defaulted.TypeName.LocalName).IsEqualTo("int");
    }

    [Test]
    public async Task Validate_DefaultedAttributeOfAQualifiedName_HasTheNamespaceOfItsDeclaration(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = TreeFixture
            .Validator(QualifiedSchema)
            .Validate(TreeFixture.Load("<p:root xmlns:p=\"urn:p\"/>").Document, cancellationToken);

        var attribute = TreeFixture.Element(result.Document, "root").FirstAttribute;
        await Assert.That(result.Document.Names.GetString(attribute.Name.NamespaceUri)).IsEqualTo("urn:p");
        await Assert.That(result.Document.Names.GetString(attribute.Name.Local)).IsEqualTo("a");
        await Assert.That(attribute.StringValue).IsEqualTo("7");
        await Assert.That(result.TryGetAnnotation(attribute, out _)).IsTrue();
    }

    [Test]
    [Arguments("size", "5", "int")]
    [Arguments("tag", "t", "string")]
    public async Task Validate_DefaultedElementValue_IsTheContentOfTheElementInTheCopy(
        string local,
        string value,
        string type,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = TreeFixture.Load("<root><" + local + "/></root>");

        var result = TreeFixture.Validator(Schema).Validate(input.Document, cancellationToken);

        var element = TreeFixture.Element(result.Document, local);
        await Assert.That(TreeFixture.Element(input, local).StringValue).IsEqualTo(string.Empty);
        await Assert.That(element.StringValue).IsEqualTo(value);
        await Assert.That(element.FirstChild.Kind).IsEqualTo(XmlNodeKind.Text);
        await Assert.That(result.TryGetAnnotation(element, out var annotation)).IsTrue();
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo(type);
        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
    }

    [Test]
    public async Task Validate_ElementWithItsOwnText_KeepsItWithoutTheDefault(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = TreeFixture
            .Validator(Schema)
            .Validate(TreeFixture.Load("<root><size>3</size></root>").Document, cancellationToken);

        await Assert.That(TreeFixture.Element(result.Document, "size").StringValue).IsEqualTo("3");
    }

    [Test]
    public async Task Data_XPathOverTheDefaultedNodes_ReturnsTheirTypedValues(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture
            .Validator(Schema)
            .Validate(TreeFixture.Load("<root><size/></root>").Document, cancellationToken);
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(result.Document.Document)));

        var element = result.Data(XPathExpression.Compile("/root/size").Evaluate(context));
        var attribute = result.Data(XPathExpression.Compile("/root/@a").Evaluate(context));

        await Assert.That(element.Count).IsEqualTo(1);
        await Assert.That(element[0].TryGetInteger(out var size)).IsTrue();
        await Assert.That(size).IsEqualTo(5L);
        await Assert.That(attribute.Count).IsEqualTo(1);
        await Assert.That(attribute[0].TryGetInteger(out var a)).IsTrue();
        await Assert.That(a).IsEqualTo(7L);
    }

    [Test]
    [MethodDataSource(nameof(Documents))]
    public async Task Document_WithDefaults_GivesTheSameIssuesAsTheStreamingPath(
        string instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var streamed = TreeFixture
            .Streaming(Schema)
            .Validate(instance.AsSpan(), TreeFixture.InstanceId, cancellationToken);

        var tree = TreeFixture.Validator(Schema).Validate(TreeFixture.Load(instance).Document, cancellationToken);

        await Assert.That(TreeFixture.Describe(tree.Result)).IsEqualTo(TreeFixture.Describe(streamed));
    }

    [Test]
    public async Task TryGetIssueNode_WithDefaultsInTheCopy_StillMapsToTheElementOfTheIssue(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture
            .Validator(Schema)
            .Validate(TreeFixture.Load("<root><size/><tag/><item>x</item></root>").Document, cancellationToken);

        var found = result.TryGetIssueNode(result.Result.Issues[0], out var node);

        await Assert.That(found).IsTrue();
        await Assert.That(node).IsEqualTo(TreeFixture.Element(result.Document, "item"));
    }

    [Test]
    public async Task TryGetIssueNode_IssueOfAnElementWithAFixedValue_IsTheElement(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture
            .Validator(Schema)
            .Validate(TreeFixture.Load("<root><tag>u</tag></root>").Document, cancellationToken);

        var found = result.TryGetIssueNode(result.Result.Issues[0], out var node);

        await Assert.That(found).IsTrue();
        await Assert.That(node).IsEqualTo(TreeFixture.Element(result.Document, "tag"));
    }
}
