namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:string</c>: the string value of a node, the canonical form of an atomic value, the zero-length string for the empty
/// sequence, and the string of the context item without an argument.
/// </summary>
internal sealed class StringFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="StringFunction"/> class.</summary>
    public StringFunction()
        : base("string", 0, 1, [new XPathParameterType(XPathItemType.Item, Occurrence.ZeroOrOne)]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var text = string.Empty;
        if (invocation.Arguments.Length == 0)
        {
            text = Of(invocation.Context.ContextItem ?? throw XPathErrors.Exception(XPathErrorCodes.XPDY0002));
        }
        else if (!invocation.Arguments[0].IsEmpty)
        {
            text = Of(invocation.Arguments[0][0]);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromString(text));
        return XPathEvaluationStatus.Ok;
    }

    private static string Of(in XdmItem item) => item.TryGetNode(out var node) ? node.StringValue() : OfAtomic(in item);

    /// <summary>Gets the string value of an atomic item through the casting table.</summary>
    /// <param name="item">The item, not a node.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="XPathException">The item has no string form in the casting table (XPTY0004 or the error of the cast).</exception>
    internal static string OfAtomic(in XdmItem item)
    {
        var status = CastingCatalog.Default.Table.TryCast(in item, XdmTypeId.XsString, out var result);
        if (status != CastStatus.Ok)
        {
            throw XPathErrors.Exception(status.ToErrorCode());
        }

        _ = result.TryGetString(out var text);
        return text!;
    }
}
