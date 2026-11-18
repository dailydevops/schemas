namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The adapter that joins the items of one value-of with one space.</summary>
public class ValueOfSinkTests
{
    [Test]
    public async Task Kind_IsText()
    {
        var sink = new ValueOfSink();

        await Assert.That(sink.Kind).IsEqualTo(SequenceSinkKind.Text);
    }

    [Test]
    public async Task Item_JoinsTheItemsWithOneSpace_AndBeginStartsAgain()
    {
        using var into = new TextSink(100);
        var sink = new ValueOfSink();
        var one = XdmItem.FromInteger(1);
        var two = XdmItem.FromInteger(2);

        sink.Begin(into);
        sink.Item(in one);
        sink.Item(in two);
        sink.Begin(into);
        sink.Item(in one);

        await Assert.That(into.ToString()).IsEqualTo("1 21");
    }

    [Test]
    public async Task Text_IsHandedToTheTextSink()
    {
        using var into = new TextSink(100);
        var sink = new ValueOfSink();

        sink.Begin(into);
        sink.Text("abc".AsSpan());

        await Assert.That(into.ToString()).IsEqualTo("abc");
    }

    [Test]
    public async Task Content_Throws()
    {
        var sink = new ValueOfSink();

        await Assert.That(() => sink.Content).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task BeginTree_Throws()
    {
        var sink = new ValueOfSink();

        await Assert.That(() => sink.BeginTree()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task EndTree_Throws()
    {
        var sink = new ValueOfSink();

        await Assert.That(() => sink.EndTree(null!)).Throws<InvalidOperationException>();
    }
}
