namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

public class SlotTypedValuesTests
{
    private static XPathNode Node(XmlNode node) => new(node);

    [Test]
    public async Task TryGetTypedValues_ListWithSeveralItems_YieldsAllItems()
    {
        var run = AnnotationFixture.Run("<r><l>1 2</l></r>");

        var filled = run.Source.TryGetTypedValues(Node(run.Element("l")), out var values);

        await Assert.That(filled).IsTrue();
        await Assert.That(values.Count).IsEqualTo(2);
        await Assert.That(values[1].TryGetInteger(out var second)).IsTrue();
        await Assert.That(second).IsEqualTo(2L);
    }

    [Test]
    public async Task TryGetTypedValues_EmptyList_YieldsNoItems()
    {
        var run = AnnotationFixture.Run("<r><l/></r>");

        var filled = run.Source.TryGetTypedValues(Node(run.Element("l")), out var values);

        await Assert.That(filled).IsTrue();
        await Assert.That(values.IsEmpty).IsTrue();
    }

    [Test]
    public async Task TryGetTypedValues_InvalidNode_YieldsTheUntypedAtomicValue()
    {
        var run = AnnotationFixture.Run("<r><i>abc</i></r>");

        _ = run.Source.TryGetTypedValues(Node(run.Element("i")), out var values);

        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task TryGetTypedValues_NodeThatIsNotAnnotated_LeavesTheSlotEmpty()
    {
        var run = AnnotationFixture.Run("<r><i>1</i></r>");

        var filled = run.Source.TryGetTypedValues(Node(run.Tree.Document), out var values);

        await Assert.That(filled).IsFalse();
        await Assert.That(values.IsEmpty).IsTrue();
    }

    [Test]
    public async Task HasTypedValue_ElementOnlyContent_IsFalse_AndOtherNodesHaveOne()
    {
        var run = AnnotationFixture.Run("<r><c><n>2</n></c><i>1</i></r>");
        var elementOnly = Node(run.Element("c"));

        await Assert.That(run.Source.TryGetTypedValues(elementOnly, out var values)).IsFalse();
        await Assert.That(values.IsEmpty).IsTrue();
        await Assert.That(run.Source.HasTypedValue(elementOnly)).IsFalse();
        await Assert.That(run.Source.HasTypedValue(Node(run.Element("i")))).IsTrue();
        await Assert.That(run.Source.HasTypedValue(Node(run.Tree.Document))).IsTrue();
    }

    [Test]
    public async Task Data_ListWithSeveralItems_AtomizesToAllItems()
    {
        var run = AnnotationFixture.Run("<r><l>1 2</l><i>3</i></r>");
        var sequence = XdmSequence.Create([
            XdmItem.FromNode(Node(run.Element("l"))),
            XdmItem.FromNode(Node(run.Element("i"))),
        ]);

        var data = run.Source.Data(sequence);

        await Assert.That(data.Count).IsEqualTo(3);
    }

    [Test]
    public async Task IsNilled_ReflectsTheRecord()
    {
        var table = new AnnotationTable();
        var tree = AnnotationFixture.AnnotatedRun.Build("<r><a/></r>");
        table.Element(new AnnotationRecord(1, -1, SchemaFixture.Simple("integer"), XsdValidity.Valid, true));
        table.Element(new AnnotationRecord(2, -1, SchemaFixture.Simple("integer"), XsdValidity.Valid, false));
        var source = new AnnotationSource(table, tree, new ScriptedAssessor());
        var root = tree.Document.FirstChild;

        await Assert.That(source.IsNilled(Node(root))).IsTrue();
        await Assert.That(source.IsNilled(Node(root.FirstChild))).IsFalse();
        await Assert.That(source.IsNilled(Node(tree.Document))).IsFalse();
    }
}
