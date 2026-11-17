namespace NetEvolve.Schemas.XPath.Xdm.Nodes;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The slot that supplies type annotations and typed values for nodes; schema-aware processing fills it (XSD-16).</summary>
internal interface IXdmTypeAnnotationSource
{
    /// <summary>Gets the type annotation of a node.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The annotation.</returns>
    XdmTypeId GetTypeAnnotation(in XPathNode node);

    /// <summary>Gets the typed value of an annotated node.</summary>
    /// <param name="node">The node.</param>
    /// <param name="typedValue">The typed value, <see langword="default"/> when the node is not annotated.</param>
    /// <returns><see langword="true"/> when the slot is filled for the node; otherwise the untyped default applies.</returns>
    bool TryGetTypedValue(in XPathNode node, out XdmItem typedValue);
}
