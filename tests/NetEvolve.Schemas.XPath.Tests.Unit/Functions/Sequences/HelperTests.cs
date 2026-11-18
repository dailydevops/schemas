namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Sequences;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The shared helpers of the group: value equality with its hash, the deep comparison of nodes, and the collation argument.</summary>
public class HelperTests
{
    [Test]
    [Arguments("integer:1", "decimal:1.0")]
    [Arguments("integer:1", "double:1")]
    [Arguments("integer:1", "float:1")]
    [Arguments("decimal:2.5", "double:2.5")]
    [Arguments("double:0", "double:-0")]
    [Arguments("string:a", "untypedAtomic:a")]
    [Arguments("string:a", "anyURI:a")]
    [Arguments("boolean:true", "boolean:true")]
    [Arguments("double:NaN", "float:NaN")]
    public async Task AreEqual_ValuesThatAreEqual_HaveTheSameHashCode(string first, string second)
    {
        var (left, right) = Pair(first + "|" + second);

        await Assert.That(AtomicEquality.AreEqual(in left, in right, nanEqual: true)).IsTrue();
        await Assert.That(AtomicEquality.HashCode(in left)).IsEqualTo(AtomicEquality.HashCode(in right));
    }

    [Test]
    [Arguments("double:NaN", "double:NaN")]
    [Arguments("float:NaN", "double:NaN")]
    public async Task AreEqual_NaN_IsEqualToNaNOnlyWhenAskedTo(string first, string second)
    {
        var (left, right) = Pair(first + "|" + second);

        await Assert.That(AtomicEquality.AreEqual(in left, in right, nanEqual: false)).IsFalse();
        await Assert.That(AtomicEquality.AreEqual(in left, in right, nanEqual: true)).IsTrue();
    }

    [Test]
    [Arguments("integer:1", "integer:2")]
    [Arguments("integer:1", "string:1")]
    [Arguments("boolean:true", "integer:1")]
    [Arguments("string:a", "string:A")]
    [Arguments("node:d1", "node:d2")]
    [Arguments("node:d1", "string:a")]
    public async Task AreEqual_ValuesThatAreDifferentOrCannotBeCompared_AreNotEqual(string first, string second)
    {
        var (left, right) = Pair(first + "|" + second);

        await Assert.That(AtomicEquality.AreEqual(in left, in right, nanEqual: true)).IsFalse();
    }

    [Test]
    public async Task HashCode_OfDefaultItem_IsStable()
    {
        var none = default(XdmItem);

        await Assert.That(AtomicEquality.HashCode(in none)).IsEqualTo(AtomicEquality.HashCode(in none));
    }

    [Test]
    public async Task DeepEqual_DefaultNode_ThrowsArgumentNullException()
    {
        var node = SequencesHarness.ParseItem("node:d1");
        _ = node.TryGetNode(out var real);
        var none = default(XPathNode);

        await Assert.That(() => NodeEquality.DeepEqual(in none, in real)).Throws<ArgumentNullException>();
        await Assert.That(() => NodeEquality.DeepEqual(in real, in none)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task DeepEqual_NodesOfATreeWithoutSpanAccess_AreComparedByTheirStringValues()
    {
        var first = new XPathNode(new FakeNodeSource().Text);
        var second = new XPathNode(new FakeNodeSource().Text);
        var attribute = new XPathNode(new FakeNodeSource().Attribute);

        await Assert.That(NodeEquality.DeepEqual(in first, in second)).IsTrue();
        await Assert.That(NodeEquality.DeepEqual(in first, in attribute)).IsFalse();
    }

    [Test]
    public async Task DeepEqual_ADeepTree_IsComparedWithoutRecursion()
    {
        var left = SequencesHarness.ParseItem("node:deep1");
        var right = SequencesHarness.ParseItem("node:deep2");
        _ = left.TryGetNode(out var first);
        _ = right.TryGetNode(out var second);

        await Assert.That(NodeEquality.DeepEqual(in first, in second)).IsTrue();
    }

    [Test]
    public async Task CollationArgument_Check_AbsentArgumentAndCodepointUri_Pass()
    {
        XdmSequence[] none = [XdmSequence.Empty];
        XdmSequence[] codepoint =
        [
            XdmSequence.Empty,
            XdmSequence.Create(XdmItem.FromString(SequencesHarness.Codepoint)),
        ];

        await Assert.That(() => CollationArgument.Check(none, 1)).ThrowsNothing();
        await Assert.That(() => CollationArgument.Check(codepoint, 1)).ThrowsNothing();
    }

    [Test]
    public async Task CollationArgument_Check_AnyOtherUri_RaisesFOCH0002()
    {
        XdmSequence[] other = [XdmSequence.Empty, XdmSequence.Create(XdmItem.FromString("urn:other"))];

        var failure = Assert.Throws<XPathException>(() => CollationArgument.Check(other, 1));

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("FOCH0002");
    }

    [Test]
    public async Task CollationArgument_Check_NegativeIndex_Throws()
    {
        XdmSequence[] none = [];

        await Assert.That(() => CollationArgument.Check(none, -1)).Throws<ArgumentOutOfRangeException>();
    }

    private static (XdmItem Left, XdmItem Right) Pair(string both)
    {
        var parsed = SequencesHarness.Parse(both);
        return (parsed[0], parsed[1]);
    }
}
