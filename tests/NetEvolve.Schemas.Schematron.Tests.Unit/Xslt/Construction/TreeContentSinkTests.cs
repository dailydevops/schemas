namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;

using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class TreeContentSinkTests
{
    [Test]
    public async Task Kind_IsTree()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());

        await Assert.That(sink.Kind).IsEqualTo(SequenceSinkKind.Tree);
    }

    [Test]
    public async Task Tree_VariableContent_GivesDocumentNode()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());

        var node = SinkTestSupport.Node(sink.Document);

        await Assert.That(node.Kind).IsEqualTo(XmlNodeKind.Document);
        await Assert.That(node.FirstChild.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Tree_ValueOfPush_GivesOneTextNode()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());

        sink.Text("hello");

        var child = SinkTestSupport.Node(sink.Document).FirstChild;
        await Assert.That(child.Kind).IsEqualTo(XmlNodeKind.Text);
        await Assert.That(child.StringValue).IsEqualTo("hello");
        await Assert.That(child.NextSibling.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Tree_AdjacentText_MergesAndEmptyTextCreatesNone()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());

        sink.Text("a");
        sink.Text("");
        sink.Text("b");

        var child = SinkTestSupport.Node(sink.Document).FirstChild;
        await Assert.That(child.StringValue).IsEqualTo("ab");
        await Assert.That(child.NextSibling.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Tree_AtomicValues_AreSpaceSeparated()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());
        var one = XdmItem.FromInteger(1);
        var two = XdmItem.FromInteger(2);
        var yes = XdmItem.FromBoolean(true);

        sink.Item(in one);
        sink.Item(in two);
        sink.Item(in yes);

        await Assert.That(SinkTestSupport.Node(sink.Document).StringValue).IsEqualTo("1 2 true");
    }

    [Test]
    public async Task Tree_TextCall_ResetsAdjacency()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());
        var one = XdmItem.FromInteger(1);
        var s = XdmItem.FromString("s");

        sink.Item(in one);
        sink.Text("-");
        sink.Item(in s);

        await Assert.That(SinkTestSupport.Node(sink.Document).StringValue).IsEqualTo("1-s");
    }

    [Test]
    public async Task Tree_NodeIsCopiedThroughPump_NewIdentity()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());
        var source = SinkTestSupport.Element("a", "x");

        sink.Item(in source);

        var copy = SinkTestSupport.Node(sink.Document).FirstChild;
        await Assert.That(copy.Kind).IsEqualTo(XmlNodeKind.Element);
        await Assert.That(copy.StringValue).IsEqualTo("x");
        await Assert.That(copy).IsNotEqualTo(SinkTestSupport.Node(source));
        await Assert.That(SinkTestSupport.Node(source).StringValue).IsEqualTo("x");
    }

    [Test]
    public async Task Tree_DocumentNodeContributesItsChildren()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());
        var document = SinkTestSupport.Item(SinkTestSupport.Node(SinkTestSupport.Element("a", "x")).Parent);

        sink.Item(in document);

        var child = SinkTestSupport.Node(sink.Document).FirstChild;
        await Assert.That(child.Kind).IsEqualTo(XmlNodeKind.Element);
    }

    [Test]
    public async Task Tree_AttributeInDocumentContent_Fails()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());
        var attribute = SinkTestSupport.Attribute("id", "1");

        sink.Item(in attribute);

        await Assert.That(sink.Problem).IsNotNull();
        await Assert.That(sink.Problem!.Value.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(sink.Problem!.Value.Constraint).IsEqualTo("XTDE0420");
    }

    [Test]
    public async Task Tree_TwoConstructedTrees_HaveDistinctIdentity()
    {
        var names = SinkTestSupport.NewTable();
        using var first = new TreeContentSink(names);
        using var second = new TreeContentSink(names);
        first.Text("a");
        second.Text("a");

        await Assert.That(SinkTestSupport.Node(first.Document)).IsNotEqualTo(SinkTestSupport.Node(second.Document));
    }

    [Test]
    public async Task Tree_NodesOfOneTree_AreInDocumentOrder()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());
        var source = SinkTestSupport.Element("a", "x");
        sink.Item(in source);
        sink.Text("t");

        var document = SinkTestSupport.Node(sink.Document);
        var element = document.FirstChild;
        var text = element.NextSibling;
        await Assert.That(document.CompareDocumentOrder(element)).IsLessThan(0);
        await Assert.That(element.CompareDocumentOrder(text)).IsLessThan(0);
        await Assert.That(text.CompareDocumentOrder(element)).IsGreaterThan(0);
    }

    [Test]
    public async Task Tree_ContentWithNameAbsentFromInstance_BuildsAndLeavesInstanceTableAlone()
    {
        var instanceNames = SinkTestSupport.NewTable();
        var before = instanceNames.Count;
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());
        var content = sink.Content;
        var name = content.Names.Intern("", "brandNew", "");

        content.StartElement(name, default);
        content.EndAttributes();
        content.EndElement(name);

        await Assert.That(SinkTestSupport.Node(sink.Document).FirstChild.Kind).IsEqualTo(XmlNodeKind.Element);
        await Assert.That(instanceNames.Count).IsEqualTo(before);
        await Assert.That(instanceNames.IsFrozen).IsFalse();
    }

    [Test]
    public async Task Tree_NestedTree_CopiesChildrenWhenPassedToItem()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());

        var nested = sink.BeginTree();
        nested.Text("n", XmlTextKind.Text);
        var document = sink.EndTree(nested);
        sink.Item(in document);

        var child = SinkTestSupport.Node(sink.Document).FirstChild;
        await Assert.That(child.Kind).IsEqualTo(XmlNodeKind.Text);
        await Assert.That(child.StringValue).IsEqualTo("n");
    }

    [Test]
    public async Task Tree_UnsupportedAtomicKind_ReportsProblem()
    {
        using var sink = new TreeContentSink(SinkTestSupport.NewTable());
        var d = XdmItem.FromDouble(1.5);

        sink.Item(in d);

        await Assert.That(sink.Problem).IsNotNull();
    }
}
