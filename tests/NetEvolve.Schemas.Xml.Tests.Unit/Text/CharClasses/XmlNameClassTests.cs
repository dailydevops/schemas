namespace NetEvolve.Schemas.Xml.Tests.Unit.Text.CharClasses;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Text.CharClasses;

public class XmlNameClassTests
{
    private const NameEdition Second = NameEdition.Xml10SecondEdition;
    private const NameEdition Fifth = NameEdition.Xml10FifthEdition;

    [Test]
    public async Task Fifth_DelegatesToTheSharedFifthEditionClasses()
    {
        var failures = new List<string>();
        for (var cp = -1; cp <= 0x110000; cp++)
        {
            if (
                XmlNameClass.IsNameStartChar(Fifth, cp) != XmlCharClass.IsNameStartChar(cp)
                || XmlNameClass.IsNameChar(Fifth, cp) != XmlCharClass.IsNameChar(cp)
            )
            {
                failures.Add($"U+{cp:X}");
            }
        }

        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);
    }

    // The Appendix B boundaries: (code point, name start, name character) in the Second Edition.
    [Test]
    [Arguments('A', true, true)]
    [Arguments('z', true, true)]
    [Arguments('_', true, true)]
    [Arguments(':', true, true)]
    [Arguments('-', false, true)]
    [Arguments('.', false, true)]
    [Arguments('0', false, true)]
    [Arguments(0x00B7, false, true)]
    [Arguments(0x0100, true, true)]
    [Arguments(0x0131, true, true)]
    [Arguments(0x0132, false, false)]
    [Arguments(0x0133, false, false)]
    [Arguments(0x0134, true, true)]
    [Arguments(0x013E, true, true)]
    [Arguments(0x013F, false, false)]
    [Arguments(0x0300, false, true)]
    [Arguments(0x0345, false, true)]
    [Arguments(0x0346, false, false)]
    [Arguments(0x0660, false, true)]
    [Arguments(0x0387, false, true)]
    [Arguments(0x200C, false, false)]
    [Arguments(0x203F, false, false)]
    [Arguments(0x3005, false, true)]
    [Arguments(0x3007, true, true)]
    [Arguments(0x4E00, true, true)]
    [Arguments(0x9FA5, true, true)]
    [Arguments(0x9FA6, false, false)]
    [Arguments(0xAC00, true, true)]
    [Arguments(0xD7A3, true, true)]
    [Arguments(0xD7A4, false, false)]
    [Arguments(0x10000, false, false)]
    [Arguments(-1, false, false)]
    [Arguments(0x110000, false, false)]
    public async Task Second_AtTheAppendixBBoundaries(int codePoint, bool start, bool name)
    {
        await Assert.That(XmlNameClass.IsNameStartChar(Second, codePoint)).IsEqualTo(start);
        await Assert.That(XmlNameClass.IsNameChar(Second, codePoint)).IsEqualTo(name);
    }

    [Test]
    [Arguments(0x0132, true, true)]
    [Arguments(0x200C, true, true)]
    [Arguments(0x203F, false, true)]
    [Arguments(0x10000, true, true)]
    public async Task Fifth_AcceptsTheCodePointsTheSecondEditionRejects(int codePoint, bool start, bool name)
    {
        await Assert.That(XmlNameClass.IsNameStartChar(Fifth, codePoint)).IsEqualTo(start);
        await Assert.That(XmlNameClass.IsNameChar(Fifth, codePoint)).IsEqualTo(name);
    }

    [Test]
    public async Task Second_NameStartIsAlwaysAlsoANameCharacter()
    {
        var failures = new List<string>();
        for (var cp = 0; cp <= 0x10FFFF; cp++)
        {
            if (XmlNameClass.IsNameStartChar(Second, cp) && !XmlNameClass.IsNameChar(Second, cp))
            {
                failures.Add($"U+{cp:X}");
            }
        }

        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);
    }
}
