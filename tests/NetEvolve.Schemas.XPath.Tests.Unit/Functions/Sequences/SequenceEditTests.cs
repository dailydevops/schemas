namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:subsequence</c>, <c>fn:insert-before</c>, <c>fn:remove</c>, <c>fn:reverse</c>, and <c>fn:unordered</c> (Functions and
/// Operators 1.0, 15.1): positions are counted from one, and the arguments of <c>fn:subsequence</c> are rounded as <c>fn:round</c>
/// rounds, half toward positive infinity, with NaN and infinity taking part in the comparison of the specification.
/// </summary>
public class SequenceEditTests
{
    private const string Five = "integer:1|integer:2|integer:3|integer:4|integer:5";

    [Test]
    [Arguments("2", "2", "integer:2,integer:3")]
    [Arguments("1", "5", "integer:1,integer:2,integer:3,integer:4,integer:5")]
    [Arguments("4", "10", "integer:4,integer:5")]
    [Arguments("5", "1", "integer:5")]
    [Arguments("6", "1", "")]
    [Arguments("1", "0", "")]
    [Arguments("1", "-1", "")]
    [Arguments("0", "2", "integer:1")]
    [Arguments("-1", "3", "integer:1")]
    [Arguments("-5", "3", "")]
    [Arguments("-1", "7", "integer:1,integer:2,integer:3,integer:4,integer:5")]
    public async Task Subsequence_WithIntegerArguments_ReturnsThePositionsInTheWindow(
        string start,
        string length,
        string expected
    ) =>
        await Assert
            .That(SequencesHarness.Run("subsequence", Five, "integer:" + start, "integer:" + length))
            .IsEqualTo(expected);

    [Test]
    [Arguments("1.5", "2.5", "integer:2,integer:3,integer:4")]
    [Arguments("1.4", "1.4", "integer:1")]
    [Arguments("0.5", "1", "integer:1")]
    [Arguments("-0.5", "2", "integer:1")]
    [Arguments("2.5", "0.5", "integer:3")]
    [Arguments("3", "1.5", "integer:3,integer:4")]
    [Arguments("0.49999999999999994", "1", "")]
    [Arguments("2", "0.4", "")]
    public async Task Subsequence_WithFractionalArguments_RoundsHalfTowardPositiveInfinity(
        string start,
        string length,
        string expected
    ) =>
        await Assert
            .That(SequencesHarness.Run("subsequence", Five, "double:" + start, "double:" + length))
            .IsEqualTo(expected);

    [Test]
    [Arguments("NaN", "2", "")]
    [Arguments("1", "NaN", "")]
    [Arguments("NaN", "NaN", "")]
    [Arguments("-INF", "INF", "")]
    [Arguments("-INF", "3", "")]
    [Arguments("INF", "INF", "")]
    [Arguments("INF", "1", "")]
    [Arguments("2", "-INF", "")]
    [Arguments("1", "INF", "integer:1,integer:2,integer:3,integer:4,integer:5")]
    [Arguments("-3", "INF", "integer:1,integer:2,integer:3,integer:4,integer:5")]
    [Arguments("4", "1e300", "integer:4,integer:5")]
    [Arguments("-1e300", "1e300", "")]
    public async Task Subsequence_WithNaNAndInfiniteArguments_FollowsTheComparisonOfTheSpecification(
        string start,
        string length,
        string expected
    ) =>
        await Assert
            .That(SequencesHarness.Run("subsequence", Five, "double:" + start, "double:" + length))
            .IsEqualTo(expected);

    [Test]
    [Arguments("2", "integer:2,integer:3,integer:4,integer:5")]
    [Arguments("0", "integer:1,integer:2,integer:3,integer:4,integer:5")]
    [Arguments("-INF", "integer:1,integer:2,integer:3,integer:4,integer:5")]
    [Arguments("INF", "")]
    [Arguments("NaN", "")]
    [Arguments("5", "integer:5")]
    [Arguments("6", "")]
    [Arguments("1.5", "integer:2,integer:3,integer:4,integer:5")]
    public async Task Subsequence_WithoutALength_ReturnsEverythingFromTheStart(string start, string expected) =>
        await Assert.That(SequencesHarness.Run("subsequence", Five, "double:" + start)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "1", "2", "")]
    [Arguments("node:d1|node:d1/0|node:d1/0@a", "2", "2", "node:d1/0,node:d1/0@a")]
    public async Task Subsequence_OverNodesAndTheEmptySequence_ReturnsTheSelectedItems(
        string input,
        string start,
        string length,
        string expected
    ) =>
        await Assert
            .That(SequencesHarness.Run("subsequence", input, "integer:" + start, "integer:" + length))
            .IsEqualTo(expected);

    [Test]
    [Arguments("string:1", "integer:1")]
    [Arguments("integer:1", "string:1")]
    [Arguments("integer:1|integer:2", "integer:1")]
    public async Task Subsequence_WithWrongArguments_RaisesXPTY0004(string start, string length) =>
        await Assert.That(SequencesHarness.Run("subsequence", Five, start, length)).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("integer:1|integer:2|integer:3", "2", "string:x", "integer:1,string:x,integer:2,integer:3")]
    [Arguments("integer:1|integer:2|integer:3", "0", "string:x", "string:x,integer:1,integer:2,integer:3")]
    [Arguments("integer:1|integer:2|integer:3", "-5", "string:x", "string:x,integer:1,integer:2,integer:3")]
    [Arguments("integer:1|integer:2|integer:3", "1", "string:x", "string:x,integer:1,integer:2,integer:3")]
    [Arguments("integer:1|integer:2|integer:3", "4", "string:x", "integer:1,integer:2,integer:3,string:x")]
    [Arguments("integer:1|integer:2|integer:3", "10", "string:x", "integer:1,integer:2,integer:3,string:x")]
    [Arguments(
        "integer:1|integer:2|integer:3",
        "2",
        "string:x|string:y",
        "integer:1,string:x,string:y,integer:2,integer:3"
    )]
    [Arguments("integer:1|integer:2", "2", "()", "integer:1,integer:2")]
    [Arguments("()", "5", "string:x", "string:x")]
    [Arguments("()", "1", "()", "")]
    public async Task InsertBefore_PutsTheInsertsAtTheClampedPosition(
        string target,
        string position,
        string inserts,
        string expected
    ) =>
        await Assert
            .That(SequencesHarness.Run("insert-before", target, "integer:" + position, inserts))
            .IsEqualTo(expected);

    [Test]
    public async Task InsertBefore_PositionThatIsNotAnInteger_RaisesXPTY0004() =>
        await Assert
            .That(SequencesHarness.Run("insert-before", "integer:1", "decimal:1.5", "integer:2"))
            .IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("2", "integer:1,integer:3")]
    [Arguments("1", "integer:2,integer:3")]
    [Arguments("3", "integer:1,integer:2")]
    [Arguments("0", "integer:1,integer:2,integer:3")]
    [Arguments("-1", "integer:1,integer:2,integer:3")]
    [Arguments("4", "integer:1,integer:2,integer:3")]
    public async Task Remove_DropsTheItemAtThePositionAndIgnoresPositionsOutsideTheSequence(
        string position,
        string expected
    ) =>
        await Assert
            .That(SequencesHarness.Run("remove", "integer:1|integer:2|integer:3", "integer:" + position))
            .IsEqualTo(expected);

    [Test]
    [Arguments("()", "1", "")]
    [Arguments("integer:1", "1", "")]
    public async Task Remove_OfTheOnlyItemOrOfNothing_ReturnsTheEmptySequence(
        string target,
        string position,
        string expected
    ) => await Assert.That(SequencesHarness.Run("remove", target, "integer:" + position)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "")]
    [Arguments("integer:1", "integer:1")]
    [Arguments("integer:1|integer:2|integer:3", "integer:3,integer:2,integer:1")]
    [Arguments("node:d1|node:d1/0|string:a", "string:a,node:d1/0,node:d1")]
    public async Task Reverse_ReturnsTheItemsInOppositeOrder(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("reverse", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "")]
    [Arguments("integer:3|integer:1|integer:2", "integer:3,integer:1,integer:2")]
    [Arguments("node:d1/0|node:d1", "node:d1/0,node:d1")]
    public async Task Unordered_ReturnsTheItemsOfTheArgument(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("unordered", argument)).IsEqualTo(expected);
}
