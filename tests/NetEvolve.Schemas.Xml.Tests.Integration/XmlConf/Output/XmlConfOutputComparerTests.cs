namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Output;

using System.Text;

public class XmlConfOutputComparerTests
{
    private static string? Compare(string expected, string actual) =>
        XmlConfOutputComparer.Compare(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));

    [Test]
    public async Task Compare_EqualBytes_ReturnsNull()
    {
        await Assert.That(Compare("<doc a=\"1\">é</doc>", "<doc a=\"1\">é</doc>")).IsNull();
        await Assert.That(Compare(string.Empty, string.Empty)).IsNull();
    }

    [Test]
    public async Task Compare_OneByteDifference_NamesTheOffsetAndBothBytes()
    {
        var message = Compare("<doc a=\"1\"></doc>", "<doc a=\"2\"></doc>");

        await Assert.That(message).IsEqualTo("The output differs at offset 8: expected 0x31 but was 0x32.");
    }

    [Test]
    public async Task Compare_ActualEndsEarly_NamesTheOffsetWhereItEnds()
    {
        var message = Compare("<doc></doc>", "<doc></doc");

        await Assert.That(message).IsEqualTo("The output differs at offset 10: the actual output ends, expected 0x3E.");
    }

    [Test]
    public async Task Compare_ActualContinues_NamesTheOffsetWhereTheExpectedOutputEnds()
    {
        var message = Compare("<doc></doc>", "<doc></doc>\n");

        await Assert.That(message).IsEqualTo("The output differs at offset 11: the expected output ends, actual 0x0A.");
    }

    [Test]
    public async Task Compare_DifferenceInTheMiddleOfAMultiByteCharacter_ReportsTheByteOffset()
    {
        // "é" is 0xC3 0xA9; the second byte differs, so the byte offset is 6 and not the character offset 5.
        var message = XmlConfOutputComparer.Compare(
            [.. "<doc>"u8, 0xC3, 0xA9, .. "</doc>"u8],
            [.. "<doc>"u8, 0xC3, 0xA8, .. "</doc>"u8]
        );

        await Assert.That(message).IsEqualTo("The output differs at offset 6: expected 0xA9 but was 0xA8.");
    }
}
