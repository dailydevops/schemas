namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Formatting;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Formatting.FormatFixture;

/// <summary>
/// <c>format-number</c> under an XSLT binding reads the declared formats (XSLT 2.0 section 16.4): the function is the one of the XPath
/// package, and the declarations of the schema are the provider of its decimal format slot.
/// </summary>
public class FormatNumberWiringTests
{
    private const string Swiss = "#ʹ##0·00";

    private static string Run(XsltDecimalFormatTable? table, XdmItem number, string picture, string? name = null)
    {
        using var context = new XPathEvaluationContext();
        context.Host.DecimalFormats = table;
        try
        {
            return name is null
                ? RunIn(Scope(), context, "format-number", number, S(picture))
                : RunIn(Scope(), context, "format-number", number, S(picture), S(name));
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    [Test]
    public async Task FormatNumber_TwoArguments_PadsWithTheDefaultFormat()
    {
        var table = new FormatFixture().Build(FormatFixture.Format("ch", 10, "decimal-separator", ","));

        // No declaration of the unnamed format: the default format applies, with and without declared formats.
        await Assert.That(Run(table, I(5), "0000")).IsEqualTo("string:0005");
        await Assert.That(Run(null, I(5), "0000")).IsEqualTo("string:0005");
    }

    [Test]
    public async Task FormatNumber_ThreeArguments_AppliesTheNamedFormat()
    {
        var table = new FormatFixture().Build(
            FormatFixture.Format("ch", 10, "decimal-separator", "·", "grouping-separator", "ʹ")
        );

        await Assert.That(Run(table, XdmItem.FromDouble(1234.5678), Swiss, "ch")).IsEqualTo("string:1ʹ234·57");
    }

    [Test]
    public async Task FormatNumber_TheNamedFormat_LeavesTheDefaultFormatAlone()
    {
        var table = new FormatFixture().Build(
            FormatFixture.Format("ch", 10, "decimal-separator", "·", "grouping-separator", "ʹ")
        );

        await Assert.That(Run(table, XdmItem.FromDouble(1234.5), "#,##0.0")).IsEqualTo("string:1,234.5");
    }

    [Test]
    public async Task FormatNumber_UnnamedDeclaration_ReplacesTheDefaultFormat()
    {
        var table = new FormatFixture().Build(
            FormatFixture.Format(null, 10, "decimal-separator", ",", "grouping-separator", ".")
        );

        await Assert.That(Run(table, XdmItem.FromDouble(1234.5), "#.##0,00")).IsEqualTo("string:1.234,50");
    }

    [Test]
    public async Task FormatNumber_ZeroDigitOfAnotherScript_WritesTheDigitsOfThatScript()
    {
        var table = new FormatFixture().Build(FormatFixture.Format("ar", 10, "zero-digit", "٠"));

        await Assert.That(Run(table, I(123), "٠٠٠٠", "ar")).IsEqualTo("string:٠١٢٣");
    }

    [Test]
    public async Task FormatNumber_PercentAndInfinity_AreTheDeclaredSymbols()
    {
        var table = new FormatFixture().Build(
            FormatFixture.Format(null, 10, "percent", "p", "infinity", "inf", "NaN", "none")
        );

        await Assert.That(Run(table, XdmItem.FromDouble(0.25), "0p")).IsEqualTo("string:25p");
        await Assert.That(Run(table, XdmItem.FromDouble(double.PositiveInfinity), "0")).IsEqualTo("string:inf");
        await Assert.That(Run(table, XdmItem.FromDouble(double.NaN), "0")).IsEqualTo("string:none");
    }

    [Test]
    public async Task FormatNumber_UnknownFormatName_IsXtde1280()
    {
        var table = new FormatFixture().Build(FormatFixture.Format("ch", 10, "decimal-separator", ","));

        await Assert.That(Run(table, I(5), "0", "other")).IsEqualTo("error:XTDE1280");
        await Assert.That(Run(table, I(5), "0", "1a")).IsEqualTo("error:XTDE1280");
    }

    [Test]
    public async Task FormatNumber_IsTheFunctionOfTheXPathPackage()
    {
        _ = Scope().TryResolve(Fn("format-number"), 3, out var function);

        await Assert.That(function).IsNotNull();
        await Assert.That(function!.GetType().Assembly).IsEqualTo(typeof(XPathEvaluationContext).Assembly);
        await Assert.That(typeof(XsltDecimalFormatTable).Assembly).IsNotEqualTo(function.GetType().Assembly);
    }
}
