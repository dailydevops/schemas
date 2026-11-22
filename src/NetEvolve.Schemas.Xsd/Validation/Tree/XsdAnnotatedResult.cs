#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the annotated result and its annotations (design #146 DP-1, DP-18)

namespace NetEvolve.Schemas.Xsd;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Tree;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>The outcome of a tree validation: the issues and the annotated tree (design #146 DP-18).</summary>
public sealed class XsdAnnotatedResult
{
    private readonly AnnotationSource _source;
    private readonly TreeIssueLog _issues;

    internal XsdAnnotatedResult(
        ValidationResult result,
        XmlDocumentTree document,
        AnnotationSource source,
        TreeIssueLog issues
    )
    {
        Argument.ThrowIfNull(result);
        Argument.ThrowIfNull(document);
        Argument.ThrowIfNull(source);
        Argument.ThrowIfNull(issues);

        Result = result;
        Document = document;
        _source = source;
        _issues = issues;
    }

    /// <summary>Gets the issues.</summary>
    public ValidationResult Result { get; }

    /// <summary>Gets the annotated tree: a copy of the validated document or subtree, whose nodes the annotations belong to.</summary>
    public XmlDocumentTree Document { get; }

    /// <summary>Gets the annotation of a node of <see cref="Document"/>.</summary>
    /// <param name="node">The element or attribute.</param>
    /// <param name="annotation">The annotation.</param>
    /// <returns><see langword="true"/> when the validator annotated the node.</returns>
    public bool TryGetAnnotation(XmlNode node, out XsdAnnotation annotation) =>
        throw new NotSupportedException("XSD-79 (#361)");

    /// <summary>Gets the node of an issue of <see cref="Result"/>.</summary>
    /// <param name="issue">The issue.</param>
    /// <param name="node">The element that was open when the issue was reported, or the document node.</param>
    /// <returns><see langword="true"/> when the issue belongs to <see cref="Result"/>.</returns>
    public bool TryGetIssueNode(ValidationIssue issue, out XmlNode node) =>
        throw new NotSupportedException("XSD-79 (#361)");

    /// <summary>Atomizes a sequence of nodes of <see cref="Document"/> the way <c>fn:data</c> does.</summary>
    /// <param name="sequence">The sequence, typically the result of an XPath expression over <see cref="Document"/>.</param>
    /// <returns>The atomic sequence with the typed values of the annotated nodes.</returns>
    internal XdmSequence Data(in XdmSequence sequence) => throw new NotSupportedException("XSD-79 (#361)");
}
