namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using NetEvolve.Schemas.Testing.Allocations;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>The order in which one pass visits the instance and the node kinds it visits (design #150 EP-1, examples E1 to E3, E9).</summary>
public class TraversalTests
{
    [Test]
    public async Task Traversal_AllNodeKinds_InDocumentOrder()
    {
        var schema = Schema(
            Pattern("p", Rule("r1", "/", Holds()), Rule("r2", "@id", Holds()), Rule("r3", "text()", Holds()))
        );
        using var h = new PatternHarness(schema, E("a", A("id", "1"), "t"));

        _ = h.Evaluate();

        // E1: the document node, then the attribute, then the text; the element a matches no rule.
        await Assert.That(h.Log.Fired).IsEqualTo("r1,r2,r3");
    }

    [Test]
    public async Task Traversal_AttributesPrecedeChildren()
    {
        var schema = Schema(Pattern("p", Rule("r1", "*", Holds()), Rule("r2", "@*", Holds())));
        using var h = new PatternHarness(schema, E("a", A("x", "1"), E("b")));

        _ = h.Evaluate();

        // E2: a, @x, b.
        await Assert.That(h.Log.Fired).IsEqualTo("r1,r2,r1");
    }

    [Test]
    public async Task Traversal_VisitsEveryAttributeOfAnElement_BeforeItsFirstChild()
    {
        var schema = Schema(Group("g", Rule("r1", "@x", Holds()), Rule("r2", "@y", Holds()), Rule("r3", "b", Holds())));
        using var h = new PatternHarness(schema, E("a", A("x", "1"), A("y", "2"), E("b")));

        _ = h.Evaluate();

        await Assert.That(h.Log.Fired).IsEqualTo("r1,r2,r3");
    }

    [Test]
    public async Task Traversal_NamespaceNodesAreNotVisited()
    {
        var schema = Schema(Pattern("p", Rule("r1", "node()", Holds()), Rule("r2", "/", Holds())));
        using var h = new PatternHarness(schema, E("a"), namespacePrefix: "p");

        _ = h.Evaluate();

        // E3: the document node matches /, node() matches the element a, and no namespace node adds a second firing of r1.
        await Assert.That(h.Log.Fired).IsEqualTo("r2,r1");
    }

    [Test]
    public async Task Traversal_WhitespaceOnlyText_IsVisited()
    {
        var schema = Schema(Pattern("p", Rule("r1", "text()", Holds())));
        using var h = new PatternHarness(schema, E("a", " ", E("b")));

        _ = h.Evaluate();

        await Assert.That(h.Log.Fired).IsEqualTo("r1");
    }

    [Test]
    public async Task Traversal_NestedAndSiblingElements_FollowDocumentOrder()
    {
        var schema = Schema(
            Group(
                "g",
                Rule("ra", "a", Holds()),
                Rule("rb", "b", Holds()),
                Rule("rc", "c", Holds()),
                Rule("rd", "d", Holds())
            )
        );
        using var h = new PatternHarness(schema, E("a", E("b", E("c")), E("d")));

        _ = h.Evaluate();

        await Assert.That(h.Log.Fired).IsEqualTo("ra,rb,rc,rd");
    }

    [Test]
    public async Task Traversal_DeepChain_NeedsNoRecursion()
    {
        var schema = Schema(Pattern("p", Rule("r1", "*", Holds())));
        using var h = new PatternHarness(schema, PatternTree.BuildChain(20_000));

        _ = h.Evaluate();

        await Assert.That(h.Log.Rules.Count).IsEqualTo(20_000);
    }

    [Test]
    public async Task Traversal_OnePassPerActivePattern()
    {
        var schema = Schema(Pattern("p1", Rule("r1", "*", Holds())), Pattern("p2", Rule("r2", "*", Holds())));
        using var h = new PatternHarness(schema, E("a", E("b")));

        _ = h.Evaluate();

        // Pattern-major: every node of the first pass precedes the first node of the second pass.
        await Assert.That(h.Log.Sequence).IsEqualTo("pattern:p1 fired:r1 fired:r1 pattern:p2 fired:r2 fired:r2");
    }

    [Test]
    public async Task Traversal_PerNode_SteadyState_AllocatesNothing()
    {
        var schema = Schema(Pattern("p", Rule("r1", "none", Holds())));
        using var small = new PatternHarness(schema, Wide(10));
        using var large = new PatternHarness(schema, Wide(2_000));
        small.Matcher.Record = false;
        large.Matcher.Record = false;

        var perPassSmall = AllocationAssert.Measure(() => Pass(small), warmups: 20);
        var perPassLarge = AllocationAssert.Measure(() => Pass(large), warmups: 20);

        // A pass allocates what its events cost, whatever the number of visited nodes: nothing per node.
        await Assert.That(perPassLarge).IsEqualTo(perPassSmall);
        await Assert.That(large.Matcher.MatchCalls).IsGreaterThan(2_000);
    }

    [Test]
    public async Task Traversal_KindPrefilter_SkipsNodesThatCannotMatch()
    {
        var schema = Schema(Pattern("p", Rule("r1", "@x", Holds())));
        using var h = new PatternHarness(schema, E("a", A("x", "1"), E("b"), "t"));

        _ = h.Evaluate();

        // Only the one attribute reaches the matcher.
        await Assert.That(h.Matcher.MatchCalls).IsEqualTo(1);
        await Assert.That(h.Log.Fired).IsEqualTo("r1");
    }

    [Test]
    public async Task Patterns_BothFireOnOneNode_SvrlOrder()
    {
        var schema = Schema(Pattern("p1", Rule("r1", "a", Fails("x"))), Pattern("p2", Rule("r2", "a", Fails("y"))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        // E9: no state of the first pattern is visible to the second.
        await Assert.That(h.Log.Sequence).IsEqualTo("pattern:p1 fired:r1 failed:x pattern:p2 fired:r2 failed:y");
    }

    [Test]
    public async Task ActivePattern_CarriesIdNameAndGroupFlag()
    {
        var schema = Schema(
            PatternWith("p1", [Rule("r1", "a", Holds())], title: "Title"),
            Group("g1", Rule("r2", "a", Holds()))
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Patterns.Count).IsEqualTo(2);
        await Assert.That(h.Log.Patterns[0]).IsEqualTo(("p1", string.Empty, "Title", false));
        await Assert.That(h.Log.Patterns[1]).IsEqualTo(("g1", string.Empty, string.Empty, true));
        await Assert.That(h.Log.Events[0]).IsEqualTo("pattern:p1");
    }

    [Test]
    public async Task FiredRule_CarriesIdAndContext_AndEmptyRoleAndFlagsWhenNoneIsSet()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Holds())));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Rules[0]).IsEqualTo(("r1", "a", string.Empty, string.Empty, string.Empty));
    }

    [Test]
    public async Task Evaluate_NoActivePattern_DeliversNothing_AndIsNotTruncated()
    {
        var schema = Schema();
        using var h = new PatternHarness(schema, E("a"));

        var summary = h.Evaluate();

        await Assert.That(summary.IsTruncated).IsFalse();
        await Assert.That(h.Log.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Evaluate_NeverDeliversBeginOrFinish_TheCompositionOwnsThem()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Fails("x"))));
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Events).DoesNotContain("begin");
        await Assert.That(h.Log.Events).DoesNotContain("finish");
    }

    private static void Pass(PatternHarness h)
    {
        h.Log.Events.Clear();
        h.Log.Patterns.Clear();
        _ = h.Evaluate();
    }
}
