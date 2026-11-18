namespace NetEvolve.Schemas.XPath.Casting.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.QNames;

/// <summary>The namespace context without any binding and without a default namespace: what a cast sees when no static context is at hand.</summary>
/// <remarks>Stateless, so the one instance is shared.</remarks>
internal sealed class NoNamespaceContext : INamespaceContext
{
    /// <summary>Gets the instance.</summary>
    public static NoNamespaceContext Instance { get; } = new();

    private NoNamespaceContext() { }

    /// <inheritdoc />
    public bool TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName)
    {
        namespaceName = null;
        return false;
    }
}
