namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

/// <summary>Reads a document through <see cref="XmlPipelineReader"/> with DTD processing on and scripted external access.</summary>
internal static class GeneralHarness
{
    internal const string DocumentUri = LoaderHarness.DocumentUri;

    /// <summary>The entity <c>e</c> as an external general entity that the resolver serves as <c>e.ent</c> next to the document.</summary>
    internal const string ExternalE = "<!ENTITY e SYSTEM \"e.ent\">";

    internal const string EntityUri = "http://example.org/doc/e.ent";

    /// <summary>A document whose root holds <paramref name="content"/> and whose internal subset holds <paramref name="declarations"/>.</summary>
    internal static string Doc(string declarations, string content) =>
        $"<!DOCTYPE doc [{declarations}]>\n<doc>{content}</doc>";

    /// <summary>A resolver that serves the entity <c>e.ent</c> with the given bytes.</summary>
    internal static RecordingResolver Serving(byte[] entity, int maxRead = int.MaxValue, string? finalUri = null) =>
        new RecordingResolver().Add(EntityUri, entity, finalUri, maxRead);

    internal static RecordingResolver Serving(string entity, int maxRead = int.MaxValue, string? finalUri = null) =>
        Serving(LoaderHarness.Utf8(entity), maxRead, finalUri);

    /// <summary>The options of the reader: DTD processing on, external access when <paramref name="access"/> is given.</summary>
    internal static XmlReaderOptions Options(
        DtdValidationOptions? access,
        XmlReaderLimits? limits = null,
        string? baseUri = DocumentUri
    )
    {
        var options = XmlReaderOptions.Default.WithDtdProcessing(true).WithBaseUri(baseUri);
        if (access is not null)
        {
            options = options.WithExternalAccess(access);
        }

        return limits is null ? options : options.WithLimits(limits);
    }

    /// <summary>Reads the document with every external flag on and the given resolver.</summary>
    internal static GeneralRun Read(string xml, IResourceResolver resolver, XmlReaderLimits? limits = null) =>
        Read(xml, Options(LoaderHarness.Open(resolver), limits));

    /// <summary>Reads the document with the options.</summary>
    internal static GeneralRun Read(string xml, XmlReaderOptions options)
    {
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), options);

        return Pump(reader);
    }

    /// <summary>Pumps the reader to its end.</summary>
    internal static GeneralRun Pump(XmlPipelineReader reader)
    {
        var sink = new GeneralSink(() => reader.BaseUri);
        var status = reader.Pump(ref sink);

        return new GeneralRun(
            status,
            reader.Diagnostic?.Code.Alias,
            reader.Diagnostic?.Position,
            sink.Events,
            sink.Bases,
            reader.BaseUri
        );
    }
}
