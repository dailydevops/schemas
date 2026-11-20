namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// Reads the annotations of one run tree-based, by the nodes of a tree of the same document: it fills the annotation slot of the XPath
/// node adapter, so that atomization returns the typed value of an annotated node and <c>instance of</c> sees its type (XSD-16, #305;
/// design #146 DP-18). The validator streams the annotations into an <see cref="AnnotationTable"/> by ordinal; this class maps the nodes
/// of the tree to those ordinals with one preorder walk when it is created, so a lookup is constant time and the source is safe for
/// concurrent reads.
/// </summary>
/// <remarks>
/// <para>
/// The tree must be built from the same input as the run, so that the n-th element of the tree is the n-th element start event of the
/// validator; the attribute index counts the attributes of the element in source order without the attributes of the schema instance
/// namespace that control assessment, which the validator hands to no listener (XSD-55, #308). Defaulted attributes have records but no node.
/// </para>
/// <para>
/// Limitations: a node that the validator did not annotate (an unassessed element, a node of another tree) is not annotated and reads as
/// untyped. The slot of the node adapter holds one item per node, so it is filled for a typed value of exactly one item; a list type with
/// another number of items, a nilled element, and an element with element-only content leave it empty, and atomization then falls back to
/// <c>xs:untypedAtomic</c>. Reading those through <see cref="XsdAnnotation.CopyTypedValue"/> is complete. The XPath evaluator does not take an
/// annotation source yet, so <c>fn:data</c> inside an expression sees the untyped default; <see cref="Data"/> applies the atomization of
/// <c>fn:data</c> with this source. The evaluator wiring and the multi-item slot are #791.
/// </para>
/// </remarks>
internal sealed class AnnotationSource : IXdmTypeAnnotationSource
{
    private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    private readonly AnnotationTable _table;
    private readonly XmlTree _tree;
    private readonly Dictionary<int, int> _ordinals = [];

    /// <summary>Initializes a new instance of the <see cref="AnnotationSource"/> class.</summary>
    /// <param name="table">The table that the run filled.</param>
    /// <param name="tree">The tree of the same document.</param>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    internal AnnotationSource(AnnotationTable table, XmlTree tree, ISimpleValueAssessor assessor)
    {
        Argument.ThrowIfNull(table);
        Argument.ThrowIfNull(tree);
        Argument.ThrowIfNull(assessor);

        _table = table;
        _tree = tree;
        Assessor = assessor;
        NumberElements();
    }

    /// <summary>Gets the datatype contract.</summary>
    internal ISimpleValueAssessor Assessor { get; }

    /// <summary>Gets the annotation of a node.</summary>
    /// <param name="node">The element or attribute.</param>
    /// <param name="annotation">The annotation.</param>
    /// <returns><see langword="true"/> when the validator annotated the node.</returns>
    public bool TryGetAnnotation(XmlNode node, out XsdAnnotation annotation)
    {
        annotation = default;
        if (!ReferenceEquals(node.Tree, _tree))
        {
            return false;
        }

        AnnotationRecord record;
        switch (node.Kind)
        {
            case XmlNodeKind.Element:
                if (!_table.TryGetElement(_ordinals[node.Index], out record))
                {
                    return false;
                }

                break;
            case XmlNodeKind.Attribute:
                if (!TryGetAttributeRecord(node, out record))
                {
                    return false;
                }

                break;
            default:
                return false;
        }

        annotation = new XsdAnnotation(this, node, record);
        return true;
    }

    /// <inheritdoc />
    public XdmTypeId GetTypeAnnotation(in XPathNode node) =>
        TryGetAnnotation(node.Node, out var annotation) ? annotation.DataModelType : node.TypeAnnotation;

    /// <inheritdoc />
    public bool TryGetTypedValue(in XPathNode node, out XdmItem typedValue)
    {
        if (TryGetAnnotation(node.Node, out var annotation))
        {
            return annotation.TryGetSingleItem(out typedValue);
        }

        typedValue = default;
        return false;
    }

    /// <summary>Atomizes a sequence the way <c>fn:data</c> does, with the typed values of the annotated nodes.</summary>
    /// <param name="sequence">The sequence.</param>
    /// <returns>The atomic sequence: atomic items pass through, and a node yields its typed value or, when it has none here, <c>xs:untypedAtomic</c>.</returns>
    public XdmSequence Data(in XdmSequence sequence) => XPathAtomization.Atomize(sequence, this);

    // The ordinal counts the element start events in document order, starting at 1, as the validator counts them.
    private void NumberElements()
    {
        var node = _tree.Document;
        var ordinal = 0;
        while (true)
        {
            if (node.Kind == XmlNodeKind.Element)
            {
                _ordinals.Add(node.Index, ++ordinal);
            }

            var child = node.FirstChild;
            if (!child.IsEmpty)
            {
                node = child;
                continue;
            }

            while (node.NextSibling.IsEmpty)
            {
                node = node.Parent;
                if (node.IsEmpty || node.Index == 0)
                {
                    return;
                }
            }

            node = node.NextSibling;
        }
    }

    private bool TryGetAttributeRecord(XmlNode attribute, out AnnotationRecord record)
    {
        record = default;
        var owner = attribute.Parent;
        var index = 0;
        for (var current = owner.FirstAttribute; current != attribute; current = current.NextAttribute)
        {
            if (!IsAssessmentControl(current))
            {
                index++;
            }
        }

        return !IsAssessmentControl(attribute) && _table.TryGetAttribute(_ordinals[owner.Index], index, out record);
    }

    // The attributes of the schema instance namespace that control assessment itself reach no listener (XSD-55, #308).
    private bool IsAssessmentControl(XmlNode attribute)
    {
        var name = attribute.Name;
        return name.NamespaceUri.Value != 0
            && string.Equals(_tree.Names.GetString(name.NamespaceUri), XsiNamespace, StringComparison.Ordinal)
            && _tree.Names.GetString(name.Local) is "type" or "nil" or "schemaLocation" or "noNamespaceSchemaLocation";
    }
}
