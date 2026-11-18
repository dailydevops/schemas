namespace NetEvolve.Schemas.Xml.Dtd.External.Loader;

using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

internal sealed class ExternalEntityLoader
{
    internal ExternalEntityLoader(DtdValidationOptions options, XmlReaderLimits limits) =>
        throw new System.NotSupportedException();

    internal bool TryLoad(
        ResourceKind kind,
        string name,
        string? publicId,
        string systemId,
        string? declaringBaseUri,
        CancellationToken cancellationToken,
        out ExternalEntityFrame? frame,
        out ExternalEntityFailure failure
    ) => throw new System.NotSupportedException();
}
