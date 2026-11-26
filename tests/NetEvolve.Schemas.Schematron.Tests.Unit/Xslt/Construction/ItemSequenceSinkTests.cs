namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class ItemSequenceSinkTests
{
    [Test]
    public async Task Kind_IsItems() =>
        await Assert.That(new ItemSequenceSink(SinkTestSupport.NewTable()).Kind).IsEqualTo(SequenceSinkKind.Items);

    [Test]
    public async Task Items_NodeIsKeptByReference()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());
        var element = SinkTestSupport.Element("a", "x");

        sink.Item(in element);

        var result = sink.Result;
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(SinkTestSupport.Node(result[0])).IsEqualTo(SinkTestSupport.Node(element));
    }

    [Test]
    public async Task Items_AdjacentText_CoalescesToOneTextNode()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        sink.Text("a");
        sink.Text("b");
        sink.Text("");

        var result = sink.Result;
        var node = SinkTestSupport.Node(result[0]);
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(node.Kind).IsEqualTo(XmlNodeKind.Text);
        await Assert.That(node.StringValue).IsEqualTo("ab");
    }

    [Test]
    public async Task Items_EmptyText_CreatesNothing()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        sink.Text("");

        await Assert.That(sink.Result.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Items_TextBetweenItems_KeepsOrderAndAtomicsUnseparated()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());
        var one = XdmItem.FromInteger(1);
        var two = XdmItem.FromInteger(2);

        sink.Item(in one);
        sink.Text("x");
        sink.Item(in two);

        var result = sink.Result;
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(SinkTestSupport.Node(result[1]).StringValue).IsEqualTo("x");
        await Assert.That(result[2]).IsEqualTo(two);
    }

    [Test]
    public async Task Items_TwoTextNodes_HaveDistinctIdentity()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());
        var one = XdmItem.FromInteger(1);

        sink.Text("a");
        sink.Item(in one);
        sink.Text("a");

        var result = sink.Result;
        await Assert.That(SinkTestSupport.Node(result[0])).IsNotEqualTo(SinkTestSupport.Node(result[2]));
    }

    [Test]
    public async Task Items_BeginEndTree_ReturnsDocumentNode()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        var tree = sink.BeginTree();
        tree.Text("t", XmlTextKind.Text);
        var document = sink.EndTree(tree);

        var node = SinkTestSupport.Node(document);
        await Assert.That(node.Kind).IsEqualTo(XmlNodeKind.Document);
        await Assert.That(node.StringValue).IsEqualTo("t");
    }

    [Test]
    public async Task Content_ThrowsOutsideTreeKind()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        await Assert.That(() => sink.Content).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Items_LongText_GrowsTheBuffer()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());
        var text = new string('x', 100);

        sink.Text(text);
        sink.Text(text);

        await Assert.That(SinkTestSupport.Node(sink.Result[0]).StringValue).IsEqualTo(text + text);
    }

    [Test]
    public async Task Reset_ForgetsItemsAndPendingText_AndKeepsTheSequenceReadBefore()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());
        var one = XdmItem.FromInteger(1);
        var two = XdmItem.FromInteger(2);
        sink.Item(in one);
        sink.Item(in two);
        var before = sink.Result;
        sink.Text("pending");

        sink.Reset();
        sink.Item(in two);

        var after = sink.Result;
        await Assert.That(before.Count).IsEqualTo(2);
        await Assert.That(before[0]).IsEqualTo(one);
        await Assert.That(after.Count).IsEqualTo(1);
        await Assert.That(after[0]).IsEqualTo(two);
    }

    [Test]
    public async Task Reset_OfAnEmptySink_StaysEmpty()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        sink.Reset();

        await Assert.That(sink.Result.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Items_MoreItemsThanTheInitialCapacity_AreAllKept()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        for (var i = 0; i < 9; i++)
        {
            var item = XdmItem.FromInteger(i);
            sink.Item(in item);
        }

        await Assert.That(sink.Result.Count).IsEqualTo(9);
    }
}
