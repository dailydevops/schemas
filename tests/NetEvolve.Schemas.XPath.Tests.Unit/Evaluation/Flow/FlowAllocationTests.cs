namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static FlowFixture;

/// <summary>The hot-path rule of #123: the flow expressions allocate no managed memory in steady state, and the range does not materialize.</summary>
public class FlowAllocationTests
{
    private static async Task AssertNoAllocation(string expression, string expected)
    {
        var compiled = Compile(expression);
        using var context = NewContext();
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = compiled.Evaluate(context), warmups: 50);

        await Assert.That(ArithmeticFixture.Describe(result)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("true() and false()", "boolean:False")]
    [Arguments("false() or true()", "boolean:True")]
    [Arguments("$zero or $one", "boolean:True")]
    [Arguments("false() and boom()", "boolean:False")]
    public async Task LogicalExpressions_AllocateNothing(string expression, string expected) =>
        await AssertNoAllocation(expression, expected);

    [Test]
    [Arguments("if (true()) then 1 else 2", "integer:1")]
    [Arguments("if ($empty) then 1 else $host", "integer:100")]
    public async Task ConditionalExpressions_AllocateNothing(string expression, string expected) =>
        await AssertNoAllocation(expression, expected);

    [Test]
    [Arguments("42", "integer:42")]
    [Arguments("'abc'", "string:abc")]
    [Arguments("$host", "integer:100")]
    [Arguments("()", "()")]
    [Arguments("(1.5)", "decimal:1.5")]
    public async Task LiteralsAndVariables_AllocateNothing(string expression, string expected) =>
        await AssertNoAllocation(expression, expected);

    [Test]
    [Arguments("1 to 1", "integer:1")]
    [Arguments("2 to 1", "()")]
    [Arguments("$host to $host", "integer:100")]
    [Arguments("() to 1", "()")]
    public async Task ARangeOfNoMoreThanOneItem_AllocatesNothing(string expression, string expected) =>
        await AssertNoAllocation(expression, expected);

    [Test]
    [Arguments("some $x in 1 to 1000 satisfies $x = 1000", "boolean:True")]
    [Arguments("every $x in 1 to 1000 satisfies $x < 1000", "boolean:False")]
    [Arguments("some $x in $numbers satisfies $x = 3", "boolean:True")]
    [Arguments("some $x in 1 to 3, $y in 1 to 3 satisfies $x + $y = 6", "boolean:True")]
    [Arguments("some $host in 1 to 10 satisfies $host = 10", "boolean:True")]
    public async Task QuantifiedExpressions_OverABinding_AllocateNothing(string expression, string expected) =>
        await AssertNoAllocation(expression, expected);

    [Test]
    [Arguments("for $x in 1 to 1000 return ()", "()")]
    [Arguments("for $x in 1 to 3 return if ($x = 2) then $x else ()", "integer:2")]
    public async Task ForExpressions_ThatCollectNoMoreThanOneItem_AllocateNothing(string expression, string expected) =>
        await AssertNoAllocation(expression, expected);

    [Test]
    public async Task ForExpressions_ThatCollectManyItems_AllocateTheResultOnly()
    {
#if !NET6_0_OR_GREATER
        Skip.Test("The allocation bound is calibrated for net6.0 and newer.");
#endif
        var compiled = Compile("for $x in 1 to 1000 return $x");
        using var context = NewContext();
        var count = 0;

        // The result array of 1,000 items is the only allocation: 24,000 bytes and its header.
        AllocationAssert.AtMost(24_256, () => count = compiled.Evaluate(context).Count, warmups: 50);

        await Assert.That(count).IsEqualTo(1000);
    }

    [Test]
    public async Task TheCommaOperator_AllocatesTheResultOnly()
    {
        var compiled = Compile("(1, 2, 3)");
        using var context = NewContext();
        var count = 0;

        AllocationAssert.AtMost(128, () => count = compiled.Evaluate(context).Count, warmups: 50);

        await Assert.That(count).IsEqualTo(3);
    }
}
