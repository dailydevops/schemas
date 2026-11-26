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
    [Arguments("(//x)[position() gt 2]")]
    [Arguments("(//x)[position() ge 3]")]
    [Arguments("(//x)[position() ge 0]")]
    [Arguments("(//x)[position() gt -1]")]
    [Arguments("(//x)[position() != 2]")]
    [Arguments("(//x)[-1]")]
    [Arguments("(//x)[2000000000]")]
    [Arguments("(//x)[position() = 0]")]
    [Arguments("(//x)[position() lt 0]")]
    [Arguments("/r/x[position() gt 1]")]
    [Arguments("(//x)[position() le 2.5]")]
    [Arguments("(//x)[last()][1]")]
    [Arguments("/r/x[position() ge 4][1]")]
    [Arguments("(//x)[position() > 1 and position() < 4]")]
    [Arguments("(//x)[position() ge 2 and position() le 3]")]
    [Arguments("(//x)[position() lt 4 and position() gt 1]")]
    [Arguments("(//x)[position() gt 1 and position() lt 4]")]
    [Arguments("(//x)[position() > 3 and position() < 2]")]
    [Arguments("(//x)[position() = 2 and position() = 3]")]
    [Arguments("(//x)[position() = 2 and position() <= 3]")]
    [Arguments("(//x)[position() > 1 and position() < 5 and position() <= 3]")]
    [Arguments("(//x)[position() > 1 and position() < 5 and position() >= 3 and position() != 4]")]
    [Arguments("(//x)[position() > -3 and position() < 100]")]
    [Arguments("(//x)[position() < 0 and position() > 2]")]
    [Arguments("(//x)[position() > 1 and position() < 2.5]")]
    [Arguments("(//x)[position() > 1.5 and position() < 4]")]
    [Arguments("(//x)[position() > 1 and position() < 4][2]")]
    [Arguments("(//x)[position() > 1 and . ne 0]")]
    [Arguments("(//x)[position() > 1 and . != 0]")]
    [Arguments("(//x)[. = 0 and position() < 3]")]
    [Arguments("(//x)[position() lt last() and position() gt 1]")]
    [Arguments("(//x)[position() gt 1 and position() le 2.5]")]
    [Arguments("(//x)[. ne 0 and position() < 3]")]
    [Arguments("(//x)[2 and position() < 3]")]
    [Arguments("(//x)[position() < 3 and 2]")]
    [Arguments("(//x)[position() > 1 and last()]")]
    [Arguments("(//x)[position() > 1 and position() < last()]")]
    [Arguments("(//x)[position() > 1 or position() < 3]")]
    [Arguments("(//x)[position() < 0 and error()]")]
    [Arguments("(//x)[position() > 0 and error()]")]
    [Arguments("(//x)[position() > 1 and position() < 4 and (. ne 0)]")]
    [Arguments("/r/x[position() > 1 and position() < 4]")]
    [Arguments("/r/x[position() >= 2 and position() <= 2]")]
    [Arguments("/r/x[position() > 3 and position() < 2]")]
    [Arguments("/r/x[position() > 1 and position() < 4][1]")]
    [Arguments("/r/x[position() > 1 and position() < 4]/following-sibling::x")]
    [Arguments("/r/x[last()]/preceding-sibling::x[position() > 1 and position() < 4]")]
    [Arguments("/r/x[last()]/preceding-sibling::x[position() > 1 and position() < 4][1]")]
    [Arguments("(1 to 9)[position() > 2 and position() < 6]")]
    [Arguments("(1 to 9)[position() > 20 and position() < 30]")]
    [Arguments("()[position() > 1 and position() < 3]")]
    public async Task Rewrite_WithTheRewriteOnAndOff_GivesTheSameResult(string expression) =>
        await Assert
            .That(Run(expression, XPathOptimizations.All, new ChildrenTree(5)))
            .IsEqualTo(Run(expression, Off, new ChildrenTree(5)));

    [Test]
    public async Task Rewrite_PositionalRangeJoinedByAnd_BecomesOneCutoffOfTheStep()
    {
        var step = (AxisPathStep)
            ((PathNode)Compile("/r/x[position() > 1 and position() < 4]", XPathOptimizations.All).Root).Steps[1];

        await Assert.That(step.Cutoff).IsEqualTo(PositionalCutoff.Range(2, 3));
        await Assert.That(step.Predicates.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("/r/x[position() > 1 and position() < 4 and position() <= 3]", 2, 3)]
    [Arguments("/r/x[position() < 5 and position() >= 3 and position() > 0]", 3, 4)]
    [Arguments("/r/x[position() ge 2 and position() le 2]", 2, 2)]
    [Arguments("/r/x[position() > 3 and position() < 2]", 4, 1)]
    [Arguments("/r/x[position() < 0 and position() > 2]", 3, -1)]
    [Arguments("/r/x[position() > 2 and position() > 5]", 6, int.MaxValue)]
    public async Task Rewrite_ChainOfAnd_IntersectsTheRanges(string expression, int first, int last)
    {
        var step = (AxisPathStep)((PathNode)Compile(expression, XPathOptimizations.All).Root).Steps[1];

        await Assert.That(step.Cutoff).IsEqualTo(PositionalCutoff.Range(first, last));
        await Assert.That(step.Predicates.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Rewrite_PositionalRangeJoinedByAnd_OfAFilterLimitsThePath()
    {
        var filter = (FilterNode)Compile("(//x)[position() > 1 and position() < 4]", XPathOptimizations.All).Root;

        await Assert.That(filter.Cutoff).IsEqualTo(PositionalCutoff.Range(2, 3));
        await Assert.That(((PathNode)filter.Primary).Limit).IsEqualTo(3);
    }

    [Test]
    [Arguments("/r/x[position() > 1 or position() < 4]")]
    [Arguments("/r/x[position() > 1 and . ne 0]")]
    [Arguments("/r/x[position() > 1 and . != 0]")]
    [Arguments("/r/x[. = 0 and position() < 4]")]
    [Arguments("/r/x[position() gt 1 and position() lt last()]")]
    [Arguments("/r/x[position() gt 1 and position() le 2.5]")]
    [Arguments("/r/x[2 and position() < 4]")]
    [Arguments("/r/x[position() < 4 and last()]")]
    [Arguments("/r/x[position() > 1 and position() < last()]")]
    [Arguments("/r/x[position() > 1 and position() < 2.5]")]
    public async Task Rewrite_AndThatIsNotAPositionalRange_StaysAPredicate(string expression)
    {
        var step = (AxisPathStep)((PathNode)Compile(expression, XPathOptimizations.All).Root).Steps[1];

        await Assert.That(step.Cutoff).IsNull();
        await Assert.That(step.Predicates.Count).IsEqualTo(1);
    }

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
