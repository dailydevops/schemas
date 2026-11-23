namespace NetEvolve.Schemas.Benchmarks.Xml.Reader;

using System.Xml;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>
/// The work of one benchmark operation: read the whole document and drop every event. Limitation: the library side
/// drives the composed reader with a discarding sink until a public entry point exists; <c>System.Xml</c> runs with
/// its defaults (DTD prohibited, no validation), the same as the library reader.
/// </summary>
internal static class ReaderRuns
{
    /// <summary>Reads the characters with this library.</summary>
    /// <param name="document">The document.</param>
    /// <returns>The status of the run.</returns>
    public static XmlReadStatus Library(string document)
    {
        using var reader = XmlPipelineReader.Create(document.AsSpan());
        return Pump(reader);
    }

    /// <summary>Reads the UTF-8 bytes with this library.</summary>
    /// <param name="document">The document.</param>
    /// <returns>The status of the run.</returns>
    public static XmlReadStatus Library(byte[] document)
    {
        using var reader = XmlPipelineReader.Create(document.AsSpan());
        return Pump(reader);
    }

    /// <summary>Reads the characters with <c>System.Xml</c>.</summary>
    /// <param name="document">The document.</param>
    /// <returns>The number of nodes.</returns>
    public static int SystemXml(string document)
    {
        using var text = new StringReader(document);
        using var reader = XmlReader.Create(text);
        return Count(reader);
    }

    /// <summary>Reads the UTF-8 bytes with <c>System.Xml</c>.</summary>
    /// <param name="document">The document.</param>
    /// <returns>The number of nodes.</returns>
    public static int SystemXml(byte[] document)
    {
        using var stream = new MemoryStream(document, writable: false);
        using var reader = XmlReader.Create(stream);
        return Count(reader);
    }

    /// <summary>Proves that both readers accept the same document.</summary>
    /// <param name="document">The document.</param>
    public static void EnsureBothAccept(string document) => EnsureBothAccept(Library(document), SystemXml(document));

    /// <summary>Proves that both readers accept the same document.</summary>
    /// <param name="document">The document.</param>
    public static void EnsureBothAccept(byte[] document) => EnsureBothAccept(Library(document), SystemXml(document));

    private static void EnsureBothAccept(XmlReadStatus library, int nodes)
    {
        if (library != XmlReadStatus.Completed || nodes == 0)
        {
            throw new InvalidOperationException("The readers disagree on the benchmark document.");
        }
    }

    private static XmlReadStatus Pump(XmlPipelineReader reader)
    {
        var sink = new DtdDiscardSink(XmlReaderLimits.Default);
        return reader.Pump(ref sink);
    }

    private static int Count(XmlReader reader)
    {
        var nodes = 0;
        while (reader.Read())
        {
            nodes++;
        }

        return nodes;
    }
}
