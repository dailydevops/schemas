namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

// Internal parser intermediate: an element outside the Schematron namespace that is a direct child of schema or library,
// kept for the XSLT registry (design #137 AP-3 as narrowed by #150 EP-11). Never part of the public model.
internal sealed class SchematronForeignElement : SchematronNode
{
    internal SchematronForeignElement(
        DocumentLocation location,
        string namespaceUri,
        string localName,
        IReadOnlyList<KeyValuePair<string, string>> attributes,
        IReadOnlyList<SchematronNode> children,
        IReadOnlyList<KeyValuePair<string, string>>? namespaceDeclarations = null
    )
        : base(location)
    {
        NamespaceUri = namespaceUri;
        LocalName = localName;
        Attributes = attributes;
        Children = children;
        NamespaceDeclarations = namespaceDeclarations ?? [];
    }

    internal string NamespaceUri { get; }

    internal string LocalName { get; }

    // Attributes in document order; the key is the expanded name in Clark form ("{uri}local", or "local" without namespace).
    internal IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }

    // Child elements (SchematronForeignElement) and character data (SchematronText) in document order.
    internal IReadOnlyList<SchematronNode> Children { get; }

    // The prefixed namespace bindings in scope at the element that its parent does not already have, as (prefix, uri); only the parser
    // of the children of schema and library fills it, because the XSLT modules need the in-scope bindings for the prefixes in values.
    internal IReadOnlyList<KeyValuePair<string, string>> NamespaceDeclarations { get; }
}
