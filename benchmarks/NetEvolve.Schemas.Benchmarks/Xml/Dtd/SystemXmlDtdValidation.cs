namespace NetEvolve.Schemas.Benchmarks.Xml.Dtd;

using System.Xml;

/// <summary>The baseline of the DTD validation benchmarks: the <c>System.Xml</c> reader with DTD validation on.</summary>
internal sealed class SystemXmlDtdValidation
{
    private readonly XmlReaderSettings _settings;
    private int _events;

    /// <summary>Initializes a new instance of the <see cref="SystemXmlDtdValidation"/> class.</summary>
    /// <param name="resolver">The resolver for the external subset, <see langword="null"/> when the document has none.</param>
    public SystemXmlDtdValidation(XmlResolver? resolver)
    {
        _settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            ValidationType = ValidationType.DTD,
            XmlResolver = resolver,
        };
        _settings.ValidationEventHandler += (_, _) => _events++;
    }

    /// <summary>Reads the whole document and returns the number of validity errors and warnings.</summary>
    /// <param name="document">The document as UTF-8.</param>
    /// <returns>The number of validation events; zero for a valid document.</returns>
    public int Validate(byte[] document)
    {
        _events = 0;
        using var stream = new MemoryStream(document, writable: false);
        using var reader = XmlReader.Create(stream, _settings, DtdBenchmarkInput.DocumentUri);
        while (reader.Read())
        {
            // Reading is the work: the reader validates each node as it moves on.
        }

        return _events;
    }
}
