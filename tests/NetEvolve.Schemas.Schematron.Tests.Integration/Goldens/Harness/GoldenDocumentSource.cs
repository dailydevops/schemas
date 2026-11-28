namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

/// <summary>Where a golden document lives: a file inside a pinned suite, never a free URL.</summary>
internal sealed record GoldenDocumentSource
{
    /// <summary>Initializes a new instance of the <see cref="GoldenDocumentSource"/> class.</summary>
    /// <param name="suiteKey">The key of the suite manifest (<c>tests/conformance/suites/&lt;suite&gt;.json</c>) that pins the archive.</param>
    /// <param name="path">
    /// The forward-slash path of the document relative to the suite root. An empty or rooted path is not rejected here;
    /// <see cref="GoldenCorpus"/> reports it as a <see cref="GoldenSourceException"/>.
    /// </param>
    public GoldenDocumentSource(string suiteKey, string path)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(suiteKey);
        ArgumentNullException.ThrowIfNull(path);
        SuiteKey = suiteKey;
        Path = path;
    }

    /// <summary>Gets the key of the suite manifest that pins the archive.</summary>
    public string SuiteKey { get; init; }

    /// <summary>Gets the forward-slash path of the document relative to the suite root.</summary>
    public string Path { get; init; }

    /// <summary>Gets the stable case id, <c>&lt;suite key&gt;/&lt;path&gt;</c>.</summary>
    public string Id => $"{SuiteKey}/{Path}";
}
