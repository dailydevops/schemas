namespace NetEvolve.Schemas.Xsd.Tests.Integration.Ubl;

using System.Collections.Concurrent;
using System.Xml;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>
/// The pinned OASIS UBL packages 2.1 to 2.4: manifest, fetch, and the run of the official sample instances. Every sample is validated
/// against the <c>maindoc</c> schema of its root namespace and is expected to be valid.
/// </summary>
internal static class UblSuite
{
    /// <summary>The versions that are pinned, one manifest each.</summary>
    public static readonly string[] Versions = ["2.1", "2.2", "2.3", "2.4"];

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    /// <summary>Gets the directory of the known-failure baselines, one file per version.</summary>
    public static string BaselineDirectory { get; } =
        Path.Combine(XstsSuite.RepositoryRoot, "tests", "conformance", "baselines", "ubl");

    /// <summary>Gets the directory of the suite manifests.</summary>
    public static string SuitesDirectory { get; } =
        Path.Combine(XstsSuite.RepositoryRoot, "tests", "conformance", "suites");

    /// <summary>Gets the test set name, the manifest key, and the baseline file name of a version.</summary>
    /// <param name="version">The UBL version, such as <c>2.1</c>.</param>
    /// <returns>The key, <c>ubl-2.1</c>.</returns>
    public static string KeyOf(string version) => "ubl-" + version;

    /// <summary>Reads the manifest that pins the archive of a version.</summary>
    /// <param name="version">The UBL version.</param>
    /// <returns>The manifest.</returns>
    public static SuiteManifest LoadManifest(string version) =>
        SuiteManifestLoader
            .LoadDirectory(SuitesDirectory)
            .Single(m => string.Equals(m.Key, KeyOf(version), StringComparison.Ordinal));

    /// <summary>Fetches the archive of a version by its pinned checksum, or reuses the cache.</summary>
    /// <param name="version">The UBL version.</param>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The suite root.</returns>
    public static Task<string> FetchAsync(string version, CancellationToken cancellationToken) =>
        new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot()).FetchAsync(
            LoadManifest(version),
            cancellationToken
        );

    /// <summary>Gets the sample instances of a package.</summary>
    /// <param name="root">The suite root.</param>
    /// <returns>The paths relative to the root, with forward slashes, in a stable order.</returns>
    public static IReadOnlyList<string> Samples(string root) =>
        [
            .. Directory
                .EnumerateFiles(Path.Combine(root, "xml"), "*.xml", SearchOption.TopDirectoryOnly)
                .Select(f => "xml/" + Path.GetFileName(f))
                .OrderBy(f => f, StringComparer.Ordinal),
        ];

    /// <summary>Maps the target namespace of every <c>xsd/maindoc</c> schema to its path.</summary>
    /// <param name="root">The suite root.</param>
    /// <returns>The paths by namespace.</returns>
    public static IReadOnlyDictionary<string, string> MainDocuments(string root)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "xsd", "maindoc"), "*.xsd"))
        {
            using var reader = XmlReader.Create(file);
            _ = reader.MoveToContent();
            map[reader.GetAttribute("targetNamespace") ?? string.Empty] = file;
        }

        return map;
    }

    /// <summary>Reads the namespace of the root element of an instance.</summary>
    /// <param name="path">The instance path.</param>
    /// <returns>The namespace, or <see langword="null"/> when the root has none or the document does not parse.</returns>
    public static string? RootNamespace(string path)
    {
        try
        {
            using var reader = XmlReader.Create(path);
            _ = reader.MoveToContent();
            return reader.NamespaceURI.Length == 0 ? null : reader.NamespaceURI;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>Validates every sample of a package against the schema of its root namespace.</summary>
    /// <param name="version">The UBL version, which is the test set of the results.</param>
    /// <param name="root">The suite root.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per sample: pass when valid, fail when rejected or when the schema does not build, skipped when no maindoc schema applies.</returns>
    public static async Task<IReadOnlyList<CaseResult>> RunAsync(
        string version,
        string root,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var testSet = KeyOf(version);
        var documents = MainDocuments(root);
        var validators = new ConcurrentDictionary<string, Task<IXsdValidator?>>(StringComparer.Ordinal);
        var results = new List<CaseResult>();
        foreach (var sample in Samples(root))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(root, sample);
            var ns = RootNamespace(path);
            if (ns is null || !documents.TryGetValue(ns, out var schema))
            {
                results.Add(new CaseResult(sample, testSet, CaseOutcome.Skipped, "No maindoc schema for the root."));
                continue;
            }

            var validator = await validators.GetOrAdd(ns, _ => BuildAsync(root, schema, cancellationToken));
            if (validator is null)
            {
                results.Add(new CaseResult(sample, testSet, CaseOutcome.Fail, "The schema does not build."));
                continue;
            }

            var result = await validator.ValidateAsync(
                await File.ReadAllBytesAsync(path, cancellationToken),
                new Uri(path).AbsoluteUri,
                cancellationToken
            );
            results.Add(
                new CaseResult(sample, testSet, result.IsValid ? CaseOutcome.Pass : CaseOutcome.Fail, "Expected valid.")
            );
        }

        return results;
    }

    private static async Task<IXsdValidator?> BuildAsync(
        string root,
        string schema,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await SchemaValidator
                .ForXsd()
                .WithVersion(XsdVersion.V10)
                .WithResolver(new SandboxedFileSystemResourceResolver(root))
                .AddSchema(await File.ReadAllBytesAsync(schema, cancellationToken), new Uri(schema).AbsoluteUri)
                .BuildAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return null;
        }
    }
}
