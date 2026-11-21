namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>Calls the schema compilation pipeline of the Xsd package.</summary>
internal sealed class SchemaCompilerBridge
{
    public SchemaCompilerBridge(XsdVersion version, IResourceResolver resolver, ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(limits);

        Version = version;
    }

    public XsdVersion Version { get; }

    public ValidationResult Compile(
        IReadOnlyList<(ReadOnlyMemory<byte> Content, string BaseUri)> documents,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("The bridge is not implemented yet.");
}
