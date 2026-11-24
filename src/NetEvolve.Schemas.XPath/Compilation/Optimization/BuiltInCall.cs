namespace NetEvolve.Schemas.XPath.Compilation.Optimization;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// Recognizes a call of a function of the built-in library by its expanded name. The compilation reaches the library through the registry
/// only, so it knows no function type; a host function that shadows a built-in name is wrapped by <see cref="HostFunctionAdapter"/> and does not match.
/// </summary>
internal static class BuiltInCall
{
    private static readonly string FunctionNamespace = XPathStaticContext.Default.DefaultFunctionNamespace;

    /// <summary>Determines whether a node is a call of a built-in function.</summary>
    /// <param name="node">The node.</param>
    /// <param name="localName">The local name in the function namespace.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <returns><see langword="true"/> when the node is a call of the built-in function with that many arguments.</returns>
    public static bool Is(ExecutableNode node, string localName, int arity) =>
        node is CallNode { Function: not HostFunctionAdapter } call
        && call.Arguments.Count == arity
        && call.Function.Name.LocalName == localName
        && call.Function.Name.NamespaceName == FunctionNamespace;
}
