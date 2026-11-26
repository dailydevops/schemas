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

    /// <summary>Gets the complete typed value of an annotated node: zero items for a nilled element or empty content, several for a list type.</summary>
    /// <param name="node">The node.</param>
    /// <param name="values">The typed value, <see cref="XdmSequence.Empty"/> when the slot is not filled for the node.</param>
    /// <returns><see langword="true"/> when the slot is filled for the node; otherwise the untyped default applies.</returns>
    bool TryGetTypedValues(in XPathNode node, out XdmSequence values);

    /// <summary>Gets a value indicating whether a node has a typed value; an element with element-only content has none, and atomizing it is an error (FOTY0012).</summary>
    /// <param name="node">The node.</param>
    /// <returns><see langword="false"/> when the node is an annotated element with element-only content; otherwise <see langword="true"/>.</returns>
    bool HasTypedValue(in XPathNode node);

    /// <summary>Gets the <c>nilled</c> property of a node.</summary>
    /// <param name="node">The node.</param>
    /// <returns><see langword="true"/> when the node is a nilled element.</returns>
    bool IsNilled(in XPathNode node);
}
