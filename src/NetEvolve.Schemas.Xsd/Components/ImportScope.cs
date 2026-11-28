namespace NetEvolve.Schemas.Xsd.Components;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The namespaces each schema document may refer to: its own, the ones it imports directly, the schema namespace, and the XML
/// namespace (<c>src-resolve</c>, Structures 1.0, 4.2.3).
/// </summary>
/// <remarks>The root documents of one schema set see each other. A document the scope knows nothing about, and a reference without a document, are not restricted.</remarks>
internal sealed class ImportScope
{
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private readonly Dictionary<string, HashSet<string>> _visible = new(StringComparer.Ordinal);

    private readonly HashSet<string> _everywhere = new(StringComparer.Ordinal);

    /// <summary>Allows every document to refer to a namespace: the namespace of a root document of a schema set that has several roots.</summary>
    /// <param name="namespaceName">The namespace, empty for none.</param>
    internal void AllowEverywhere(string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);

        _ = _everywhere.Add(namespaceName);
    }

    /// <summary>Allows a document to refer to a namespace.</summary>
    /// <param name="source">The system identifier of the document, <see langword="null"/> when it has none.</param>
    /// <param name="namespaceName">The namespace, empty for none.</param>
    internal void Allow(string? source, string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);

        if (source is null)
        {
            return;
        }

        if (!_visible.TryGetValue(source, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            _visible.Add(source, set);
        }

        _ = set.Add(namespaceName);
    }

    /// <summary>Determines whether the document of a reference may refer to the namespace of its name.</summary>
    /// <param name="location">The location of the reference.</param>
    /// <param name="name">The referenced expanded name.</param>
    /// <returns><see langword="true"/> when the reference may be resolved.</returns>
    internal bool IsVisible(DocumentLocation location, XPathQName name)
    {
        var source = location.Source;
        var space = name.NamespaceName;
        return source is null
            || !_visible.TryGetValue(source, out var set)
            || set.Contains(space)
            || _everywhere.Contains(space)
            || string.Equals(space, SchemaGrammar.Namespace, StringComparison.Ordinal)
            || string.Equals(space, XmlNamespace, StringComparison.Ordinal);
    }
}
