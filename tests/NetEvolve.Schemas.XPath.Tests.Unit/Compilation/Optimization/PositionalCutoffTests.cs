namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using static OptimizationFixture;

/// <summary>A positional predicate stops the walk or skips the evaluation of the predicate (XPath 2.0, 3.2.2).</summary>
public class PositionalCutoffTests
{
    private const XPathOptimizations Off = XPathOptimizations.All & ~XPathOptimizations.PositionalCutoff;

    [Test]
    [Arguments("(//x)[1]", 1, 1)]
    [Arguments("(//x)[3]", 3, 3)]
    [Arguments("(//x)[position() le 2]", 1, 2)]
    [Arguments("(//x)[position() lt 3]", 1, 2)]
    [Arguments("(//x)[position() <= 2]", 1, 2)]
    [Arguments("(//x)[position() = 2]", 2, 2)]
    public async Task Rewrite_PositionalPredicateOfAFilter_BecomesACutoffAndLimitsThePath(
        string expression,
        int first,
        int last
    )
    {
        var on = Compile(expression, XPathOptimizations.All);

        var filter = on.Root as FilterNode;
        await Assert.That(filter).IsNotNull();
        await Assert.That(filter!.Cutoff).IsEqualTo(PositionalCutoff.Range(first, last));
        await Assert.That(filter.Predicates.Count).IsEqualTo(0);
        await Assert.That(((PathNode)filter.Primary).Limit).IsEqualTo(last);
        await Assert.That(((FilterNode)Compile(expression, Off).Root).Cutoff).IsNull();
    }

    [Test]
    public async Task Rewrite_LastPredicateOfAFilter_BecomesACutoffAndReadsEveryNode()
    {
        var filter = (FilterNode)Compile("(//x)[last()]", XPathOptimizations.All).Root;

        await Assert.That(filter.Cutoff).IsEqualTo(PositionalCutoff.LastItem());
        await Assert.That(((PathNode)filter.Primary).Limit).IsEqualTo(0);
    }

    [Test]
    public async Task Rewrite_PositionalPredicateOfAStep_BecomesACutoffOfTheStep()
    {
        var step = (AxisPathStep)((PathNode)Compile("/r/x[2]", XPathOptimizations.All).Root).Steps[1];

        await Assert.That(step.Cutoff).IsEqualTo(PositionalCutoff.Range(2, 2));
        await Assert.That(step.Predicates.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Rewrite_PositionalPredicateBehindAnotherPredicate_IsLeftAlone()
    {
        var step = (AxisPathStep)((PathNode)Compile("/r/x[. ne 0][1]", XPathOptimizations.All).Root).Steps[1];

        await Assert.That(step.Cutoff).IsNull();
        await Assert.That(step.Predicates.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments("(//x)[1]")]
    [Arguments("(//x)[3]")]
    [Arguments("(//x)[0]")]
    [Arguments("(//x)[position() le 0]")]
    [Arguments("(//x)[position() le 2]")]
    [Arguments("(//x)[position() lt 1]")]
    [Arguments("(//x)[position() = 9]")]
    [Arguments("(//x)[last()]")]
    [Arguments("(//x)[position() le 4][2]")]
    [Arguments("(//x)[2][1]")]
    [Arguments("(//x)[1][last()]")]
    [Arguments("(//x)[position() le 3][last()]")]
    [Arguments("/r/x[1]")]
    [Arguments("/r/x[3]")]
    [Arguments("/r/x[last()]")]
    [Arguments("/r/x[position() le 2]")]
    [Arguments("/r/x[7]")]
    [Arguments("/r/x[position() le 2][last()]")]
    [Arguments("(/r/x)[1.5]")]
    [Arguments("(1 to 5)[2]")]
    [Arguments("(1 to 5)[last()]")]
    [Arguments("(1 to 5)[position() le 2]")]
    [Arguments("()[1]")]
    public async Task Rewrite_WithTheRewriteOnAndOff_GivesTheSameResult(string expression) =>
        await Assert
            .That(Run(expression, XPathOptimizations.All, new ChildrenTree(5)))
            .IsEqualTo(Run(expression, Off, new ChildrenTree(5)));

    [Test]
    public async Task Rewrite_FirstNodeOfADescendantPath_ReadsNoNodeAfterTheFirstMatch()
    {
        var on = new ChildrenTree(1000);
        var off = new ChildrenTree(1000);

        var first = Run("(//x)[1]", XPathOptimizations.All, on);
        _ = Run("(//x)[1]", Off, off);

        await Assert.That(first).IsEqualTo("#2");

        // The first x is node 2; a node after the match would be node 3.
        await Assert.That(on.FarthestRead).IsLessThanOrEqualTo(2);
        await Assert.That(off.FarthestRead).IsGreaterThan(1000);
    }

    [Test]
    public async Task Rewrite_FirstChildOfAStep_StopsTheWalkOfTheAxis()
    {
        var on = new ChildrenTree(1000);

        var result = Run("/r/x[1]", XPathOptimizations.All, on);

        await Assert.That(result).IsEqualTo("#2");
        await Assert.That(on.FarthestRead).IsLessThanOrEqualTo(2);
    }
}
