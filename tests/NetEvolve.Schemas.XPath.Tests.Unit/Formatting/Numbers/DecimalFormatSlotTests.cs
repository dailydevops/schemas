namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Numbers;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The decimal formats of the host slot (<see cref="XPathHostSlots.DecimalFormats"/>): a named format is used, an unknown name is the
/// coded error of the host, the unnamed format is the default unless the host supplies one, and an invalid format is refused.
/// </summary>
public class DecimalFormatSlotTests
{
    private static FormatNumberHarness.Formats Swiss() =>
        new FormatNumberHarness.Formats().Add(
            "ch",
            new XPathDecimalFormat { GroupingSeparator = 'ʹ', DecimalSeparator = '·' }
        );

    // A named decimal format placed in the slot is the one used (Functions and Operators 3.1, 4.7.2: the format of the name).
    [Test]
    public async Task NamedFormat_InTheSlot_IsUsed()
    {
        const string picture = "#ʹ##0·00";

        await Assert
            .That(FormatNumberHarness.Xpath31("decimal:1234.5678", picture, "ch", Swiss()))
            .IsEqualTo("1ʹ234·57");
        await Assert
            .That(FormatNumberHarness.Xslt20("decimal:1234.5678", picture, "ch", Swiss()))
            .IsEqualTo("1ʹ234·57");
    }

    // The format of the slot replaces the default for that name only: the default separators are active in no picture of a named format.
    [Test]
    public async Task NamedFormat_ReplacesTheDefaultSeparators() =>
        await Assert
            .That(FormatNumberHarness.Xpath31("decimal:1234.5", "#,##0.0", "ch", Swiss()))
            .IsEqualTo("error:FODF1310");

    // An unknown name is FODF1280 (XTDE1280 in the XSLT 2.0 host mode); the slot is present and does not know the name.
    [Test]
    [Arguments("unknown")]
    [Arguments("1a")]
    [Arguments("a b")]
    [Arguments("a:b:c")]
    public async Task UnknownOrInvalidName_IsTheCodedError(string name)
    {
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "0", name, Swiss())).IsEqualTo("error:FODF1280");
        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "0", name, Swiss())).IsEqualTo("error:XTDE1280");
    }

    // The name is looked up in the slot only: a host without a slot declared no format, so a name needs the slot (XPDY0002).
    [Test]
    public async Task NamedFormat_WithoutTheSlot_IsXPDY0002()
    {
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "0", "ch")).IsEqualTo("error:XPDY0002");
        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "0", "ch")).IsEqualTo("error:XPDY0002");
    }

    // The two-argument form never needs the slot: the unnamed format is the default.
    [Test]
    public async Task UnnamedFormat_WithoutTheSlot_IsTheDefault()
    {
        await Assert.That(FormatNumberHarness.Xpath31("decimal:1234.5", "#,##0.0")).IsEqualTo("1,234.5");
        await Assert.That(FormatNumberHarness.Xslt20("decimal:1234.5", "#,##0.0")).IsEqualTo("1,234.5");
    }

    [Test]
    public async Task UnnamedFormat_SlotWithoutAnUnnamedFormat_IsTheDefault() =>
        await Assert.That(FormatNumberHarness.Xpath31("decimal:1234.5", "#,##0.0", null, Swiss())).IsEqualTo("1,234.5");

    // The host declares the unnamed format through the slot, with the default name, as xsl:decimal-format without a name does.
    [Test]
    public async Task UnnamedFormat_SuppliedBySlot_ReplacesTheDefault()
    {
        var formats = new FormatNumberHarness.Formats().Add(
            string.Empty,
            new XPathDecimalFormat { GroupingSeparator = '.', DecimalSeparator = ',' }
        );

        await Assert
            .That(FormatNumberHarness.Xpath31("decimal:1234.5", "#.##0,00", null, formats))
            .IsEqualTo("1.234,50");
        await Assert
            .That(FormatNumberHarness.Xslt20("decimal:1234.5", "#.##0,00", null, formats))
            .IsEqualTo("1.234,50");
    }

    // 3.1: a three-argument call with the empty sequence or the zero-length string as the name is the unnamed format.
    [Test]
    public async Task EmptyName_InXPath3_IsTheUnnamedFormat()
    {
        var formats = new FormatNumberHarness.Formats().Add(
            string.Empty,
            new XPathDecimalFormat { GroupingSeparator = '.', DecimalSeparator = ',' }
        );

        await Assert
            .That(FormatNumberHarness.Xpath31("decimal:1234.5", "#.##0,00", string.Empty, formats))
            .IsEqualTo("1.234,50");
    }

    // 3.1: the name is an EQName after the whitespace is removed; a URIQualifiedName is looked up with its namespace.
    [Test]
    public async Task Name_InXPath3_IsTrimmedAndMayBeAUriQualifiedName()
    {
        var formats = new FormatNumberHarness.Formats()
            .Add("ch", new XPathDecimalFormat { GroupingSeparator = 'ʹ', DecimalSeparator = '·' })
            .Add(
                new XPathQName("urn:x", "fmt"),
                new XPathDecimalFormat { GroupingSeparator = '.', DecimalSeparator = ',' }
            );

        await Assert
            .That(FormatNumberHarness.Xpath31("decimal:1234.5", "#ʹ##0·0", "  ch  ", formats))
            .IsEqualTo("1ʹ234·5");
        await Assert
            .That(FormatNumberHarness.Xpath31("decimal:1234.5", "#.##0,0", "Q{urn:x}fmt", formats))
            .IsEqualTo("1.234,5");
        await Assert
            .That(FormatNumberHarness.Xpath31("decimal:1234.5", "#ʹ##0·0", "Q{}ch", formats))
            .IsEqualTo("1ʹ234·5");
        await Assert
            .That(FormatNumberHarness.Xpath31("decimal:1234.5", "#.##0,0", "Q{urn:y}fmt", formats))
            .IsEqualTo("error:FODF1280");
    }

    // A braced name without the closing brace, or with a local part that is no NCName, names no format.
    [Test]
    [Arguments("Q{urn:x")]
    [Arguments("Q{urn:x}1fmt")]
    [Arguments("Q{urn:x}")]
    public async Task Name_InXPath3_BracedFormThatIsMalformed_IsUnknown(string name)
    {
        var formats = new FormatNumberHarness.Formats().Add(new XPathQName("urn:x", "fmt"), new XPathDecimalFormat());

        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "0", name, formats)).IsEqualTo("error:FODF1280");
    }

    // XSLT 2.0 knows lexical QNames only, so the braced form and the surrounding whitespace are invalid.
    [Test]
    [Arguments("Q{urn:x}fmt")]
    [Arguments(" ch")]
    [Arguments("")]
    public async Task Name_InXslt20_MustBeALexicalQName(string name)
    {
        var formats = new FormatNumberHarness.Formats()
            .Add("ch", new XPathDecimalFormat())
            .Add(new XPathQName("urn:x", "fmt"), new XPathDecimalFormat());

        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "0", name, formats)).IsEqualTo("error:XTDE1280");
    }

    // A prefixed name needs the statically known namespaces, which a call does not reach (limitation, #752).
    [Test]
    public async Task PrefixedName_WithoutStaticNamespaces_IsTheCodedError()
    {
        var formats = new FormatNumberHarness.Formats().Add(new XPathQName("urn:x", "fmt"), new XPathDecimalFormat());

        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "0", "p:fmt", formats)).IsEqualTo("error:FODF1280");
        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "0", "p:fmt", formats)).IsEqualTo("error:XTDE1280");
    }

    // The characters of a format must be distinct (XSLT 2.0, XTSE1300; Functions and Operators 3.1, 4.7.1); a host that hands over a clash gets the picture error.
    [Test]
    public async Task InvalidFormat_FromTheSlot_IsTheCodedPictureError()
    {
        var formats = new FormatNumberHarness.Formats()
            .Add("clash", new XPathDecimalFormat { Percent = '.' })
            .Add("zero", new XPathDecimalFormat { ZeroDigit = 'a' });

        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "0", "clash", formats)).IsEqualTo("error:FODF1310");
        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "0", "clash", formats)).IsEqualTo("error:XTDE1310");
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "0", "zero", formats)).IsEqualTo("error:FODF1310");
        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "0", "zero", formats)).IsEqualTo("error:XTDE1310");
    }

    // The zero digit must be a character of category Nd with the digit value zero; U+0660 is one, U+0661 is not.
    [Test]
    public async Task ZeroDigit_MustBeTheZeroOfADigitFamily()
    {
        var formats = new FormatNumberHarness.Formats()
            .Add("ar", new XPathDecimalFormat { ZeroDigit = '٠' })
            .Add("one", new XPathDecimalFormat { ZeroDigit = '١' });

        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "٠", "ar", formats)).IsEqualTo("٥");
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "١", "one", formats)).IsEqualTo("error:FODF1310");
    }

    // The check reads the pinned Unicode tables: the first character of a run of ten Nd characters is a zero, the others and a character outside any run are not.
    [Test]
    [Arguments('0', true)]
    [Arguments('٠', true)]
    [Arguments('۰', true)]
    [Arguments('０', true)]
    [Arguments('٩', false)]
    [Arguments('۱', false)]
    [Arguments('９', false)]
    [Arguments('a', false)]
    public async Task ZeroDigit_IsTheFirstCharacterOfARunOfTenDigits(char candidate, bool valid)
    {
        var formats = new FormatNumberHarness.Formats().Add("f", new XPathDecimalFormat { ZeroDigit = candidate });
        var picture = candidate + "." + candidate;

        var result = FormatNumberHarness.Xpath31("integer:5", picture, "f", formats);

        await Assert.That(result.StartsWith("error:", StringComparison.Ordinal)).IsEqualTo(!valid);
    }
}
