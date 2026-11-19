namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using static FlowFixture;

/// <summary>
/// <see langword="and"/> and <see langword="or"/> (XPath 2.0, 3.6): the effective boolean value of each operand, and the evaluation of the second operand only
/// when the first does not decide.
/// </summary>
public class LogicalTests
{
    [Test]
    [Arguments("true() and true()", "boolean:True")]
    [Arguments("true() and false()", "boolean:False")]
    [Arguments("false() and true()", "boolean:False")]
    [Arguments("false() and false()", "boolean:False")]
    [Arguments("true() or true()", "boolean:True")]
    [Arguments("true() or false()", "boolean:True")]
    [Arguments("false() or true()", "boolean:True")]
    [Arguments("false() or false()", "boolean:False")]
    public async Task TruthTable(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("true() or false() and false()", "boolean:True")]
    [Arguments("(true() or false()) and false()", "boolean:False")]
    [Arguments("false() and false() or true()", "boolean:True")]
    public async Task And_BindsTighterThanOr(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("$empty or $empty", "boolean:False")]
    [Arguments("$empty or $one", "boolean:True")]
    [Arguments("$zero or $blank", "boolean:False")]
    [Arguments("$zero or $word", "boolean:True")]
    [Arguments("$nan or $zero", "boolean:False")]
    [Arguments("$node and $one", "boolean:True")]
    [Arguments("$one and $empty", "boolean:False")]
    public async Task Operands_UseTheEffectiveBooleanValue(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("false() and tick()", "boolean:False", 0)]
    [Arguments("true() and tick()", "boolean:True", 1)]
    [Arguments("true() or tick()", "boolean:True", 0)]
    [Arguments("false() or tick()", "boolean:True", 1)]
    [Arguments("false() and tick() and tick()", "boolean:False", 0)]
    [Arguments("true() or tick() or tick()", "boolean:True", 0)]
    [Arguments("tickf() and tick()", "boolean:False", 1)]
    [Arguments("tick() or tick()", "boolean:True", 1)]
    [Arguments("tick() and tick()", "boolean:True", 2)]
    [Arguments("tickf() or tickf()", "boolean:False", 2)]
    public async Task SecondOperand_IsEvaluatedOnlyWhenTheFirstDoesNotDecide(
        string expression,
        string expected,
        int calls
    )
    {
        var probe = new Probe();

        await Assert.That(Run(expression, probe)).IsEqualTo(expected);
        await Assert.That(probe.First).IsEqualTo(calls);
    }

    [Test]
    [Arguments("false() and boom()", "")]
    [Arguments("true() or boom()", "")]
    [Arguments("true() and boom()", "FORG0001")]
    [Arguments("false() or boom()", "FORG0001")]
    [Arguments("boom() and false()", "FORG0001")]
    public async Task AnErrorInTheSecondOperand_IsRaisedOnlyWhenItIsEvaluated(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    [Test]
    [Arguments("$pair and true()")]
    [Arguments("true() and $pair")]
    [Arguments("$pair or false()")]
    [Arguments("false() or $pair")]
    public async Task AnOperandWithoutAnEffectiveBooleanValue_RaisesForg0006(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FORG0006");

    [Test]
    public async Task AnOperandThatIsNotNeededIsNotChecked_ForItsEffectiveBooleanValue() =>
        await Assert.That(Code("false() and $pair")).IsEqualTo(string.Empty);
}
