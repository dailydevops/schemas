namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Numbers;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Formatting.Numbers;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The number formatter on its own: an item that is no number, and a rounding that carries into the digits that follow.</summary>
public class NumberFormatterTests
{
    // The function binds a number to its first parameter, so the formatter's own check is reached by a direct call only.
    [Test]
    public async Task Format_ItemThatIsNoNumber_ThrowsXPTY0004()
    {
        var code = string.Empty;
        try
        {
            FormatText(XdmItem.FromString("abc"));
        }
        catch (XPathException failure)
        {
            code = failure.Diagnostic.Code;
        }

        await Assert.That(code).IsEqualTo(XPathErrorCodes.XPTY0004.ToString());
    }

    // A rounding up that carries through nines leaves zeros at the end of the kept digits, and they are not digits of the number.
    [Test]
    [Arguments("decimal:12.96", "0.0", "13.0")]
    [Arguments("decimal:1.996", "0.##", "2")]
    [Arguments("decimal:0.1996", "0.00", "0.20")]
    public async Task Format_RoundingThatCarries_DropsTheTrailingZerosOfTheKeptDigits(
        string value,
        string picture,
        string expected
    ) => await Assert.That(FormatNumberHarness.Xpath31(value, picture)).IsEqualTo(expected);

    private static void FormatText(XdmItem item)
    {
        _ = NumberPictureParser.TryParse(
            "0",
            DecimalFormatModel.Default,
            NumberPictureRules.Xpath31,
            out var picture,
            out _
        );
        var builder = new TextBuilder(stackalloc char[16], XPathLimits.Default);
        try
        {
            NumberFormatter.Format(in item, picture!, DecimalFormatModel.Default, ref builder);
        }
        finally
        {
            builder.Dispose();
        }
    }
}
