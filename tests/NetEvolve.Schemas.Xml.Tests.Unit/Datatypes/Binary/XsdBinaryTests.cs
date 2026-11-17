namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Binary;

using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

public class XsdBinaryTests
{
    private static IDatatypeDescriptor<XsdBinary> Get(string name, XsdVersion version = XsdVersion.V11) =>
        (IDatatypeDescriptor<XsdBinary>)BuiltInDatatypes.For(version).Get(name);

    [Test]
    [Arguments("hexBinary", XsdVersion.V10)]
    [Arguments("hexBinary", XsdVersion.V11)]
    [Arguments("base64Binary", XsdVersion.V10)]
    [Arguments("base64Binary", XsdVersion.V11)]
    public async Task Registry_ContainsTheBinaryTypes_InBothVersions(string name, XsdVersion version)
    {
        await Assert.That(Get(name, version).Name).IsEqualTo(name);
        await Assert.That(Get(name, version).IsOrdered).IsFalse();
    }

    [Test]
    [Arguments("QUJD REVG")]
    [Arguments("Q UJD R EVG")]
    [Arguments("QUJDREVG")]
    public async Task Base64_AcceptsSpacesBetweenCharacters(string lexical)
    {
        var result = Get("base64Binary").Parse(lexical);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.ToString(BinaryKind.Base64Binary)).IsEqualTo("QUJDREVG");
        await Assert.That(result.Value.Length).IsEqualTo(6);
    }

    [Test]
    public async Task Base64_ViaTheDescriptor_CollapsesOtherWhitespace()
    {
        await Assert.That(Get("base64Binary").Parse("  QUJD\t\r\nREVG  ").Success).IsTrue();
        await Assert
            .That(XsdBinary.Parse("QUJD\tREVG", BinaryKind.Base64Binary).Error)
            .IsEqualTo(DatatypeError.InvalidCharacter);
    }

    [Test]
    [Arguments("QUJ", 3)]
    [Arguments("QU", 2)]
    [Arguments("QQ=", 3)]
    [Arguments("QQ===", 4)]
    [Arguments("Q===", 1)]
    [Arguments("====", 0)]
    [Arguments("QR==", 1)]
    [Arguments("QUJ=", 2)]
    [Arguments("QQ==QUJD", 4)]
    [Arguments("QQ=A", 3)]
    public async Task Base64_RejectsWrongPadding(string lexical, int offset)
    {
        var result = Get("base64Binary").Parse(lexical);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(result.ErrorOffset).IsEqualTo(offset);
    }

    [Test]
    [Arguments("QU!D", 2)]
    [Arguments("QUJ-", 3)]
    [Arguments("QU_D", 2)]
    [Arguments("QUJDé", 4)]
    public async Task Base64_RejectsCharactersOutsideTheAlphabet(string lexical, int offset)
    {
        var result = Get("base64Binary").Parse(lexical);

        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(result.ErrorOffset).IsEqualTo(offset);
    }

    [Test]
    public async Task Base64_AcceptsPaddingWithASpaceBetweenTheEqualsSigns()
    {
        var result = Get("base64Binary").Parse("QQ = =");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Base64_DirectParse_RejectsMisplacedSpaces()
    {
        await Assert.That(XsdBinary.Parse(" QUJD", BinaryKind.Base64Binary).Success).IsFalse();
        await Assert.That(XsdBinary.Parse("QUJD ", BinaryKind.Base64Binary).Success).IsFalse();
        await Assert.That(XsdBinary.Parse("QU  JD", BinaryKind.Base64Binary).Success).IsFalse();
    }

    [Test]
    [Arguments("QQ==", 1)]
    [Arguments("QUI=", 2)]
    [Arguments("QUJD", 3)]
    [Arguments("QUJDRA==", 4)]
    [Arguments("QUJD REVGRw==", 7)]
    [Arguments("", 0)]
    public async Task Base64_LengthIsTheOctetCount(string lexical, int octets)
    {
        var value = Get("base64Binary").Parse(lexical).Value;

        await Assert.That(value.Length).IsEqualTo(octets);
        await Assert.That(value.ToArray().Length).IsEqualTo(octets);
    }

    [Test]
    [Arguments("0FB8", 2)]
    [Arguments("0fb8", 2)]
    [Arguments("DEADBEEF", 4)]
    [Arguments("00", 1)]
    [Arguments("", 0)]
    public async Task Hex_LengthIsTheOctetCountNotTheCharacterCount(string lexical, int octets)
    {
        var value = Get("hexBinary").Parse(lexical).Value;

        await Assert.That(value.Length).IsEqualTo(octets);
    }

    [Test]
    [Arguments("0", 1)]
    [Arguments("0FB", 3)]
    [Arguments("ABCDE", 5)]
    public async Task Hex_RejectsAnOddNumberOfDigits(string lexical, int offset)
    {
        var result = Get("hexBinary").Parse(lexical);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(result.ErrorOffset).IsEqualTo(offset);
    }

    [Test]
    [Arguments("0G", 1)]
    [Arguments("0F B8", 2)]
    [Arguments("0x0F", 1)]
    public async Task Hex_RejectsCharactersThatAreNoHexDigits(string lexical, int offset)
    {
        var result = Get("hexBinary").Parse(lexical);

        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(result.ErrorOffset).IsEqualTo(offset);
    }

    [Test]
    [Arguments("0fb8", "0FB8")]
    [Arguments("deadBEEF", "DEADBEEF")]
    [Arguments("  0a  ", "0A")]
    [Arguments("", "")]
    public async Task Hex_CanonicalFormIsUpperCase(string lexical, string canonical) =>
        await Assert.That(Get("hexBinary").Parse(lexical).Value.ToString(BinaryKind.HexBinary)).IsEqualTo(canonical);

    [Test]
    public async Task Values_DecodeToTheOctets()
    {
        var fromHex = Get("hexBinary").Parse("0fB8").Value;
        var fromBase64 = Get("base64Binary").Parse("D7g=").Value;

        await Assert.That(fromHex.ToArray()).IsEquivalentTo(new byte[] { 0x0F, 0xB8 });
        await Assert.That(fromBase64.ToArray()).IsEquivalentTo(new byte[] { 0x0F, 0xB8 });
        await Assert.That(fromBase64.AsSpan().ToArray()).IsEquivalentTo(new byte[] { 0x0F, 0xB8 });
    }

    [Test]
    public async Task Equality_ComparesTheOctets()
    {
        var hex = Get("hexBinary");
        var a = hex.Parse("0FB8").Value;
        var b = hex.Parse("0fb8").Value;
        var c = hex.Parse("0FB9").Value;

        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a.Equals((object)b)).IsTrue();
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
        await Assert.That(a.Equals(c)).IsFalse();
        await Assert.That(hex.IsIdentical(a, b)).IsTrue();
        await Assert.That(hex.IsIdentical(a, c)).IsFalse();
        await Assert.That(hex.Compare(a, b)).IsEqualTo(XsdOrder.Equal);
        await Assert.That(hex.Compare(a, c)).IsEqualTo(XsdOrder.Incomparable);
        await Assert.That(default(XsdBinary).Equals(hex.Parse("").Value)).IsTrue();
        await Assert.That(default(XsdBinary).Length).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_ReportsTheErrorWithoutAValue()
    {
        var result = Get("base64Binary").Validate("QU!D");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(result.Offset).IsEqualTo(2);
        await Assert.That(Get("hexBinary").Validate("0fb8").IsValid).IsTrue();
    }

    [Test]
    public async Task Base64_RoundTripsEveryLengthModuloThree()
    {
        for (var length = 0; length <= 9; length++)
        {
            var bytes = new byte[length];
            for (var i = 0; i < length; i++)
            {
                bytes[i] = (byte)((i * 37) + 200);
            }

            var lexical = Convert.ToBase64String(bytes);
            var value = Get("base64Binary").Parse(lexical).Value;

            await Assert.That(value.ToArray()).IsEquivalentTo(bytes);
            await Assert.That(value.ToString(BinaryKind.Base64Binary)).IsEqualTo(lexical);
        }
    }
}
