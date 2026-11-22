namespace NetEvolve.Schemas.Xsd.Tests.Integration.InstanceTests;

using System.Collections.Concurrent;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>
/// The validator of the XSTS runner for the schema tests and the instance tests of XSD 1.0. An instance test builds the schema of its
/// group once through the public entry point, strict 1.0 with DTD processing for the entities of the instances, and validates the
/// instance with it; the schema documents may include and import each other inside the suite.
/// </summary>
/// <remarks>
/// A case that cannot be observed is not observed, so a defect never counts as a verdict: a case of XSD 1.1, a group without a schema
/// document (its instance names the schema with <c>xsi:schemaLocation</c>, which is XSD-77), a schema that does not build, a missing
/// document, or a pipeline that throws.
/// </remarks>
internal sealed class InstanceXstsValidator : IXstsValidator
{
    private readonly string _suiteRoot;
    private readonly SchemaXstsValidator _schemas;
    private readonly ConcurrentDictionary<string, Task<IXsdValidator?>> _validators = new(StringComparer.Ordinal);

    public InstanceXstsValidator(string suiteRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);

        _suiteRoot = Path.GetFullPath(suiteRoot);
        _schemas = new SchemaXstsValidator(_suiteRoot);
    }

    public Task<XstsOutcome> CompileSchemaAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken) =>
        _schemas.CompileSchemaAsync(xstsCase, cancellationToken);

    public async Task<XstsOutcome> ValidateInstanceAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(xstsCase);

        var instance = xstsCase.Case.InstanceDocument;
        if (
            !string.Equals(xstsCase.Version, XstsVersions.V10, StringComparison.Ordinal)
            || xstsCase.Case.SchemaDocuments.Count == 0
            || instance is null
        )
        {
            return XstsOutcome.NotImplemented;
        }

        var path = Path.GetFullPath(Path.Combine(_suiteRoot, instance));
        var validator = await ValidatorOf(xstsCase);
        if (validator is null || !File.Exists(path))
        {
            return XstsOutcome.NotImplemented;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            var result = await validator.ValidateAsync(bytes, new Uri(path).AbsoluteUri, cancellationToken);
            return result.IsValid ? XstsOutcome.Valid : XstsOutcome.Invalid;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return XstsOutcome.NotImplemented;
        }
    }

    // The build is shared by every case of the group; two cases that race for it build twice at worst and keep one result.
    private Task<IXsdValidator?> ValidatorOf(XstsSelectedCase xstsCase) =>
        _validators.GetOrAdd(
            xstsCase.TestSet + "|" + xstsCase.Version + "|" + xstsCase.Case.Group,
            static (_, state) => state.Validator.BuildAsync(state.Documents, CancellationToken.None),
            (Validator: this, Documents: xstsCase.Case.SchemaDocuments)
        );

    private async Task<IXsdValidator?> BuildAsync(IReadOnlyList<string> documents, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var builder = SchemaValidator
                .ForXsd()
                .WithVersion(XsdVersion.V10)
                .WithDtdProcessing(true)
                .WithSchemaLocationHints(true)
                .WithResolver(new SandboxedFileSystemResourceResolver(_suiteRoot));
            foreach (var relative in documents)
            {
                var path = Path.GetFullPath(Path.Combine(_suiteRoot, relative));
                _ = builder.AddSchema(await File.ReadAllBytesAsync(path, cancellationToken), new Uri(path).AbsoluteUri);
            }

            return await builder.BuildAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return null;
        }
    }
}
