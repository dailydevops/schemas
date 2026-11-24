namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using static OptimizationFixture;

/// <summary>Constant folding evaluates literal operators once, and never hides or moves a dynamic error (XPath 2.0, 2.3.4).</summary>
public class ConstantFoldingTests
{
    private const XPathOptimizations Off = XPathOptimizations.All & ~XPathOptimizations.ConstantFolding;

    private static readonly ChildrenTree Tree = new(2);

    [Test]
    [Arguments("1 + 2 * 3", "7")]
    [Arguments("-(2 * 3)", "-6")]
    [Arguments("10 idiv 3", "3")]
    [Arguments("1 < 2", "true")]
    [Arguments("3 ge 4", "false")]
    [Arguments("1 = 1 and 2 = 3", "false")]
    [Arguments("1 = 2 or 2 = 2", "true")]
    public async Task Fold_LiteralOperators_BecomeOneConstantWithTheSameResult(string expression, string expected)
    {
        var on = Compile(expression, XPathOptimizations.All);
        var off = Compile(expression, Off);

        await Assert.That(on.Root).IsTypeOf<ConstantNode>();
        await Assert.That(off.Root).IsNotTypeOf<ConstantNode>();
        await Assert.That(Run(expression, XPathOptimizations.All, Tree)).IsEqualTo(expected);
        await Assert.That(Run(expression, Off, Tree)).IsEqualTo(expected);
    }

    [Test]
    public async Task Fold_ConditionOverLiteral_KeepsTheTakenBranchOnly()
    {
        var on = Compile("if (1 = 2) then 1 div 0 else 5", XPathOptimizations.All);
        var off = Compile("if (1 = 2) then 1 div 0 else 5", Off);

        await Assert.That(on.Root).IsTypeOf<ConstantNode>();
        await Assert.That(off.Root).IsTypeOf<IfNode>();
        await Assert.That(Run("if (1 = 2) then 1 div 0 else 5", XPathOptimizations.All, Tree)).IsEqualTo("5");
    }

    [Test]
    public async Task Fold_BranchThatIsNeverEvaluated_RaisesNothingAtCompileTimeOrRunTime()
    {
        await Assert.That(Run("if (1 = 1) then 1 else 1 div 0", XPathOptimizations.All, Tree)).IsEqualTo("1");
        await Assert.That(Run("if (1 = 1) then 1 else 1 div 0", Off, Tree)).IsEqualTo("1");
    }

    [Test]
    [Arguments("1 div 0", "FOAR0001")]
    [Arguments("1 idiv 0", "FOAR0001")]
    [Arguments("if (1 = 1) then 1 div 0 else 1", "FOAR0001")]
    public async Task Fold_DynamicError_StaysAnErrorOfTheEvaluationAndIsNotRaisedWhileCompiling(
        string expression,
        string code
    )
    {
        // Compiling succeeds with the rewrite on, and the error is raised where the operator runs.
        var on = Compile(expression, XPathOptimizations.All);

        await Assert.That(on.Root).IsNotTypeOf<ConstantNode>();
        await Assert.That(Run(expression, XPathOptimizations.All, Tree)).IsEqualTo(code);
        await Assert.That(Run(expression, Off, Tree)).IsEqualTo(code);
    }

    [Test]
    public async Task Fold_OperandThatIsNotALiteral_StaysAnOperator()
    {
        var on = Compile("count(//x) + 1", XPathOptimizations.All);

        await Assert.That(on.Root).IsTypeOf<ArithmeticNode>();
        await Assert.That(Run("count(//x) + 1", XPathOptimizations.All, Tree)).IsEqualTo("3");
    }

    [Test]
    public async Task Fold_StringComparison_IsLeftToTheEvaluationBecauseTheCollationIsADynamicSetting()
    {
        var on = Compile("'a' eq 'b'", XPathOptimizations.All);

        await Assert.That(on.Root).IsTypeOf<ValueComparisonNode>();
    }
}
