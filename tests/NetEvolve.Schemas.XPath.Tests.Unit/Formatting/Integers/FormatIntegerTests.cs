namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Integers;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>format-integer</c> (Functions and Operators 3.1, 4.6): decimal digit patterns, alphabetic, roman, and English word sequences, the
/// ordinal modifier, and the errors of an invalid picture.
/// </summary>
public class FormatIntegerTests
{
    // The examples of the specification (4.6.1); the Italian variation of the last one is not applied, the language is English.
    [Test]
    [Arguments("123", "0000", "0123")]
    [Arguments("123", "w", "one hundred and twenty-three")]
    [Arguments("21", "Ww;o", "Twenty-First")]
    [Arguments("7", "a", "g")]
    [Arguments("27", "a", "aa")]
    [Arguments("57", "I", "LVII")]
    [Arguments("1234", "#;##0;", "1;234")]
    [Arguments("1234", "#,##0", "1,234")]
    [Arguments("1", "1;o", "1st")]
    [Arguments("14", "010;o(-º)", "014th")]
    public async Task Examples_OfTheSpecification(string value, string picture, string expected) =>
        await Assert.That(FormatIntegerHarness.Format(value, picture)).IsEqualTo(expected);

    [Test]
    public async Task EmptySequence_IsTheZeroLengthString() =>
        await Assert.That(FormatIntegerHarness.Format("()", "1")).IsEqualTo(string.Empty);

    [Test]
    [Arguments("0", "1", "0")]
    [Arguments("-5", "01", "-05")]
    [Arguments("0", "a", "0")]
    [Arguments("0", "I", "0")]
    [Arguments("0", "w", "zero")]
    [Arguments("-27", "a", "-aa")]
    [Arguments("-57", "i", "-lvii")]
    [Arguments("-21", "w", "-twenty-one")]
    [Arguments("18446744073709551616", "1", "18446744073709551616")]
    [Arguments("-18446744073709551616", "1", "-18446744073709551616")]
    [Arguments("18446744073709551616", "a", "gkgwbylwrxtlpp")]
    [Arguments("18446744073709551616", "A", "GKGWBYLWRXTLPP")]
    [Arguments("18446744073709551616", "i", "18446744073709551616")]
    [Arguments(
        "18446744073709551616",
        "w",
        "eighteen quintillion four hundred and forty-six quadrillion seven hundred and forty-four trillion seventy-three billion seven hundred and nine million five hundred and fifty-one thousand six hundred and sixteen"
    )]
    [Arguments("1000000000000000000000000000000000000", "w", "1000000000000000000000000000000000000")]
    [Arguments("4000", "i", "4000")]
    [Arguments("3999", "I", "MMMCMXCIX")]
    [Arguments("1994", "i", "mcmxciv")]
    [Arguments("4", "i", "iv")]
    [Arguments("52", "a", "az")]
    [Arguments("53", "A", "BA")]
    public async Task ZeroNegativeAndWideValues(string value, string picture, string expected) =>
        await Assert.That(FormatIntegerHarness.Format(value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("1", "w", "one")]
    [Arguments("13", "w", "thirteen")]
    [Arguments("20", "w", "twenty")]
    [Arguments("100", "w", "one hundred")]
    [Arguments("101", "w", "one hundred and one")]
    [Arguments("1000", "w", "one thousand")]
    [Arguments("1001", "w", "one thousand and one")]
    [Arguments("1100", "w", "one thousand one hundred")]
    [Arguments("1234", "w", "one thousand two hundred and thirty-four")]
    [Arguments("1000000", "w", "one million")]
    [Arguments("123", "W", "ONE HUNDRED AND TWENTY-THREE")]
    [Arguments("123", "Ww", "One Hundred And Twenty-Three")]
    public async Task Words(string value, string picture, string expected) =>
        await Assert.That(FormatIntegerHarness.Format(value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("1", "1;o", "1st")]
    [Arguments("2", "1;o", "2nd")]
    [Arguments("3", "1;o", "3rd")]
    [Arguments("4", "1;o", "4th")]
    [Arguments("11", "1;o", "11th")]
    [Arguments("12", "1;o", "12th")]
    [Arguments("13", "1;o", "13th")]
    [Arguments("21", "1;o", "21st")]
    [Arguments("23", "1;o", "23rd")]
    [Arguments("111", "1;o", "111th")]
    [Arguments("0", "1;o", "0th")]
    [Arguments("-2", "1;o", "-2nd")]
    [Arguments("1", "w;o", "first")]
    [Arguments("2", "w;o", "second")]
    [Arguments("3", "w;o", "third")]
    [Arguments("5", "w;o", "fifth")]
    [Arguments("8", "w;o", "eighth")]
    [Arguments("9", "w;o", "ninth")]
    [Arguments("12", "w;o", "twelfth")]
    [Arguments("20", "w;o", "twentieth")]
    [Arguments("23", "w;o", "twenty-third")]
    [Arguments("100", "w;o", "one hundredth")]
    [Arguments("101", "w;o", "one hundred and first")]
    [Arguments("4", "w;o", "fourth")]
    [Arguments("0", "w;o", "zeroth")]
    [Arguments("1", "w;c", "one")]
    [Arguments("1", "1;c", "1")]
    [Arguments("1", "1;ot", "1st")]
    [Arguments("1", "1;a", "1")]
    [Arguments("1", "1;o(-er)", "1st")]
    [Arguments("1", "1;o(-er)a", "1st")]
    [Arguments("3", "a;o", "c")]
    public async Task Modifiers(string value, string picture, string expected) =>
        await Assert.That(FormatIntegerHarness.Format(value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("1234567", "#,##0", "1,234,567")]
    [Arguments("12", "#,##0", "12")]
    [Arguments("123", "#,##0", "123")]
    [Arguments("5", "0,000", "0,005")]
    [Arguments("1234567", "#,##,##0", "12,34,567")]
    [Arguments("123456789", "#,##,##0", "1234,56,789")]
    [Arguments("1234567", "#.##0", "1.234.567")]
    [Arguments("1234567", "#,##0,0", "123,456,7")]
    [Arguments("5", "###0", "5")]
    [Arguments("5", "#", "5")]
    [Arguments("5", "###", "5")]
    [Arguments("12345678", "0 000", "12 345 678")]
    [Arguments("5", "٠٠٠", "٠٠٥")]
    [Arguments("5", "１", "５")]
    public async Task DecimalPictures(string value, string picture, string expected) =>
        await Assert.That(FormatIntegerHarness.Format(value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("")]
    [Arguments(";o")]
    [Arguments("1;x")]
    [Arguments("1;oo")]
    [Arguments("1;o()")]
    [Arguments("1;o(")]
    [Arguments("1;ao")]
    [Arguments("0#")]
    [Arguments("#,0")]
    [Arguments("0,")]
    [Arguments(",0")]
    [Arguments("0,,0")]
    [Arguments("0٠")]
    [Arguments("0a")]
    [Arguments("0½")]
    public async Task InvalidPicture_IsFODF1310(string picture) =>
        await Assert.That(FormatIntegerHarness.Format("5", picture)).IsEqualTo("error:FODF1310");

    [Test]
    public async Task EmptyModifier_IsValid() =>
        await Assert.That(FormatIntegerHarness.Format("5", "1;")).IsEqualTo("5");

    [Test]
    [Arguments("5", "α", "5")]
    [Arguments("5", "WW", "5")]
    [Arguments("5", "x", "5")]
    [Arguments("5", "A;o", "E")]
    public async Task UnknownSequence_FallsBackToDecimal(string value, string picture, string expected) =>
        await Assert.That(FormatIntegerHarness.Format(value, picture)).IsEqualTo(expected);

    [Test]
    [Arguments("21", "w;o", "de", "twenty-first")]
    [Arguments("5", "1", "en", "5")]
    [Arguments("5", "1", "", "5")]
    public async Task Language_FallsBackToEnglish(string value, string picture, string language, string expected) =>
        await Assert.That(FormatIntegerHarness.Format(value, picture, language)).IsEqualTo(expected);

    [Test]
    [Arguments(2, XPathVersion.V20, false)]
    [Arguments(2, XPathVersion.V30, true)]
    [Arguments(3, XPathVersion.V30, true)]
    [Arguments(2, XPathVersion.V31, true)]
    [Arguments(3, XPathVersion.V31, true)]
    [Arguments(1, XPathVersion.V31, false)]
    [Arguments(4, XPathVersion.V31, false)]
    public async Task FormatInteger_IsFoundFromXPath30On(int arity, XPathVersion version, bool expected)
    {
        var found = FormatIntegerHarness.TryResolve(arity, out var function, version);

        await Assert.That(found).IsEqualTo(expected);
        if (found)
        {
            await Assert
                .That(function!.Name)
                .IsEqualTo(new XPathQName(FormatIntegerHarness.FunctionNamespace, "format-integer"));
        }
    }
}
