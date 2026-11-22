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

/// <summary>The outcome of a tree validation: the issues and the annotated tree (XSD-79, #361; design #146 DP-18).</summary>
/// <remarks>
/// <para>
/// The annotated tree is a copy of the validated document or subtree, built while the pump feeds the validator; the input tree is not modified
/// and has no annotations. A subtree result is a document whose only child is the element. Positions of elements are those of the input.
/// </para>
/// <para>
/// Limitations: defaulted attributes and defaulted elements are not nodes of the copy (design #146 DP-19, accepted in #305); the model holds no
/// document type declaration and no unparsed entities, so a case that depends on them can differ from the streaming path (#731); an XPath
/// expression reads the typed values of the annotated tree only through the annotations, because the evaluator takes no annotation source yet
/// (#791); an issue of an attribute maps to the element that owns it. The type of an annotation is the declared or <c>xsi:type</c> type, and
/// anonymous types have no name (#792).
/// </para>
/// </remarks>
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
    /// <returns><see langword="true"/> when the validator annotated the node; <see langword="false"/> for a node that is not an element or attribute of <see cref="Document"/>, and for a node the validator did not assess.</returns>
    public bool TryGetAnnotation(XmlNode node, out XsdAnnotation annotation) =>
        _source.TryGetAnnotation(node, out annotation);

    /// <summary>Gets the node of an issue of <see cref="Result"/>.</summary>
    /// <param name="issue">The issue.</param>
    /// <param name="node">The element that was open when the issue was reported, or the document node for an issue that a listener reports at the end of the run, such as an IDREF without an ID, although its location points at an element; an issue of an attribute belongs to its element.</param>
    /// <returns><see langword="true"/> when the issue belongs to <see cref="Result"/>.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="issue"/> is <see langword="null"/>.</exception>
    public bool TryGetIssueNode(ValidationIssue issue, out XmlNode node)
    {
        Argument.ThrowIfNull(issue);

        var issues = Result.Issues;
        for (var i = 0; i < issues.Count && i < _issues.Count; i++)
        {
            if (!ReferenceEquals(issues[i], issue))
            {
                continue;
            }

            var ordinal = _issues[i];
            if (ordinal == 0)
            {
                node = Document.Document;
                return true;
            }

            return _source.TryGetElement(ordinal, out node);
        }

        node = default;
        return false;
    }

    /// <summary>Atomizes a sequence of nodes of <see cref="Document"/> the way <c>fn:data</c> does.</summary>
    /// <param name="sequence">The sequence, typically the result of an XPath expression over <see cref="Document"/>.</param>
    /// <returns>The atomic sequence with the typed values of the annotated nodes.</returns>
    internal XdmSequence Data(in XdmSequence sequence) => _source.Data(sequence);
}
