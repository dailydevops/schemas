namespace NetEvolve.Schemas.XPath.Xdm.Nodes;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The default annotation source: nothing is annotated, so every node keeps its untyped annotation.</summary>
internal sealed class UntypedAnnotationSource : IXdmTypeAnnotationSource
{
    private UntypedAnnotationSource() { }

    /// <summary>Gets the shared instance.</summary>
    public static UntypedAnnotationSource Instance { get; } = new();

    /// <inheritdoc />
    public XdmTypeId GetTypeAnnotation(in XPathNode node) => node.TypeAnnotation;

    /// <inheritdoc />
    public bool TryGetTypedValue(in XPathNode node, out XdmItem typedValue)
    {
        typedValue = default;
        return false;
    }
}
