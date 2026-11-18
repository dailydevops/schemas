namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static LimitsFixture;

/// <summary>
/// The hot-path rule of #123: a budget check allocates nothing, so a path evaluation under limits that are not reached allocates as it
/// did without them (see <c>PathAllocationTests</c>; a result of two or more items is copied into one array and is not measured here).
/// </summary>
public class LimitAllocationTests
{
    private static readonly XPathLimits Active = XPathLimits
        .Default.WithMaxSequenceSize(1_000)
        .WithMaxRecursionDepth(50);

    [Test]
    [Arguments("/a/b[1]", "doc")]
    [Arguments("//i", "doc")]
    [Arguments("child::b[2]", "a")]
    [Arguments("a/b/i", "doc")]
    [Arguments("/descendant::i", "doc")]
    public async Task PathEvaluation_UnderLimitsThatAreNotReached_AllocatesNothing(string expression, string focus)
    {
        var compiled = CompileOrThrow(expression, Active);
        using var context = NewContext(Active);
        context.SetContextItem(XdmItem.FromNode(AxisFixture.Node(focus)));
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = compiled.Evaluate(context), warmups: 50);

        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TheLimitHelpers_WithinTheLimit_AllocateNothing()
    {
        var limits = XPathLimits.Default;
        var location = new SourceSpan(0, 1);

        AllocationAssert.NoAllocation(
            () =>
            {
                limits.ThrowIfTooDeep(limits.MaxRecursionDepth, location);
                limits.ThrowIfRangeTooLarge(1, limits.MaxRangeSize, location);
                limits.ThrowIfStringTooLong(limits.MaxStringLength, location);
            },
            warmups: 50
        );

        await Assert.That(limits.MaxRangeSize).IsGreaterThan(0L);
    }

    [Test]
    public async Task ABoundedList_InSteadyState_AllocatesNothing()
    {
        var list = new XdmItemList(100, new SourceSpan(0, 1));
        try
        {
            AllocationAssert.NoAllocation(
                () =>
                {
                    list.Clear();
                    for (var i = 0; i < 50; i++)
                    {
                        list.Add(XdmItem.FromInteger(i));
                    }
                },
                warmups: 50
            );

            await Assert.That(list.Count).IsEqualTo(50);
        }
        finally
        {
            list.Dispose();
        }
    }
}
