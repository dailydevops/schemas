namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

/// <summary>An element that takes the default or fixed value of its declaration is annotated with that value (design #146 DP-19).</summary>
public class AnnotationDefaultTests
{
    private static readonly XdmTypeId DateTimeType = TypedValues.DataModelType(SchemaFixture.Simple("dateTime"));

    private static AnnotationFixture.AnnotatedRun Run(string type, ElementValueConstraint constraint, string xml)
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple(type), constraint);
        return AnnotationFixture.Run(schema, xml);
    }

    [Test]
    [Arguments("<r/>", 7L)]
    [Arguments("<r></r>", 7L)]
    [Arguments("<r>3</r>", 3L)]
    public async Task Annotation_ElementWithADefault_HasTheDefaultAsItsTypedValueWhenEmpty(string xml, long expected)
    {
        var run = Run("integer", new ElementValueConstraint(ElementValueConstraintKind.Default, "7"), xml);

        var annotation = run.Annotation("r");
        var items = run.TypedValue(annotation);

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("integer");
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Atomic);
        await Assert.That(annotation.TypedValueCount).IsEqualTo(1);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(items[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<r/>")]
    [Arguments("<r>7</r>")]
    public async Task Annotation_ElementWithAFixedValue_HasTheFixedValueAsItsTypedValue(string xml)
    {
        var run = Run("integer", new ElementValueConstraint(ElementValueConstraintKind.Fixed, "7"), xml);

        var annotation = run.Annotation("r");
        var items = run.TypedValue(annotation);

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(items.Length).IsEqualTo(1);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(items[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(7L);
    }

    [Test]
    public async Task Atomization_OfAnEmptyElementWithADefault_ReturnsTheDefault()
    {
        var run = Run("integer", new ElementValueConstraint(ElementValueConstraintKind.Default, "7"), "<r/>");
        var node = XdmItem.FromNode(new XPathNode(run.Element("r")));

        var data = run.Source.Data(XdmSequence.Create(node));
        var slot = run.Source.TryGetTypedValue(new XPathNode(run.Element("r")), out var typed);

        await Assert.That(data.Count).IsEqualTo(1);
        await Assert.That(data[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(data[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(7L);
        await Assert.That(slot).IsTrue();
        await Assert.That(typed.TypeId).IsEqualTo(XdmTypeId.XsInteger);
    }

    [Test]
    public async Task Annotation_ElementWithADefaultThatTheTypeRejects_IsInvalidWithTheDefaultAsItsUntypedValue()
    {
        var run = Run("boolean", new ElementValueConstraint(ElementValueConstraintKind.Default, "maybe"), "<r/>");

        var annotation = run.Annotation("r");
        var items = run.TypedValue(annotation);

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Invalid);
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Untyped);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
        await Assert.That(items[0].TryGetString(out var text)).IsTrue();
        await Assert.That(text).IsEqualTo("maybe");
    }

    [Test]
    public async Task Annotation_BuiltInTypeWithoutACastingRow_KeepsTheItemOfItsNearestAncestorWithOne()
    {
        var run = Run("dateTimeStamp", default, "<r>2020-01-02T03:04:05Z</r>");

        var annotation = run.Annotation("r");
        var items = run.TypedValue(annotation);

        // The casting table has no row for xs:dateTimeStamp, so the item is an xs:dateTime, while the node reports its own type.
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("dateTimeStamp");
        await Assert.That(items.Length).IsEqualTo(1);
        await Assert.That(items[0].TypeId).IsEqualTo(DateTimeType);
        await Assert.That(annotation.DataModelType).IsNotEqualTo(DateTimeType);
    }

    [Test]
    [Arguments("<r>name</r>")]
    [Arguments("<r xmlns:p=\"urn:p\">p:name</r>")]
    public async Task Annotation_QNameElement_KeepsTheValueAsUntypedAtomic(string xml)
    {
        var run = Run("QName", default, xml);

        var annotation = run.Annotation("r");
        var items = run.TypedValue(annotation);

        // The lexical form needs the namespaces in scope of the node (#604), so the item stays untyped.
        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("QName");
        await Assert.That(items.Length).IsEqualTo(1);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
    }
}
