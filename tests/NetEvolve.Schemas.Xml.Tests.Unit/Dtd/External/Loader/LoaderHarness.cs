namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;

/// <summary>Builds loaders over a recording resolver and drains frames; the byte helpers write the encodings of the tests.</summary>
internal static class LoaderHarness
{
    internal const string DocumentUri = "http://example.org/doc/main.xml";

    /// <summary>Options with every external flag on and the given resolver.</summary>
    internal static DtdValidationOptions Open(IResourceResolver resolver) =>
        DtdValidationOptions
            .Default.WithResolver(resolver)
            .WithAllowExternalSubset(true)
            .WithAllowExternalGeneralEntities(true)
            .WithAllowExternalParameterEntities(true);

    internal static ExternalEntityLoader Loader(
        IResourceResolver resolver,
        XmlReaderLimits? limits = null,
        DtdValidationOptions? options = null
    ) => new(options ?? Open(resolver), limits ?? XmlReaderLimits.Default);

    /// <summary>Requests a general entity.</summary>
    internal static (ExternalEntityFrame? Frame, ExternalEntityFailure Failure) Load(
        ExternalEntityLoader loader,
        string name,
        string systemId,
        string? baseUri,
        CancellationToken cancellationToken,
        ResourceKind kind = ResourceKind.ExternalEntity,
        string? publicId = null
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = loader.TryLoad(kind, name, publicId, systemId, baseUri, cancellationToken, out var frame, out var failure);

        return (frame, failure);
    }

    /// <summary>Reads the frame to its end or its first error.</summary>
    internal static (string Text, XmlErrorCode? Error) Drain(ExternalEntityFrame frame)
    {
        var builder = new StringBuilder();
        while (frame.TryRead(out var chunk))
        {
            _ = builder.Append(chunk.Span);
        }

        return (builder.ToString(), frame.Error);
    }

    internal static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>Encodes as UTF-16 little endian with a byte order mark.</summary>
    internal static byte[] Utf16Le(string text)
    {
        var body = Encoding.Unicode.GetBytes(text);
        var result = new byte[body.Length + 2];
        result[0] = 0xFF;
        result[1] = 0xFE;
        body.CopyTo(result, 2);

        return result;
    }
}
