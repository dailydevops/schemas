namespace NetEvolve.Schemas.Tests.Unit.Inputs;

using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;

/// <summary>A format core that records nothing and allocates nothing, for allocation assertions.</summary>
public sealed class SilentValidator : SchemaValidatorBase
{
    public SilentValidator()
        : base(ValidationLimits.Default, DenyAllResourceResolver.Instance) { }

    protected override ValidationResult ValidateCore(in DocumentSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValidationResult.Valid;
    }
}
