namespace NetEvolve.Schemas.Benchmarks.Xsd.Validation;

using System.Xml;
using System.Xml.Schema;
using NetEvolve.Schemas.Resources;

/// <summary>
/// One schema with both engines compiled from the same documents and one instance, so a benchmark compares identical work.
/// Limitation: <c>System.Xml</c> has no span input, so its stream read is the baseline of the span benchmarks too.
/// </summary>
internal sealed class XsdScenario
{
    private readonly XsdBenchmarkInput.Corpus _corpus;
    private readonly XmlReaderSettings _settings;
    private int _events;

    public XsdScenario(XsdBenchmarkInput.Corpus corpus, byte[] instance)
    {
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentNullException.ThrowIfNull(instance);

        _corpus = corpus;
        Instance = instance;
        Validator = CompileOwn();
        _settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = CompileSystemXml() };
        _settings.ValidationEventHandler += (_, _) => _events++;

        // Proves that both engines do the same work: each accepts the instance.
        if (ValidateSystemXml() != 0 || !Validator.Validate(instance).IsValid)
        {
            throw new InvalidOperationException("The engines disagree on the instance of the benchmark.");
        }
    }

    public byte[] Instance { get; }

    public IXsdValidator Validator { get; }

    /// <summary>Compiles the schema with <c>System.Xml.Schema</c>; the baseline of the compilation benchmarks.</summary>
    /// <returns>The compiled set.</returns>
    public XmlSchemaSet CompileSystemXml()
    {
        var set = new XmlSchemaSet { XmlResolver = new MemoryResolver(_corpus.Schemas) };
        _ = set.Add(null, _corpus.RootUri.AbsoluteUri);
        set.Compile();

        return set;
    }

    /// <summary>Compiles the schema with this library.</summary>
    /// <returns>The validator.</returns>
    public IXsdValidator CompileOwn() =>
        SchemaValidator
            .ForXsd()
            .WithResolver(new MemoryResourceResolver(_corpus.Schemas))
            .AddSchema(_corpus.Schemas[_corpus.RootUri], _corpus.RootUri.AbsoluteUri)
            .Build();

    /// <summary>Validates the instance with <c>System.Xml</c> through a stream.</summary>
    /// <returns>The number of validation events; zero for a valid instance.</returns>
    public int ValidateSystemXml()
    {
        _events = 0;
        using var stream = new MemoryStream(Instance, writable: false);
        using var reader = XmlReader.Create(stream, _settings);
        while (reader.Read())
        {
            // Reading is the work: the reader validates each node as it moves on.
        }

        return _events;
    }

    /// <summary>Validates the instance with this library through a stream.</summary>
    /// <returns>The number of issues.</returns>
    public int ValidateStream()
    {
        using var stream = new MemoryStream(Instance, writable: false);

        return Validator.Validate(stream).Issues.Count;
    }

    /// <summary>Validates the instance with this library through a span.</summary>
    /// <returns>The number of issues.</returns>
    public int ValidateSpan() => Validator.Validate(Instance.AsSpan()).Issues.Count;

    private sealed class MemoryResolver : XmlResolver
    {
        private readonly IReadOnlyDictionary<Uri, byte[]> _documents;

        public MemoryResolver(IReadOnlyDictionary<Uri, byte[]> documents) => _documents = documents;

        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn) =>
            new MemoryStream(_documents[absoluteUri], writable: false);
    }

    private sealed class MemoryResourceResolver : IResourceResolver
    {
        private readonly IReadOnlyDictionary<Uri, byte[]> _documents;

        public MemoryResourceResolver(IReadOnlyDictionary<Uri, byte[]> documents) => _documents = documents;

        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken) =>
            request.ResolvedUri is { } uri && _documents.TryGetValue(new Uri(uri), out var content)
                ? ResourceResult.Found(content, uri)
                : ResourceResult.NotFound(request.Reference);
    }
}
