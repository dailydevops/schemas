namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Input.Sequence;

/// <summary>The piece reader of the segment cursor: its bound is a precondition, so a bad one fails instead of looping.</summary>
public class XmlSegmentCursorTests
{
    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task TryRead_MaxBelowOne_Throws(int max)
    {
        var cursor = new XmlSegmentCursor<char>(SequenceBuilder.Chunks("abc".ToCharArray(), 2));

        await Assert.That(() => Read(ref cursor, max)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TryRead_MaxOne_WalksEveryElement()
    {
        var cursor = new XmlSegmentCursor<char>(SequenceBuilder.Chunks("abc".ToCharArray(), 2));
        var read = string.Empty;

        while (cursor.TryRead(1, out var piece))
        {
            read += piece.Span.ToString();
        }

        await Assert.That(read).IsEqualTo("abc");
    }

    private static bool Read(ref XmlSegmentCursor<char> cursor, int max) => cursor.TryRead(max, out _);
}
