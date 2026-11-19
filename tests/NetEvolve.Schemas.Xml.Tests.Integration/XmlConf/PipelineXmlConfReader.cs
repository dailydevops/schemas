namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Reads the document of a case through <see cref="XmlPipelineReader"/> (XML-53).</summary>
/// <remarks>
/// Document type declarations are read with DTD processing on (XML-14), and references to the internal general entities
/// it declares are expanded (XML-15). Limitations: these cases are <see cref="XmlConfObservation.NotImplemented"/> so they
/// never pass by accident: a case that needs namespace processing switched off, and a case whose document is missing from
/// the pinned archive (<c>eduni/namespaces/misc</c>). The external subset and the external parameter entities are read
/// (XML-20) through a file resolver that is confined to the suite directory. External general entities and attribute
/// defaults are not read yet (XML-62, XML-16), so cases that need them stay in the baseline; a reference to an external
/// general entity is <c>XML2002</c>.
/// </remarks>
/// <param name="suiteRoot">The suite root directory.</param>
internal sealed class PipelineXmlConfReader(string suiteRoot) : IXmlConfReader
{
    private readonly DtdValidationOptions _externalAccess = DtdValidationOptions
        .Default.WithResolver(new SandboxedFileSystemResourceResolver(suiteRoot))
        .WithAllowExternalSubset(true)
        .WithAllowExternalParameterEntities(true);

    public async Task<XmlConfObservation> ReadAsync(XmlConfCase xmlConfCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!xmlConfCase.Namespaces)
        {
            return XmlConfObservation.NotImplemented;
        }

        var path = Path.Combine(suiteRoot, xmlConfCase.Uri);
        return File.Exists(path)
            ? Observe(await File.ReadAllBytesAsync(path, cancellationToken), new Uri(path).AbsoluteUri)
            : XmlConfObservation.NotImplemented;
    }

    private XmlConfObservation Observe(byte[] bytes, string baseUri)
    {
        var options = XmlReaderOptions
            .Default.WithDtdProcessing(true)
            .WithExternalAccess(_externalAccess)
            .WithBaseUri(baseUri);
        using var reader = XmlPipelineReader.Create(bytes.AsSpan(), options);
        var sink = new DiscardingXmlSink();
        return reader.Pump(ref sink) switch
        {
            XmlReadStatus.Completed => XmlConfObservation.Accepted,
            _ => XmlConfObservation.FatalError,
        };
    }

    private sealed class DiscardingXmlSink : IXmlSink
    {
        public XmlNameTable Names { get; } = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);

        public void StartDocument(in XmlDeclaration declaration) { }

        public void EndDocument() { }

        public void StartElement(XmlName name, XmlPosition position) { }

        public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

        public void Attribute(XmlName name, ReadOnlySpan<char> value) { }

        public void EndAttributes() { }

        public void EndElement(XmlName name) { }

        public void Text(ReadOnlySpan<char> text, XmlTextKind kind) { }

        public void Comment(ReadOnlySpan<char> text) { }

        public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

        public void Doctype(in XmlDoctypeInfo doctype) { }
    }
}
