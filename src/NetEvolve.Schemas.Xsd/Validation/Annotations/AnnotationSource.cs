namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>Reads the annotations of one run tree-based: it fills the annotation slot of the XPath node adapter.</summary>
internal sealed class AnnotationSource : IXdmTypeAnnotationSource
{
    /// <summary>Initializes a new instance of the <see cref="AnnotationSource"/> class.</summary>
    /// <param name="table">The table that the run filled.</param>
    /// <param name="tree">The tree of the same document.</param>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    internal AnnotationSource(AnnotationTable table, XmlTree tree, ISimpleValueAssessor assessor)
    {
        Argument.ThrowIfNull(table);
        Argument.ThrowIfNull(tree);
        Argument.ThrowIfNull(assessor);
    }

    /// <summary>Gets the annotation of a node.</summary>
    /// <param name="node">The element or attribute.</param>
    /// <param name="annotation">The annotation.</param>
    /// <returns><see langword="true"/> when the validator annotated the node.</returns>
    public bool TryGetAnnotation(XmlNode node, out XsdAnnotation annotation) =>
        throw new NotSupportedException("Not implemented yet.");

    /// <inheritdoc />
    public XdmTypeId GetTypeAnnotation(in XPathNode node) => throw new NotSupportedException("Not implemented yet.");

    /// <inheritdoc />
    public bool TryGetTypedValue(in XPathNode node, out XdmItem typedValue) =>
        throw new NotSupportedException("Not implemented yet.");

    /// <summary>Atomizes a sequence the way <c>fn:data</c> does, with the typed values of the annotated nodes.</summary>
    /// <param name="sequence">The sequence.</param>
    /// <returns>The atomic sequence.</returns>
    public XdmSequence Data(in XdmSequence sequence) => throw new NotSupportedException("Not implemented yet.");
}
