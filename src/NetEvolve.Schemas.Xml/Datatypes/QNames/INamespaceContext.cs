namespace NetEvolve.Schemas.Xml.Datatypes.QNames;

using System;

/// <summary>Resolves the prefixes of a QName or NOTATION lexical form against the namespace declarations in scope.</summary>
public interface INamespaceContext
{
    /// <summary>Resolves a prefix.</summary>
    /// <param name="prefix">The prefix; empty for the default namespace.</param>
    /// <param name="namespaceName">The namespace name the prefix is bound to.</param>
    /// <returns>
    /// <see langword="true"/> when the prefix is bound. For the empty prefix, <see langword="false"/> (or an empty
    /// <paramref name="namespaceName"/>) means that no default namespace is in scope, which is not an error.
    /// </returns>
    bool TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName);
}
