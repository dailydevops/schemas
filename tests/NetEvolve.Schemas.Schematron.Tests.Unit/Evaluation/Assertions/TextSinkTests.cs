namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
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
    [Arguments("1.50", "1.5")]
    [Arguments("-0.25", "-0.25")]
    [Arguments("7", "7")]
    [Arguments("0", "0")]
    public async Task Text_Decimal_ConvertsToItsCanonicalForm(string lexical, string expected)
    {
        using var sink = new TextSink(100);
        var number = XdmItem.FromDecimal(XsdDecimal.Parse(lexical.AsSpan()).Value);

        sink.Item(in number);

        await Assert.That(sink.ToString()).IsEqualTo(expected);
        await Assert.That(sink.Failure).IsNull();
    }

    [Test]
    [Arguments(0L, "0")]
    [Arguments(42L, "42")]
    [Arguments(-12L, "-12")]
    [Arguments(long.MaxValue, "9223372036854775807")]
    [Arguments(long.MinValue, "-9223372036854775808")]
    public async Task Text_Integer_ConvertsEveryEdge(long value, string expected)
    {
        using var viaItem = new TextSink(100);
        using var direct = new TextSink(100);
        var number = Item(value);

        viaItem.Item(in number);
        direct.Integer(value);

        await Assert.That(viaItem.ToString()).IsEqualTo(expected);
        await Assert.That(direct.ToString()).IsEqualTo(expected);
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
    public async Task Text_ElementAndDocument_AppendTheirDescendantText_WithoutComments()
    {
        using var sink = new TextSink(100);
        var root = AssertionHarness.NodeItem("root");
        var document = AssertionHarness.NodeItem("doc");

        sink.Item(in root);
        sink.Text("|".AsSpan());
        sink.Item(in document);

        await Assert.That(sink.ToString()).IsEqualTo("txt12|txt12");
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
    public async Task Text_AtomicWithoutCastToString_RecordsFailure_AndAppendsNothing()
    {
        using var sink = new TextSink(100);
        var none = default(XdmItem);

        sink.Text("a".AsSpan());
        sink.Item(in none);

        await Assert.That(sink.ToString()).IsEqualTo("a");
        await Assert.That(sink.Failure).IsNotNull();
    }

    [Test]
    [Arguments(1.5, "1.5")]
    [Arguments(1e21, "1.0E21")]
    [Arguments(double.NaN, "NaN")]
    [Arguments(double.PositiveInfinity, "INF")]
    [Arguments(double.NegativeInfinity, "-INF")]
    [Arguments(-0.0, "-0")]
    public async Task Text_Double_UsesTheCanonicalForm(double value, string expected)
    {
        using var sink = new TextSink(100);
        var item = XdmItem.FromDouble(value);

        sink.Item(in item);

        await Assert.That(sink.ToString()).IsEqualTo(expected);
        await Assert.That(sink.Failure).IsNull();
    }

    [Test]
    [Arguments(1.5f, "1.5")]
    [Arguments(1e10f, "1.0E10")]
    [Arguments(float.NaN, "NaN")]
    [Arguments(float.NegativeInfinity, "-INF")]
    public async Task Text_Float_UsesTheCanonicalForm(float value, string expected)
    {
        using var sink = new TextSink(100);
        var item = XdmItem.FromFloat(value);

        sink.Item(in item);

        await Assert.That(sink.ToString()).IsEqualTo(expected);
    }

    [Test]
    [Arguments(BinaryKind.HexBinary, "0AFF", "0AFF")]
    [Arguments(BinaryKind.Base64Binary, "AQID", "AQID")]
    public async Task Text_Binary_UsesTheCanonicalForm(BinaryKind kind, string lexical, string expected)
    {
        using var sink = new TextSink(100);
        var item = XdmItem.FromBinary(XsdBinary.Parse(lexical.AsSpan(), kind).Value, kind);

        sink.Item(in item);

        await Assert.That(sink.ToString()).IsEqualTo(expected);
    }

    [Test]
    [Arguments(XsdDateTimeKind.GYear, "2024")]
    [Arguments(XsdDateTimeKind.GMonth, "--05")]
    [Arguments(XsdDateTimeKind.GDay, "---07")]
    [Arguments(XsdDateTimeKind.GYearMonth, "2024-05")]
    [Arguments(XsdDateTimeKind.GMonthDay, "--05-07")]
    public async Task Text_Gregorian_UsesTheCanonicalForm(XsdDateTimeKind kind, string lexical)
    {
        using var sink = new TextSink(100);
        var item = XdmItem.FromTemporal(XsdDateTime.Parse(lexical.AsSpan(), kind, XsdVersion.V10).Value);

        sink.Item(in item);

        await Assert.That(sink.ToString()).IsEqualTo(lexical);
    }

    [Test]
    public async Task Text_QName_UsesThePrefixedForm()
    {
        using var sink = new TextSink(100);
        var name = XsdQName.Parse("b:local".AsSpan(), new Bindings()).Value;
        var item = XdmItem.FromQName(new XdmQName(name, "b"));

        sink.Item(in item);

        await Assert.That(sink.ToString()).IsEqualTo("b:local");
    }

    [Test]
    public async Task Text_ConvertedKinds_AreSpaceSeparated()
    {
        using var sink = new TextSink(100);
        var one = Item(1);
        var two = XdmItem.FromDouble(2.5);

        sink.Item(in one);
        sink.Item(in two);

        await Assert.That(sink.ToString()).IsEqualTo("1 2.5");
    }

    private sealed class Bindings : INamespaceContext
    {
        public bool TryResolvePrefix(ReadOnlySpan<char> name, out string? resolved)
        {
            resolved = name.SequenceEqual("b".AsSpan()) ? "urn:b" : null;
            return resolved is not null;
        }
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
    public async Task Text_CutBetweenSurrogates_BacksOffTheHighSurrogate()
    {
        using var sink = new TextSink(3);

        sink.Text("ab😀".AsSpan());

        await Assert.That(sink.ToString()).IsEqualTo("ab");
        await Assert.That(sink.IsTruncated).IsTrue();
    }

    [Test]
    public async Task Text_SurrogatePairThatFits_IsKept()
    {
        using var sink = new TextSink(4);

        sink.Text("ab😀".AsSpan());

        await Assert.That(sink.ToString()).IsEqualTo("ab😀");
        await Assert.That(sink.IsTruncated).IsFalse();
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
        var number = default(XdmItem);
        sink.Text("abc".AsSpan());
        sink.Item(in number);

        sink.Clear();

        await Assert.That(sink.Written.Length).IsEqualTo(0);
        await Assert.That(sink.IsTruncated).IsFalse();
        await Assert.That(sink.Failure).IsNull();
    }

    [Test]
    public async Task Rewind_DropsTheTextWrittenAfterTheMark_AndResetsAdjacency()
    {
        using var sink = new TextSink(100);
        var one = Item(1);
        var two = Item(2);
        sink.Text("ab".AsSpan());
        var mark = sink.Length;
        sink.Item(in one);

        sink.Rewind(mark);
        sink.Item(in two);

        await Assert.That(mark).IsEqualTo(2);
        await Assert.That(sink.ToString()).IsEqualTo("ab2");
    }

    [Test]
    public async Task Rewind_OutsideTheWrittenText_Throws()
    {
        using var sink = new TextSink(100);
        sink.Text("ab".AsSpan());

        await Assert.That(() => sink.Rewind(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => sink.Rewind(3)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Written_IsTheSameTextAsToString()
    {
        using var sink = new TextSink(100);

        sink.Text("hello".AsSpan());

        await Assert.That(sink.Written.ToString()).IsEqualTo("hello");
    }

    [Test]
    public async Task Text_NamespaceNode_AppendsItsUri()
    {
        using var sink = new TextSink(100);
        var node = AssertionHarness.NodeItem("ns");

        sink.Item(in node);

        await Assert.That(sink.ToString()).IsEqualTo("urn:p");
    }

    [Test]
    public async Task Text_GrowingPastTheFirstBuffer_KeepsTheEarlierText()
    {
        using var sink = new TextSink(1000);
        var first = new string('a', 200);
        var second = new string('b', 100);
        var third = new string('c', 700);

        sink.Text(first.AsSpan());
        sink.Text(second.AsSpan());
        sink.Text(third.AsSpan());

        await Assert.That(sink.ToString()).IsEqualTo(first + second + third);
        await Assert.That(sink.IsTruncated).IsFalse();
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
