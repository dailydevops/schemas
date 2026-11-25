namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Paths;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using static PathFixture;

/// <summary>
/// union, intersect, and except (XPath 2.0, 3.3.3): operands are sequences of nodes, the result is in document order without
/// duplicates. Document order of the tree: doc a id lang b1 t1 c pi b2 t2 i x t3 e.
/// </summary>
public class SetOperatorTests
{
    [Test]
    [Arguments("child::b | child::p:e", "a", "b1 b2 e")]
    [Arguments("child::p:e | child::b", "a", "b1 b2 e")]
    [Arguments("child::node() | child::b", "a", "b1 c pi b2 e")]
    [Arguments("child::b | child::b", "a", "b1 b2")]
    [Arguments("child::b union child::p:e", "a", "b1 b2 e")]
    [Arguments("child::b | ()", "a", "b1 b2")]
    [Arguments("() | child::b", "a", "b1 b2")]
    [Arguments("$nodes | child::b", "a", "b1 b2 e")]
    [Arguments("ancestor-or-self::* | child::*", "b2", "a b2 i")]
    [Arguments("ancestor::* | ancestor::node()", "i", "doc a b2")]
    [Arguments("/a/b | /a/p:e", "doc", "b1 b2 e")]
    [Arguments("(child::b | child::p:e)/child::i", "a", "i")]
    [Arguments("(child::b/self::* | child::p:e)/child::i", "a", "i")]
    [Arguments("((child::b | child::c) | child::p:e)/self::p:e", "a", "e")]
    public async Task Union_OnOverlappingSequences_IsInDocumentOrderWithoutDuplicates(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    [Arguments("child::node() intersect child::b", "a", "b1 b2")]
    [Arguments("child::b intersect child::p:e", "a", "()")]
    [Arguments("child::b intersect child::b[1]", "a", "b1")]
    [Arguments("child::b intersect child::b", "a", "b1 b2")]
    [Arguments("$nodes intersect child::node()", "a", "b1 b2 e")]
    [Arguments("child::node() intersect $nodes", "a", "b1 b2 e")]
    [Arguments("ancestor::* intersect ancestor-or-self::*", "i", "a b2")]
    [Arguments("child::b intersect ()", "a", "()")]
    [Arguments("() intersect child::b", "a", "()")]
    public async Task Intersect_OnOverlappingSequences_IsInDocumentOrderWithoutDuplicates(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    [Arguments("child::node() except child::b", "a", "c pi e")]
    [Arguments("child::b except child::b", "a", "()")]
    [Arguments("child::b except child::p:e", "a", "b1 b2")]
    [Arguments("$nodes except child::b", "a", "e")]
    [Arguments("$nodes except child::p:e", "a", "b1 b2")]
    [Arguments("child::b except $nodes", "a", "()")]
    [Arguments("child::node() except $nodes", "a", "c pi")]
    [Arguments("() except child::b", "a", "()")]
    [Arguments("child::b except ()", "a", "b1 b2")]
    public async Task Except_OnOverlappingSequences_IsInDocumentOrderWithoutDuplicates(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // intersect and except bind tighter than union, and the operators are left associative.
    [Test]
    [Arguments("child::node() except child::b | child::p:e", "a", "c pi e")]
    [Arguments("child::node() except child::b except child::p:e", "a", "c pi")]
    [Arguments("child::b | child::node() intersect child::p:e", "a", "b1 b2 e")]
    public async Task SetOperators_Combined_FollowThePrecedenceOfTheGrammar(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    [Arguments("$atoms | child::b")]
    [Arguments("child::b | $atoms")]
    [Arguments("$mixed intersect child::b")]
    [Arguments("child::b except 1")]
    [Arguments("child::b union 'x'")]
    [Arguments("() | $atoms")]
    [Arguments("$atoms except ()")]
    [Arguments("child::b/1 | child::b")]
    [Arguments("child::b | child::b/1")]
    [Arguments("child::b/1 except child::b")]
    public async Task SetOperator_WithANonNodeOperand_RaisesXpty0004(string expression)
    {
        var error = Raised(expression, "a");

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPTY0004);
    }

    // An operand that is a path or a set operation already is in document order; the others are sorted before the merge.
    [Test]
    [Arguments("child::b | child::p:e", "None", "None")]
    [Arguments("$nodes | child::b", "Sort", "None")]
    [Arguments("child::b intersect $nodes", "None", "Sort")]
    [Arguments("ancestor::* except child::b", "None", "None")]
    [Arguments("(child::b | child::p:e) except child::b", "None", "None")]
    [Arguments("$nodes[1] | $nodes", "Sort", "Sort")]
    [Arguments("(child::b)[1] | child::p:e", "None", "None")]
    [Arguments(". | child::b", "None", "None")]
    public async Task SetOperator_SortsOnlyTheOperandsThatAreNotKnownToBeOrdered(
        string expression,
        string left,
        string right
    )
    {
        var node = (SetOperationNode)CompileOrThrow(expression).Root;

        await Assert.That(node.LeftRequirement.ToString()).IsEqualTo(left);
        await Assert.That(node.RightRequirement.ToString()).IsEqualTo(right);
    }
}
