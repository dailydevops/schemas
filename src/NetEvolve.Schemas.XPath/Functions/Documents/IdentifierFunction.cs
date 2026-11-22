namespace NetEvolve.Schemas.XPath.Functions.Documents;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Nodes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of <c>fn:id</c> and <c>fn:idref</c>: the arguments <c>$arg as xs:string*</c> and <c>$node as node()</c>, the node of a call
/// (the second argument, else the context item), the index of its tree, and the result in document order without duplicates.
/// </summary>
/// <remarks>Limitation: see <see cref="IdIndex"/> for the ID typing that the functions read.</remarks>
internal abstract class IdentifierFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="IdentifierFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected IdentifierFunction(string localName)
        : base(
            localName,
            1,
            2,
            [
                new XPathParameterType(XPathItemType.String, Occurrence.ZeroOrMore),
                new XPathParameterType(XPathItemType.Node, Occurrence.ExactlyOne),
            ]
        ) { }

    /// <summary>Gets the index of the tree of the node of a call.</summary>
    /// <param name="invocation">The call.</param>
    /// <returns>The index.</returns>
    /// <exception cref="XPathException">The call has no second argument and the context item is absent (XPDY0002) or not a node (XPTY0004).</exception>
    protected static IdIndex IndexOf(ref XPathFunctionCall invocation)
    {
        _ = TryGetNode(ref invocation, 1, out var node);
        return IdIndex.For(node.Tree!);
    }

    /// <summary>Creates the list that collects the nodes of a result, bounded by the sequence limit of the context.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The list; the caller disposes it.</returns>
    protected static XdmItemList NewList(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new(context.Limits.MaxSequenceSize, new SourceSpan(-1, 0));
    }

    /// <summary>Brings the collected nodes into document order without duplicates and copies them into a sequence.</summary>
    /// <param name="found">The collected nodes; every item is a node. The list is reused.</param>
    /// <returns>The sequence.</returns>
    protected static XdmSequence Ordered(ref XdmItemList found)
    {
        if (found.Count < 2)
        {
            return found.ToSequence();
        }

        var source = found.GetNodeSource();
        var ordered = XPathDocumentOrder.SortDistinct(ref source, null);
        try
        {
            found.Clear();
            foreach (var node in ordered.Nodes)
            {
                found.Add(XdmItem.FromNode(node));
            }
        }
        finally
        {
            ordered.Dispose();
        }

        return found.ToSequence();
    }
}
