namespace NetEvolve.Schemas.XPath.Functions.Documents;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:id($arg as xs:string*[, $node as node()]) as element()*</c> (Functions and Operators 1.0, 15.5.2): the elements, of the tree of
/// the node, that have an attribute of type ID whose value is one of the whitespace separated tokens of the strings of the argument,
/// in document order and without duplicates. The values are compared as written, with case. Of several elements with the same ID
/// the first in document order is returned. Without a second argument the context item is the node.
/// </summary>
/// <remarks>
/// Errors: XPDY0002 when there is no context item and XPTY0004 when it is no node. Limitations: see <see cref="IdIndex"/>; no element is an
/// ID without the attribute declarations of a document type declaration, and FODC0001 is never raised because every tree is rooted in a
/// document node.
/// </remarks>
internal sealed class IdFunction : IdentifierFunction
{
    /// <summary>Initializes a new instance of the <see cref="IdFunction"/> class.</summary>
    public IdFunction()
        : base("id") { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The call has no second argument and the context item is absent (XPDY0002) or not a node (XPTY0004).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var index = IndexOf(ref invocation);
        var found = NewList(invocation.Context);
        try
        {
            var values = invocation.Arguments[0];
            for (var i = 0; i < values.Count; i++)
            {
                _ = values[i].TryGetString(out var text);
                var position = 0;
                while (IdIndex.TryReadToken(text!, ref position, out var token))
                {
                    if (index.TryGetElement(token, out var element))
                    {
                        found.Add(XdmItem.FromNode(new XPathNode(element)));
                    }
                }
            }

            invocation.Result = Ordered(ref found);
        }
        finally
        {
            found.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }
}
