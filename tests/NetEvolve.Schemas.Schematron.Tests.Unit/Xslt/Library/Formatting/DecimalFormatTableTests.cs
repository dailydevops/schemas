namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Formatting;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Formatting.FormatFixture;

/// <summary>
/// The decimal formats of a schema (XSLT 2.0 section 16.4.1): declarations of one name are merged attribute by attribute, the highest
/// import precedence wins, and two values of one attribute at one precedence are XTSE1290.
/// </summary>
public class DecimalFormatTableTests
{
    private static XPathQName Name(string name) => new(string.Empty, name);

    private static (bool Ok, XsltDecimalFormatTable Table) Build(
        FormatFixture fixture,
        params (int, XmlElem)[] declarations
    )
    {
        var builder = new XsltDecimalFormatTableBuilder();
        foreach (var (precedence, element) in declarations)
        {
            builder.Add(fixture.CompileOne(element)!, precedence);
        }

        var ok = builder.TryBuild(fixture.Problems, out var table);
        return (ok, table);
    }

    private static XmlElem F(string? name, int line, params string[] attributes) => Format(name, line, attributes);

    private static XPathDecimalFormat Get(XsltDecimalFormatTable table, XPathQName name)
    {
        _ = table.TryGetDecimalFormat(name, out var format);
        return format!;
    }

    [Test]
    public async Task Build_UnnamedAndNamedFormats_AreFoundByTheirNames()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(fixture, (1, F(null, 10, "percent", "p")), (1, F("ch", 20, "percent", "q")));

        await Assert.That(ok).IsTrue();
        await Assert.That(table.Count).IsEqualTo(2);
        await Assert.That(Get(table, default).Percent).IsEqualTo('p');
        await Assert.That(Get(table, Name("ch")).Percent).IsEqualTo('q');
    }

    [Test]
    public async Task TryGetDecimalFormat_AnUndeclaredName_IsFalse()
    {
        var fixture = new FormatFixture();
        var (_, table) = Build(fixture, (1, F("ch", 10)));

        await Assert.That(table.TryGetDecimalFormat(Name("other"), out var format)).IsFalse();
        await Assert.That(format).IsNull();
        // No declaration of the unnamed format: the XPath package applies the default format.
        await Assert.That(table.TryGetDecimalFormat(default, out _)).IsFalse();
    }

    [Test]
    public async Task Build_AttributesThatAreLeftOut_HaveTheDefaultsOfTheSpecification()
    {
        var fixture = new FormatFixture();

        var (_, table) = Build(fixture, (1, F(null, 10)));

        var format = Get(table, default);
        var defaults = new XPathDecimalFormat();
        await Assert.That(format.DecimalSeparator).IsEqualTo(defaults.DecimalSeparator);
        await Assert.That(format.GroupingSeparator).IsEqualTo(defaults.GroupingSeparator);
        await Assert.That(format.Percent).IsEqualTo(defaults.Percent);
        await Assert.That(format.PerMille).IsEqualTo(defaults.PerMille);
        await Assert.That(format.ZeroDigit).IsEqualTo(defaults.ZeroDigit);
        await Assert.That(format.Digit).IsEqualTo(defaults.Digit);
        await Assert.That(format.PatternSeparator).IsEqualTo(defaults.PatternSeparator);
        await Assert.That(format.MinusSign).IsEqualTo(defaults.MinusSign);
        await Assert.That(format.Infinity).IsEqualTo("Infinity");
        await Assert.That(format.NaN).IsEqualTo("NaN");
    }

    [Test]
    public async Task Build_AllAttributes_ReachTheDecimalFormat()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(
            fixture,
            (
                1,
                F(
                    "all",
                    10,
                    "decimal-separator",
                    ",",
                    "grouping-separator",
                    ".",
                    "percent",
                    "p",
                    "per-mille",
                    "m",
                    "zero-digit",
                    "٠",
                    "digit",
                    "_",
                    "pattern-separator",
                    "|",
                    "minus-sign",
                    "~",
                    "infinity",
                    "inf",
                    "NaN",
                    "none"
                )
            )
        );

        var format = Get(table, Name("all"));
        await Assert.That(ok).IsTrue();
        await Assert.That(format.DecimalSeparator).IsEqualTo(',');
        await Assert.That(format.GroupingSeparator).IsEqualTo('.');
        await Assert.That(format.Percent).IsEqualTo('p');
        await Assert.That(format.PerMille).IsEqualTo('m');
        await Assert.That(format.ZeroDigit).IsEqualTo('٠');
        await Assert.That(format.Digit).IsEqualTo('_');
        await Assert.That(format.PatternSeparator).IsEqualTo('|');
        await Assert.That(format.MinusSign).IsEqualTo('~');
        await Assert.That(format.Infinity).IsEqualTo("inf");
        await Assert.That(format.NaN).IsEqualTo("none");
    }

    // The declarations of one name are grouped into one format; each attribute comes from a declaration that sets it.
    [Test]
    public async Task Build_DeclarationsOfOneName_AreMergedAttributeByAttribute()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(
            fixture,
            (1, F("ch", 10, "decimal-separator", ",")),
            (1, F("ch", 20, "grouping-separator", "."))
        );

        var format = Get(table, Name("ch"));
        await Assert.That(ok).IsTrue();
        await Assert.That(table.Count).IsEqualTo(1);
        await Assert.That(format.DecimalSeparator).IsEqualTo(',');
        await Assert.That(format.GroupingSeparator).IsEqualTo('.');
    }

    [Test]
    public async Task Build_TheSameValueTwiceAtOnePrecedence_IsNoConflict()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(
            fixture,
            (1, F("ch", 10, "decimal-separator", ",", "grouping-separator", ".")),
            (1, F("ch", 20, "decimal-separator", ","))
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(fixture.Problems.Problems).IsEmpty();
        await Assert.That(Get(table, Name("ch")).DecimalSeparator).IsEqualTo(',');
    }

    [Test]
    public async Task Build_SameNameInTwoNamespaces_AreTwoFormats()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(fixture, (1, F("ch", 10, "percent", "p")), (1, F("f:ch", 20, "percent", "q")));

        await Assert.That(ok).IsTrue();
        await Assert.That(table.Count).IsEqualTo(2);
    }

    // XTSE1290: two conflicting values for the same attribute of one format at the same import precedence.
    [Test]
    public async Task Build_ConflictingValuesAtOnePrecedence_AreXtse1290AtTheSecondDeclaration()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(
            fixture,
            (1, F("ch", 10, "decimal-separator", ",")),
            (1, F("ch", 20, "decimal-separator", ";")),
            (1, F("other", 30))
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.LetDuplicate + "/XTSE1290");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(20);
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("decimal-separator").And.Contains("ch");
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("line 10");
        // The format with the conflict is left out, the other one stays.
        await Assert.That(table.TryGetDecimalFormat(Name("ch"), out _)).IsFalse();
        await Assert.That(table.TryGetDecimalFormat(Name("other"), out _)).IsTrue();
    }

    [Test]
    public async Task Build_TwoConflictingUnnamedDeclarations_AreXtse1290()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(fixture, (1, F(null, 10, "NaN", "a")), (1, F(null, 20, "NaN", "b")));

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.LetDuplicate + "/XTSE1290");
        await Assert.That(table.TryGetDecimalFormat(default, out _)).IsFalse();
    }

    [Test]
    public async Task Build_ConflictsOfTwoAttributes_AreBothReported()
    {
        var fixture = new FormatFixture();

        var (ok, _) = Build(
            fixture,
            (1, F("ch", 10, "decimal-separator", ",", "infinity", "a")),
            (1, F("ch", 20, "decimal-separator", ";", "infinity", "b"))
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Build_ThreeDeclarationsWithTwoValues_IsOneConflictPerAttribute()
    {
        var fixture = new FormatFixture();

        var (ok, _) = Build(
            fixture,
            (1, F("ch", 10, "percent", "p")),
            (1, F("ch", 20, "percent", "q")),
            (1, F("ch", 30, "percent", "q"))
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(20);
    }

    // The highest import precedence wins, whatever the order of the declarations, and then there is no conflict.
    [Test]
    public async Task Build_HigherPrecedence_WinsInEitherOrder()
    {
        var fixture = new FormatFixture();

        var (okLowFirst, lowFirst) = Build(
            fixture,
            (1, F("ch", 10, "decimal-separator", ",")),
            (2, F("ch", 20, "decimal-separator", ";"))
        );
        var (okHighFirst, highFirst) = Build(
            fixture,
            (2, F("ch", 30, "decimal-separator", ";")),
            (1, F("ch", 40, "decimal-separator", ","))
        );

        await Assert.That(okLowFirst).IsTrue();
        await Assert.That(okHighFirst).IsTrue();
        await Assert.That(fixture.Problems.Problems).IsEmpty();
        await Assert.That(Get(lowFirst, Name("ch")).DecimalSeparator).IsEqualTo(';');
        await Assert.That(Get(highFirst, Name("ch")).DecimalSeparator).IsEqualTo(';');
    }

    // A lower precedence still gives the attributes that the higher one does not set.
    [Test]
    public async Task Build_LowerPrecedence_GivesTheAttributesTheHigherOneLeavesOut()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(
            fixture,
            (1, F("ch", 10, "decimal-separator", ",", "grouping-separator", ".")),
            (2, F("ch", 20, "decimal-separator", ";"))
        );

        var format = Get(table, Name("ch"));
        await Assert.That(ok).IsTrue();
        await Assert.That(format.DecimalSeparator).IsEqualTo(';');
        await Assert.That(format.GroupingSeparator).IsEqualTo('.');
    }

    // "Unless there is another definition of the same attribute with higher import precedence": the conflict below is overruled.
    [Test]
    public async Task Build_ConflictAtALowerPrecedence_IsOverruledByAHigherDefinition()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(
            fixture,
            (1, F("ch", 10, "decimal-separator", ",")),
            (1, F("ch", 20, "decimal-separator", ";")),
            (2, F("ch", 30, "decimal-separator", "!"))
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(fixture.Problems.Problems).IsEmpty();
        await Assert.That(Get(table, Name("ch")).DecimalSeparator).IsEqualTo('!');
    }

    // The conflict of the top precedence stays a conflict, also when a lower precedence has a third value.
    [Test]
    public async Task Build_ConflictAtTheHighestPrecedence_IsNotOverruledByALowerOne()
    {
        var fixture = new FormatFixture();

        var (ok, _) = Build(
            fixture,
            (1, F("ch", 10, "decimal-separator", "!")),
            (2, F("ch", 20, "decimal-separator", ",")),
            (2, F("ch", 30, "decimal-separator", ";"))
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.LetDuplicate + "/XTSE1290");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(30);
    }

    // XTSE1295: the zero digit has to be a digit of category Nd with the value zero.
    [Test]
    [Arguments("x")]
    [Arguments("1")]
    public async Task Build_ZeroDigitThatIsNoZero_IsXtse1295(string zeroDigit)
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(fixture, (1, F("ch", 10, "zero-digit", zeroDigit)));

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeInvalid + "/XTSE1295");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
        await Assert.That(table.Count).IsEqualTo(0);
    }

    // XTSE1300: the characters of a picture are distinct, in the merged format and not per declaration.
    [Test]
    public async Task Build_CharactersThatAreNotDistinct_AreXtse1300()
    {
        var fixture = new FormatFixture();

        var (ok, table) = Build(
            fixture,
            (1, F("ch", 10, "decimal-separator", ",")),
            (1, F("ch", 20, "grouping-separator", ","))
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeInvalid + "/XTSE1300");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
        await Assert.That(table.TryGetDecimalFormat(Name("ch"), out _)).IsFalse();
    }

    [Test]
    public async Task Build_ZeroDigitThatClashesWithAnotherCharacter_IsXtse1300()
    {
        var fixture = new FormatFixture();

        var (ok, _) = Build(fixture, (1, F("ch", 10, "zero-digit", "٠", "digit", "٠")));

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeInvalid + "/XTSE1300");
    }

    // The minus sign is no character of a picture (XSLT 2.0 XTSE1300 lists seven variables, and it is none of them).
    [Test]
    public async Task Build_MinusSignEqualToAPictureCharacter_IsAccepted()
    {
        var fixture = new FormatFixture();

        var (ok, _) = Build(fixture, (1, F("ch", 10, "minus-sign", ",")));

        await Assert.That(ok).IsTrue();
    }

    [Test]
    public async Task Build_NoDeclaration_IsAnEmptyTable()
    {
        var builder = new XsltDecimalFormatTableBuilder();
        var fixture = new FormatFixture();

        var ok = builder.TryBuild(fixture.Problems, out var table);

        await Assert.That(ok).IsTrue();
        await Assert.That(table.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Builder_WithNullArguments_Throws()
    {
        var builder = new XsltDecimalFormatTableBuilder();

        await Assert.That(() => builder.Add(null!, 1)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.TryBuild(null!, out _)).Throws<ArgumentNullException>();
    }
}
