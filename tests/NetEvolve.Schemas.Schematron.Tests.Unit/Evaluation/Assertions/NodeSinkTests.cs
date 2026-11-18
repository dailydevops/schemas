namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The sink that counts the items of a subject or of the path of a name.</summary>
public class NodeSinkTests
{
    [Test]
    public async Task Kind_IsItems()
    {
        var sink = new NodeSink();

        await Assert.That(sink.Kind).IsEqualTo(SequenceSinkKind.Items);
    }

    [Test]
    public async Task Item_FirstNode_IsKept_AndEveryItemCounts()
    {
        var sink = new NodeSink();
        var first = AssertionHarness.NodeItem("order1");
        var second = AssertionHarness.NodeItem("order2");

        sink.Item(in first);
        sink.Item(in second);

        await Assert.That(sink.Count).IsEqualTo(2);
        await Assert.That(sink.FirstIsNode).IsTrue();
        await Assert.That(sink.First).IsEqualTo(AssertionTree.Node("order1"));
    }

    [Test]
    public async Task Item_FirstAtomic_IsNotANode()
    {
        var sink = new NodeSink();
        var atomic = XdmItem.FromString("x");

        sink.Item(in atomic);

        await Assert.That(sink.Count).IsEqualTo(1);
        await Assert.That(sink.FirstIsNode).IsFalse();
    }

    [Test]
    public async Task Text_NonEmpty_CountsAsOneValue()
    {
        var sink = new NodeSink();

        sink.Text("abc".AsSpan());

        await Assert.That(sink.Count).IsEqualTo(1);
        await Assert.That(sink.FirstIsNode).IsFalse();
    }

    [Test]
    public async Task Text_Empty_CountsNothing()
    {
        var sink = new NodeSink();

        sink.Text(ReadOnlySpan<char>.Empty);

        await Assert.That(sink.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Reset_ForgetsWhatWasPushed()
    {
        var sink = new NodeSink();
        var node = AssertionHarness.NodeItem("order1");
        sink.Item(in node);

        sink.Reset();

        await Assert.That(sink.Count).IsEqualTo(0);
        await Assert.That(sink.FirstIsNode).IsFalse();
    }

    [Test]
    public async Task Content_Throws()
    {
        var sink = new NodeSink();

        await Assert.That(() => sink.Content).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task BeginTree_Throws()
    {
        var sink = new NodeSink();

        await Assert.That(() => sink.BeginTree()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task EndTree_Throws()
    {
        var sink = new NodeSink();

        await Assert.That(() => sink.EndTree(null!)).Throws<InvalidOperationException>();
    }
}
