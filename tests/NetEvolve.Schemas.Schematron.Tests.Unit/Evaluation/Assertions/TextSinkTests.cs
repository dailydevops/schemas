namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The text-only sink of design #150 EP-8, examples S2 and S6.</summary>
public class TextSinkTests
{
    private static XdmItem Item(long value) => XdmItem.FromInteger(value);

    [Test]
    public async Task Kind_IsText()
    {
        using var sink = new TextSink(100);

        await Assert.That(sink.Kind).IsEqualTo(SequenceSinkKind.Text);
    }

    [Test]
    public async Task Text_AdjacentCalls_AreAppended()
    {
        using var sink = new TextSink(100);

        sink.Text("ab".AsSpan());
        sink.Text("cd".AsSpan());
        sink.Text(ReadOnlySpan<char>.Empty);

        await Assert.That(sink.ToString()).IsEqualTo("abcd");
    }

    [Test]
    public async Task Text_AtomicValues_AreSpaceSeparated_LikeTree()
    {
        using var sink = new TextSink(100);
        var one = Item(1);
        var two = Item(2);

        sink.Item(in one);
        sink.Item(in two);

        await Assert.That(sink.ToString()).IsEqualTo("1 2");
    }

    [Test]
    public async Task Text_AtomicKinds_ConvertToTheirLexicalForm()
    {
        using var sink = new TextSink(100);
        var text = XdmItem.FromString("s");
        var yes = XdmItem.FromBoolean(true);
        var no = XdmItem.FromBoolean(false);
        var minus = Item(-12);

        sink.Item(in text);
        sink.Item(in yes);
        sink.Item(in no);
        sink.Item(in minus);

        await Assert.That(sink.ToString()).IsEqualTo("s true false -12");
        await Assert.That(sink.Failure).IsNull();
    }

    [Test]
    public async Task Text_TextCall_ResetsAdjacency()
    {
        using var sink = new TextSink(100);
        var one = Item(1);
        var two = Item(2);

        sink.Item(in one);
        sink.Text("x".AsSpan());
        sink.Item(in two);

        await Assert.That(sink.ToString()).IsEqualTo("1x2");
    }

    [Test]
    public async Task Text_Node_AppendsStringValue()
    {
        using var sink = new TextSink(100);
        var element = AssertionHarness.NodeItem("item1");
        var attribute = AssertionHarness.NodeItem("id");
        var group = AssertionHarness.NodeItem("order1");

        sink.Item(in element);
        sink.Item(in attribute);
        sink.Item(in group);

        await Assert.That(sink.ToString()).IsEqualTo("17txt12");
    }

    [Test]
    public async Task Text_NodeAfterAtomic_IsNotSeparated_AndResetsAdjacency()
    {
        using var sink = new TextSink(100);
        var one = Item(1);
        var element = AssertionHarness.NodeItem("item2");
        var three = Item(3);

        sink.Item(in one);
        sink.Item(in element);
        sink.Item(in three);

        await Assert.That(sink.ToString()).IsEqualTo("123");
    }

    [Test]
    public async Task Text_UnsupportedAtomicKind_RecordsFailure_AndAppendsNothing()
    {
        using var sink = new TextSink(100);
        var number = XdmItem.FromDouble(1.5);

        sink.Text("a".AsSpan());
        sink.Item(in number);

        await Assert.That(sink.ToString()).IsEqualTo("a");
        await Assert.That(sink.Failure).IsNotNull();
    }

    [Test]
    public async Task Text_BeyondMaximum_IsCutAndFlagged()
    {
        using var sink = new TextSink(3);

        sink.Text("ab".AsSpan());
        sink.Text("cdef".AsSpan());
        sink.Text("g".AsSpan());

        await Assert.That(sink.ToString()).IsEqualTo("abc");
        await Assert.That(sink.IsTruncated).IsTrue();
    }

    [Test]
    public async Task Text_AtMaximum_IsNotTruncated()
    {
        using var sink = new TextSink(3);

        sink.Text("abc".AsSpan());

        await Assert.That(sink.IsTruncated).IsFalse();
    }

    [Test]
    public async Task Clear_EmptiesTheSink_AndResetsFlags()
    {
        using var sink = new TextSink(2);
        var number = XdmItem.FromDouble(1.5);
        sink.Text("abc".AsSpan());
        sink.Item(in number);

        sink.Clear();

        await Assert.That(sink.Written.Length).IsEqualTo(0);
        await Assert.That(sink.IsTruncated).IsFalse();
        await Assert.That(sink.Failure).IsNull();
    }

    [Test]
    public async Task Written_IsTheSameTextAsToString()
    {
        using var sink = new TextSink(100);

        sink.Text("hello".AsSpan());

        await Assert.That(sink.Written.ToString()).IsEqualTo("hello");
    }

    [Test]
    public async Task Text_BeginTree_Throws()
    {
        using var sink = new TextSink(100);

        await Assert.That(() => sink.BeginTree()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Text_EndTree_Throws()
    {
        using var sink = new TextSink(100);

        await Assert.That(() => sink.EndTree(null!)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Text_Content_Throws()
    {
        using var sink = new TextSink(100);

        await Assert.That(() => sink.Content).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Constructor_NegativeMaximum_Throws() =>
        await Assert.That(() => new TextSink(-1)).Throws<ArgumentOutOfRangeException>();
}
