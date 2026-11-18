namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Paths;

using NetEvolve.Schemas.XPath.Xdm.Values;
using static PathFixture;

/// <summary>
/// Path expressions (XPath 2.0, 3.2). Document order of the tree: doc a id lang b1 t1 c pi b2 t2 i x t3 e; the nested tree is
/// <c>N.r(N.s1(N.s2(N.t1) N.t2) N.t3)</c>.
/// </summary>
public class PathEvaluationTests
{
    [Test]
    [Arguments("/", "t2", "doc")]
    [Arguments("/", "doc", "doc")]
    [Arguments("/", "N.t1", "N.doc")]
    [Arguments("/a", "t2", "a")]
    [Arguments("/*", "x", "a")]
    [Arguments("/a/b", "x", "b1 b2")]
    [Arguments("/a/*", "doc", "b1 b2 e")]
    [Arguments("/a/node()", "doc", "b1 c pi b2 e")]
    [Arguments("/a/b/i", "i", "i")]
    [Arguments("/a/b/text()", "doc", "t1 t2 t3")]
    [Arguments("/a/@id", "doc", "id")]
    [Arguments("/a/@*", "doc", "id lang")]
    [Arguments("/a/b/@*", "doc", "()")]
    [Arguments("/x", "doc", "()")]
    public async Task LeadingSlash_StartsAtTheRootOfTheTreeOfTheContextNode(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    [Arguments("b", "a", "b1 b2")]
    [Arguments("b/i", "a", "i")]
    [Arguments("./b", "a", "b1 b2")]
    [Arguments(".", "b2", "b2")]
    [Arguments("..", "i", "b2")]
    [Arguments("../..", "i", "a")]
    [Arguments("../../b", "i", "b1 b2")]
    [Arguments("@id", "a", "id")]
    [Arguments("@*", "a", "id lang")]
    [Arguments("@p:lang", "a", "lang")]
    [Arguments("@*", "id", "()")]
    [Arguments(".//i", "a", "i")]
    [Arguments("self::node()/b", "a", "b1 b2")]
    [Arguments("a/b", "doc", "b1 b2")]
    [Arguments("a/@id", "doc", "id")]
    [Arguments("/a/b/.", "doc", "b1 b2")]
    [Arguments("/a/./b", "doc", "b1 b2")]
    public async Task RelativePath_AndTheAbbreviatedForms_StartAtTheContextItem(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // Several context nodes reach the same node: the result holds it once, in document order.
    [Test]
    [Arguments("b/..", "a", "a")]
    [Arguments("b/../b", "a", "b1 b2")]
    [Arguments("*/..", "a", "a")]
    [Arguments("b/following-sibling::*", "a", "b2 e")]
    [Arguments("*/preceding-sibling::node()", "a", "b1 c pi b2")]
    [Arguments("*/ancestor::*", "a", "a")]
    [Arguments("*/ancestor-or-self::*", "a", "a b1 b2 e")]
    [Arguments("*/following::node()", "a", "c pi b2 t2 i x t3 e")]
    public async Task Path_WhereContextNodesOverlap_IsInDocumentOrderWithoutDuplicates(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    [Arguments("//b", "t3", "b1 b2")]
    [Arguments("//i", "doc", "i")]
    [Arguments("//node()", "doc", "a b1 t1 c pi b2 t2 i x t3 e")]
    [Arguments("//*", "doc", "a b1 b2 i e")]
    [Arguments("//@*", "doc", "id lang")]
    [Arguments("//@id", "t1", "id")]
    [Arguments("//text()", "doc", "t1 t2 x t3")]
    [Arguments("a//i", "doc", "i")]
    [Arguments("//b//i", "doc", "i")]
    [Arguments("//b/i", "doc", "i")]
    [Arguments("//b/text()", "doc", "t1 t2 t3")]
    [Arguments("//i/ancestor::*", "doc", "a b2")]
    public async Task DoubleSlash_OnTheTreeOfTheAxisFixture_ReturnsEachNodeOnceInDocumentOrder(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // s1 contains s2, and both contain t1: // must still return every node once.
    [Test]
    [Arguments("//s//t", "N.doc", "N.t1 N.t2")]
    [Arguments("//t", "N.doc", "N.t1 N.t2 N.t3")]
    [Arguments("//s/t", "N.doc", "N.t1 N.t2")]
    [Arguments("/r/s//s", "N.doc", "N.s2")]
    [Arguments("//s//s", "N.doc", "N.s2")]
    [Arguments("//s/..", "N.doc", "N.r N.s1")]
    [Arguments("//*/t", "N.doc", "N.t1 N.t2 N.t3")]
    [Arguments("//t/ancestor::s", "N.doc", "N.s1 N.s2")]
    [Arguments("//s/ancestor-or-self::*", "N.doc", "N.r N.s1 N.s2")]
    [Arguments("//s/descendant-or-self::*", "N.doc", "N.s1 N.s2 N.t1 N.t2")]
    [Arguments("/descendant::*[last()]", "N.doc", "N.t3")]
    public async Task DoubleSlash_OnNestedStructures_ReturnsEachNodeOnceInDocumentOrder(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // //x[1] is descendant-or-self::node()/child::x[1]: the first x child of each node, not the first x of the document.
    // A shortcut to descendant::x would be wrong here.
    [Test]
    [Arguments("//t[1]", "N.doc", "N.t1 N.t2 N.t3")]
    [Arguments("(//t)[1]", "N.doc", "N.t1")]
    [Arguments("//s[1]", "N.doc", "N.s1 N.s2")]
    [Arguments("/descendant::t[1]", "N.doc", "N.t1")]
    [Arguments("//*[1]", "doc", "a b1 i")]
    [Arguments("//*[last()]", "doc", "a i e")]
    [Arguments("//b[2]", "doc", "b2")]
    [Arguments("//b[1]", "doc", "b1")]
    [Arguments("(//b)[2]", "doc", "b2")]
    public async Task DoubleSlash_WithAPredicate_KeepsTheMeaningOfTheExpandedForm(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // The last step may deliver atomic values; they come back as they are, in order, and are not deduplicated.
    [Test]
    [Arguments("/a/b/1", "doc", "integer:1 integer:1")]
    [Arguments("/a/b/'x'", "doc", "string:x string:x")]
    [Arguments("/a/b/(1, 2)", "doc", "integer:1 integer:2 integer:1 integer:2")]
    [Arguments("/a/b/position()", "doc", "integer:1 integer:2")]
    [Arguments("/a/*/last()", "doc", "integer:3 integer:3 integer:3")]
    public async Task LastStep_ReturningAtomicValues_IsAllowed(string expression, string context, string expected) =>
        await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // A step that is not an axis step is evaluated once per context node, with that node, its position, and the size as the focus.
    [Test]
    [Arguments("/a/(b | p:e)", "doc", "b1 b2 e")]
    [Arguments("/a/(b)[2]", "doc", "b2")]
    [Arguments("/a/(b, p:e)", "doc", "b1 b2 e")]
    [Arguments("/a/b/(i, ..)", "doc", "a i")]
    [Arguments("$nodes/self::*", "a", "b1 b2 e")]
    [Arguments("$nodes/..", "a", "a")]
    public async Task PathStep_ThatIsAnExpression_IsEvaluatedWithTheNodesAsTheFocus(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    [Arguments("child::element(b)", "a", "b1 b2")]
    [Arguments("child::element(b, xs:untyped)", "a", "b1 b2")]
    [Arguments("child::element(b, xs:string)", "a", "()")]
    [Arguments("child::element(*, xs:untyped)", "a", "b1 b2 e")]
    [Arguments("attribute::attribute(id, xs:untypedAtomic)", "a", "id")]
    [Arguments("attribute::attribute(id, xs:string)", "a", "()")]
    public async Task TypedKindTest_InAStep_ComparesTheTypeAnnotation(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // The name tests of the same step bind to the name table of each tree separately.
    [Test]
    public async Task NameTest_AppliedToNodesOfTwoTrees_BindsToEachTree()
    {
        using var context = NewContext();
        context.Variables.Set(Atoms, XdmSequence.Create([XdmItem.FromNode(Node("a")), XdmItem.FromNode(Node("N.r"))]));

        var result = CompileOrThrow("$atoms/*").Evaluate(context);

        await Assert.That(Render(result)).IsEqualTo("b1 b2 e N.s1 N.t3");
    }
}
