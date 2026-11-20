namespace NetEvolve.Schemas.Xsd.Validation.Xsi;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.QNames;

/// <summary>
/// The namespace declarations in scope while the validator walks an instance, kept as one flat stack of prefix and namespace
/// symbols: an element marks the depth of the stack when it opens and cuts back to it when it closes.
/// </summary>
/// <remarks>Limitation: the implicit <c>xml</c> prefix is not bound, because no schema type lives in that namespace.</remarks>
internal sealed class XsiNamespaceScope : INamespaceContext
{
    internal XsiNamespaceScope(XmlNameTable names) => throw new NotSupportedException();

    /// <summary>Gets the number of declarations in scope.</summary>
    internal int Count => throw new NotSupportedException();

    /// <summary>Adds a declaration; it shadows an earlier declaration of the same prefix.</summary>
    /// <param name="prefix">The prefix, empty for the default namespace.</param>
    /// <param name="uri">The namespace name, empty when the default namespace is undeclared.</param>
    internal void Declare(XmlSymbol prefix, XmlSymbol uri) => throw new NotSupportedException();

    /// <summary>Removes the declarations that were added after the stack had <paramref name="count"/> entries.</summary>
    /// <param name="count">The size to cut back to.</param>
    internal void Truncate(int count) => throw new NotSupportedException();

    /// <inheritdoc />
    public bool TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName) =>
        throw new NotSupportedException();
}
