namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Paths;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static PathFixture;

/// <summary>
/// Paths of axis steps without predicates run in one nested walk (#1247). The walk must deliver what the list pipeline delivers: the
/// same nodes in the same order, the same early stop, and the same sequence size error. The list pipeline is forced by a predicate that
/// is true for every node.
/// </summary>
public class PathDirectWalkTests
{
    [Test]
    [Arguments("child::b/child::i", "a")]
    [Arguments("child::*/child::node()", "a")]
    [Arguments("descendant::i/ancestor::*", "doc")]
    [Arguments("descendant::*/following-sibling::*", "a")]
    [Arguments("descendant::*/preceding-sibling::*", "a")]
    [Arguments("descendant-or-self::*/child::b", "doc")]
    [Arguments("child::a/attribute::*", "doc")]
    [Arguments("child::a/child::b/parent::*", "doc")]
    [Arguments("descendant::i/ancestor-or-self::*", "doc")]
    [Arguments("child::a/child::e/following::*", "doc")]
    [Arguments("descendant::i/preceding::*", "doc")]
    [Arguments("child::a/child::b/self::b/child::text()", "doc")]
    [Arguments("/child::a/child::b/child::i", "t1")]
    [Arguments("/descendant::s/child::t", "N.t1")]
    [Arguments("child::s/child::s/child::t", "N.r")]
    public async Task DirectWalk_DeliversWhatTheListPipelineDelivers(string expression, string context)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var forced = Run(PredicateOnEveryStep(expression), context);

        await Assert.That(Run(expression, context)).IsEqualTo(forced);
    }

    [Test]
    [Arguments("a/b/i", "doc", "i")]
    [Arguments("b/i", "a", "i")]
    [Arguments("child::b/child::text()", "a", "t1 t2 t3")]
    [Arguments("child::*/child::*", "N.r", "N.s2 N.t2")]
    public async Task DirectWalk_ReturnsTheNodesInDocumentOrder(string expression, string context, string expected) =>
        await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    public async Task DirectWalk_RaisesTheSequenceSizeErrorForAnIntermediateStep()
    {
        // Three children of the root element, one node at the end: the intermediate step is over the limit.
        var compiled = CompileOrThrow("child::a/child::*/child::i");
        using var context = NewContext();
        context.Limits = XPathLimits.Default.WithMaxSequenceSize(2);
        context.SetContextItem(XdmItem.FromNode(Node("doc")));

        var error = Assert.Throws<XPathException>(() => compiled.Evaluate(context));

        await Assert.That(error.Diagnostic.Code).IsEqualTo(XPathLimitErrors.SequenceSizeExceeded.ToString());
    }

    [Test]
    public async Task DirectWalk_WithoutContextItem_RaisesXPDY0002() =>
        await Assert.That(Raised("child::b/child::i")?.Diagnostic.Code).IsEqualTo("XPDY0002");

    [Test]
    public async Task DirectWalk_WithAtomicContextItem_RaisesXPTY0020()
    {
        var error = Assert.Throws<XPathException>(() => EvaluateWithAtomicContext("child::b/child::i"));

        await Assert.That(error.Diagnostic.Code).IsEqualTo("XPTY0020");
    }

    private static string PredicateOnEveryStep(string expression) =>
        string.Join(
            "/",
            System.Linq.Enumerable.Select(expression.Split('/'), static s => s.Length == 0 ? s : s + "[1 = 1]")
        );
}
