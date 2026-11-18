namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Paths;

using System;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static PathFixture;

/// <summary>
/// The static ordering flag of the steps (XP-54, #191): what each step of a path needs after it ran, so that sorting runs only when
/// needed. The table is computed while lowering from the axes of the steps, starting from a single context node.
/// </summary>
public class PathOrderingTests
{
    private static string Requirements(string expression) =>
        string.Join(" ", ((PathNode)CompileOrThrow(expression).Root).Requirements);

    [Test]
    [Arguments("a/b", "None None")]
    [Arguments("a/b/c/d", "None None None None")]
    [Arguments("a/@id", "None None")]
    [Arguments("a/*/@*", "None None None")]
    [Arguments("a/b[1]/c[last()]", "None None None")]
    [Arguments("/", "")]
    [Arguments("/a/b", "None None")]
    [Arguments("./b", "None None")]
    [Arguments("a/./b", "None None None")]
    [Arguments("descendant::node()", "None")]
    [Arguments("following::*", "None")]
    [Arguments("following-sibling::*", "None")]
    [Arguments("/a//i", "None None")]
    [Arguments(".//i", "None None")]
    [Arguments("//b", "None")]
    [Arguments("//b/i", "None Sort")]
    public async Task Requirements_OfStepsFromASingleNode_AreOnlyAsStrongAsTheAxesNeed(
        string expression,
        string expected
    ) => await Assert.That(Requirements(expression)).IsEqualTo(expected);

    // One context node and a reverse axis: the nodes arrive in reverse document order and are reversed once, not sorted.
    [Test]
    [Arguments("ancestor::*", "Reverse")]
    [Arguments("ancestor-or-self::node()", "Reverse")]
    [Arguments("preceding::*", "Reverse")]
    [Arguments("preceding-sibling::*", "Reverse")]
    [Arguments("/ancestor-or-self::node()", "Reverse")]
    [Arguments("..", "None")]
    [Arguments("self::node()", "None")]
    public async Task Requirements_OfAReverseAxisFromASingleNode_IsReverse(string expression, string expected) =>
        await Assert.That(Requirements(expression)).IsEqualTo(expected);

    // Several context nodes can reach the same node or arrive out of order, so the step must sort.
    [Test]
    [Arguments("a/..", "None Sort")]
    [Arguments("b/../b", "None Sort Sort")]
    [Arguments("*/preceding-sibling::node()", "None Sort")]
    [Arguments("*/following-sibling::*", "None Sort")]
    [Arguments("*/ancestor::*", "None Sort")]
    [Arguments("//b//i", "None Sort")]
    [Arguments("/a/b/..", "None None Sort")]
    [Arguments("(b | p:e)/i", "None Sort")]
    [Arguments("$nodes/b", "Sort Sort")]
    [Arguments("$nodes/..", "Sort Sort")]
    [Arguments("/a/(b)[2]/..", "None Sort Sort")]
    public async Task Requirements_OfStepsFromSeveralNodes_SortWhereOrderOrDuplicatesAreNotGuaranteed(
        string expression,
        string expected
    ) => await Assert.That(Requirements(expression)).IsEqualTo(expected);

    // A statically ordered path never compares two nodes in document order; the others do.
    [Test]
    [Arguments("/*", false)]
    [Arguments("/*/self::*", false)]
    [Arguments("descendant::*", false)]
    [Arguments("/*/following-sibling::*", true)]
    [Arguments("/*/..", true)]
    public async Task EvaluatingAPath_SortsOnlyWhenTheFlagSaysSo(string expression, bool sorts)
    {
        var tree = new CountingTree(5);
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(tree.At(0)));
        var compiled = CompileOrThrow(expression);

        var result = compiled.Evaluate(context);

        await Assert.That(result.Count).IsGreaterThan(0);
        await Assert.That(tree.Comparisons > 0).IsEqualTo(sorts);
    }
}
