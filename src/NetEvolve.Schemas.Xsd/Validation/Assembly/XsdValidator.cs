namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>The immutable, thread-safe XSD validator: every input kind of the base class ends in the one core.</summary>
/// <remarks>
/// Limitation: tree validation and annotated results are added by later work packages (XSD-79). A <see cref="System.IO.Stream"/> is
/// buffered completely before the core runs (see <see cref="SchemaValidatorBase"/>).
/// </remarks>
internal sealed class XsdValidator : SchemaValidatorBase, IXsdValidator
{
    private readonly IValidationCore _core;

    internal XsdValidator(IValidationCore core, XsdVersion version, ValidationLimits limits, IResourceResolver resolver)
        : base(limits, resolver)
    {
        Argument.ThrowIfNull(core);

        _core = core;
        Version = version;
    }

    /// <inheritdoc />
    public XsdVersion Version { get; }

    /// <inheritdoc />
    protected override ValidationResult ValidateCore(in DocumentSource source, CancellationToken cancellationToken) =>
        _core.Validate(in source, cancellationToken);
}
