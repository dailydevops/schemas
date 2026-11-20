namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Numbers;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Abstractions;

/// <summary>
/// The value side of <c>format-number</c>: <c>xs:decimal</c>, <c>xs:integer</c>, <c>xs:float</c>, and <c>xs:double</c>, rounding to the
/// maximum fraction digits with a tie to the even neighbor, negative zero, NaN, the infinities, and the empty sequence.
/// </summary>
public class NumberValueTests
{
    // The rounded number is the one that round-half-to-even gives for the maximum number of fraction digits.
    [Test]
    [Arguments("decimal:0.5", "0", "0")]
    [Arguments("decimal:1.5", "0", "2")]
    [Arguments("decimal:2.5", "0", "2")]
    [Arguments("decimal:-2.5", "0", "-2")]
    [Arguments("decimal:0.125", "0.00", "0.12")]
    [Arguments("decimal:0.135", "0.00", "0.14")]
    [Arguments("decimal:9.995", "0.00", "10.00")]
    [Arguments("decimal:0.996", "0.0", "1.0")]
    [Arguments("decimal:99.96", "0.0", "100.0")]
    [Arguments("decimal:0.04", "0.0", "0.0")]
    [Arguments("decimal:0.0004", "0", "0")]
    [Arguments("decimal:0.5000000000000000000000001", "0", "1")]
    [Arguments("decimal:-0.4", "0", "-0")]
    [Arguments("double:2.5", "0", "2")]
    [Arguments("double:0.125", "0.00", "0.12")]
    [Arguments("float:0.125", "0.00", "0.12")]
    public async Task Value_IsRoundedHalfToEvenToTheMaximumFractionDigits(string value, string picture, string expected)
    {
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo(expected);
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);
    }

    // A double or float is formatted from the shortest decimal that round-trips, not from its exact binary value.
    [Test]
    [Arguments("double:0.1", "0.0000000000", "0.1000000000")]
    [Arguments("float:0.1", "0.0000000000", "0.1000000000")]
    [Arguments("double:1E21", "0", "1000000000000000000000")]
    [Arguments("double:123456789012345678", "0", "123456789012345680")]
    [Arguments("double:0.14", "0%", "14%")]
    [Arguments("double:1E-8", "0.0", "0.0")]
    [Arguments("double:100", "#,##0.00", "100.00")]
    [Arguments("float:16777216", "#,##0", "16,777,216")]
    public async Task Value_OfAFloatingPointType_UsesTheShortestRoundTripDigits(
        string value,
        string picture,
        string expected
    )
    {
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo(expected);
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);
    }

    // The integer and decimal paths are exact at any width.
    [Test]
    [Arguments("decimal:123456789012345678901234567890.5", "#,##0.0", "123,456,789,012,345,678,901,234,567,890.5")]
    [Arguments("integer:-9223372036854775808", "#,##0", "-9,223,372,036,854,775,808")]
    [Arguments("integer:9223372036854775807", "0", "9223372036854775807")]
    [Arguments("decimal:0.000000000000000000000001", "0.000000000000000000000000", "0.000000000000000000000001")]
    [Arguments("decimal:1000000000000000000000000", "0", "1000000000000000000000000")]
    public async Task Value_OfADecimalOrIntegerType_IsExact(string value, string picture, string expected)
    {
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo(expected);
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);
    }

    // Negative zero of a double or float takes the negative sub-picture; a decimal or integer zero takes the positive one.
    [Test]
    [Arguments("double:-0", "0;(0)", "(0)")]
    [Arguments("float:-0", "0;(0)", "(0)")]
    [Arguments("double:-0", "0", "-0")]
    [Arguments("double:0", "0;(0)", "0")]
    [Arguments("decimal:0.0", "0;(0)", "0")]
    [Arguments("integer:0", "0;(0)", "0")]
    [Arguments("integer:0", "0", "0")]
    public async Task Value_ZeroTakesTheSubPictureOfItsSign(string value, string picture, string expected)
    {
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo(expected);
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);
    }

    // NaN is the NaN string alone: the prefix and the suffix of the picture are not added. The empty sequence is NaN.
    [Test]
    [Arguments("double:NaN", "x0y", "NaN")]
    [Arguments("float:NaN", "0", "NaN")]
    [Arguments("()", "0", "NaN")]
    [Arguments("()", "x0y;(0)", "NaN")]
    public async Task Value_NaNAndTheEmptySequence_AreTheNaNString(string value, string picture, string expected)
    {
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo(expected);
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);
    }

    // An infinity is the infinity string between the prefix and the suffix of the sub-picture of its sign.
    [Test]
    [Arguments("double:INF", "0", "Infinity")]
    [Arguments("double:-INF", "0", "-Infinity")]
    [Arguments("double:-INF", "0;(0)", "(Infinity)")]
    [Arguments("double:INF", "x0y", "xInfinityy")]
    [Arguments("double:-INF", "x0y", "-xInfinityy")]
    [Arguments("float:INF", "0.0", "Infinity")]
    public async Task Value_Infinity_IsTheInfinityStringWithPrefixAndSuffix(
        string value,
        string picture,
        string expected
    )
    {
        await Assert.That(FormatNumberHarness.Xslt20(value, picture)).IsEqualTo(expected);
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);
    }

    // A percent that overflows a double is no error: the adjusted number is infinity (4.7.5).
    [Test]
    public async Task Value_PercentOverflow_IsInfinity() =>
        await Assert.That(FormatNumberHarness.Xpath31("double:1E308", "0%")).IsEqualTo("Infinity%");

    [Test]
    public async Task Value_AnUntypedAtomicValue_IsReadAsADouble() =>
        await Assert.That(FormatNumberHarness.Xpath31("untypedAtomic:12.5", "0.0")).IsEqualTo("12.5");

    [Test]
    public async Task Value_ThatIsNotANumber_IsXPTY0004() =>
        await Assert.That(FormatNumberHarness.Xpath31("string:12", "0")).IsEqualTo("error:XPTY0004");

    // The decimal format supplies the strings of NaN and infinity and the minus sign.
    [Test]
    public async Task Value_StringsAndMinusSign_AreTheOnesOfTheFormat()
    {
        var formats = new FormatNumberHarness.Formats().Add(
            "fr",
            new XPathDecimalFormat
            {
                Infinity = "∞",
                NaN = "n/a",
                MinusSign = '−',
            }
        );

        await Assert.That(FormatNumberHarness.Xpath31("integer:-5", "0", "fr", formats)).IsEqualTo("−5");
        await Assert.That(FormatNumberHarness.Xpath31("double:INF", "0", "fr", formats)).IsEqualTo("∞");
        await Assert.That(FormatNumberHarness.Xpath31("double:-INF", "0", "fr", formats)).IsEqualTo("−∞");
        await Assert.That(FormatNumberHarness.Xpath31("double:NaN", "0", "fr", formats)).IsEqualTo("n/a");
        await Assert.That(FormatNumberHarness.Xslt20("integer:-5", "0;(0)", "fr", formats)).IsEqualTo("(5)");
    }
}
