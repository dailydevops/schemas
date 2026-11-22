namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Tree;

/// <summary>The one synchronous core behind every input kind of the XSD validator (design #146 DP-11).</summary>
internal interface IValidationCore
{
    /// <summary>Gets the datatype contract of the core, which tells the accepting member of a union when annotations are read.</summary>
    ISimpleValueAssessor Assessor { get; }

    /// <summary>Validates one instance document.</summary>
    /// <param name="source">The document, as the base class of the validator hands it over.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The result.</returns>
    ValidationResult Validate(in DocumentSource source, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a document or an element subtree of the document model: the model-to-sink pump drives the same run as a reader does
    /// (XSD-79, #361), so there is no second validator and no tree walker of its own.
    /// </summary>
    /// <param name="node">The document node, or the element that roots the subtree.</param>
    /// <param name="annotations">The sink that receives the annotations of the run.</param>
    /// <param name="issues">The log that receives the element ordinal of every issue.</param>
    /// <param name="copy">The copy of the pumped document or subtree that the annotations belong to.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The issues of the run.</returns>
    ValidationResult ValidateTree(
        XmlNode node,
        IAnnotationSink annotations,
        TreeIssueLog issues,
        out XmlDocumentTree copy,
        CancellationToken cancellationToken
    );
}
