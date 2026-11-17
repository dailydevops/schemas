namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

using System;
using System.IO;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Parses a schema document into located syntax nodes in one pass over the reader contract, without a document tree.
/// </summary>
/// <remarks>
/// Limitations: an attribute carries the location of the start tag of its element (design #108 AP9, exact attribute
/// positions are a separate issue); text, comments, and processing instructions are not part of the model, and the content of
/// <c>appinfo</c> and <c>documentation</c> is skipped; see <see cref="SchemaGrammar"/> for the structural checks that are
/// left to the component builders. A <see cref="Stream"/> is read to its end before parsing (thin adapter over the span
/// core). An error of the reader ends the parse; the nodes read so far are discarded and the error is on the result.
/// </remarks>
internal static class SchemaDocumentParser
{
    /// <summary>Parses a schema document given as characters.</summary>
    /// <param name="schema">The document.</param>
    /// <param name="baseUri">The document base URI, or <see langword="null"/>.</param>
    /// <param name="reporter">Receives the structural errors.</param>
    /// <returns>The parsed document.</returns>
    internal static SchemaDocument Parse(ReadOnlySpan<char> schema, string? baseUri, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(reporter);

        using var reader = XmlPipelineReader.Create(schema);
        return Parse(reader, baseUri, reporter);
    }

    /// <summary>Parses a schema document given as bytes; the encoding follows XML 1.0 Appendix F.</summary>
    /// <param name="schema">The document.</param>
    /// <param name="baseUri">The document base URI, or <see langword="null"/>.</param>
    /// <param name="reporter">Receives the structural errors.</param>
    /// <returns>The parsed document.</returns>
    internal static SchemaDocument Parse(ReadOnlySpan<byte> schema, string? baseUri, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(reporter);

        using var reader = XmlPipelineReader.Create(schema);
        return Parse(reader, baseUri, reporter);
    }

    /// <summary>Parses a schema document read from a stream.</summary>
    /// <param name="schema">The stream, read to its end and left open.</param>
    /// <param name="baseUri">The document base URI, or <see langword="null"/>.</param>
    /// <param name="reporter">Receives the structural errors.</param>
    /// <returns>The parsed document.</returns>
    internal static SchemaDocument Parse(Stream schema, string? baseUri, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(schema);
        Argument.ThrowIfNull(reporter);

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
        Argument.ThrowIfNull(reader);
        Argument.ThrowIfNull(reporter);

        var sink = new SchemaSyntaxSink(baseUri, reporter);
        var status = reader.Pump(ref sink);
        return status == XmlReadStatus.Completed
            ? new SchemaDocument(baseUri, sink.Root, null, true)
            : new SchemaDocument(baseUri, null, reader.Diagnostic, false);
    }
}
