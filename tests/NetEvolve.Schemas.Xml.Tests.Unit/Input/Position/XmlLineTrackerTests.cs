namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Position;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Input.Position;

public class XmlLineTrackerTests
{
    private static string Normalize(ref XmlLineTracker tracker, string input)
    {
        var destination = new char[input.Length];
        var written = tracker.Normalize(input.AsSpan(), destination);
        return new string(destination, 0, written);
    }

    [Test]
    [Arguments("a\rb", "a\nb")]
    [Arguments("a\nb", "a\nb")]
    [Arguments("a\r\nb", "a\nb")]
    public async Task Normalize_CrLfAndCrLf_ProduceOneLineFeedAndOneLine(string input, string expected)
    {
        var tracker = new XmlLineTracker();

        var result = Normalize(ref tracker, input);

        await Assert.That(result).IsEqualTo(expected);
        await Assert.That(tracker.Position.Line).IsEqualTo(2);
        await Assert.That(tracker.Position.Column).IsEqualTo(2);
        await Assert.That(tracker.Position.CharOffset).IsEqualTo(input.Length);
    }

    [Test]
    public async Task Normalize_CrLfSplitBetweenChunks_CountsOneLineBreak()
    {
        var tracker = new XmlLineTracker();

        var first = Normalize(ref tracker, "a\r");
        var second = Normalize(ref tracker, "\nb");

        await Assert.That(first + second).IsEqualTo("a\nb");
        await Assert.That(tracker.Position.Line).IsEqualTo(2);
        await Assert.That(tracker.Position.Column).IsEqualTo(2);
        await Assert.That(tracker.Position.CharOffset).IsEqualTo(4);
    }

    [Test]
    public async Task Normalize_CrAtChunkEndFollowedByOtherChar_CountsOneLineBreak()
    {
        var tracker = new XmlLineTracker();

        var result = Normalize(ref tracker, "a\r") + Normalize(ref tracker, "b");

        await Assert.That(result).IsEqualTo("a\nb");
        await Assert.That(tracker.Position.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Normalize_EmptyChunkBetweenCrAndLf_KeepsPendingCr()
    {
        var tracker = new XmlLineTracker();

        _ = Normalize(ref tracker, "\r");
        _ = Normalize(ref tracker, string.Empty);
        var result = Normalize(ref tracker, "\n");

        await Assert.That(result).IsEqualTo(string.Empty);
        await Assert.That(tracker.Position.Line).IsEqualTo(2);
        await Assert.That(tracker.Position.CharOffset).IsEqualTo(2);
    }

    [Test]
    public async Task Normalize_DoubleCrAndCrCrLf_CountEachBreakOnce()
    {
        var tracker = new XmlLineTracker();

        var result = Normalize(ref tracker, "a\r\rb\r\r\nc");

        await Assert.That(result).IsEqualTo("a\n\nb\n\nc");
        await Assert.That(tracker.Position.Line).IsEqualTo(5);
        await Assert.That(tracker.Position.Column).IsEqualTo(2);
    }

    [Test]
    public async Task Position_AfterMixedLineEnds_PointsAtMarker()
    {
        var tracker = new XmlLineTracker();
        const string input = "ab\r\ncd\ref\n!";

        var result = Normalize(ref tracker, input);

        await Assert.That(result).IsEqualTo("ab\ncd\nef\n!");
        await Assert.That(tracker.Position.Line).IsEqualTo(4);
        await Assert.That(tracker.Position.Column).IsEqualTo(2);
        await Assert.That(tracker.Position.CharOffset).IsEqualTo(input.Length);
    }

    [Test]
    public async Task Position_SameForEveryChunking()
    {
        const string input = "ab\r\ncd\ref\n\r\n\rg\th\r";
        var whole = new XmlLineTracker();
        var wholeText = Normalize(ref whole, input);

        for (var size = 1; size < input.Length; size++)
        {
            var chunked = new XmlLineTracker();
            var text = string.Empty;
            for (var i = 0; i < input.Length; i += size)
            {
                text += Normalize(ref chunked, input.Substring(i, Math.Min(size, input.Length - i)));
            }

            await Assert.That(text).IsEqualTo(wholeText);
            await Assert.That(chunked.Position).IsEqualTo(whole.Position);
        }
    }

    [Test]
    public async Task Position_CountsUtf16CodeUnits_NotCodePoints()
    {
        var tracker = new XmlLineTracker();

        _ = Normalize(ref tracker, "a\U0001F600b");

        await Assert.That(tracker.Position.Column).IsEqualTo(5);
        await Assert.That(tracker.Position.CharOffset).IsEqualTo(4);
    }

    [Test]
    public async Task Position_Tab_CountsAsOneColumn()
    {
        var tracker = new XmlLineTracker();

        _ = Normalize(ref tracker, "\t\tx");

        await Assert.That(tracker.Position.Column).IsEqualTo(4);
    }

    [Test]
    public async Task Position_Initial_IsLineOneColumnOneOffsetZero()
    {
        var tracker = new XmlLineTracker();

        await Assert.That(tracker.Position).IsEqualTo(new Schemas.Xml.Contracts.XmlPosition(1, 1, 0));
    }

    [Test]
    public async Task Normalize_DestinationTooSmall_Throws()
    {
        var tracker = new XmlLineTracker();

        await Assert.That(() => tracker.Normalize("abc".AsSpan(), new char[2])).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Normalize_InputWithoutCr_AllocatesNothing()
    {
        var input = new string('a', 64) + "\n" + new string('b', 64);
        var destination = new char[input.Length];

        AllocationAssert.NoAllocation(() =>
        {
            var tracker = new XmlLineTracker();
            _ = tracker.Normalize(input.AsSpan(), destination);
        });
    }
}
