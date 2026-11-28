namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The two steps <c>descendant-or-self::node()/attribute::T</c> that <c>//@T</c> stands for, in one walk: the elements at and below the
/// context node, and the attributes of each that pass the node test, in document order. The node list of the first step and the
/// attribute step per node of it are not built.
/// </summary>
/// <remarks>
/// Only a step without predicates takes this form: a predicate counts among the attributes of one element, so it stays on the expanded
/// form. The results are those of the two steps for each context node, one context node after the other, so the ordering analysis of
/// the two steps applies unchanged.
/// </remarks>
internal sealed class DescendantAttributeStep : PathStep
{
    private static readonly XPathNodeTest Elements = XPathNodeTest.Element(null, null);

    private readonly XPathNodeTest _test;

    /// <summary>Initializes a new instance of the <see cref="DescendantAttributeStep"/> class.</summary>
    /// <param name="test">The node test of the attribute step.</param>
    public DescendantAttributeStep(XPathNodeTest test) => _test = test;

    /// <inheritdoc />
    public override void EvaluateFromContext(XPathEvaluationContext context, ref XdmItemList results)
    {
        ArgumentNullException.ThrowIfNull(context);

        var item = context.ContextItem ?? throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
        var bound = new Bound();
        Process(in item, ref bound, ref results);
    }

    /// <inheritdoc />
    public override void EvaluateOver(
        XPathEvaluationContext context,
        ReadOnlySpan<XdmItem> inputs,
        ref XdmItemList results
    )
    {
        var bound = new Bound();
        for (var i = 0; i < inputs.Length; i++)
        {
            Process(in inputs[i], ref bound, ref results);
        }
    }

    /// <inheritdoc />
    public override XPathOrderProperties Compose(XPathOrderProperties input) =>
        XPathOrderAnalysis.Step(XPathOrderAnalysis.Step(input, Axis.DescendantOrSelf), Axis.Attribute);

    private void Process(in XdmItem input, ref Bound bound, ref XdmItemList results)
    {
        if (!input.TryGetNode(out var node))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0020);
        }

        // The tests are bound to the name table of each tree once, not once per node.
        var tree = node.Tree;
        if (!ReferenceEquals(tree, bound.Tree))
        {
            bound.Tree = tree;
            bound.Elements = Elements.Bind(tree!.Names);
            bound.Attributes = _test.Bind(tree.Names);
        }

        if (TryProcessIndexed(node, in bound.Attributes, ref results))
        {
            return;
        }

        var walk = new XPathAxisIterator(node, Axis.DescendantOrSelf, bound.Elements);
        while (walk.MoveNext())
        {
            var attributes = new XPathAxisIterator(walk.Current, Axis.Attribute, bound.Attributes);
            while (attributes.MoveNext())
            {
                results.Add(XdmItem.FromNode(attributes.Current));
            }
        }
    }

    // The attributes of a large subtree of a document tree with one local name come from the attribute index of the tree: the same rows in
    // the same order, since the attributes of the elements of a subtree are exactly the attribute rows inside it.
    private static bool TryProcessIndexed(in XPathNode node, in XPathNodeMatcher attributes, ref XdmItemList results)
    {
        if (
            node.Node.Tree is not XmlDocumentTree tree
            || !attributes.TryGetLocal(attributes: true, out var local)
            || tree.GetKind(node.Node.Index) is not (XmlNodeKind.Document or XmlNodeKind.Element)
        )
        {
            return false;
        }

        var row = node.Node.Index;
        var end = tree.GetEnd(row);
        if (end - row < XPathAxisIterator.MinimumIndexedSubtree || tree.GetAttributeIndex() is not { } index)
        {
            return false;
        }

        index.Find(local, row, end, out var rows, out var first, out var last);
        for (var slot = first; slot < last; slot++)
        {
            if (attributes.Matches(tree, rows[slot]))
            {
                results.Add(XdmItem.FromNode(new XPathNode(new XmlNode(tree, rows[slot]))));
            }
        }

        return true;
    }

    // The tree the tests are bound for; the context nodes of a step are mostly of one tree.
    private struct Bound
    {
        public XmlTree? Tree;
        public XPathNodeMatcher Elements;
        public XPathNodeMatcher Attributes;
    }
}
