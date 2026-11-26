namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation;

using System;
using System.Linq;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Paths;

/// <summary>Allocation budget of the lowering: no temporary lists, no empty arrays, no unused fields per step.</summary>
public class LoweringAllocationTests
{
    private const int Rounds = 64;

    private static long BytesPerCompile(string expression)
    {
        // Warm up: type initializers, the lowering registry, and tiered JIT do not count.
        for (var i = 0; i < 8; i++)
        {
            _ = XPathExpression.TryCompile(expression);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Rounds; i++)
        {
            _ = XPathExpression.TryCompile(expression);
        }

        return (GC.GetAllocatedBytesForCurrentThread() - before) / Rounds;
    }

    [Test]
    public async Task TryCompile_PathWithPredicate_StaysWithinTheAllocationBudget()
    {
        var bytes = BytesPerCompile("a/b[@c = 'x']/d");

        await Assert.That(bytes).IsLessThan(3800);
    }

    [Test]
    public async Task TryCompile_CallWithoutVariables_StaysWithinTheAllocationBudget()
    {
        var bytes = BytesPerCompile("concat(a, 'x', string(b))");

        await Assert.That(bytes).IsLessThan(3300);
    }

    // The lowering merges descendant-or-self::node() with the next step; the executable shape must not change.
    [Test]
    [Arguments("a/b", "AxisPathStep,AxisPathStep")]
    [Arguments("//a", "AxisPathStep")]
    [Arguments("//a[@b = 1]", "AxisPathStep")]
    [Arguments("//a[1]", "AxisPathStep,CutoffAxisPathStep")]
    [Arguments("//@b", "DescendantAttributeStep")]
    [Arguments("//(a | b)", "ExpressionPathStep")]
    [Arguments("a//b", "AxisPathStep,AxisPathStep")]
    [Arguments("a//@b", "AxisPathStep,DescendantAttributeStep")]
    [Arguments("//node()", "AxisPathStep")]
    [Arguments("a/(b | c)", "AxisPathStep,ExpressionPathStep")]
    public async Task TryCompile_Path_KeepsTheStepShape(string expression, string expected)
    {
        var result = XPathExpression.TryCompile(expression);

        await Assert.That(result.Success).IsTrue();
        var path = (PathNode)result.Expression!.Compiled.Root;
        await Assert.That(string.Join(",", path.Steps.Select(static step => step.GetType().Name))).IsEqualTo(expected);
    }

    [Test]
    public async Task TryCompile_ForVariable_StillResolves()
    {
        var result = XPathExpression.TryCompile("for $x in (1, 2) return $x + 1");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(XPathExpression.TryCompile("$undeclared").Success).IsFalse();
    }
}
