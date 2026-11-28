namespace NetEvolve.Schemas.Xsd.Validation.Xsi;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.QNames;

/// <summary>
/// The namespace declarations in scope while the validator walks an instance, kept as one flat stack of prefix and namespace
/// symbols: an element marks the depth of the stack when it opens and cuts back to it when it closes.
/// </summary>
/// <remarks>The <c>xml</c> prefix is always bound, as Namespaces in XML requires, although the reader does not report a declaration for it.</remarks>
internal sealed class XsiNamespaceScope : INamespaceContext
{
    private const string XmlPrefix = "xml";
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private readonly XmlNameTable _names;
    private XmlSymbol[] _prefixes = [];
    private XmlSymbol[] _uris = [];

    internal XsiNamespaceScope(XmlNameTable names)
    {
        ArgumentNullException.ThrowIfNull(names);

        _names = names;
    }

    /// <summary>Gets the number of declarations in scope.</summary>
    internal int Count { get; private set; }

    /// <summary>Adds a declaration; it shadows an earlier declaration of the same prefix.</summary>
    /// <param name="prefix">The prefix, empty for the default namespace.</param>
    /// <param name="uri">The namespace name, empty when the default namespace is undeclared.</param>
    internal void Declare(XmlSymbol prefix, XmlSymbol uri)
    {
        if (Count == _prefixes.Length)
        {
            var size = Math.Max(8, Count * 2);
            Array.Resize(ref _prefixes, size);
            Array.Resize(ref _uris, size);
        }

        _prefixes[Count] = prefix;
        _uris[Count] = uri;
        Count++;
    }

    /// <summary>Removes the declarations that were added after the stack had <paramref name="count"/> entries.</summary>
    /// <param name="count">The size to cut back to.</param>
    internal void Truncate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Count);

        Count = count;
    }

    /// <inheritdoc />
    public bool TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            if (prefix.Equals(_names.GetString(_prefixes[i]).AsSpan(), StringComparison.Ordinal))
            {
                namespaceName = _names.GetString(_uris[i]);
                return true;
            }
        }

        var isXml = prefix.Equals(XmlPrefix.AsSpan(), StringComparison.Ordinal);
        namespaceName = isXml ? XmlNamespace : null;
        return isXml;
    }
}
