namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Numbers;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The exponent separator: Functions and Operators 3.1 (4.7.3 to 4.7.5) formats a number in scientific notation, and XSLT 2.0 (16.4) has
/// no exponent separator, so its rules treat the character as a literal where the grammar allows one and reject the picture where it does not.
/// </summary>
public class ExponentTests
{
    // The examples of Functions and Operators 3.1 with the default exponent separator e, and the notes of 4.7.4.
    [Test]
    [Arguments("decimal:1234.5678", "00.000e0", "12.346e2")]
    [Arguments("decimal:0.234", "0.0e0", "2.3e-1")]
    [Arguments("decimal:0.234", "#.00e0", "0.23e0")]
    [Arguments("decimal:0.234", ".00e0", ".23e0")]
    [Arguments("decimal:0.123", "#.e9", "0.1e0")]
    [Arguments("decimal:0.1", ".9e9", ".1e0")]
    [Arguments("decimal:0.1", "#.9e9", "0.1e0")]
    public async Task Exponent31_FormatsTheNumberInScientificNotation(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("integer:0", "0.0e0", "0.0e0")]
    [Arguments("double:1234", "0.0e000", "1.2e003")]
    [Arguments("integer:-1234", "0.0e0", "-1.2e3")]
    [Arguments("double:12345", "0.00e0", "1.23e4")]
    [Arguments("integer:5", "0.0e00", "5.0e00")]
    [Arguments("double:1E-300", "0.0e0", "1.0e-300")]
    [Arguments("decimal:12345", "00e0", "12e3")]
    [Arguments("decimal:0.00012345", "0.000e0", "1.234e-4")]
    [Arguments("integer:1234", "x0.0e0y", "x1.2e3y")]
    [Arguments("integer:-1234", "0.0e0;(0.0e0)", "(1.2e3)")]
    [Arguments("double:INF", "0.0e0", "Infinity")]
    [Arguments("double:NaN", "0.0e0", "NaN")]
    public async Task Exponent31_SizesSignAndSpecialValues(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    // Rounding the mantissa up to 10 to the power of the scaling factor moves the exponent: 9.96 is 1.0e1, not 10.0e0.
    [Test]
    [Arguments("decimal:9.96", "0.0e0", "1.0e1")]
    [Arguments("decimal:99.96", "00.0e0", "10.0e1")]
    [Arguments("decimal:0.996", ".0e0", ".1e1")]
    public async Task Exponent31_MantissaThatRoundsUpToTheNextPower_IsNormalizedAgain(
        string value,
        string picture,
        string expected
    ) => await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    // XPath 3.0 takes the same rules as 3.1.
    [Test]
    public async Task Exponent_InXPath30_FormatsLikeXPath31()
    {
        var result = FormatNumberHarness.Run(
            XPathVersion.V30,
            XPathHostRequirements.None,
            "decimal:1234.5678",
            "00.000e0",
            null,
            null
        );

        await Assert.That(result).IsEqualTo("12.346e2");
    }

    // The character is an exponent separator only between two active characters; at the edges it is a literal.
    [Test]
    [Arguments("integer:5", "0e", "5e")]
    [Arguments("integer:5", "e0", "e5")]
    [Arguments("integer:5", "e0e", "e5e")]
    public async Task Exponent31_SeparatorAtTheEdgeOfThePicture_IsALiteral(
        string value,
        string picture,
        string expected
    ) => await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("0.0e0%")]
    [Arguments("0.0e0‰")]
    [Arguments("0e#")]
    [Arguments("0e0#")]
    [Arguments("0e0.0")]
    [Arguments("0e0,0")]
    [Arguments("0e00e0")]
    public async Task Exponent31_PictureThatBreaksAnExponentRule_IsFODF1310(string picture) =>
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", picture)).IsEqualTo("error:FODF1310");

    // XSLT 2.0 knows no exponent separator: between two active characters the character is a passive one, which the rules reject there.
    [Test]
    [Arguments("0.0e0")]
    [Arguments("00.000e0")]
    [Arguments(".00e0")]
    public async Task ExponentXslt20_SeparatorBetweenActiveCharacters_IsRejected(string picture) =>
        await Assert.That(FormatNumberHarness.Xslt20("decimal:1234.5678", picture)).IsEqualTo("error:XTDE1310");

    // At the edge it is a literal prefix or suffix, as for any passive character.
    [Test]
    [Arguments("decimal:1.5", "0.0e", "1.5e")]
    [Arguments("integer:5", "e0", "e5")]
    [Arguments("integer:5", "0e", "5e")]
    public async Task ExponentXslt20_SeparatorAtTheEdge_IsALiteral(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo(expected);
}
