namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Tree;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>The immutable, thread-safe XSD validator: every input kind of the base class ends in the one core.</summary>
/// <remarks>
/// A <see cref="System.IO.Stream"/> is buffered completely before the core runs (see <see cref="SchemaValidatorBase"/>). The limitations of
/// tree validation are those of <see cref="IXsdValidator.ValidateTree(XmlTree, CancellationToken)"/>.
/// </remarks>
internal sealed class XsdValidator : SchemaValidatorBase, IXsdValidator
{
    private readonly IValidationCore _core;
    private readonly TreeValidator _tree;

    internal XsdValidator(IValidationCore core, XsdVersion version, ValidationLimits limits, IResourceResolver resolver)
        : base(limits, resolver)
    {
        ArgumentNullException.ThrowIfNull(core);

        _core = core;
        _tree = new TreeValidator(core);
        Version = version;
    }

    /// <inheritdoc />
    public XsdVersion Version { get; }

    /// <inheritdoc />
    public XsdAnnotatedResult ValidateTree(XmlTree document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(document);

        return _tree.Validate(document.Document, cancellationToken);
    }

    /// <inheritdoc />
    public XsdAnnotatedResult ValidateTree(
        XmlTree document,
        XmlNode element,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(document);
        Argument.ThrowIfNull(element.Tree, nameof(element));
        Argument.ThrowIfNotEqual(element.Tree.TreeId, document.TreeId, nameof(element));
        Argument.ThrowIfNotEqual((int)element.Kind, (int)XmlNodeKind.Element, nameof(element));

        return _tree.Validate(element, cancellationToken);
    }

    /// <inheritdoc />
    protected override ValidationResult ValidateCore(in DocumentSource source, CancellationToken cancellationToken) =>
        _core.Validate(in source, cancellationToken);
}
