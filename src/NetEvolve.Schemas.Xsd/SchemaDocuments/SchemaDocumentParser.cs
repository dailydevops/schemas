namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

using System;
using System.IO;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Parses a schema document into located syntax nodes in one pass over the reader contract, without a document tree.
/// </summary>
/// <remarks>
/// An attribute is located at its name when the reader implements <see cref="IXmlPositionSource"/> (the readers of this package
/// do), and at the start tag of its element otherwise. Limitations: text, comments, and processing instructions are not part of the model, and the content of
/// <c>appinfo</c> and <c>documentation</c> is skipped; see <see cref="SchemaGrammar"/> for the structural checks that are
/// left to the component builders. A <see cref="Stream"/> is read to its end before parsing (thin adapter over the span
/// core). An error of the reader ends the parse; the nodes read so far are discarded and the error is on the result.
/// </remarks>
internal static class SchemaDocumentParser
{
    // A schema document may carry a document type declaration (the XML Signature schema bundled with UBL does): the internal
    // subset is read and its internal entities are expanded under the default limits of the reader; the external subset is not
    // loaded, because the default external access of the reader is denied.
    // A schema document may also declare XML 1.1; it is read by the XML 1.0 rules (#861).
    private static readonly XmlReaderOptions ReaderOptions = XmlReaderOptions
        .Default.WithDtdProcessing(true)
        .WithAllowXml11Declaration(true);

    /// <summary>Parses a schema document given as characters.</summary>
    /// <param name="schema">The document.</param>
    /// <param name="baseUri">The document base URI, or <see langword="null"/>.</param>
    /// <param name="reporter">Receives the structural errors.</param>
    /// <returns>The parsed document.</returns>
    internal static SchemaDocument Parse(ReadOnlySpan<char> schema, string? baseUri, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        using var reader = XmlPipelineReader.Create(schema, ReaderOptions);
        return Parse(reader, baseUri, reporter);
    }

    /// <summary>Parses a schema document given as bytes; the encoding follows XML 1.0 Appendix F.</summary>
    /// <param name="schema">The document.</param>
    /// <param name="baseUri">The document base URI, or <see langword="null"/>.</param>
    /// <param name="reporter">Receives the structural errors.</param>
    /// <returns>The parsed document.</returns>
    internal static SchemaDocument Parse(ReadOnlySpan<byte> schema, string? baseUri, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        using var reader = XmlPipelineReader.Create(schema, ReaderOptions);
        return Parse(reader, baseUri, reporter);
    }

    /// <summary>Parses a schema document read from a stream.</summary>
    /// <param name="schema">The stream, read to its end and left open.</param>
    /// <param name="baseUri">The document base URI, or <see langword="null"/>.</param>
    /// <param name="reporter">Receives the structural errors.</param>
    /// <returns>The parsed document.</returns>
    internal static SchemaDocument Parse(Stream schema, string? baseUri, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(reporter);

        using var buffer = new MemoryStream();
        schema.CopyTo(buffer);
        return Parse(new ReadOnlySpan<byte>(buffer.GetBuffer(), 0, (int)buffer.Length), baseUri, reporter);
    }

    /// <summary>Parses a schema document from a reader.</summary>
    /// <param name="reader">The reader, holding the whole document.</param>
    /// <param name="baseUri">The document base URI, or <see langword="null"/>.</param>
    /// <param name="reporter">Receives the structural errors.</param>
    /// <returns>The parsed document.</returns>
    internal static SchemaDocument Parse(IXmlReader reader, string? baseUri, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(reporter);

        var sink = new SchemaSyntaxSink(baseUri, reporter);
        sink.UsePositions(reader as IXmlPositionSource);
        var status = reader.Pump(ref sink);
        return status == XmlReadStatus.Completed
            ? new SchemaDocument(baseUri, sink.Root, null, true, sink.InvalidLanguages)
            : new SchemaDocument(baseUri, null, reader.Diagnostic, false);
    }
}
