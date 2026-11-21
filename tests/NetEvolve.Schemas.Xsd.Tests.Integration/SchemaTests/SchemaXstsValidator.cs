namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The validator of the XSTS runner for the schema tests of XSD 1.0.</summary>
internal sealed class SchemaXstsValidator : IXstsValidator
{
    public SchemaXstsValidator(string suiteRoot) => ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);

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
