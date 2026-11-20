namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;

public class RegexOptionsParserTests
{
    [Test]
    [Arguments("", RegexOptions.None)]
    [Arguments("s", RegexOptions.DotAll)]
    [Arguments("m", RegexOptions.MultiLine)]
    [Arguments("i", RegexOptions.IgnoreCase)]
    [Arguments("x", RegexOptions.FreeSpacing)]
    public async Task TryParse_EachFlagAlone(string letters, RegexOptions expected)
    {
        var error = RegexOptionsParser.TryParse(letters, out var flags, out var position);

        await Assert.That(error).IsEqualTo(RegexError.None);
        await Assert.That(flags).IsEqualTo(expected);
        await Assert.That(position).IsEqualTo(-1);
    }

    [Test]
    [Arguments("sm", RegexOptions.DotAll | RegexOptions.MultiLine)]
    [Arguments("ms", RegexOptions.DotAll | RegexOptions.MultiLine)]
    [Arguments("si", RegexOptions.DotAll | RegexOptions.IgnoreCase)]
    [Arguments("sx", RegexOptions.DotAll | RegexOptions.FreeSpacing)]
    [Arguments("mi", RegexOptions.MultiLine | RegexOptions.IgnoreCase)]
    [Arguments("mx", RegexOptions.MultiLine | RegexOptions.FreeSpacing)]
    [Arguments("ix", RegexOptions.IgnoreCase | RegexOptions.FreeSpacing)]
    [Arguments("smi", RegexOptions.DotAll | RegexOptions.MultiLine | RegexOptions.IgnoreCase)]
    [Arguments("xims", XPathRegex.All)]
    [Arguments("smix", XPathRegex.All)]
    [Arguments("ssmmiixx", XPathRegex.All)]
    [Arguments("iiii", RegexOptions.IgnoreCase)]
    public async Task TryParse_InAnyOrderAndRepeated(string letters, RegexOptions expected)
    {
        var error = RegexOptionsParser.TryParse(letters, out var flags, out _);

        await Assert.That(error).IsEqualTo(RegexError.None);
        await Assert.That(flags).IsEqualTo(expected);
    }

    // The flag q of the later Functions and Operators editions is deferred by the approved design (AP-13 A, #404).
    [Test]
    [Arguments("q", 0)]
    [Arguments("iq", 1)]
    [Arguments("qs", 0)]
    [Arguments("a", 0)]
    [Arguments("S", 0)]
    [Arguments("s m", 1)]
    [Arguments("smixz", 4)]
    [Arguments("é", 0)]
    public async Task TryParse_InvalidLetter_IsACodedErrorAtItsIndex(string letters, int index)
    {
        var error = RegexOptionsParser.TryParse(letters, out var flags, out var position);

        await Assert.That(error).IsEqualTo(RegexError.InvalidFlag);
        await Assert.That(position).IsEqualTo(index);
        await Assert.That(flags).IsEqualTo(RegexOptions.None);
    }
}
