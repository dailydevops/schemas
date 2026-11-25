namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Paths;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static PathFixture;

/// <summary>
/// The hot-path rule of #123: steps, predicates, and set operators allocate no managed memory in steady state. A result of two or more
/// items is copied into one array and is not measured here; these expressions deliver one node.
/// </summary>
public class PathAllocationTests
{
    private static async Task AssertNoAllocation(string expression, string context, string expected)
    {
        var compiled = CompileOrThrow(expression);
        using var evaluation = NewContext();
        evaluation.SetContextItem(XdmItem.FromNode(Node(context)));
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = compiled.Evaluate(evaluation), warmups: 50);

        await Assert.That(Render(result)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("/a/b[1]", "doc", "b1")]
    [Arguments("child::b[last()]", "a", "b2")]
    [Arguments("/descendant::i", "doc", "i")]
    [Arguments("//i", "doc", "i")]
    [Arguments("a/b/i", "doc", "i")]
    [Arguments("ancestor::*[1]", "i", "b2")]
    [Arguments("preceding-sibling::node()[position() = 2]", "e", "pi")]
    [Arguments("/a/*/..", "doc", "a")]
    [Arguments("//b[2]/following-sibling::*", "doc", "e")]
    [Arguments("child::b[position() > 1]", "a", "b2")]
    public async Task PathEvaluation_AllocatesNothing(string expression, string context, string expected) =>
        await AssertNoAllocation(expression, context, expected);

    [Test]
    [Arguments("child::b[1] | child::b[1]", "a", "b1")]
    [Arguments("child::b[1] intersect child::b[position() < 2]", "a", "b1")]
    [Arguments("child::b[1] except child::b[2]", "a", "b1")]
    [Arguments("child::b[2] | child::b[2]", "a", "b2")]
    [Arguments("(child::b | child::p:e)/child::i", "a", "i")]
    [Arguments("(child::b/self::* | child::p:e)/child::i", "a", "i")]
    public async Task SetOperators_AllocateNothing(string expression, string context, string expected) =>
        await AssertNoAllocation(expression, context, expected);
}
