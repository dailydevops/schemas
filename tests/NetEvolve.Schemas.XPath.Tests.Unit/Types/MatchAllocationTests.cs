namespace NetEvolve.Schemas.XPath.Tests.Unit.Types;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static TypeFixture;

public class MatchAllocationTests
{
    private const int Length = 1_000;

    private static XdmSequence Of(Func<int, XdmItem> item)
    {
        var items = new XdmItem[Length];
        for (var i = 0; i < Length; i++)
        {
            items[i] = item(i);
        }

        return Seq(items);
    }

    private static async Task AssertNoAllocation(
        string type,
        XdmSequence sequence,
        IXdmTypeAnnotationSource? source = null
    )
    {
        var matcher = Matcher(type);
        var matched = false;

        AllocationAssert.NoAllocation(() =>
            matched = source is null ? matcher.Matches(sequence) : matcher.Matches(sequence, source)
        );

        await Assert.That(sequence.Count).IsEqualTo(Length);
        await Assert.That(matched).IsTrue();
    }

    [Test]
    public async Task Match_ThousandIntegers_AllocatesNothing() =>
        await AssertNoAllocation("xs:decimal*", Of(static i => XdmItem.FromInteger(i)));

    [Test]
    public async Task Match_ThousandStrings_AgainstAnyAtomicType_AllocatesNothing() =>
        await AssertNoAllocation("xs:anyAtomicType+", Of(static _ => XdmItem.FromString("s")));

    [Test]
    public async Task Match_ThousandItems_AgainstItem_AllocatesNothing() =>
        await AssertNoAllocation("item()*", Of(static i => XdmItem.FromBoolean(i % 2 == 0)));

    [Test]
    public async Task Match_ThousandElementsWithNameTest_AllocatesNothing() =>
        await AssertNoAllocation("element(b)*", Of(static _ => Node("b1")));

    [Test]
    public async Task Match_ThousandNodesWithKindTest_AllocatesNothing() =>
        await AssertNoAllocation("node()*", Of(static i => Node(i % 2 == 0 ? "t1" : "c")));

    [Test]
    public async Task Match_ThousandElementsWithTypeTest_ThroughAnAnnotationSource_AllocatesNothing()
    {
        var source = new FakeAnnotations(AxisFixture.Node("b1"), XdmTypes.Get("integer"));

        await AssertNoAllocation("element(b, xs:decimal)*", Of(static _ => Node("b1")), source);
    }

    [Test]
    public async Task Match_ThousandUntypedAttributes_WithTypeTest_AllocatesNothing() =>
        await AssertNoAllocation("attribute(id, xs:untypedAtomic)*", Of(static _ => Node("id")));

    [Test]
    public async Task Match_FailureOnTheLastItem_AllocatesNothing()
    {
        var sequence = Of(static i => i == Length - 1 ? XdmItem.FromString("x") : XdmItem.FromInteger(i));
        var matcher = Matcher("xs:integer*");
        var matched = true;

        AllocationAssert.NoAllocation(() => matched = matcher.Matches(sequence));

        await Assert.That(matched).IsFalse();
    }
}
