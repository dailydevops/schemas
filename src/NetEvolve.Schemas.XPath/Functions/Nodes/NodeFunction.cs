namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>The base of the node, context, and accessor functions: every one is a built-in function of the <c>fn</c> namespace, and the node forms share one way to read the node of a call.</summary>
/// <remarks>
/// Limitation: the group follows XPath 2.0 and its Functions and Operators 1.0. The design names the folders <c>Functions/Node</c>,
/// <c>Functions/Accessor</c>, and <c>Functions/Context</c>; the work package owns <c>Functions/Nodes</c> and puts the three in one registration file.
/// </remarks>
internal abstract class NodeFunction : XPathFunction
{
    private static readonly string FunctionNamespace = XPathStaticContext.Default.DefaultFunctionNamespace;

    /// <summary>Initializes a new instance of the <see cref="NodeFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> or <paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected NodeFunction(string localName, int minArity, int maxArity, IReadOnlyList<XPathParameterType> parameters)
        : base(new XPathQName(FunctionNamespace, localName), minArity, maxArity, parameters) { }

    /// <summary>Reads the node of a call: the argument at <paramref name="index"/> when the call has one, else the context item.</summary>
    /// <param name="invocation">The call; the function conversion has already checked that the argument is a node or empty.</param>
    /// <param name="index">The position of the argument.</param>
    /// <param name="node">The node; <see langword="default"/> for the empty sequence.</param>
    /// <returns><see langword="true"/> for a node, <see langword="false"/> for the empty sequence.</returns>
    /// <exception cref="XPathException">The call has no argument at <paramref name="index"/> and the context item is absent (XPDY0002) or not a node (XPTY0004).</exception>
    protected static bool TryGetNode(ref XPathFunctionCall invocation, int index, out XPathNode node)
    {
        if (invocation.Arguments.Length > index)
        {
            var argument = invocation.Arguments[index];
            if (argument.IsEmpty)
            {
                node = default;
                return false;
            }

            _ = argument[0].TryGetNode(out node);
            return true;
        }

        var item = invocation.Context.ContextItem ?? throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
        return item.TryGetNode(out node) ? true : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
