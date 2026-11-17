namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Reads the document of a case through <see cref="XmlPipelineReader"/> (XML-53).</summary>
/// <remarks>
/// Limitations: these cases are <see cref="XmlConfObservation.NotImplemented"/> so they never pass by accident: a case
/// that needs namespace processing switched off, a document with a document type declaration (rejected with
/// <c>XML2001</c> until XML-15), and a case whose document is missing from the pinned archive (<c>eduni/namespaces/misc</c>).
/// </remarks>
/// <param name="suiteRoot">The suite root directory.</param>
internal sealed class PipelineXmlConfReader(string suiteRoot) : IXmlConfReader
{
    public async Task<XmlConfObservation> ReadAsync(XmlConfCase xmlConfCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!xmlConfCase.Namespaces)
        {
            return XmlConfObservation.NotImplemented;
        }

        var path = Path.Combine(suiteRoot, xmlConfCase.Uri);
        return File.Exists(path)
            ? Observe(await File.ReadAllBytesAsync(path, cancellationToken))
            : XmlConfObservation.NotImplemented;
    }

    private static XmlConfObservation Observe(byte[] bytes)
    {
        using var reader = XmlPipelineReader.Create(bytes.AsSpan());
        var sink = new DiscardingXmlSink();
        return reader.Pump(ref sink) switch
        {
            XmlReadStatus.Completed => XmlConfObservation.Accepted,
            _ when ReferenceEquals(reader.Diagnostic?.Code, XmlErrorCodes.XML2001) => XmlConfObservation.NotImplemented,
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
