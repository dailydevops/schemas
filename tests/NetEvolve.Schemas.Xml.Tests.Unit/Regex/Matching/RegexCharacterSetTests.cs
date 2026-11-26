namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Matching;

using NetEvolve.Schemas.Xml.Regex.Matching;

public class RegexCharacterSetTests
{
    [Test]
    public async Task Contains_AgreesWithTheRangePairsAcrossTheAsciiBoundary()
    {
        int[] ranges = [0, 0, 5, 9, 63, 65, 126, 130, 0x10000, 0x10FFFF];
        var set = new RegexCharacterSet(ranges);

        for (var codePoint = -1; codePoint < 300; codePoint++)
        {
            var expected = false;
            for (var i = 0; i < ranges.Length; i += 2)
            {
                expected |= codePoint >= ranges[i] && codePoint <= ranges[i + 1];
            }

            await Assert.That(set.Contains(codePoint)).IsEqualTo(expected);
        }

        await Assert.That(set.Contains(0xFFFF)).IsFalse();
        await Assert.That(set.Contains(0x10000)).IsTrue();
        await Assert.That(set.Contains(0x110000)).IsFalse();
        await Assert.That(set.Contains(int.MaxValue)).IsFalse();
        await Assert.That(set.Contains(int.MinValue)).IsFalse();
    }

    [Test]
    public async Task Contains_AnAllAsciiSetRejectsEverythingAbove()
    {
        var set = new RegexCharacterSet([0, 127]);

        await Assert.That(set.Contains(127)).IsTrue();
        await Assert.That(set.Contains(128)).IsFalse();
    }
}
