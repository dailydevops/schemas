namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using System.IO.Compression;
using System.Security.Cryptography;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>A pinned in-memory suite archive, a counting transport, and a temporary cache folder.</summary>
internal sealed class GoldenFixture : IDisposable
{
    public const string SuiteKey = "golden-rules";

    private readonly string _cache = Directory.CreateTempSubdirectory("golden-harness-").FullName;

    public GoldenFixture()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in new[] { "docs/a.xml", "docs/b.xml", "docs/c.xml", "docs/d.xml" })
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write("<doc/>");
            }
        }

        var payload = stream.ToArray();
        Manifest = new SuiteManifest(
            SuiteKey,
            new Uri("https://example.invalid/golden-rules.zip"),
            HexEncoding.ToLower(SHA256.HashData(payload)),
            ArchiveType.Zip
        );
        Transport = new CountingTransport(payload);
        Fetcher = new SuiteFetcher(Transport, _cache);
    }

    public SuiteManifest Manifest { get; }

    public CountingTransport Transport { get; }

    public SuiteFetcher Fetcher { get; }

    public GoldenCorpus Corpus => new([Manifest], Fetcher);

    public static GoldenDocumentSource Source(string name)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
        return new(SuiteKey, $"docs/{name}.xml");
    }

    public void Dispose() => Directory.Delete(_cache, recursive: true);

    internal sealed class CountingTransport(byte[] payload) : ISuiteTransport
    {
        public int Calls { get; private set; }

        public Task<Stream> OpenAsync(Uri source, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult<Stream>(new MemoryStream(payload, writable: false));
        }
    }
}
