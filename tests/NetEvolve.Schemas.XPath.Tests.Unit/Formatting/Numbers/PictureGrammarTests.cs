namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Numbers;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Abstractions;

/// <summary>
/// The picture grammar of <c>format-number</c>, once under the XSLT 2.0 rules (XSLT 2.0, 16.4) and once under the 3.1 rules (Functions
/// and Operators 3.1, 4.7): grouping separators, minimum and maximum digits, percent and per-mille, the negative sub-picture, a
/// non-default digit family, and the pictures that both rule sets reject. A row that appears in both tables is a rule the two share.
/// </summary>
public class PictureGrammarTests
{
    private const string ArabicZero = "٠";
    private const string ArabicOne = "١";
    private const string ArabicFive = "٥";
    private const string ArabicSeven = "٧";

    // The rows both rule sets answer the same way.
    [Test]
    [Arguments("decimal:12345.6", "#,###.00", "12,345.60")]
    [Arguments("decimal:12345678.9", "0,000.00", "12,345,678.90")]
    [Arguments("decimal:1234567.5", "#,##0.00", "1,234,567.50")]
    [Arguments("integer:1234567", "#,##,##0", "12,34,567")]
    [Arguments("decimal:123.9", "0000", "0124")]
    [Arguments("decimal:0.23", "#", "0")]
    [Arguments("decimal:3.14159", "0.00", "3.14")]
    [Arguments("decimal:1.5", "0.0#", "1.5")]
    [Arguments("decimal:1.25", "0.0#", "1.25")]
    [Arguments("decimal:1.245", "0.0#", "1.24")]
    [Arguments("integer:5", "00.00", "05.00")]
    [Arguments("decimal:12345.678", "#.#", "12345.7")]
    [Arguments("decimal:0.123456", "0.000,000", "0.123,456")]
    [Arguments("decimal:0.14", "00%", "14%")]
    [Arguments("decimal:0.0145", "000‰", "014‰")]
    [Arguments("integer:-6", "000", "-006")]
    [Arguments("integer:-6", "000;(000)", "(006)")]
    [Arguments("integer:6", "000;(000)", "006")]
    [Arguments("integer:5", "x#0y", "x5y")]
    [Arguments("integer:-5", "x#0y", "-x5y")]
    [Arguments("integer:-5", "x#0y;<#0>", "<5>")]
    public async Task Picture_SharedRows_FormatTheSameUnderBothRuleSets(string value, string picture, string expected)
    {
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo(expected);
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);
    }

    // 3.1: any member of the digit family is a mandatory digit (4.7.3), so 9 is a digit sign like 0.
    [Test]
    [Arguments("decimal:12345678.9", "9,999.99", "12,345,678.90")]
    [Arguments("decimal:123.9", "9999", "0124")]
    [Arguments("integer:5", "999", "005")]
    [Arguments("integer:5", "001", "005")]
    public async Task Picture31_AnyDigitOfTheFamilyIsAMandatoryDigit(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    // XSLT 2.0: only the zero-digit character is a digit sign, so the picture 999 has no digit sign at all.
    [Test]
    [Arguments("integer:5", "999")]
    [Arguments("decimal:12345678.9", "9,999.99")]
    public async Task Picture20_OnlyTheZeroDigitIsADigitSign_AnotherDigitIsPassive(string value, string picture) =>
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo("error:XTDE1310");

    // 2.0: one separator, or separators at N, 2N, 3N, are regular and extend to every multiple of N.
    // 3.1: every multiple of the size up to the digits of the integer part must carry a separator (4.7.4).
    [Test]
    [Arguments("integer:123456789", "#####,###", "123,456,789", "123456,789")]
    [Arguments("integer:123456789012345", "#########,###,###", "123,456,789,012,345", "123456789,012,345")]
    [Arguments("integer:123456789", "#,###", "123,456,789", "123,456,789")]
    [Arguments("integer:123456789", "#,###,###", "123,456,789", "123,456,789")]
    [Arguments("integer:642120", "##,##", "64,21,20", "64,21,20")]
    [Arguments("integer:642120", "####,##", "64,21,20", "6421,20")]
    [Arguments("integer:642120", "###,##", "64,21,20", "6421,20")]
    [Arguments("integer:642120", "##,#,#", "6,4,2,1,2,0", "6421,2,0")]
    [Arguments("integer:642120", "#,#,#", "6,4,2,1,2,0", "6,4,2,1,2,0")]
    [Arguments("integer:642120", "00,00", "64,21,20", "64,21,20")]
    [Arguments("integer:3000000", ",##0", "3,000,000", "3,000,000")]
    [Arguments("integer:123456789", "###,##,00", "1,23,45,67,89", "12345,67,89")]
    [Arguments("integer:123456789", "#,##,##,00", "1,23,45,67,89", "1,23,45,67,89")]
    [Arguments("integer:123456789", "####,###,##,0", "123,456,78,9", "123,456,78,9")]
    [Arguments("integer:987654321", "###,##0,00.00", "9876,543,21.00", "9876,543,21.00")]
    public async Task Picture_RegularGrouping_FollowsTheRulesOfEachSpecification(
        string value,
        string picture,
        string expected20,
        string expected31
    )
    {
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo(expected20);
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected31);
    }

    // 2.0 allows a grouping separator at the end of the integer part, 3.1 does not; the separator has no digit to its right and shows nothing.
    [Test]
    public async Task Picture_GroupingSeparatorAtTheEndOfTheIntegerPart_Is31OnlyAnError()
    {
        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "0,")).IsEqualTo("5");
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "0,")).IsEqualTo("error:FODF1310");
    }

    [Test]
    public async Task Picture31_TwoAdjacentGroupingSeparators_AreAnError() =>
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "#,,##0")).IsEqualTo("error:FODF1310");

    // 3.1 adjusts the sizes so that a picture without a mandatory digit still shows one (4.7.4); 2.0 sets the minimum from the zero digits only.
    [Test]
    [Arguments("decimal:0", "#.#", ".0")]
    [Arguments("decimal:0.5", "#.#", ".5")]
    [Arguments("decimal:0.25", "#.##", ".25")]
    [Arguments("decimal:0.25", ".##", ".25")]
    [Arguments("decimal:0", "#", "0")]
    public async Task Picture31_MinimumSizesAreAdjusted(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    // A digit family other than 0 to 9: the zero digit comes from the decimal format.
    [Test]
    public async Task Picture_NonDefaultDigitFamily_FormatsInTheDigitsOfTheFormat()
    {
        var formats = new FormatNumberHarness.Formats().Add("ar", new XPathDecimalFormat { ZeroDigit = ArabicZero[0] });
        var picture = ArabicZero + ArabicZero + ArabicZero;

        await Assert
            .That(FormatNumberHarness.Xslt20("integer:7", picture, "ar", formats))
            .IsEqualTo(ArabicZero + ArabicZero + ArabicSeven);
        await Assert
            .That(FormatNumberHarness.Xpath31("integer:7", picture, "ar", formats))
            .IsEqualTo(ArabicZero + ArabicZero + ArabicSeven);
    }

    // 3.1: a member of the family other than the zero digit is mandatory; 2.0: it is a passive character.
    [Test]
    public async Task Picture_AnotherMemberOfTheNonDefaultFamily_IsMandatoryIn31AndPassiveInXslt20()
    {
        var formats = new FormatNumberHarness.Formats().Add("ar", new XPathDecimalFormat { ZeroDigit = ArabicZero[0] });
        var picture = ArabicOne + ArabicZero;

        await Assert
            .That(FormatNumberHarness.Xpath31("integer:5", picture, "ar", formats))
            .IsEqualTo(ArabicZero + ArabicFive);
        await Assert
            .That(FormatNumberHarness.Xslt20("integer:5", picture, "ar", formats))
            .IsEqualTo(ArabicOne + ArabicFive);
    }

    // The format of the Functions and Operators example: grouping separator U+02B9 and decimal separator U+00B7.
    [Test]
    public async Task Picture_CustomSeparators_AreTheOnesOfTheFormat()
    {
        var formats = new FormatNumberHarness.Formats().Add(
            "ch",
            new XPathDecimalFormat { GroupingSeparator = 'ʹ', DecimalSeparator = '·' }
        );
        const string picture = "#ʹ##0·00";

        await Assert
            .That(FormatNumberHarness.Xslt20("decimal:1234.5678", picture, "ch", formats))
            .IsEqualTo("1ʹ234·57");
        await Assert
            .That(FormatNumberHarness.Xpath31("decimal:1234.5678", picture, "ch", formats))
            .IsEqualTo("1ʹ234·57");
    }

    // The pictures that break a rule that both specifications state: the same cause, the code of each host.
    [Test]
    [Arguments("")]
    [Arguments("abc")]
    [Arguments(";0")]
    [Arguments("0;")]
    [Arguments("0;0;0")]
    [Arguments("0.0.0")]
    [Arguments("0%‰")]
    [Arguments("0%%")]
    [Arguments("0‰‰")]
    [Arguments("0x0")]
    [Arguments("0,.0")]
    [Arguments("0.,0")]
    [Arguments("0#")]
    [Arguments("0.#0")]
    [Arguments("#0#")]
    public async Task Picture_RuleViolations_AreCodedErrors(string picture)
    {
        await Assert.That(FormatNumberHarness.Xslt20("integer:5", picture)).IsEqualTo("error:XTDE1310");
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", picture)).IsEqualTo("error:FODF1310");
    }

    // A violation in the second sub-picture is as much a violation as one in the first.
    [Test]
    public async Task Picture_ViolationInTheNegativeSubPicture_IsACodedError()
    {
        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "0;0x0")).IsEqualTo("error:XTDE1310");
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "0;0x0")).IsEqualTo("error:FODF1310");
    }

    // The picture is checked even when the value does not use the sub-picture that breaks the rule.
    [Test]
    public async Task Picture_IsCheckedWholeBeforeTheValueIsRead() =>
        await Assert.That(FormatNumberHarness.Xpath31("double:NaN", "0;0x0")).IsEqualTo("error:FODF1310");

    [Test]
    public async Task Picture_PercentOfTheNegativeSubPicture_AppliesToNegativeValuesOnly()
    {
        await Assert.That(FormatNumberHarness.Xpath31("decimal:-0.5", "0;0%")).IsEqualTo("50%");
        await Assert.That(FormatNumberHarness.Xpath31("decimal:0.5", "0;0%")).IsEqualTo("0");
    }
}
