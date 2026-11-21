namespace NetEvolve.Schemas.Xsd.Tests.Integration.InstanceTests;

using NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The validator of the XSTS runner for the schema tests and the instance tests of XSD 1.0.</summary>
internal sealed class InstanceXstsValidator : IXstsValidator
{
    private readonly SchemaXstsValidator _schemas;

    public InstanceXstsValidator(string suiteRoot) => _schemas = new SchemaXstsValidator(suiteRoot);

    public Task<XstsOutcome> CompileSchemaAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken) =>
        _schemas.CompileSchemaAsync(xstsCase, cancellationToken);

    public Task<XstsOutcome> ValidateInstanceAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken) =>
        Task.FromResult(XstsOutcome.NotImplemented);
}
