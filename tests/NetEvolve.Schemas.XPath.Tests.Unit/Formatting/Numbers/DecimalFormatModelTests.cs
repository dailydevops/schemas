namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Numbers;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Formatting.Numbers;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The decimal format model with all its properties, the exponent separator that the 3.1 rules add, the validation that a host reaches
/// before a call, and the function built over its own namespace context.
/// </summary>
public class DecimalFormatModelTests
{
    [Test]
    public async Task DefaultFormat_HasTheSpecificationDefaults()
    {
        var format = DecimalFormatModel.Default;

        await Assert.That(format.DecimalSeparator).IsEqualTo('.');
        await Assert.That(format.GroupingSeparator).IsEqualTo(',');
        await Assert.That(format.ExponentSeparator).IsEqualTo('e');
        await Assert.That(format.MinusSign).IsEqualTo('-');
        await Assert.That(format.Percent).IsEqualTo('%');
        await Assert.That(format.PerMille).IsEqualTo('‰');
        await Assert.That(format.ZeroDigit).IsEqualTo('0');
        await Assert.That(format.Digit).IsEqualTo('#');
        await Assert.That(format.PatternSeparator).IsEqualTo(';');
        await Assert.That(format.Infinity).IsEqualTo("Infinity");
        await Assert.That(format.NaN).IsEqualTo("NaN");
    }

    [Test]
    public async Task TryCreate_SnapshotsEveryProperty_SoALaterChangeDoesNotReachTheModel()
    {
        var source = new XPathDecimalFormat
        {
            DecimalSeparator = ',',
            GroupingSeparator = '.',
            ExponentSeparator = 'E',
            MinusSign = '~',
            Percent = 'p',
            PerMille = 'm',
            ZeroDigit = '٠',
            Digit = 'd',
            PatternSeparator = '|',
            Infinity = "inf",
            NaN = "nan",
        };

        var created = DecimalFormatModel.TryCreate(source, NumberPictureRules.Xpath31, out var model, out var problem);
        source.DecimalSeparator = ';';

        await Assert.That(created).IsTrue();
        await Assert.That(problem).IsNull();
        await Assert.That(model!.DecimalSeparator).IsEqualTo(',');
        await Assert.That(model.GroupingSeparator).IsEqualTo('.');
        await Assert.That(model.ExponentSeparator).IsEqualTo('E');
        await Assert.That(model.MinusSign).IsEqualTo('~');
        await Assert.That(model.Percent).IsEqualTo('p');
        await Assert.That(model.PerMille).IsEqualTo('m');
        await Assert.That(model.ZeroDigit).IsEqualTo('٠');
        await Assert.That(model.Digit).IsEqualTo('d');
        await Assert.That(model.PatternSeparator).IsEqualTo('|');
        await Assert.That(model.Infinity).IsEqualTo("inf");
        await Assert.That(model.NaN).IsEqualTo("nan");
    }

    [Test]
    public async Task XPathDecimalFormat_ExponentSeparator_DefaultsToTheLowercaseLetterE() =>
        await Assert.That(new XPathDecimalFormat().ExponentSeparator).IsEqualTo('e');

    // The characters a picture reads must differ: the 3.1 rules add the exponent separator and every digit of the family.
    [Test]
    public async Task TryCreate_ExponentSeparatorThatClashes_IsInvalidUnder31AndValidUnderXslt20()
    {
        var format = new XPathDecimalFormat { ExponentSeparator = '#' };

        var xpath31 = DecimalFormatModel.TryCreate(format, NumberPictureRules.Xpath31, out _, out var problem31);
        var xslt20 = DecimalFormatModel.TryCreate(format, NumberPictureRules.Xslt20, out var model, out _);

        await Assert.That(xpath31).IsFalse();
        await Assert.That(problem31).IsNotNull();
        await Assert.That(xslt20).IsTrue();
        await Assert.That(model).IsNotNull();
    }

    [Test]
    public async Task TryCreate_AnotherDigitOfTheFamilyAsASeparator_IsInvalidUnder31AndValidUnderXslt20()
    {
        var format = new XPathDecimalFormat { DecimalSeparator = '5' };

        await Assert.That(DecimalFormatModel.TryCreate(format, NumberPictureRules.Xpath31, out _, out _)).IsFalse();
        await Assert.That(DecimalFormatModel.TryCreate(format, NumberPictureRules.Xslt20, out _, out _)).IsTrue();
    }

    [Test]
    public async Task TryCreate_NullStrings_AreInvalid()
    {
        await Assert
            .That(
                DecimalFormatModel.TryCreate(
                    new XPathDecimalFormat { Infinity = null! },
                    NumberPictureRules.Xpath31,
                    out _,
                    out _
                )
            )
            .IsFalse();
        await Assert
            .That(
                DecimalFormatModel.TryCreate(
                    new XPathDecimalFormat { NaN = null! },
                    NumberPictureRules.Xslt20,
                    out _,
                    out _
                )
            )
            .IsFalse();
    }

    [Test]
    public async Task UnknownFormatError_NullName_ThrowsArgumentNullException() =>
        await Assert.That(() => FormatNumberErrors.UnknownFormat(false, null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task TryCreate_NullFormat_ThrowsArgumentNullException() =>
        await Assert
            .That(() => DecimalFormatModel.TryCreate(null!, NumberPictureRules.Xpath31, out _, out _))
            .Throws<ArgumentNullException>();

    // The examples of Functions and Operators 3.1 (4.7.2) with the exponent separator E of the decimal format fortran.
    [Test]
    [Arguments("decimal:1234.5678", "00.000E0", "12.346E2")]
    [Arguments("decimal:0.234", "0.0E0", "2.3E-1")]
    [Arguments("decimal:0.234", "#.00E0", "0.23E0")]
    [Arguments("decimal:0.234", ".00E0", ".23E0")]
    [Arguments("integer:-1234", "0.0E0", "-1.2E3")]
    public async Task Exponent_WithTheExponentSeparatorOfTheFormat_UsesThatCharacter(
        string value,
        string picture,
        string expected
    )
    {
        var formats = new FormatNumberHarness.Formats().Add(
            "fortran",
            new XPathDecimalFormat { ExponentSeparator = 'E' }
        );

        await Assert.That(FormatNumberHarness.Xpath31(value, picture, "fortran", formats)).IsEqualTo(expected);
    }

    // The default exponent separator is the lowercase e, so the capital letter is a passive character of a picture for the default format.
    [Test]
    public async Task Exponent_CapitalLetterWithTheDefaultFormat_IsPassive() =>
        await Assert.That(FormatNumberHarness.Xpath31("decimal:1.5", "0.0E0")).IsEqualTo("error:FODF1310");

    // The exponent separator is read under the 3.1 rules only: a clash with another character is no error under XSLT 2.0.
    [Test]
    public async Task Exponent_PropertyThatClashesWithTheDigitSign_IsIgnoredUnderXslt20()
    {
        var formats = new FormatNumberHarness.Formats().Add(
            "clash",
            new XPathDecimalFormat { ExponentSeparator = '#' }
        );

        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "#0", "clash", formats)).IsEqualTo("5");
        await Assert.That(FormatNumberHarness.Xpath31("integer:5", "#0", "clash", formats)).IsEqualTo("error:FODF1310");
    }

    // A picture whose mantissa has no decimal separator and no mandatory digit gets the adjusted fraction size, and the separator is written (limitation).
    [Test]
    public async Task Exponent_PictureWithoutADecimalSeparator_GetsTheAdjustedFractionSize() =>
        await Assert.That(FormatNumberHarness.Xpath31("decimal:0.123", "#e0")).IsEqualTo("0.1e0");

    // A prefixed name is resolved by the namespace context the function was built with; the default catalog has none (#752).
    [Test]
    public async Task PrefixedName_WithANamespaceContext_IsResolved()
    {
        var formats = new FormatNumberHarness.Formats().Add(
            new XPathQName("urn:x", "fmt"),
            new XPathDecimalFormat { GroupingSeparator = '.', DecimalSeparator = ',' }
        );
        var namespaces = new Namespaces("p", "urn:x");
        using var context = new XPathEvaluationContext();
        context.Host.DecimalFormats = formats;
        XdmSequence[] arguments =
        [
            XdmSequence.Create(XdmItem.FromDecimal(XsdDecimal.Parse("1234.5").Value)),
            XdmSequence.Create(XdmItem.FromString("#.##0,0")),
            XdmSequence.Create(XdmItem.FromString("p:fmt")),
        ];

        var xpath3 = FormatNumberHarness.Call(FormatNumberFunction.ForXPath3(namespaces), context, arguments);
        var xslt20 = FormatNumberHarness.Call(FormatNumberFunction.ForXslt20(namespaces), context, arguments);
        var unbound = FormatNumberHarness.Call(FormatNumberFunction.ForXPath3(), context, arguments);

        await Assert.That(xpath3).IsEqualTo("1.234,5");
        await Assert.That(xslt20).IsEqualTo("1.234,5");
        await Assert.That(unbound).IsEqualTo("error:FODF1280");
    }

    private sealed class Namespaces(string prefix, string namespaceName) : INamespaceContext
    {
        public bool TryResolvePrefix(ReadOnlySpan<char> candidate, out string? resolved)
        {
            var found = candidate.SequenceEqual(prefix.AsSpan());
            resolved = found ? namespaceName : null;
            return found;
        }
    }
}
