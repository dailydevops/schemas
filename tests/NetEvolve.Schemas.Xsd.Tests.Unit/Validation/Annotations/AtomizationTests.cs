namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

public class AtomizationTests
{
    private static XPathNode Node(XmlNode node) => new(node);

    [Test]
    public async Task Data_ElementTypedInteger_ReturnsAnInteger()
    {
        var run = AnnotationFixture.Run("<r><i>42</i></r>");
        var sequence = XdmSequence.Create(XdmItem.FromNode(Node(run.Element("i"))));

        var data = run.Source.Data(sequence);

        await Assert.That(data.Count).IsEqualTo(1);
        await Assert.That(data[0].IsNode).IsFalse();
        await Assert.That(data[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(data[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(42L);
    }

    [Test]
    public async Task Data_AttributeTypedInteger_ReturnsAnInteger()
    {
        var run = AnnotationFixture.Run("<r><w a=\"5\"/></r>");
        var attribute = run.Attribute(run.Element("w"), "a");

        var data = run.Source.Data(XdmSequence.Create(XdmItem.FromNode(Node(attribute))));

        await Assert.That(data[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(data[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(5L);
    }

    [Test]
    public async Task Data_UnvalidatedNode_ReturnsUntypedAtomic()
    {
        var run = AnnotationFixture.Run(AnnotationFixture.LaxSchema(), "<r><unknown>5</unknown></r>");

        var data = run.Source.Data(XdmSequence.Create(XdmItem.FromNode(Node(run.Element("unknown")))));

        await Assert.That(data.Count).IsEqualTo(1);
        await Assert.That(data[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task Data_AtomicItem_PassesThrough()
    {
        var run = AnnotationFixture.Run("<r/>");
        var sequence = XdmSequence.Create(XdmItem.FromInteger(3));

        var data = run.Source.Data(sequence);

        await Assert.That(data == sequence).IsTrue();
    }

    [Test]
    public async Task Data_InvalidNode_KeepsTheUntypedValue()
    {
        var run = AnnotationFixture.Run("<r><i>abc</i></r>");

        var data = run.Source.Data(XdmSequence.Create(XdmItem.FromNode(Node(run.Element("i")))));

        await Assert.That(data[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task TypeAnnotation_OfNodesTheValidatorNeverAssessed_IsTheUntypedDefault()
    {
        var run = AnnotationFixture.Run(AnnotationFixture.LaxSchema(), "<r><unknown a=\"1\">t<!--c--></unknown></r>");
        var element = Node(run.Element("unknown"));
        var attribute = Node(run.Attribute(run.Element("unknown"), "a"));
        var text = Node(run.Element("unknown").FirstChild);
        var document = Node(run.Tree.Document);

        await Assert.That(run.Source.GetTypeAnnotation(element)).IsEqualTo(XdmTypeId.Untyped);
        await Assert.That(run.Source.GetTypeAnnotation(attribute)).IsEqualTo(XdmTypeId.UntypedAtomic);
        await Assert.That(run.Source.GetTypeAnnotation(text)).IsEqualTo(XdmTypeId.UntypedAtomic);
        await Assert.That(run.Source.GetTypeAnnotation(document)).IsEqualTo(XdmTypeId.Untyped);
    }

    [Test]
    public async Task TypeAnnotation_OfValidNodes_IsTheNearestBuiltInType()
    {
        var run = AnnotationFixture.Run(
            "<r><i>1</i><c><n>2</n></c><m/><sc>3</sc><rn>4</rn><l>1</l><weird>q</weird><w a=\"5\"/></r>"
        );

        await Assert.That(run.Source.GetTypeAnnotation(Node(run.Element("i")))).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(run.Source.GetTypeAnnotation(Node(run.Element("c")))).IsEqualTo(TypedValues.AnyType);
        await Assert.That(run.Source.GetTypeAnnotation(Node(run.Element("m")))).IsEqualTo(TypedValues.AnyType);
        await Assert.That(run.Source.GetTypeAnnotation(Node(run.Element("sc")))).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(run.Source.GetTypeAnnotation(Node(run.Element("rn")))).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(run.Source.GetTypeAnnotation(Node(run.Element("l")))).IsEqualTo(TypedValues.AnySimpleType);
        await Assert
            .That(run.Source.GetTypeAnnotation(Node(run.Element("weird"))))
            .IsEqualTo(TypedValues.AnySimpleType);
        await Assert
            .That(run.Source.GetTypeAnnotation(Node(run.Attribute(run.Element("w"), "a"))))
            .IsEqualTo(XdmTypeId.XsInteger);
    }

    [Test]
    public async Task TypeAnnotation_OfInvalidNodes_IsAnyTypeForElementsAndAnySimpleTypeForAttributes()
    {
        var run = AnnotationFixture.Run("<r><i>abc</i><w a=\"zz\"/></r>");

        await Assert.That(run.Source.GetTypeAnnotation(Node(run.Element("i")))).IsEqualTo(TypedValues.AnyType);
        await Assert
            .That(run.Source.GetTypeAnnotation(Node(run.Attribute(run.Element("w"), "a"))))
            .IsEqualTo(TypedValues.AnySimpleType);
    }

    [Test]
    public async Task TypedValue_OfAnAtomicNode_FillsTheSlot()
    {
        var run = AnnotationFixture.Run("<r><i>42</i></r>");

        var filled = run.Source.TryGetTypedValue(Node(run.Element("i")), out var item);

        await Assert.That(filled).IsTrue();
        await Assert.That(item.TypeId).IsEqualTo(XdmTypeId.XsInteger);
    }

    [Test]
    public async Task TypedValue_OfAListWithSeveralItems_LeavesTheSlotEmpty()
    {
        var run = AnnotationFixture.Run("<r><l>1 2</l><l>1</l><l/></r>");
        var first = run.Element("l");

        await Assert.That(run.Source.TryGetTypedValue(Node(first), out _)).IsFalse();
        await Assert.That(run.Source.TryGetTypedValue(Node(first.NextSibling), out var single)).IsTrue();
        await Assert.That(single.TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(run.Source.TryGetTypedValue(Node(first.NextSibling.NextSibling), out _)).IsFalse();
    }

    [Test]
    public async Task TypedValue_OfNodesWithoutAnAtomicTypedValue_LeavesTheSlotEmpty()
    {
        var run = AnnotationFixture.Run("<r><c><n>2</n></c><m>t</m><e/><i>abc</i></r>");

        await Assert.That(run.Source.TryGetTypedValue(Node(run.Element("c")), out _)).IsFalse();
        await Assert.That(run.Source.TryGetTypedValue(Node(run.Element("m")), out _)).IsFalse();
        await Assert.That(run.Source.TryGetTypedValue(Node(run.Element("e")), out _)).IsFalse();
        await Assert.That(run.Source.TryGetTypedValue(Node(run.Element("i")), out _)).IsFalse();
        await Assert.That(run.Source.TryGetTypedValue(Node(run.Tree.Document), out _)).IsFalse();
    }

    [Test]
    public async Task Source_NodeOfAnotherTree_IsNotAnnotated()
    {
        var run = AnnotationFixture.Run("<r><i>1</i></r>");
        var other = AnnotationFixture.AnnotatedRun.Build("<r><i>1</i></r>");

        await Assert.That(run.Source.TryGetAnnotation(other.Document.FirstChild.FirstChild, out _)).IsFalse();
        await Assert.That(run.Source.TryGetAnnotation(default, out _)).IsFalse();
    }

    [Test]
    public async Task Source_NodesThatAreNeitherElementsNorAttributes_AreNotAnnotated()
    {
        var run = AnnotationFixture.Run("<r><m>t<!--c--></m></r>");
        var mixed = run.Element("m");

        await Assert.That(run.Source.TryGetAnnotation(run.Tree.Document, out _)).IsFalse();
        await Assert.That(run.Source.TryGetAnnotation(mixed.FirstChild, out _)).IsFalse();
        await Assert.That(run.Source.TryGetAnnotation(mixed.FirstChild.NextSibling, out _)).IsFalse();
    }
}
