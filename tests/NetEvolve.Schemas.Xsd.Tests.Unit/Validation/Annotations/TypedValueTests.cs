namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

public class TypedValueTests
{
    private static readonly XdmTypeId AnyType = new(16);

    private static (AnnotationSource Source, XmlDocumentTree Tree) Manual(string xml, AnnotationRecord record)
    {
        var table = new AnnotationTable();
        if (record.IsAttribute)
        {
            table.Attribute(record);
        }
        else
        {
            table.Element(record);
        }

        var tree = AnnotationFixture.AnnotatedRun.Build(xml);
        return (new AnnotationSource(table, tree, new ScriptedAssessor()), tree);
    }

    [Test]
    public async Task Annotation_ElementTypedAnyType_IsMixedAndHasTheStringValueAsUntypedAtomic()
    {
        var run = AnnotationFixture.Run(AnnotationFixture.Schema(), "<r><t>some <b/>text</t></r>");

        var annotation = run.Annotation("t");
        var items = run.TypedValue(annotation);

        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("anyType");
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Untyped);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
        await Assert.That(run.Source.GetTypeAnnotation(new XPathNode(run.Element("t")))).IsEqualTo(AnyType);
    }

    [Test]
    public async Task Annotation_BuiltInTypeComponents_YieldTheirTypedValues()
    {
        var run = AnnotationFixture.Run(AnnotationFixture.Schema(), "<r><bi>8</bi><bl>a b c</bl></r>");

        var number = run.Annotation("bi");
        var tokens = run.TypedValue(run.Annotation("bl"));
        var items = run.TypedValue(number);

        await Assert.That(number.TypeName.LocalName).IsEqualTo("integer");
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(tokens.Length).IsEqualTo(3);
        await Assert.That(tokens[2].TryGetString(out var last)).IsTrue();
        await Assert.That(last).IsEqualTo("c");
        await Assert
            .That(run.Source.GetTypeAnnotation(new XPathNode(run.Element("bi"))))
            .IsEqualTo(XdmTypeId.XsInteger);
    }

    [Test]
    public async Task Annotation_UnknownBuiltInList_KeepsTheValueAsOneUntypedAtomicItem()
    {
        var run = AnnotationFixture.Run(AnnotationFixture.Schema(), "<r><nolist>a b</nolist></r>");

        var items = run.TypedValue(run.Annotation("nolist"));

        await Assert.That(items.Length).IsEqualTo(1);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task Annotation_UnionWithoutAnAcceptingMember_KeepsTheValueUntyped()
    {
        // The scripted datatype accepts a value of an anonymous union without naming a member.
        var schema = AnnotationFixture.Schema();
        var tree = AnnotationFixture.AnnotatedRun.Build("<r><u/></r>");
        var union = IdFixture.UnionOf(schema, SchemaFixture.Simple("integer"), SchemaFixture.Simple("boolean"));
        var table = new AnnotationTable();
        table.Element(new AnnotationRecord(2, -1, union, XsdValidity.Valid, false));
        var source = new AnnotationSource(table, tree, new ScriptedAssessor());

        _ = source.TryGetAnnotation(tree.Document.FirstChild.FirstChild, out var annotation);
        var items = new XdmItem[annotation.TypedValueCount];
        _ = annotation.CopyTypedValue(items);

        await Assert.That(items.Length).IsEqualTo(1);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task Annotation_Untyped_CopiesNothingIntoAnEmptyDestination()
    {
        var run = AnnotationFixture.Run(AnnotationFixture.Schema(), "<r><i>abc</i></r>");

        var written = run.Annotation("i").CopyTypedValue([]);

        await Assert.That(written).IsEqualTo(0);
    }

    [Test]
    public async Task Annotation_RecordThatIsNotKnown_MapsToUntyped()
    {
        var (source, tree) = Manual(
            "<r a=\"1\"/>",
            new AnnotationRecord(1, -1, SchemaFixture.Simple("integer"), XsdValidity.NotKnown, false)
        );
        var element = tree.Document.FirstChild;

        _ = source.TryGetAnnotation(element, out var annotation);

        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("untyped");
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Untyped);
        await Assert.That(source.GetTypeAnnotation(new XPathNode(element))).IsEqualTo(XdmTypeId.Untyped);
    }

    [Test]
    public async Task Annotation_AttributeRecordThatIsNotKnown_MapsToUntypedAtomic()
    {
        var (source, tree) = Manual(
            "<r a=\"1\"/>",
            new AnnotationRecord(1, 0, SchemaFixture.Simple("integer"), XsdValidity.NotKnown, false)
        );
        var attribute = tree.Document.FirstChild.FirstAttribute;

        _ = source.TryGetAnnotation(attribute, out var annotation);

        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("untypedAtomic");
        await Assert.That(source.GetTypeAnnotation(new XPathNode(attribute))).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task Annotation_ValidRecordWithoutAType_HasAnAnonymousName()
    {
        var (source, tree) = Manual("<r/>", new AnnotationRecord(1, -1, null, XsdValidity.Valid, false));

        _ = source.TryGetAnnotation(tree.Document.FirstChild, out var annotation);

        await Assert.That(annotation.TypeName.IsAnonymous).IsTrue();
    }

    [Test]
    public async Task Source_AnAttributeInAnotherNamespace_HasItsOwnIndex()
    {
        var table = new AnnotationTable();
        table.Attribute(new AnnotationRecord(1, 0, SchemaFixture.Simple("integer"), XsdValidity.Valid, false));
        var tree = AnnotationFixture.AnnotatedRun.Build("<r xmlns:p=\"urn:p\" p:x=\"1\"/>");
        var source = new AnnotationSource(table, tree, new ScriptedAssessor());

        var found = source.TryGetAnnotation(tree.Document.FirstChild.FirstAttribute, out var annotation);

        await Assert.That(found).IsTrue();
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("integer");
    }

    [Test]
    public async Task Source_TreeWithoutElements_HoldsNoAnnotations()
    {
        var builder = new XmlTreeBuilder();
        builder.StartDocument(default);
        builder.EndDocument();
        var tree = builder.Complete();
        builder.Dispose();
        var source = new AnnotationSource(new AnnotationTable(), tree, new ScriptedAssessor());

        await Assert.That(source.TryGetAnnotation(tree.Document, out _)).IsFalse();
    }

    [Test]
    public async Task Source_SiblingsAndNestedElements_AreNumberedInDocumentOrder()
    {
        var tree = AnnotationFixture.AnnotatedRun.Build("<r><a><b/><c/></a><d/><e><f/></e></r>");
        var table = new AnnotationTable();
        for (var ordinal = 1; ordinal <= 7; ordinal++)
        {
            table.Element(new AnnotationRecord(ordinal, -1, SchemaFixture.Simple("string"), XsdValidity.Valid, false));
        }

        var source = new AnnotationSource(table, tree, new ScriptedAssessor());
        var names = new List<string>();
        Walk(tree.Document.FirstChild, source, tree, names);

        await Assert.That(string.Join(",", names)).IsEqualTo("r,a,b,c,d,e,f");
    }

    private static void Walk(XmlNode node, AnnotationSource source, XmlTree tree, List<string> names)
    {
        if (source.TryGetAnnotation(node, out _))
        {
            names.Add(tree.Names.GetString(node.Name.Local));
        }

        for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            Walk(child, source, tree, names);
        }
    }
}
