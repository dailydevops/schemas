namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>The immutable XSD validator.</summary>
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
        throw new NotSupportedException("The XSD validator is not available yet.");
}
