namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using static FlowFixture;

/// <summary>The conditional expression (XPath 2.0, 3.8): the condition uses the effective boolean value and only the chosen branch is evaluated.</summary>
public class IfTests
{
    [Test]
    [Arguments("if (true()) then tick() else tock()", 1, 0)]
    [Arguments("if (false()) then tick() else tock()", 0, 1)]
    [Arguments("if (tickf()) then tick() else tock()", 1, 1)]
    public async Task Evaluates_ExactlyOneBranch(string expression, int first, int second)
    {
        var probe = new Probe();

        _ = Run(expression, probe);

        await Assert.That(probe.First).IsEqualTo(first);
        await Assert.That(probe.Second).IsEqualTo(second);
    }

    [Test]
    [Arguments("if (true()) then 1 else boom()", "")]
    [Arguments("if (false()) then boom() else 2", "")]
    [Arguments("if (true()) then boom() else 2", "FORG0001")]
    [Arguments("if (boom()) then 1 else 2", "FORG0001")]
    public async Task TheBranchThatIsNotChosen_IsNeverEvaluated(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    [Test]
    [Arguments("if ($empty) then 1 else 2", "integer:2")]
    [Arguments("if ($zero) then 1 else 2", "integer:2")]
    [Arguments("if ($one) then 1 else 2", "integer:1")]
    [Arguments("if ($blank) then 1 else 2", "integer:2")]
    [Arguments("if ($word) then 1 else 2", "integer:1")]
    [Arguments("if ($nan) then 1 else 2", "integer:2")]
    [Arguments("if ($node) then 1 else 2", "integer:1")]
    [Arguments("if (0.0) then 1 else 2", "integer:2")]
    [Arguments("if (0.5) then 1 else 2", "integer:1")]
    [Arguments("if ('false') then 1 else 2", "integer:1")]
    [Arguments("if (()) then 1 else 2", "integer:2")]
    public async Task TheCondition_UsesTheEffectiveBooleanValue(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("if ($pair) then 1 else 2")]
    [Arguments("if ((1, 2)) then 1 else 2")]
    public async Task ACondition_WithoutAnEffectiveBooleanValue_RaisesForg0006(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FORG0006");

    [Test]
    public async Task TheCondition_IsEvaluatedOnce()
    {
        var probe = new Probe();

        _ = Run("if (tick()) then 1 else 2", probe);

        await Assert.That(probe.First).IsEqualTo(1);
    }

    [Test]
    [Arguments("if (true()) then (1, 2) else ()", "integer:1, integer:2")]
    [Arguments("if (false()) then (1, 2) else ()", "()")]
    [Arguments("if (true()) then () else 1", "()")]
    [Arguments("if (true()) then if (false()) then 1 else 2 else 3", "integer:2")]
    public async Task ABranch_MayBeAnySequence(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);
}
