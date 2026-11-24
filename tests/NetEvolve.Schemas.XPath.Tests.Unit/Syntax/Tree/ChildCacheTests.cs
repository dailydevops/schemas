namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;

public class ChildCacheTests
{
    private const string Text = "for $x in (1, 2) return a/b[c = $x]/f(1, 'two', ?) + (if (1) then 2 else 3)";

    private static Expression Parse() =>
        XPathParser.TryParse(Text.AsSpan(), out var expression, out _)
            ? expression!
            : throw new InvalidOperationException("The expression was rejected.");

    private static int Walk(SyntaxNode node)
    {
        var count = 1;
        var children = node.Children;
        for (var i = 0; i < children.Count; i++)
        {
            count += Walk(children[i]);
        }
        return count;
    }

    [Test]
    public async Task Children_ReturnsTheSameListOnEveryAccess_ForEveryNode()
    {
        var root = Parse();
        var stack = new System.Collections.Generic.Stack<SyntaxNode>();
        stack.Push(root);
        var visited = 0;
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            await Assert.That(node.Children).IsSameReferenceAs(node.Children);
            visited++;
            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }

        await Assert.That(visited).IsGreaterThan(20);
    }

    [Test]
    public async Task GenericTraversal_AfterTheFirstWalk_AllocatesNothing()
    {
        var root = Parse();
        var nodes = 0;

        AllocationAssert.NoAllocation(() => nodes = Walk(root));

        await Assert.That(nodes).IsGreaterThan(20);
    }
}
