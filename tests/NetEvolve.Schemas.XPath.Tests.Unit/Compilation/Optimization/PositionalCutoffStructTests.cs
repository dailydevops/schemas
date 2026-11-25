namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static OptimizationFixture;

/// <summary>The positions a <see cref="PositionalCutoff"/> keeps, and the paths that <see cref="PathNode.TryLimit"/> accepts.</summary>
public class PositionalCutoffStructTests
{
    [Test]
    [Arguments(1, 3, 5, "1 2 3", 3)]
    [Arguments(2, 3, 5, "2 3", 3)]
    [Arguments(4, 9, 5, "4 5", 9)]
    [Arguments(6, 9, 5, "", 9)]
    [Arguments(1, 0, 5, "", 1)]
    [Arguments(3, int.MaxValue, 5, "3 4 5", 0)]
    public async Task Apply_Range_KeepsTheNamedPositions(int first, int last, int count, string expected, int budget)
    {
        var items = Items(count);
        try
        {
            var cutoff = PositionalCutoff.Range(first, last);

            cutoff.Apply(ref items);

            await Assert.That(Show(items)).IsEqualTo(expected);
            await Assert.That(cutoff.Budget).IsEqualTo(budget);
        }
        finally
        {
            items.Dispose();
        }
    }

    [Test]
    [Arguments(0, "")]
    [Arguments(1, "1")]
    [Arguments(4, "4")]
    public async Task Apply_LastItem_KeepsTheLastItem(int count, string expected)
    {
        var items = Items(count);
        try
        {
            var cutoff = PositionalCutoff.LastItem();

            cutoff.Apply(ref items);

            await Assert.That(Show(items)).IsEqualTo(expected);
            await Assert.That(cutoff.Budget).IsEqualTo(0);
        }
        finally
        {
            items.Dispose();
        }
    }

    [Test]
    public async Task Range_PositionBelowOne_IsRefused() =>
        await Assert.That(static () => PositionalCutoff.Range(0, 1)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task TryLimit_PathOfAxisStepsWithoutPredicates_KeepsTheFirstLimit()
    {
        var path = (PathNode)Compile("//x", XPathOptimizations.None).Root;

        await Assert.That(path.Limit).IsEqualTo(0);
        await Assert.That(path.TryLimit(2)).IsTrue();
        await Assert.That(path.Limit).IsEqualTo(2);
        await Assert.That(path.TryLimit(1)).IsTrue();
        await Assert.That(path.TryLimit(3)).IsFalse();
        await Assert.That(path.Limit).IsEqualTo(2);
        await Assert.That(path.Root).IsEqualTo(PathRoot.RootDescendants);
    }

    [Test]
    [Arguments("//x[. ne 0]")]
    [Arguments("//x/string(.)")]
    [Arguments("//x/ancestor::*")]
    public async Task TryLimit_PathThatCannotStopEarly_IsRefused(string expression)
    {
        var path = (PathNode)Compile(expression, XPathOptimizations.None).Root;

        await Assert.That(path.TryLimit(1)).IsFalse();
        await Assert.That(path.Limit).IsEqualTo(0);
    }

    [Test]
    public async Task TryLimit_LimitBelowOne_IsRefused()
    {
        var path = (PathNode)Compile("//x", XPathOptimizations.None).Root;

        await Assert.That(() => path.TryLimit(0)).Throws<ArgumentOutOfRangeException>();
    }

    private static XdmItemList Items(int count)
    {
        var items = new XdmItemList(100, new SourceSpan(0, 0));
        for (var i = 1; i <= count; i++)
        {
            items.Add(XdmItem.FromInteger(i));
        }

        return items;
    }

    private static string Show(XdmItemList items)
    {
        var text = new System.Text.StringBuilder();
        foreach (var item in items.Items)
        {
            _ = item.TryGetInteger(out var number);
            _ = text.Append(text.Length == 0 ? string.Empty : " ").Append(number);
        }

        return text.ToString();
    }
}
