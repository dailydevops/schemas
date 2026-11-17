namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The validator of this package: none is bound, so every case is a known failure.</summary>
internal sealed class UnboundXstsValidator : IXstsValidator
{
    public static UnboundXstsValidator Instance { get; } = new();

    public Task<XstsOutcome> CompileSchemaAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(XstsOutcome.NotImplemented);
    }

    public Task<XstsOutcome> ValidateInstanceAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(XstsOutcome.NotImplemented);
    }
}
