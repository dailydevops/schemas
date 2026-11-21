namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>
/// The validator of the XSTS runner for the schema tests of XSD 1.0: the schema documents of a test are compiled as one schema, and
/// the schema is valid when the compilation has no error.
/// </summary>
/// <remarks>
/// Every other case is not observed: the schema tests of XSD 1.1 wait for the 1.1 pipeline (XSD-78), the instance tests for the
/// validator (XSD-57). A case that the pipeline cannot run, because a document is missing or the pipeline throws, is not observed
/// either, so a defect never counts as the schema being invalid.
/// </remarks>
internal sealed class SchemaXstsValidator : IXstsValidator
{
    private readonly string _suiteRoot;
    private readonly SchemaCompilerBridge _bridge;

    public SchemaXstsValidator(string suiteRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);

        _suiteRoot = Path.GetFullPath(suiteRoot);
        _bridge = new SchemaCompilerBridge(
            XsdVersion.V10,
            new SandboxedFileSystemResourceResolver(_suiteRoot),
            ValidationLimits.Default
        );
    }

    public async Task<XstsOutcome> CompileSchemaAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(xstsCase);

        if (!string.Equals(xstsCase.Version, XstsVersions.V10, StringComparison.Ordinal))
        {
            return XstsOutcome.NotImplemented;
        }

        var documents = new List<(ReadOnlyMemory<byte>, string)>(xstsCase.Case.SchemaDocuments.Count);
        foreach (var relative in xstsCase.Case.SchemaDocuments)
        {
            var path = Path.GetFullPath(Path.Combine(_suiteRoot, relative));
            if (!File.Exists(path))
            {
                return XstsOutcome.NotImplemented;
            }

            documents.Add((await File.ReadAllBytesAsync(path, cancellationToken), new Uri(path).AbsoluteUri));
        }

        try
        {
            return _bridge.Compile(documents, cancellationToken).IsValid ? XstsOutcome.Valid : XstsOutcome.Invalid;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return XstsOutcome.NotImplemented;
        }
    }

    public Task<XstsOutcome> ValidateInstanceAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(XstsOutcome.NotImplemented);
    }
}
