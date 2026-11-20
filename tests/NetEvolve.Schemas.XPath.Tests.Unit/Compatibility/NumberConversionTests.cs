namespace NetEvolve.Schemas.XPath.Tests.Unit.Compatibility;

using static CompatibilityFixture;

/// <summary>
/// The number conversion of XPath 2.0, 3.4 and Appendix I: in compatibility mode an arithmetic operand that is a boolean, a string, a
/// decimal (an integer included), a float, or untyped is converted to <c>xs:double</c> by <c>fn:number</c>, an empty operand gives NaN,
/// and every operator works on doubles. Without the mode each of these has its own operator, an empty operand gives the empty
/// sequence, and a string or a boolean is XPTY0004.
/// </summary>
public class NumberConversionTests
{
    [Test]
    [Arguments("$text3 + 1", "double:4", "XPTY0004")]
    [Arguments("'3' * '4'", "double:12", "XPTY0004")]
    [Arguments("$yes + 1", "double:2", "XPTY0004")]
    [Arguments("$no * 5", "double:0", "XPTY0004")]
    [Arguments("$word + 1", "double:NaN", "FORG0001")]
    [Arguments("'x' - 1", "double:NaN", "XPTY0004")]
    [Arguments("+$text3", "double:3", "XPTY0004")]
    [Arguments("-$yes", "double:-1", "XPTY0004")]
    public async Task Operand_IsConvertedByFnNumber(string expression, string compatibility, string standard) =>
        await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("$none + 1", "double:NaN", "()")]
    [Arguments("1 + $none", "double:NaN", "()")]
    [Arguments("$none * $none", "double:NaN", "()")]
    [Arguments("-$none", "double:NaN", "()")]
    [Arguments("$none + $two", "double:NaN", "()")]
    public async Task EmptyOperand_GivesNaN(string expression, string compatibility, string standard) =>
        await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("$dec + $dec", "double:5", "decimal:5")]
    [Arguments("$flt + 1", "double:2.5", "float:2.5")]
    [Arguments("7 div 2", "double:3.5", "decimal:3.5")]
    [Arguments("3 mod 2", "double:1", "integer:1")]
    [Arguments("2 * 3", "double:6", "integer:6")]
    [Arguments("1 div 0", "double:INF", "FOAR0001")]
    [Arguments("-1 div 0", "double:-INF", "FOAR0001")]
    [Arguments("0 div 0", "double:NaN", "FOAR0001")]
    public async Task NumericOperands_AreDoubles(string expression, string compatibility, string standard) =>
        await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("7 idiv 2")]
    [Arguments("$u3 + 1")]
    [Arguments("1.5e0 + 1.5e0")]
    public async Task ADoubleOrAnUntypedOperand_GivesTheSameResultInBothModes(string expression) =>
        await Assert.That(Run(expression, compatibility: true)).IsEqualTo(Run(expression, compatibility: false));
}
