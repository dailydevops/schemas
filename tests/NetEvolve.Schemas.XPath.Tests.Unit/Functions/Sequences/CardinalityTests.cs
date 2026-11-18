namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:empty</c>, <c>fn:exists</c>, <c>fn:count</c>, <c>fn:zero-or-one</c>, <c>fn:one-or-more</c>, and <c>fn:exactly-one</c>
/// (Functions and Operators 1.0, 15.1 and 15.2): the argument is not atomized and not converted, so nodes and every atomic type count.
/// </summary>
public class CardinalityTests
{
    [Test]
    [Arguments("()", "boolean:true")]
    [Arguments("integer:1", "boolean:false")]
    [Arguments("integer:1|integer:2", "boolean:false")]
    [Arguments("node:d1", "boolean:false")]
    [Arguments("string:", "boolean:false")]
    [Arguments("boolean:false", "boolean:false")]
    [Arguments("double:NaN", "boolean:false")]
    public async Task Empty_IsTrueOnlyForTheEmptySequence(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("empty", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "boolean:false")]
    [Arguments("integer:0", "boolean:true")]
    [Arguments("integer:1|integer:2", "boolean:true")]
    [Arguments("node:d1/0", "boolean:true")]
    [Arguments("string:", "boolean:true")]
    [Arguments("boolean:false", "boolean:true")]
    public async Task Exists_IsTrueForAnyNonEmptySequence(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("exists", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "integer:0")]
    [Arguments("integer:1", "integer:1")]
    [Arguments("integer:1|integer:1|integer:1", "integer:3")]
    [Arguments("node:d1|node:d1/0|node:d1/0@a", "integer:3")]
    [Arguments("string:a|integer:1|boolean:true|double:NaN", "integer:4")]
    [Arguments("string:|string:", "integer:2")]
    public async Task Count_IsTheNumberOfItems_DuplicatesIncluded(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("count", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "")]
    [Arguments("integer:7", "integer:7")]
    [Arguments("integer:1|integer:2", "error:FORG0003")]
    [Arguments("node:d1|node:d1/0", "error:FORG0003")]
    public async Task ZeroOrOne_ReturnsTheArgumentOrRaisesForMoreThanOneItem(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("zero-or-one", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "error:FORG0004")]
    [Arguments("integer:7", "integer:7")]
    [Arguments("integer:1|string:b", "integer:1,string:b")]
    [Arguments("node:d1|node:d1/0", "node:d1,node:d1/0")]
    public async Task OneOrMore_ReturnsTheArgumentOrRaisesForTheEmptySequence(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("one-or-more", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "error:FORG0005")]
    [Arguments("integer:7", "integer:7")]
    [Arguments("integer:1|integer:2", "error:FORG0005")]
    [Arguments("node:d1/0", "node:d1/0")]
    public async Task ExactlyOne_ReturnsTheItemOrRaisesForAnyOtherCount(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("exactly-one", argument)).IsEqualTo(expected);
}
