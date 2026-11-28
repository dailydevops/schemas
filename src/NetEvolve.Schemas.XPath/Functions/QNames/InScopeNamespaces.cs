namespace NetEvolve.Schemas.XPath.Functions.QNames;

using System;
using System.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The namespaces in scope on an element, read through its namespace nodes, with the <c>xml</c> binding that every element has.</summary>
/// <remarks>
/// A namespace node of the tree carries the prefix as its local name and the namespace name as its string value; the default namespace
/// has an empty prefix, and a declaration that removes it (<c>xmlns=""</c>) has no node. The binding of the <c>xml</c> prefix is implicit
/// (Namespaces in XML 1.0, 3), so it is supplied here when the element does not declare it, and listed once.
/// </remarks>
internal static class InScopeNamespaces
{
    /// <summary>The namespace name that the <c>xml</c> prefix is bound to.</summary>
    internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private const string XmlPrefix = "xml";

    /// <summary>Looks a prefix up in the in-scope namespaces of an element.</summary>
    /// <param name="element">The element.</param>
    /// <param name="prefix">The prefix; empty for the default namespace.</param>
    /// <param name="namespaceName">The namespace name when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the prefix is bound; an unbound prefix and an undeclared default namespace give <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="default"/>.</exception>
    internal static bool TryResolve(in XPathNode element, ReadOnlySpan<char> prefix, out string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(element.Tree);

        var names = element.Tree.Names;
        for (var node = element.Node.FirstNamespace; !node.IsEmpty; node = node.NextNamespace)
        {
            var local = node.Name.Local;
            if ((local.IsEmpty ? ReadOnlySpan<char>.Empty : names.GetString(local).AsSpan()).SequenceEqual(prefix))
            {
                namespaceName = node.StringValue;
                return true;
            }
        }

        var implicitXml = prefix.SequenceEqual(XmlPrefix.AsSpan());
        namespaceName = implicitXml ? XmlNamespace : string.Empty;
        return implicitXml;
    }

    /// <summary>Lists the prefixes in scope on an element, each once, the default namespace as the zero-length string.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The prefixes as <c>xs:string</c> items, in no particular order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="default"/>.</exception>
    internal static XdmSequence Prefixes(in XPathNode element)
    {
        ArgumentNullException.ThrowIfNull(element.Tree);

        var names = element.Tree.Names;
        var count = 1;
        for (var node = element.Node.FirstNamespace; !node.IsEmpty; node = node.NextNamespace)
        {
            count++;
        }

        var pool = ArrayPool<XdmItem>.Shared;
        var items = pool.Rent(count);
        try
        {
            var length = 0;
            var hasXml = false;
            for (var node = element.Node.FirstNamespace; !node.IsEmpty; node = node.NextNamespace)
            {
                var local = node.Name.Local;
                var prefix = local.IsEmpty ? string.Empty : names.GetString(local);
                hasXml |= string.Equals(prefix, XmlPrefix, StringComparison.Ordinal);
                items[length++] = XdmItem.FromString(prefix);
            }

            if (!hasXml)
            {
                items[length++] = XdmItem.FromString(XmlPrefix);
            }

            return XdmSequence.Create(new ReadOnlySpan<XdmItem>(items, 0, length));
        }
        finally
        {
            pool.Return(items, clearArray: true);
        }
    }
}
