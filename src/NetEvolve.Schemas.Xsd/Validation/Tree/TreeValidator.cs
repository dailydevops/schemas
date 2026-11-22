namespace NetEvolve.Schemas.Xsd.Validation.Tree;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Assembly;

/// <summary>Validates a document or an element subtree of the document model and returns the annotated result (XSD-79, #361).</summary>
internal sealed class TreeValidator
{
    private readonly IValidationCore _core;

    internal TreeValidator(IValidationCore core)
    {
        Argument.ThrowIfNull(core);

        _core = core;
    }

    /// <summary>Validates a node and its descendants.</summary>
    /// <param name="node">The document node, or the element that roots the subtree.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The annotated result.</returns>
    internal XsdAnnotatedResult Validate(XmlNode node, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(node.Tree, nameof(node));
        Argument.ThrowIfGreaterThan((int)node.Kind, (int)XmlNodeKind.Element, nameof(node));

        var table = new AnnotationTable();
        var issues = new TreeIssueLog();
        var result = _core.ValidateTree(node, table, issues, out var copy, cancellationToken);

        return new XsdAnnotatedResult(result, copy, new AnnotationSource(table, copy, _core.Assessor), issues);
    }
}
