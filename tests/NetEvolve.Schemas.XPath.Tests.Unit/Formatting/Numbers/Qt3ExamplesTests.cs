namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Numbers;

using System.Threading.Tasks;

/// <summary>
/// Cases of the QT3 test set <c>fn/format-number</c> that run with the default decimal format, as a table of value, picture, and result
/// under the 3.1 rules. The set itself joins the QT3 selection with XP-74; these rows keep the engine in line with it until then. The name
/// of each case is in the comment of its group.
/// </summary>
public class Qt3ExamplesTests
{
    // numberformat01 to 08, 36, 72, 80, 84, 89 to 95, and numberformat60a.
    [Test]
    [Arguments("decimal:87504.4812", "000,000.000000", "087,504.481200")]
    [Arguments("decimal:1235464.8812", "##,###,000.000###", "1,235,464.8812")]
    [Arguments("decimal:-102136.4812", "000,000.000###", "-102,136.4812")]
    [Arguments("decimal:0.4857", "###.###%", "48.57%")]
    [Arguments("decimal:0.4857", "###.###‰", "485.7‰")]
    [Arguments("decimal:95.4857", "¤###.####", "¤95.4857")]
    [Arguments("decimal:185.2812", "PREFIX##00.000###SUFFIX", "PREFIX185.2812SUFFIX")]
    [Arguments("decimal:239236.588", "00000.00", "239236.59")]
    [Arguments("decimal:1234567890.123456", "000.000", "1234567890.123")]
    [Arguments("decimal:12.34", "##.##", "12.34")]
    [Arguments("decimal:123456789.34", "#,###.##", "123,456,789.34")]
    [Arguments("decimal:12.34", "9,999.99", "0,012.34")]
    [Arguments("decimal:12.34", "#,##9.99", "12.34")]
    [Arguments("integer:42", "001", "042")]
    [Arguments("decimal:12345.678", "9.", "12346")]
    [Arguments("decimal:12345.000", "99999.", "12345")]
    [Arguments("decimal:12345.678", ".9", "12345.7")]
    [Arguments("decimal:0.2", ".#", ".2")]
    [Arguments("double:1E25", "#,######", "10,000000,000000,000000,000000")]
    [Arguments("double:1E10", "#####################", "10000000000")]
    [Arguments(
        "double:1E100",
        "#####################",
        "10000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"
    )]
    public async Task Qt3_PlainPictures(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    // numberformat201 to 229 and cbcl-fn-format-number-018 to 052: the minimum sizes of the integer and the fraction part.
    [Test]
    [Arguments("decimal:0.2", "#", "0")]
    [Arguments("decimal:1.2", "#", "1")]
    [Arguments("decimal:0.2", ".#", ".2")]
    [Arguments("decimal:1.2", ".#", "1.2")]
    [Arguments("decimal:0.2", ".0", ".2")]
    [Arguments("decimal:0.2", "#.#", ".2")]
    [Arguments("decimal:1.2", "#.#", "1.2")]
    [Arguments("decimal:0.2", "0.0", "0.2")]
    [Arguments("decimal:0.2", "0.#", "0.2")]
    [Arguments("decimal:0.2", "#.0", ".2")]
    [Arguments("decimal:0.2", "#.", "0")]
    [Arguments("decimal:1.2", "#.", "1")]
    [Arguments("decimal:0.2", "0.", "0")]
    [Arguments("decimal:0.2", "this.#", "this.2")]
    [Arguments("decimal:1.2", "this.#", "this1.2")]
    [Arguments("decimal:1.2", "this.0", "this1.2")]
    [Arguments("decimal:0.2", "#.suffix", "0suffix")]
    [Arguments("decimal:1.2", "#.suffix", "1suffix")]
    [Arguments("decimal:0.2", "000.0", "000.2")]
    [Arguments("decimal:0", "#.#", ".0")]
    [Arguments("decimal:0", "#.00", ".00")]
    [Arguments("decimal:0", "#.", "0")]
    [Arguments("decimal:0", "###,###.##", ".0")]
    [Arguments("decimal:0.1", "###,###.##", ".1")]
    [Arguments("double:0", "#.##", ".0")]
    [Arguments("double:0", "0.00", "0.00")]
    [Arguments("double:0.01", "0.00", "0.01")]
    [Arguments("double:0.9952832031249997", "###,##0.00", "1.00")]
    [Arguments("decimal:1.10203040506070809", "0.00000000000000000%", "110.20304050607080900%")]
    [Arguments("double:1e308", "0%", "Infinity%")]
    [Arguments("double:-1e308", "0%", "-Infinity%")]
    public async Task Qt3_MinimumSizes(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    // numberformat310 to 319 (the grouping of 642120) and numberformat320, cbcl-fn-format-number-011 to 016 and 023 to 029.
    [Test]
    [Arguments("integer:642120", "####,##", "6421,20")]
    [Arguments("integer:642120", "###,##", "6421,20")]
    [Arguments("integer:642120", "##,##", "64,21,20")]
    [Arguments("integer:642120", "#,##", "64,21,20")]
    [Arguments("integer:642120", "0000,00", "6421,20")]
    [Arguments("integer:642120", "000,00", "6421,20")]
    [Arguments("integer:642120", "00,00", "64,21,20")]
    [Arguments("integer:642120", "0,00", "64,21,20")]
    [Arguments("integer:642120", "#,#,#", "6,4,2,1,2,0")]
    [Arguments("integer:642120", "##,#,#", "6421,2,0")]
    [Arguments("decimal:123456789", "#,#", "1,2,3,4,5,6,7,8,9")]
    [Arguments("decimal:123456789", "#,##,00", "1,23,45,67,89")]
    [Arguments("decimal:123456789", "##,##,00", "1,23,45,67,89")]
    [Arguments("decimal:123456789", "#,##,##,00", "1,23,45,67,89")]
    [Arguments("decimal:123456789", "###,##,00", "12345,67,89")]
    [Arguments("decimal:123456789", "####,###,##,0", "123,456,78,9")]
    [Arguments("double:123456789", "#,##,00", "1,23,45,67,89")]
    [Arguments("double:123456789", "###,##,00", "12345,67,89")]
    [Arguments("integer:987654321", "###,##0,00.00", "9876,543,21.00")]
    public async Task Qt3_GroupingOfIntegerDigits(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    // numberformat157 and 158: grouping of the fraction part.
    [Test]
    [Arguments("decimal:12345.6789012345", "#.#,##,#", "12345.6,78,9")]
    [Arguments("decimal:12345.6789012345", "#.##,##,##", "12345.67,89,01")]
    public async Task Qt3_GroupingOfFractionDigits(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    // numberformat101, 113, 115 to 120, 129 to 146, 231 to 263, 301 to 304, and 321 to 327.
    [Test]
    [Arguments("decimal:12345.678", "9.9999e999", "1.2346e004")]
    [Arguments("decimal:12345.678", "9.9999eDog", "12345.6780eDog")]
    [Arguments("integer:12345", "9.9999e99", "1.2345e04")]
    [Arguments("integer:5", "9.9999e99", "5.0000e00")]
    [Arguments("decimal:0.05", "9.9999e99", "5.0000e-02")]
    [Arguments("decimal:0.900001000020000345", "9.99999999999999999e99", "9.00001000020000345e-01")]
    [Arguments("integer:123456789012345678", "9.99999999999999999e99", "1.23456789012345678e17")]
    [Arguments("decimal:12345.678", "999.99e99", "123.46e02")]
    [Arguments("decimal:-12345.678", "999.99e99", "-123.46e02")]
    [Arguments("decimal:0.00012345678", "9.99e99", "1.23e-04")]
    [Arguments("decimal:-0.00012345678", "9.99e99", "-1.23e-04")]
    [Arguments("decimal:12345.678", "9.9999e99end", "1.2346e04end")]
    [Arguments("decimal:12345.678", "9.9999e99e", "1.2346e04e")]
    [Arguments("decimal:12345.678", "end9.9999e99end", "end1.2346e04end")]
    [Arguments("decimal:12345.678", "#99.99e99", "12.35e03")]
    [Arguments("decimal:12345.678", "#.99e99", "0.12e05")]
    [Arguments("decimal:45.234", "#.#e99", "0.5e02")]
    [Arguments("decimal:0.2", ".#e9", ".2e0")]
    [Arguments("decimal:0.2", ".999e9", ".200e0")]
    [Arguments("decimal:0.2", "#.e9", "0.2e0")]
    [Arguments("decimal:0.2", "9e9", "2e-1")]
    [Arguments("decimal:12345.678", "999.e99", "123e02")]
    [Arguments("decimal:12345.678", "999e9", "123e2")]
    [Arguments("decimal:0.2", "#e0", "0.2e0")]
    [Arguments("decimal:1.2", "#e0", "0.1e1")]
    [Arguments("decimal:0.2", "0e0", "2e-1")]
    [Arguments("decimal:1.2", "0e0", "1e0")]
    [Arguments("decimal:0.2", ".#e0", ".2e0")]
    [Arguments("decimal:1.2", ".#e0", ".1e1")]
    [Arguments("decimal:0.2", "#.#e0", "0.2e0")]
    [Arguments("decimal:1.2", "#.#e0", "0.1e1")]
    [Arguments("decimal:0.2", "0.0e0", "2.0e-1")]
    [Arguments("decimal:1.2", "0.0e0", "1.2e0")]
    [Arguments("decimal:0.2", "0.#e0", "2e-1")]
    [Arguments("decimal:1.2", "0.#e0", "1.2e0")]
    [Arguments("decimal:0.2", "#.0e0", "0.2e0")]
    [Arguments("decimal:1.2", "#.0e0", "0.1e1")]
    [Arguments("decimal:0.2", "0.e0", "2e-1")]
    [Arguments("decimal:1.2", "0.e0", "1e0")]
    [Arguments("decimal:0.2", "this.#e0", "this.2e0")]
    [Arguments("decimal:1.2", "this.#e0", "this.1e1")]
    [Arguments("decimal:0.2", "000.0e0", "200.0e-3")]
    [Arguments("decimal:0.002", ".0e0", ".2e-2")]
    [Arguments("decimal:0.002", ".000e0", ".200e-2")]
    [Arguments("decimal:0.002", "#.###e0", "0.2e-2")]
    [Arguments("double:0", "0.0e0", "0.0e0")]
    [Arguments("double:-0", "0.0e0", "-0.0e0")]
    [Arguments("double:0", "0.0e01", "0.0e00")]
    [Arguments("integer:0", "0.0e01", "0.0e00")]
    [Arguments("integer:0", "0e01", "0e00")]
    [Arguments("integer:0", "#.#e9", "0e0")]
    [Arguments("decimal:0.00000000", "#.0e9", "0.0e0")]
    public async Task Qt3_ExponentPictures(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    // The pictures that the QT3 set expects to be FODF1310: numberformat102, 108, 110, 114, 128, 139, 150 to 153, 156, 902err, 905err.
    [Test]
    [Arguments("9.9999E999")]
    [Arguments("9.99e99e99")]
    [Arguments("9.9999e999%")]
    [Arguments("9.9999e,")]
    [Arguments(".e99")]
    [Arguments("#,.##")]
    [Arguments("#.,##")]
    [Arguments("#,")]
    [Arguments("#,,###")]
    [Arguments("#.#,,#")]
    [Arguments("000.##0")]
    [Arguments("fred.ginger")]
    public async Task Qt3_PicturesThatAreErrors(string picture) =>
        await Assert.That(FormatNumberHarness.Xpath31("decimal:12345.678", picture)).IsEqualTo("error:FODF1310");

    // numberformat85, cbcl-fn-format-number-002 and 003: the sign of a zero.
    [Test]
    [Arguments("integer:0", "#", "0")]
    [Arguments("decimal:0.0", "#", "0")]
    [Arguments("double:0", "#", "0")]
    [Arguments("float:0", "#", "0")]
    [Arguments("double:-0", "#", "-0")]
    public async Task Qt3_SignOfZero(string value, string picture, string expected) =>
        await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);
}
