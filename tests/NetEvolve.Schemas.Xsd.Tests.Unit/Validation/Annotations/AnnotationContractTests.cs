namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Model.Traversal;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>The contract tests that the design of #146 (section 6) names for the annotations, under their names.</summary>
public class AnnotationContractTests
{
    private const string Instance =
        "<r xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:p=\"urn:p\">"
        + "<i>1</i><c><n>2</n></c><w xsi:nil=\"false\" p:z=\"0\" a=\"5\" s=\"x\"/><l>1 2</l><i>bad</i></r>";

    private static AnnotationFixture.AnnotatedRun Run(string xml) =>
        AnnotationFixture.Run(AnnotationFixture.Schema(), xml);

    private static AnnotationTable Pumped(SchemaFixture schema, XmlTree tree)
    {
        var limits = ValidationLimits.Default;
        var table = new AnnotationTable();
        var validator = new StreamingValidator(
            schema.Model(),
            new DescriptorValueAssessor(XsdVersion.V11),
            new XsdIssueReporter(new ValidationIssueCollector(limits)),
            limits,
            ValidationHarness.Source,
            [new AnnotationListener(table)]
        );
        XmlTreePump.Pump(tree.Document, ref validator);
        return table;
    }

    [Test]
    public async Task Annotation_ValidFull_CarriesTypeName()
    {
        var run = Run("<r><i>42</i><c><n>7</n></c><w a=\"5\"/></r>");

        await Assert
            .That(run.Annotation("i").TypeName)
            .IsEqualTo(new XsdTypeName(AnnotationFixture.Xs, "integer", false));
        await Assert.That(run.Annotation("c").TypeName).IsEqualTo(new XsdTypeName(string.Empty, "Named", false));
        await Assert
            .That(run.Annotation(run.Attribute(run.Element("w"), "a")).TypeName)
            .IsEqualTo(new XsdTypeName(AnnotationFixture.Xs, "integer", false));
    }

    [Test]
    public async Task Annotation_InvalidOrPartial_MapsToAnyTypeAndAnySimpleType()
    {
        var run = Run("<r><i>abc</i><w a=\"zz\"/></r>");

        var element = run.Annotation("i");
        var attribute = run.Annotation(run.Attribute(run.Element("w"), "a"));

        await Assert.That(element.TypeName.LocalName).IsEqualTo("anyType");
        await Assert.That(attribute.TypeName.LocalName).IsEqualTo("anySimpleType");
        await Assert.That(element.TypeName.NamespaceName).IsEqualTo(AnnotationFixture.Xs);
        await Assert.That(attribute.TypeName.NamespaceName).IsEqualTo(AnnotationFixture.Xs);
    }

    [Test]
    public async Task Annotation_NotKnown_MapsToUntyped()
    {
        var run = AnnotationFixture.Run(AnnotationFixture.LaxSchema(), "<r><unknown a=\"1\"/></r>");
        var element = new XPathNode(run.Element("unknown"));
        var attribute = new XPathNode(run.Attribute(run.Element("unknown"), "a"));

        await Assert.That(run.Source.TryGetAnnotation(run.Element("unknown"), out var annotation)).IsFalse();
        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.NotKnown);
        await Assert.That(run.Source.GetTypeAnnotation(element)).IsEqualTo(XdmTypeId.Untyped);
        await Assert.That(run.Source.GetTypeAnnotation(attribute)).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task Annotation_TypedValue_EmptyWhenNilled_AndNoneForElementOnly()
    {
        var run = Run("<r><c><n>2</n></c></r>");
        var table = new AnnotationTable();
        var tree = AnnotationFixture.AnnotatedRun.Build("<r/>");
        table.Element(new AnnotationRecord(1, -1, SchemaFixture.Simple("integer"), XsdValidity.Valid, true));
        var nilled = new AnnotationSource(table, tree, new ScriptedAssessor());

        _ = nilled.TryGetAnnotation(tree.Document.FirstChild, out var empty);
        var elementOnly = run.Annotation("c");

        await Assert.That(empty.TypedValueKind).IsEqualTo(XsdTypedValueKind.Empty);
        await Assert.That(empty.TypedValueCount).IsEqualTo(0);
        await Assert.That(elementOnly.TypedValueKind).IsEqualTo(XsdTypedValueKind.None);
        await Assert.That(elementOnly.TypedValueCount).IsEqualTo(0);
    }

    [Test]
    public async Task Annotation_Ordinal_IsIdenticalForStreamAndPump()
    {
        var schema = AnnotationFixture.Schema();
        var streamed = AnnotationFixture.Run(schema, Instance);

        var pumped = Pumped(schema, streamed.Tree);

        await Assert.That(pumped.ElementOrdinalLimit).IsEqualTo(streamed.Table.ElementOrdinalLimit);
        await Assert.That(pumped.AttributeCount).IsEqualTo(streamed.Table.AttributeCount);
        for (var ordinal = 1; ordinal <= streamed.Table.ElementOrdinalLimit; ordinal++)
        {
            var found = streamed.Table.TryGetElement(ordinal, out var expected);
            var same = pumped.TryGetElement(ordinal, out var actual);
            await Assert.That(same).IsEqualTo(found);
            await Assert.That(actual.Validity).IsEqualTo(expected.Validity);
            await Assert.That(ReferenceEquals(actual.Type, expected.Type)).IsTrue();
        }
    }

    [Test]
    public async Task Annotation_DefaultStructs_AreSafe()
    {
        XsdAnnotation annotation = default;
        XsdTypeName name = default;

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.NotKnown);
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo(string.Empty);
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.None);
        await Assert.That(name.NamespaceName).IsEqualTo(string.Empty);
        await Assert.That(name.IsAnonymous).IsFalse();
    }

    [Test]
    public async Task Annotation_NamespaceDeclarations_NotCounted()
    {
        var run = Run("<r xmlns:p=\"urn:p\"><w xmlns:q=\"urn:q\" a=\"5\"/></r>");

        var annotation = run.Annotation(run.Attribute(run.Element("w"), "a"));

        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("integer");
        await Assert.That(run.Table.TryGetAttribute(2, 0, out _)).IsTrue();
    }

    [Test]
    public async Task Annotation_TryGet_IsConstantTime()
    {
        var table = new AnnotationTable();
        for (var ordinal = 1; ordinal <= 50_000; ordinal++)
        {
            table.Element(new AnnotationRecord(ordinal, -1, SchemaFixture.Simple("integer"), XsdValidity.Valid, false));
        }

        _ = table.TryGetElement(1, out _);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var all = true;
        for (var ordinal = 1; ordinal <= 50_000; ordinal++)
        {
            all &= table.TryGetElement(ordinal, out _);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        await Assert.That(all).IsTrue();
        await Assert.That(allocated).IsEqualTo(0L);
    }

    [Test]
    public async Task Annotation_NullSource_Throws() =>
        await Assert.That(() => new XsdAnnotation(null!, default, default)).Throws<ArgumentNullException>();
}
