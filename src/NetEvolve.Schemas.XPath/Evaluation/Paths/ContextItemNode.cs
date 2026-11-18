namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of the context item expression <c>.</c> (XPath 2.0, 3.1.4).</summary>
internal sealed class ContextItemNode : ExecutableNode, IOrderedNodeSequence
{
    /// <inheritdoc />
    /// <remarks>One item is in order, and it has no duplicate.</remarks>
    public bool IsDocumentOrdered => true;

    /// <inheritdoc />
    /// <exception cref="XPathException">The context item is absent (XPDY0002).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        Argument.ThrowIfNull(context);

        return context.ContextItem is { } item
            ? XdmSequence.Create(item)
            : throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
    }
}
