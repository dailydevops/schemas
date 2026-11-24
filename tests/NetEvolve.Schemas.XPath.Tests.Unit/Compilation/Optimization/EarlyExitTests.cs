namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using static OptimizationFixture;

/// <summary>
/// <c>fn:exists</c>, <c>fn:empty</c>, the effective boolean value, and comparisons of <c>fn:count</c> against a constant read as few
/// nodes as the answer needs (XPath 2.0, 2.3.4 and 3.2.2).
/// </summary>
public class EarlyExitTests
{
    private const XPathOptimizations Existence = XPathOptimizations.ExistenceExit;
    private const XPathOptimizations Boolean = XPathOptimizations.BooleanExit;

    [Test]
    [Arguments("exists(//x)", "true")]
    [Arguments("empty(//x)", "false")]
    [Arguments("exists(//y)", "false")]
    [Arguments("empty(//y)", "true")]
    public async Task ExistsAndEmpty_OverAPath_LimitThePathToOneNode(string expression, string expected)
    {
        await Assert.That(Run(expression, XPathOptimizations.All, new ChildrenTree(10))).IsEqualTo(expected);
        await Assert.That(CountedLimit(expression, XPathOptimizations.All)).IsEqualTo(1);
    }

    [Test]
    public async Task Exists_OverAPathWithAMatch_ReadsNoNodeAfterTheFirstMatch()
    {
        var on = new ChildrenTree(1000);
        var off = new ChildrenTree(1000);

        _ = Run("exists(//x)", XPathOptimizations.All, on);
        _ = Run("exists(//x)", XPathOptimizations.All & ~Existence, off);

        // The first x is node 2.
        await Assert.That(on.FarthestRead).IsLessThanOrEqualTo(2);
        await Assert.That(off.FarthestRead).IsGreaterThan(1000);
    }

    [Test]
    public async Task Empty_OverAPathWithAMatch_ReadsNoNodeAfterTheFirstMatch()
    {
        var on = new ChildrenTree(1000);

        _ = Run("empty(//x)", XPathOptimizations.All, on);

        await Assert.That(on.FarthestRead).IsLessThanOrEqualTo(2);
    }

    [Test]
    [Arguments("count(//x) > 0", 1, "true")]
    [Arguments("count(//x) >= 1", 2, "true")]
    [Arguments("count(//x) gt 0", 1, "true")]
    [Arguments("0 < count(//x)", 1, "true")]
    [Arguments("count(//x) = 0", 1, "false")]
    [Arguments("count(//x) ne 3", 4, "true")]
    public async Task CountComparison_AgainstAnIntegerLiteral_StopsOneNodeBehindTheLiteral(
        string expression,
        int limit,
        string expected
    )
    {
        var tree = new ChildrenTree(1000);

        var result = Run(expression, XPathOptimizations.All, tree);

        await Assert.That(result).IsEqualTo(expected);
        await Assert.That(CountedLimit(expression, XPathOptimizations.All)).IsEqualTo(limit);

        // The children up to the limit are the nodes 2 and up.
        await Assert.That(tree.FarthestRead).IsLessThanOrEqualTo(limit + 1);
    }

    [Test]
    public async Task CountComparison_GreaterThanZeroOverAMatch_ReadsNoNodeAfterTheFirstMatch()
    {
        var tree = new ChildrenTree(1000);

        _ = Run("count(//x) > 0", XPathOptimizations.All, tree);

        await Assert.That(tree.FarthestRead).IsLessThanOrEqualTo(2);
    }

    [Test]
    [Arguments("if (//x) then 1 else 2", "1")]
    [Arguments("if (//y) then 1 else 2", "2")]
    [Arguments("(//x) and (//y)", "false")]
    [Arguments("(//y) or (//x)", "true")]
    [Arguments("/r[x]/x[2]", "#3")]
    [Arguments("/r[y]", "")]
    [Arguments("/r[not(y)]/x[1]", "#2")]
    public async Task EffectiveBooleanValue_OfAPath_KeepsTheResult(string expression, string expected)
    {
        await Assert.That(Run(expression, XPathOptimizations.All, new ChildrenTree(10))).IsEqualTo(expected);
        await Assert.That(Run(expression, XPathOptimizations.None, new ChildrenTree(10))).IsEqualTo(expected);
    }

    [Test]
    public async Task EffectiveBooleanValue_OfAConditionOnAPath_ReadsNoNodeAfterTheFirstMatch()
    {
        var tree = new ChildrenTree(1000);

        _ = Run("if (//x) then 1 else 2", XPathOptimizations.All, tree);

        await Assert.That(tree.FarthestRead).IsLessThanOrEqualTo(2);
        await Assert.That(CountedLimit("if (//x) then 1 else 2", XPathOptimizations.All)).IsEqualTo(1);
        await Assert.That(CountedLimit("if (//x) then 1 else 2", XPathOptimizations.All & ~Boolean)).IsEqualTo(0);
    }

    [Test]
    public async Task EffectiveBooleanValue_OfALogicalOperand_LimitsThePath()
    {
        var node = (LogicalNode)Compile("(//x) and (//y)", XPathOptimizations.All).Root;

        await Assert.That(((PathNode)node.Left).Limit).IsEqualTo(1);
        await Assert.That(((PathNode)node.Right).Limit).IsEqualTo(1);
    }

    [Test]
    public async Task EffectiveBooleanValue_OfAPredicateOnAPath_LimitsThePredicatePath()
    {
        var step = (AxisPathStep)((PathNode)Compile("/r[x]", XPathOptimizations.All).Root).Steps[0];

        await Assert.That(((PathNode)step.Predicates[0]).Limit).IsEqualTo(1);
    }

    [Test]
    [Arguments("exists(//x/string(.))")]
    [Arguments("empty(//x/ancestor::*)")]
    [Arguments("exists(//x/..)")]
    [Arguments("count(//x/ancestor::r) > 0")]
    [Arguments("count(//x/..) = 1")]
    [Arguments("if (//x/parent::r) then 1 else 2")]
    public async Task EarlyExit_WhereThePathCannotStopEarly_KeepsTheResult(string expression) =>
        await Assert
            .That(Run(expression, XPathOptimizations.All, new ChildrenTree(10)))
            .IsEqualTo(Run(expression, XPathOptimizations.None, new ChildrenTree(10)));

    [Test]
    [Arguments("exists(//x)", (int)XPathOptimizations.ExistenceExit)]
    [Arguments("count(//x) > 0", (int)XPathOptimizations.CountComparisonExit)]
    [Arguments("if (//x) then 1 else 2", (int)XPathOptimizations.BooleanExit)]
    public async Task EarlyExit_WithItsRewriteSwitchedOff_LeavesThePathAlone(string expression, int rewrite)
    {
        await Assert.That(CountedLimit(expression, XPathOptimizations.All & ~(XPathOptimizations)rewrite)).IsEqualTo(0);
        await Assert.That(CountedLimit(expression, XPathOptimizations.None)).IsEqualTo(0);
    }

    // The limit of the path that the expression reads, wherever the rewrite put it.
    private static int CountedLimit(string expression, XPathOptimizations optimizations) =>
        LimitOf(Compile(expression, optimizations).Root);

    private static int LimitOf(ExecutableNode node) =>
        node switch
        {
            CallNode call => LimitOf(call.Arguments[0]),
            ValueComparisonNode value => LimitOf(value.Left is CallNode ? value.Left : value.Right),
            GeneralComparisonNode general => LimitOf(general.Left is CallNode ? general.Left : general.Right),
            IfNode condition => LimitOf(condition.Condition),
            PathNode path => path.Limit,
            _ => -1,
        };
}
