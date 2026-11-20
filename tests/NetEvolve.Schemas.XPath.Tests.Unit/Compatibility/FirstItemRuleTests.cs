namespace NetEvolve.Schemas.XPath.Tests.Unit.Compatibility;

using static CompatibilityFixture;

/// <summary>
/// The first-item rule of XPath 2.0, Appendix I: where a single value is expected, XPath 1.0 used the first item of a node-set, and
/// compatibility mode does the same (3.4 for the operands of the arithmetic, 3.1.5 for the arguments of a function call). Without the
/// mode a sequence of more than one item is XPTY0004.
/// </summary>
public class FirstItemRuleTests
{
    [Test]
    [Arguments("$two + 10", "double:11", "XPTY0004")]
    [Arguments("10 + $two", "double:11", "XPTY0004")]
    [Arguments("$two * $two", "double:1", "XPTY0004")]
    [Arguments("$a + 10", "double:11", "XPTY0004")]
    [Arguments("$a - $a", "double:0", "XPTY0004")]
    [Arguments("-$two", "double:-1", "XPTY0004")]
    [Arguments("+$a", "double:1", "XPTY0004")]
    public async Task ArithmeticOperand_UsesTheFirstItem(string expression, string compatibility, string standard) =>
        await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("string-length($strs)", "integer:2", "XPTY0004")]
    [Arguments("upper-case($strs)", "string:BB", "XPTY0004")]
    [Arguments("substring('abcdef', $two)", "string:abcdef", "XPTY0004")]
    [Arguments("string($a)", "string:1", "XPTY0004")]
    [Arguments("name($a)", "string:a", "XPTY0004")]
    [Arguments("translate($a, '1', '9')", "string:9", "XPTY0004")]
    public async Task FunctionArgument_UsesTheFirstItem(string expression, string compatibility, string standard) =>
        await AssertModes(expression, compatibility, standard);

    [Test]
    public async Task ASingleItemOperand_IsNotAffectedByTheRule()
    {
        await Assert.That(Run("$two[1] + 10", compatibility: true)).IsEqualTo("double:11");
        await Assert.That(Run("$two[1] + 10", compatibility: false)).IsEqualTo("integer:11");
    }
}
