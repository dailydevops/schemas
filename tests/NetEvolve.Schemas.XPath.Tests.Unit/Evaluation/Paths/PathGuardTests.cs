namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Paths;

using System;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The guards at the boundaries of the path evaluation: arguments that cannot be right fail at once, not later in a loop.</summary>
public class PathGuardTests
{
    private static Exception? Catch(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    [Test]
    [Arguments(-1)]
    [Arguments(2)]
    public async Task ItemList_Truncate_OutsideTheCount_Throws(int count)
    {
        var error = Catch(() =>
        {
            var list = default(XdmItemList);
            try
            {
                list.Add(XdmItem.FromInteger(1));
                list.Truncate(count);
            }
            finally
            {
                list.Dispose();
            }
        });

        await Assert.That(error).IsTypeOf<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task PredicateTruth_Test_WithAPositionBelowOne_Throws(int position) =>
        await Assert
            .That(Catch(() => PredicateTruth.Test(XdmSequence.Create(XdmItem.FromInteger(1)), position)))
            .IsTypeOf<ArgumentOutOfRangeException>();

    [Test]
    public async Task ItemListNodeSource_WithANegativeCount_Throws() =>
        await Assert.That(Catch(() => _ = new XdmItemListNodeSource(null, -1))).IsTypeOf<ArgumentOutOfRangeException>();

    [Test]
    [Arguments(-1)]
    [Arguments(99)]
    public async Task AxisPathStep_WithAnUndefinedAxis_Throws(int axis) =>
        await Assert
            .That(Catch(() => _ = new AxisPathStep((Axis)axis, XPathNodeTest.AnyNode, null, [])))
            .IsTypeOf<ArgumentOutOfRangeException>();

    [Test]
    [Arguments(-1)]
    [Arguments(3)]
    public async Task PathNode_WithAnUndefinedRoot_Throws(int root) =>
        await Assert
            .That(Catch(() => _ = new PathNode((PathRoot)root, [new ExpressionPathStep(new ContextItemNode(), true)])))
            .IsTypeOf<ArgumentOutOfRangeException>();

    [Test]
    public async Task PathNode_RelativeWithoutASteps_Throws() =>
        await Assert.That(Catch(() => _ = new PathNode(PathRoot.Relative, []))).IsTypeOf<ArgumentOutOfRangeException>();
}
