namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Tree;

internal static class TreeWalker
{
    /// <summary>Returns the node and all of its descendants, parents first.</summary>
    public static List<SyntaxNode> Descendants(SyntaxNode root)
    {
        var result = new List<SyntaxNode>();
        Collect(root, result);
        return result;
    }

    private static void Collect(SyntaxNode node, List<SyntaxNode> result)
    {
        result.Add(node);
        foreach (var child in node.Children)
        {
            Collect(child, result);
        }
    }
}
