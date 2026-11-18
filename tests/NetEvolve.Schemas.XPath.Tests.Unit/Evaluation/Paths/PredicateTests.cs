namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Paths;

using NetEvolve.Schemas.XPath.Errors;
using static PathFixture;

/// <summary>
/// Predicates (XPath 2.0, 3.2.2) and filter expressions (3.3.2). Document order of the tree: doc a id lang b1 t1 c pi b2 t2 i x t3 e.
/// <c>position()</c> and <c>last()</c> are test-local stand-ins (#264).
/// </summary>
public class PredicateTests
{
    [Test]
    [Arguments("child::b[1]", "a", "b1")]
    [Arguments("child::b[2]", "a", "b2")]
    [Arguments("child::b[3]", "a", "()")]
    [Arguments("child::b[last()]", "a", "b2")]
    [Arguments("child::node()[1]", "a", "b1")]
    [Arguments("child::node()[last()]", "a", "e")]
    [Arguments("child::node()[last() - 1]", "a", "b2")]
    [Arguments("descendant::node()[3]", "a", "c")]
    [Arguments("following-sibling::node()[1]", "b1", "c")]
    [Arguments("following-sibling::node()[last()]", "b1", "e")]
    [Arguments("attribute::*[2]", "a", "lang")]
    [Arguments("child::b[position() = 2]", "a", "b2")]
    [Arguments("child::b[position() = last()]", "a", "b2")]
    public async Task Predicate_Positional_OnAForwardAxis_SelectsByPositionInDocumentOrder(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // A reverse axis counts positions in reverse document order, nearest node first; the result still comes back in document order.
    [Test]
    [Arguments("ancestor::*[1]", "i", "b2")]
    [Arguments("ancestor::*[2]", "i", "a")]
    [Arguments("ancestor::*[last()]", "i", "a")]
    [Arguments("ancestor::node()[1]", "i", "b2")]
    [Arguments("ancestor-or-self::*[1]", "i", "i")]
    [Arguments("preceding-sibling::node()[1]", "e", "b2")]
    [Arguments("preceding-sibling::node()[last()]", "e", "b1")]
    [Arguments("preceding-sibling::node()[position() = 2]", "e", "pi")]
    [Arguments("preceding::*[1]", "e", "i")]
    [Arguments("preceding::*[2]", "e", "b2")]
    [Arguments("preceding::*[last()]", "e", "b1")]
    public async Task Predicate_Positional_OnAReverseAxis_CountsInReverseDocumentOrder(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // A filter expression keeps the order of its primary: (ancestor::*) is in document order, ancestor::* alone is not.
    [Test]
    [Arguments("ancestor::*[1]", "i", "b2")]
    [Arguments("(ancestor::*)[1]", "i", "a")]
    [Arguments("(preceding-sibling::node())[1]", "e", "b1")]
    [Arguments("preceding-sibling::node()[1]", "e", "b2")]
    [Arguments("(preceding::*)[1]", "e", "b1")]
    public async Task Filter_OnAParenthesizedReverseAxis_CountsInDocumentOrder(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    [Arguments("preceding-sibling::node()", "e", "b1 c pi b2")]
    [Arguments("ancestor::*", "i", "a b2")]
    [Arguments("ancestor-or-self::node()", "i", "doc a b2 i")]
    [Arguments("preceding::node()", "e", "b1 t1 c pi b2 t2 i x t3")]
    [Arguments("ancestor::node()[position() > 1]", "i", "doc a")]
    public async Task ReverseAxisStep_DeliversDocumentOrder_AlsoFromASingleContextNode(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // A numeric predicate is a position test: a value that is not an integer matches no position, and NaN matches none.
    [Test]
    [Arguments("child::node()[2.0]", "a", "c")]
    [Arguments("child::node()[2e0]", "a", "c")]
    [Arguments("child::node()[1.5]", "a", "()")]
    [Arguments("child::node()[1.5e0]", "a", "()")]
    [Arguments("child::node()[1 div 2]", "a", "()")]
    [Arguments("child::node()[0]", "a", "()")]
    [Arguments("child::node()[-1]", "a", "()")]
    [Arguments("child::node()[0e0 div 0e0]", "a", "()")]
    [Arguments("child::node()[100000000000000000000]", "a", "()")]
    public async Task Predicate_NumericThatIsNotAnIntegerPosition_SelectsNothing(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    // Every other predicate value goes through the effective boolean value.
    [Test]
    [Arguments("child::*[i]", "a", "b2")]
    [Arguments("child::*[@id]", "a", "()")]
    [Arguments("self::*[@id]", "a", "a")]
    [Arguments("child::node()['x']", "a", "b1 c pi b2 e")]
    [Arguments("child::node()['']", "a", "()")]
    [Arguments("child::node()[position() > 3]", "a", "b2 e")]
    [Arguments("child::node()[position() < 3]", "a", "b1 c")]
    [Arguments("child::node()[position() != 2]", "a", "b1 pi b2 e")]
    public async Task Predicate_Boolean_KeepsTheNodesWhereTheEffectiveBooleanValueIsTrue(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    public async Task Predicate_UntypedAtomicValue_IsNotAPositionTest() =>
        await Assert.That(Run("child::node()[$u]", "a")).IsEqualTo("b1 c pi b2 e");

    [Test]
    public async Task Predicate_SequenceOfTwoAtomicValues_RaisesForg0006()
    {
        var error = Raised("child::node()[$atoms]", "a");

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.FORG0006);
    }

    // The second predicate sees the positions and the size that the first one produced.
    [Test]
    [Arguments("child::node()[position() > 1][1]", "a", "c")]
    [Arguments("child::node()[position() > 1][2]", "a", "pi")]
    [Arguments("child::node()[position() > 1][last()]", "a", "e")]
    [Arguments("child::node()[position() < 4][position() > 2]", "a", "pi")]
    [Arguments("child::node()[self::b][2]", "a", "b2")]
    [Arguments("child::node()[2][self::b]", "a", "()")]
    [Arguments("child::node()[1][1][1]", "a", "b1")]
    [Arguments("child::node()[2][2]", "a", "()")]
    [Arguments("ancestor::node()[position() > 1][1]", "i", "a")]
    public async Task Predicates_Chained_TheSecondSeesThePositionsOfTheFirst(
        string expression,
        string context,
        string expected
    ) => await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    [Arguments("(1, 2, 3)[2]", "integer:2")]
    [Arguments("(1, 2, 3)[last()]", "integer:3")]
    [Arguments("(1, 2, 3)[. > 1]", "integer:2 integer:3")]
    [Arguments("(1, 2, 3)[1.5]", "()")]
    [Arguments("(1, 2, 3)[position() > 1][1]", "integer:2")]
    [Arguments("$atoms[2]", "integer:2")]
    [Arguments("()[1]", "()")]
    public async Task Filter_OnAtomicValues_AppliesThePredicatesToTheSequence(string expression, string expected) =>
        await Assert.That(Run(expression, "a")).IsEqualTo(expected);

    // The variable holds its nodes unsorted and with a duplicate; a filter never sorts or deduplicates.
    [Test]
    [Arguments("$nodes[1]", "e")]
    [Arguments("$nodes[2]", "b1")]
    [Arguments("$nodes[3]", "b1")]
    [Arguments("$nodes[last()]", "b2")]
    [Arguments("$nodes[position() < 3]", "e b1")]
    public async Task Filter_OnNodes_KeepsTheOrderOfThePrimary(string expression, string expected) =>
        await Assert.That(Run(expression, "a")).IsEqualTo(expected);

    [Test]
    [Arguments(".", "b2", "b2")]
    [Arguments("(.)[1]", "b2", "b2")]
    [Arguments(".[self::b]", "b2", "b2")]
    [Arguments(".[self::i]", "b2", "()")]
    public async Task ContextItemExpression_ReturnsTheContextItem(string expression, string context, string expected) =>
        await Assert.That(Run(expression, context)).IsEqualTo(expected);

    [Test]
    public async Task ContextItemExpression_OnAnAtomicContextItem_ReturnsTheValue() =>
        await Assert.That(Render(EvaluateWithAtomicContext("."))).IsEqualTo("integer:5");

    [Test]
    public async Task ContextItemExpression_WithoutAFocus_RaisesXpdy0002()
    {
        var error = Raised(".");

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPDY0002);
    }
}
