namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ComparisonFixture;

/// <summary>The value comparisons eq and ne of <c>xs:hexBinary</c>, <c>xs:base64Binary</c>, and <c>xs:QName</c> (F&amp;O, 10.4, 9.2.1, 11.2).</summary>
public class BinaryQNameComparisonTests
{
    private static XdmSequence Hex(string lexical) =>
        XdmSequence.Create(
            XdmItem.FromBinary(XsdBinary.Parse(lexical, BinaryKind.HexBinary).Value, BinaryKind.HexBinary)
        );

    private static XdmSequence Base64(string lexical) =>
        XdmSequence.Create(
            XdmItem.FromBinary(XsdBinary.Parse(lexical, BinaryKind.Base64Binary).Value, BinaryKind.Base64Binary)
        );

    private static XdmSequence QName(string ns, string local, string? prefix) =>
        XdmSequence.Create(XdmItem.FromQName(new XdmQName(new XsdQName(ns, local), prefix)));

    private static string Run(string op, XdmSequence left, XdmSequence right) =>
        Outcome(() => ComparisonFixture.Run(Operator(op), left, right));

    [Test]
    [Arguments("eq", "FF", "ff", "true")]
    [Arguments("ne", "FF", "ff", "false")]
    [Arguments("eq", "FF", "00", "false")]
    [Arguments("ne", "FF", "00", "true")]
    [Arguments("eq", "", "", "true")]
    [Arguments("eq", "FF", "FF00", "false")]
    public async Task HexBinary_ComparesOctets(string op, string left, string right, string expected) =>
        await Assert.That(Run(op, Hex(left), Hex(right))).IsEqualTo(expected);

    [Test]
    [Arguments("eq", "aaaa", "aaaa", "true")]
    [Arguments("ne", "aaaa", "aaaa", "false")]
    [Arguments("eq", "aaaa", "AAAA", "false")]
    [Arguments("ne", "aaaa", "AAAA", "true")]
    public async Task Base64Binary_ComparesOctets(string op, string left, string right, string expected) =>
        await Assert.That(Run(op, Base64(left), Base64(right))).IsEqualTo(expected);

    [Test]
    [Arguments("eq", "p", "q", "true")]
    [Arguments("ne", "p", "q", "false")]
    public async Task QName_IgnoresThePrefix(string op, string leftPrefix, string rightPrefix, string expected) =>
        await Assert
            .That(Run(op, QName("urn:a", "n", leftPrefix), QName("urn:a", "n", rightPrefix)))
            .IsEqualTo(expected);

    [Test]
    [Arguments("eq", "urn:a", "n", "urn:b", "n", "false")]
    [Arguments("eq", "urn:a", "n", "urn:a", "m", "false")]
    [Arguments("ne", "urn:a", "n", "urn:b", "n", "true")]
    public async Task QName_ComparesNamespaceAndLocalName(
        string op,
        string leftNs,
        string leftLocal,
        string rightNs,
        string rightLocal,
        string expected
    ) => await Assert.That(Run(op, QName(leftNs, leftLocal, "p"), QName(rightNs, rightLocal, "p"))).IsEqualTo(expected);

    [Test]
    [Arguments("lt")]
    [Arguments("le")]
    [Arguments("gt")]
    [Arguments("ge")]
    public async Task OrderingOperators_RemainXpty0004(string op)
    {
        await Assert.That(Run(op, Hex("FF"), Hex("FF"))).IsEqualTo("XPTY0004");
        await Assert.That(Run(op, Base64("aaaa"), Base64("aaaa"))).IsEqualTo("XPTY0004");
        await Assert.That(Run(op, QName("urn:a", "n", null), QName("urn:a", "n", null))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task MixedTypes_RemainXpty0004()
    {
        await Assert.That(Run("eq", Hex("FF"), Base64("aaaa"))).IsEqualTo("XPTY0004");
        await Assert.That(Run("eq", Hex("FF"), One("s:FF"))).IsEqualTo("XPTY0004");
        await Assert.That(Run("eq", QName("urn:a", "n", null), One("s:n"))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task Implementations_RejectAnOperandOfAnotherType()
    {
        var text = XdmItem.FromString("FF");
        var hex = XdmItem.FromBinary(XsdBinary.Parse("FF", BinaryKind.HexBinary).Value, BinaryKind.HexBinary);

        await Assert
            .That(() => EqualityComparison.CompareBinary(ComparisonRelation.Equal, text, hex))
            .Throws<NetEvolve.Schemas.XPath.Errors.XPathException>();
        await Assert
            .That(() => EqualityComparison.CompareQName(ComparisonRelation.Equal, hex, text))
            .Throws<NetEvolve.Schemas.XPath.Errors.XPathException>();
    }
}
