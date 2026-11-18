namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:deep-equal</c> (Functions and Operators 1.0, 15.3.1): sequences are equal when they have the same length and their items are
/// equal in turn. Atomic items are equal by <c>eq</c> (NaN equals NaN, and values that cannot be compared are different without an
/// error); nodes are equal by kind, name, value, attributes (in any order), and children, where comments and processing
/// instructions among the children are not looked at. Trees d1 to d9 are written in <see cref="SequencesHarness"/>: d1 is the
/// reference, d2 differs only in attribute order and in the comment and instruction that d1 has, and d3 to d9 each differ in one respect.
/// </summary>
public class DeepEqualTests
{
    [Test]
    [Arguments("()", "()", "true")]
    [Arguments("()", "integer:1", "false")]
    [Arguments("integer:1", "()", "false")]
    [Arguments("integer:1|integer:2", "integer:1|integer:2", "true")]
    [Arguments("integer:1|integer:2", "integer:2|integer:1", "false")]
    [Arguments("integer:1|integer:2", "integer:1", "false")]
    [Arguments("integer:1", "decimal:1.0", "true")]
    [Arguments("decimal:1.0", "double:1", "true")]
    [Arguments("integer:1", "string:1", "false")]
    [Arguments("string:a", "string:b", "false")]
    [Arguments("string:a", "untypedAtomic:a", "true")]
    [Arguments("anyURI:a", "string:a", "true")]
    [Arguments("boolean:true", "boolean:true", "true")]
    [Arguments("boolean:true", "integer:1", "false")]
    [Arguments("double:NaN", "double:NaN", "true")]
    [Arguments("double:NaN", "float:NaN", "true")]
    [Arguments("double:NaN", "integer:1", "false")]
    [Arguments("double:0", "double:-0", "true")]
    [Arguments("double:INF", "double:INF", "true")]
    [Arguments("date:2000-01-01", "date:2000-01-01", "true")]
    [Arguments("date:2000-01-01", "string:2000-01-01", "false")]
    public async Task DeepEqual_OnAtomicItems_ComparesByEqualityWithoutAnError(
        string first,
        string second,
        string expected
    ) => await Assert.That(SequencesHarness.Run("deep-equal", first, second)).IsEqualTo("boolean:" + expected);

    [Test]
    [Arguments("d1", "d1", "true")]
    [Arguments("d1", "d2", "true")]
    [Arguments("d1/0", "d2/0", "true")]
    [Arguments("d1", "d3", "false")]
    [Arguments("d1", "d4", "false")]
    [Arguments("d1", "d5", "false")]
    [Arguments("d1", "d6", "false")]
    [Arguments("d1", "d7", "false")]
    [Arguments("d1", "d8", "false")]
    [Arguments("d1", "d9", "false")]
    [Arguments("d9", "d1", "false")]
    [Arguments("w1", "w2", "true")]
    [Arguments("d1/0/1", "d4/0/1", "false")]
    [Arguments("d1/0/1", "d2/0/1", "true")]
    [Arguments("d1/0/2", "d9/0/1", "false")]
    public async Task DeepEqual_OnTrees_ComparesNamesAttributesAndChildren(
        string first,
        string second,
        string expected
    ) =>
        await Assert
            .That(SequencesHarness.Run("deep-equal", "node:" + first, "node:" + second))
            .IsEqualTo("boolean:" + expected);

    [Test]
    [Arguments("d1/0/0", "d3/0/0", "true")]
    [Arguments("d1/0/1/0", "d4/0/1/0", "false")]
    [Arguments("d1/0@a", "d2/0@a", "true")]
    [Arguments("d1/0@a", "d3/0@a", "false")]
    [Arguments("d1/0@a", "d1/0@b", "false")]
    [Arguments("n1/0@a", "n2/0@a", "false")]
    [Arguments("d1/0/3", "c1/0/0", "true")]
    [Arguments("d1/0/3", "c2/0/0", "false")]
    [Arguments("d1/0/4", "i1/0/0", "true")]
    [Arguments("d1/0/4", "i2/0/0", "false")]
    [Arguments("d1/0/4", "i3/0/0", "false")]
    [Arguments("d1/0", "d1/0/0", "false")]
    [Arguments("d1/0/0", "d1/0@a", "false")]
    public async Task DeepEqual_OnSingleNodes_ComparesTheirKindNameAndValue(
        string first,
        string second,
        string expected
    ) =>
        await Assert
            .That(SequencesHarness.Run("deep-equal", "node:" + first, "node:" + second))
            .IsEqualTo("boolean:" + expected);

    [Test]
    [Arguments("node:d1/0/1|node:d1/0/2", "node:d2/0/1|node:d2/0/2", "true")]
    [Arguments("node:d1/0/1|node:d1/0/2", "node:d2/0/2|node:d2/0/1", "false")]
    [Arguments("node:d1/0/1|node:d1/0/2", "node:d2/0/1", "false")]
    [Arguments("node:d1/0@a|string:x", "node:d2/0@a|string:x", "true")]
    [Arguments("node:d1/0@a", "string:1", "false")]
    [Arguments("string:1", "node:d1/0@a", "false")]
    public async Task DeepEqual_OnSequencesOfNodes_ComparesLengthAndOrder(
        string first,
        string second,
        string expected
    ) => await Assert.That(SequencesHarness.Run("deep-equal", first, second)).IsEqualTo("boolean:" + expected);
}
