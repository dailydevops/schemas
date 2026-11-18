namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a path expression (XPath 2.0, 3.2): an optional start at the root, then the steps. Each step runs once per
/// item of the previous step, with that item, its position, and the count as the focus; the combined results of a step that are nodes
/// are brought into document order without duplicates, and only when the static ordering flag of that step says it is needed.
/// </summary>
/// <remarks>
/// The flag is computed from the axes of the steps while the node is built, starting from a single context node (XP-54, #191). The
/// last step may deliver atomic values, which are returned as they are. Limitation: a result of two or more items is copied into one
/// array; the intermediate lists are pooled.
/// </remarks>
internal sealed class PathNode : ExecutableNode, IOrderedNodeSequence
{
    private const XPathOrderProperties Kept = XPathOrderProperties.Single | XPathOrderProperties.NonNested;

    private readonly PathRoot _root;
    private readonly PathStep[] _steps;
    private readonly XPathOrderRequirement[] _requirements;

    /// <summary>Initializes a new instance of the <see cref="PathNode"/> class.</summary>
    /// <param name="root">How the path starts; <see cref="PathRoot.RootDescendants"/> is handled by the lowering, which adds its step.</param>
    /// <param name="steps">The steps, at least one unless the path starts at the root.</param>
    public PathNode(PathRoot root, PathStep[] steps)
    {
        Argument.ThrowIfNull(steps);
        if (root == PathRoot.Relative)
        {
            Argument.ThrowIfLessThan(steps.Length, 1);
        }

        _root = root;
        _steps = steps;
        _requirements = Analyze(steps);
    }

    /// <summary>Gets what each step needs after it ran: nothing, a reversal, duplicate removal, or a sort.</summary>
    public IReadOnlyList<XPathOrderRequirement> Requirements => _requirements;

    /// <inheritdoc />
    public bool IsDocumentOrdered => true;

    /// <inheritdoc />
    /// <exception cref="XPathException">
    /// The context item is absent (XPDY0002), an axis step or the leading slash finds an atomic value (XPTY0020), the root is not a
    /// document node (XPDY0050), a step before the last delivers an atomic value (XPTY0019), or the last step delivers nodes and
    /// atomic values (XPTY0018).
    /// </exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        Argument.ThrowIfNull(context);

        var current = default(XdmItemList);
        var next = default(XdmItemList);
        try
        {
            var index = 0;
            if (_root == PathRoot.Relative)
            {
                _steps[0].EvaluateFromContext(context, ref current);
                Finish(ref current, 0);
                index = 1;
            }
            else
            {
                current.Add(XdmItem.FromNode(GetRoot(context)));
            }

            for (; index < _steps.Length; index++)
            {
                _steps[index].EvaluateOver(context, current.Items, ref next);
                Finish(ref next, index);
                (current, next) = (next, current);
                next.Clear();
            }

            return current.ToSequence();
        }
        finally
        {
            current.Dispose();
            next.Dispose();
        }
    }

    // The properties start from one node: the context item, or the root.
    private static XPathOrderRequirement[] Analyze(PathStep[] steps)
    {
        var requirements = new XPathOrderRequirement[steps.Length];
        var properties = XPathOrderAnalysis.SingleNode;
        for (var i = 0; i < steps.Length; i++)
        {
            var composed = steps[i].Compose(properties);
            var requirement = XPathOrderAnalysis.GetRequirement(composed);
            requirements[i] = requirement;
            properties =
                requirement == XPathOrderRequirement.None
                    ? composed
                    : (composed & Kept) | XPathOrderProperties.Ordered | XPathOrderProperties.DuplicateFree;
        }

        return requirements;
    }

    // fn:root(.) treat as document-node() (XPath 2.0, 3.2, leading slash).
    private static XPathNode GetRoot(XPathEvaluationContext context)
    {
        var item = context.ContextItem ?? throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
        if (!item.TryGetNode(out var root))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0020);
        }

        for (var parent = root.Parent(); !parent.IsEmpty(); parent = root.Parent())
        {
            root = parent;
        }

        return root.Kind == XdmNodeKind.Document ? root : throw XPathErrors.Exception(XPathErrorCodes.XPDY0050);
    }

    // The type rule of E1/E2: all nodes, or all atomic values in the last step.
    private void Finish(ref XdmItemList results, int index)
    {
        var items = results.Items;
        var nodes = 0;
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i].IsNode)
            {
                nodes++;
            }
        }

        if (nodes == items.Length)
        {
            Normalize(ref results, _requirements[index]);
        }
        else if (index != _steps.Length - 1)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0019);
        }
        else if (nodes > 0)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0018);
        }
    }

    private static void Normalize(ref XdmItemList results, XPathOrderRequirement requirement)
    {
        if (requirement == XPathOrderRequirement.None || results.Count < 2)
        {
            return;
        }

        var source = results.GetNodeSource();
        var buffer = XPathDocumentOrder.Normalize(ref source, requirement, null);
        try
        {
            results.Clear();
            foreach (var node in buffer.Nodes)
            {
                results.Add(XdmItem.FromNode(node));
            }
        }
        finally
        {
            buffer.Dispose();
        }
    }
}
