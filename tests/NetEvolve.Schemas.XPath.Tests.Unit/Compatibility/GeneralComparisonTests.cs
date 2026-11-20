namespace NetEvolve.Schemas.XPath.Tests.Unit.Compatibility;

using static CompatibilityFixture;

/// <summary>
/// The general comparisons of XPath 2.0, 3.5.2 and Appendix I in compatibility mode: a single boolean operand turns the other operand
/// into its effective boolean value, the relational operators convert every item by <c>fn:number</c>, and a pair with a number is
/// compared as two doubles. Without the mode a boolean against another type, a number against a string, and a failed cast are errors,
/// and two strings are compared by codepoint.
/// </summary>
public class GeneralComparisonTests
{
    [Test]
    [Arguments("$yes = 'false'", "true", "XPTY0004")]
    [Arguments("$no = 'false'", "false", "XPTY0004")]
    [Arguments("'x' = $yes", "true", "XPTY0004")]
    [Arguments("$yes = $two", "FORG0006", "XPTY0004")]
    [Arguments("$no = $a", "false", "FORG0001")]
    [Arguments("$yes != $b", "false", "FORG0001")]
    [Arguments("$yes = 0.5", "true", "XPTY0004")]
    public async Task ASingleBooleanOperand_ConvertsTheOtherOperandToABoolean(
        string expression,
        string compatibility,
        string standard
    ) => await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("$yes > 0.5", "false", "XPTY0004")]
    [Arguments("$no < 0.5", "false", "XPTY0004")]
    public async Task ABooleanAgainstANumber_IsNotComparedAsNumbersInXPath20(
        string expression,
        string compatibility,
        string standard
    ) =>
        // Appendix I: true() > number('0.5') is true in XPath 1.0 and false in XPath 2.0, compatibility mode included.
        await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("'10' < '9'", "false", "true")]
    [Arguments("'10' > '9'", "true", "false")]
    [Arguments("'abc' < 'abd'", "false", "true")]
    [Arguments("$strs > 'a'", "false", "true")]
    [Arguments("$word <= 1", "false", "FORG0001")]
    public async Task RelationalOperators_ConvertEveryItemToADouble(
        string expression,
        string compatibility,
        string standard
    ) => await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("1 = '1'", "true", "XPTY0004")]
    [Arguments("'1.0' = 1", "true", "XPTY0004")]
    [Arguments("'x' = 1", "false", "XPTY0004")]
    [Arguments("'x' != 1", "true", "XPTY0004")]
    [Arguments("$word = 1", "false", "FORG0001")]
    [Arguments("$word != 1", "true", "FORG0001")]
    [Arguments("$text3 = 3.0", "true", "XPTY0004")]
    [Arguments("$dec = '2.5'", "true", "XPTY0004")]
    public async Task APairWithANumber_IsComparedAsTwoDoubles(
        string expression,
        string compatibility,
        string standard
    ) => await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("$u3 = 3")]
    [Arguments("$two = 2")]
    [Arguments("$two != 1")]
    [Arguments("'a' = 'a'")]
    [Arguments("$a = '2'")]
    [Arguments("$yes = $yes")]
    public async Task ARuleThatXPath20KeepsInCompatibilityMode_GivesTheSameResultInBothModes(string expression) =>
        await Assert.That(Run(expression, compatibility: true)).IsEqualTo(Run(expression, compatibility: false));
}
