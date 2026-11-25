namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:innermost</c> and <c>fn:outermost</c> (Functions and Operators 3.1, 14.2.1 and 14.2.2): the nodes of the argument that have no
/// descendant, or no ancestor, in the argument, in document order and without duplicates.
/// </summary>
/// <remarks>
/// The specification defines both over the axes: an element is an ancestor of its attributes, but an attribute is not a descendant
/// of its element. So <c>outermost</c> drops an attribute whose element is in the argument, and <c>innermost</c> keeps an element
/// whose attribute is in the argument.
/// </remarks>
internal sealed class NodeSelectionFunction : Delta3Function
{
    private readonly bool _outermost;

    /// <summary>Initializes a new instance of the <see cref="NodeSelectionFunction"/> class.</summary>
    /// <param name="outermost"><see langword="true"/> for <c>fn:outermost</c>, <see langword="false"/> for <c>fn:innermost</c>.</param>
    public NodeSelectionFunction(bool outermost)
        : base(outermost ? "outermost" : "innermost", 1, 1, [NodeItems]) => _outermost = outermost;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (argument.IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        var nodes = new XPathNode[argument.Count];
        for (var i = 0; i < nodes.Length; i++)
        {
            _ = argument[i].TryGetNode(out nodes[i]);
        }

        Array.Sort(nodes, static (left, right) => left.CompareDocumentOrder(in right));
        var count = 0;
        for (var i = 0; i < nodes.Length; i++)
        {
            if (count == 0 || !nodes[count - 1].IsSameNode(in nodes[i]))
            {
                nodes[count++] = nodes[i];
            }
        }

        var selected = _outermost ? Outermost(nodes, count) : Innermost(nodes, count);
        var items = new XdmItem[selected.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var node = selected[i];
            items[i] = XdmItem.FromNode(in node);
        }

        invocation.Result = XdmSequence.Create(items);
        return XPathEvaluationStatus.Ok;
    }

    private static List<XPathNode> Outermost(XPathNode[] nodes, int count)
    {
        var all = new HashSet<XPathNode>();
        for (var i = 0; i < count; i++)
        {
            _ = all.Add(nodes[i]);
        }

        var result = new List<XPathNode>();
        for (var i = 0; i < count; i++)
        {
            var covered = false;
            for (var parent = nodes[i].Parent(); !covered && !parent.IsEmpty(); parent = parent.Parent())
            {
                covered = all.Contains(parent);
            }

            if (!covered)
            {
                result.Add(nodes[i]);
            }
        }

        return result;
    }

    private static List<XPathNode> Innermost(XPathNode[] nodes, int count)
    {
        // An ancestor is marked once; the walk stops at the first one that is marked, because the rest of the chain is marked too.
        var hasDescendant = new HashSet<XPathNode>();
        for (var i = 0; i < count; i++)
        {
            if (nodes[i].Kind is XdmNodeKind.Attribute or XdmNodeKind.Namespace)
            {
                continue;
            }

            var parent = nodes[i].Parent();
            while (!parent.IsEmpty() && hasDescendant.Add(parent))
            {
                parent = parent.Parent();
            }
        }

        var result = new List<XPathNode>();
        for (var i = 0; i < count; i++)
        {
            if (!hasDescendant.Contains(nodes[i]))
            {
                result.Add(nodes[i]);
            }
        }

        return result;
    }
}
