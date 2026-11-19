namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Sequence;

/// <summary>The fill step of the sequence feeders: the wanted count is a precondition, so a bad one fails instead of looping.</summary>
public class XmlSequenceFeederTests
{
    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task Fill_Chars_WantedBelowOne_Throws(int wanted)
    {
        using var feeder = new XmlCharSequenceFeeder(
            SequenceBuilder.Chunks("<a/>".ToCharArray(), 2),
            XmlReaderOptions.Default,
            null
        );

        await Assert.That(() => feeder.Fill(wanted)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task Fill_Bytes_WantedBelowOne_Throws(int wanted)
    {
        using var feeder = new XmlByteSequenceFeeder(
            SequenceBuilder.Chunks("<a/>"u8.ToArray(), 2),
            XmlReaderOptions.Default,
            null,
            null
        );

        await Assert.That(() => feeder.Fill(wanted)).Throws<ArgumentOutOfRangeException>();
    }
}
