namespace NetEvolve.Schemas.Benchmarks.Xml.Pipeline;

using System.Buffers;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.Xml.Model.Traversal;
using NetEvolve.Schemas.Xml.Writer;

/// <summary>Model-to-bytes write of a loaded document, against <c>XDocument.Save</c> on the same infoset.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DocumentWriteBenchmarks
#pragma warning restore CA1515
{
    // Static so it is not an owned disposable field; it holds only managed memory and lives as long as the process.
    private static readonly MemoryStream _stream = new(64 * 1024);
    private static readonly XmlWriterOptions Options = new(omitXmlDeclaration: true);
    private static readonly XmlWriterSettings Settings = new()
    {
        OmitXmlDeclaration = true,
        Encoding = new UTF8Encoding(false),
    };

    private readonly ArrayBufferWriter<byte> _output = new(64 * 1024);
    private XmlDocumentTree _tree = null!;
    private XDocument _document = null!;

    /// <summary>Loads the input and proves that both writers produce the same bytes.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _tree = XmlDocumentLoader.Load(PipelineBenchmarkInput.Document.AsSpan());
        _document = XDocument.Load(new MemoryStream(PipelineBenchmarkInput.Document));
        WriteDocumentSystemXml();
        _ = WriteDocument();
        if (!_stream.GetBuffer().AsSpan(0, (int)_stream.Length).SequenceEqual(_output.WrittenSpan))
        {
            throw new InvalidOperationException("The writers produce different bytes.");
        }
    }

    /// <summary>Baseline: <c>XDocument.Save</c> writes through a <c>System.Xml</c> writer into a stream.</summary>
    [Benchmark(Baseline = true)]
    public void WriteDocumentSystemXml()
    {
        _stream.SetLength(0);
        using var writer = System.Xml.XmlWriter.Create(_stream, Settings);
        _document.Save(writer);
    }

    /// <summary>Pumps the tree into the writer, which writes UTF-8 into a buffer.</summary>
    /// <returns>The number of bytes written.</returns>
    [Benchmark]
    public int WriteDocument()
    {
        _output.ResetWrittenCount();
        using var writer = new NetEvolve.Schemas.Xml.Writer.XmlWriter(_output, _tree.Names, Options);
        XmlTreePump.Pump(_tree.Document, writer);
        return _output.WrittenCount;
    }
}
