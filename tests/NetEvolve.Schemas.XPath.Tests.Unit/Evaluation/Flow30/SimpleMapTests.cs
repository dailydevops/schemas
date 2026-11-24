namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow30;

using NetEvolve.Schemas.XPath.Limits;
using static Flow30Fixture;

/// <summary>The simple map operator (XPath 3.0, 3.9): the right side runs once per item with a new focus; no sort and no duplicate removal.</summary>
public class SimpleMapTests
{
    [Test]
    [Arguments("(1, 2, 3) ! (. * 2)", "integer:2, integer:4, integer:6")]
    [Arguments("() ! 1", "()")]
    [Arguments("(1, 2) ! ()", "()")]
    [Arguments("(1, 2) ! (., .)", "integer:1, integer:1, integer:2, integer:2")]
    [Arguments("1 ! (. + 1)", "integer:2")]
    public async Task AcceptsAtomicItemsOnTheLeft_AndConcatenatesTheResults(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("(1, 1, 2, 1) ! .", "integer:1, integer:1, integer:2, integer:1")]
    [Arguments("(3, 1, 2) ! .", "integer:3, integer:1, integer:2")]
    [Arguments("(1, 1) ! 5", "integer:5, integer:5")]
    public async Task KeepsDuplicates_AndTheOrderOfEvaluation(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("('a', 'b', 'c') ! position()", "integer:1, integer:2, integer:3")]
    [Arguments("('a', 'b', 'c') ! last()", "integer:3, integer:3, integer:3")]
    [Arguments(
        "(1, 2) ! ((10, 20, 30) ! position())",
        "integer:1, integer:2, integer:3, integer:1, integer:2, integer:3"
    )]
    [Arguments(
        "(1, 2) ! ((10, 20, 30) ! (. + 1))",
        "integer:11, integer:21, integer:31, integer:11, integer:21, integer:31"
    )]
    [Arguments("(1, 2, 3) ! last() ! position()", "integer:1, integer:2, integer:3")]
    public async Task SetsPositionAndLast_ForEveryItem(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    public async Task TheFocusOfTheOuterExpression_IsRestored() =>
        await Assert
            .That(Run("(5, 6) ! ((1, 2) ! 0, position(), last())"))
            .IsEqualTo("integer:0, integer:0, integer:1, integer:2, integer:0, integer:0, integer:2, integer:2");

    [Test]
    public async Task AResultLargerThanTheSequenceSizeLimit_RaisesTheLimitError()
    {
        var limits = XPathLimits.Default.WithMaxSequenceSize(5);

        await Assert.That(Code("(1, 2, 3) ! (., .)", limits)).IsEqualTo("XP0004");
    }
}
