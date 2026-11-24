namespace NetEvolve.Schemas.Benchmarks.Xsd.Validation;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// The corpora of the XSD benchmarks: the UBL 2.1 and CII D16B invoice schemas with one invoice each. The archives are never
/// committed; they are fetched by pinned checksum and cached in the temp folder (override with
/// <c>SCHEMAS_BENCHMARK_CACHE</c>). Both engines read the same documents from memory.
/// </summary>
internal static partial class XsdBenchmarkInput
{
    private const string UblUrl = "https://docs.oasis-open.org/ubl/os-UBL-2.1/UBL-2.1.zip";
    private const string UblSha256 = "60b80d76394a8a2add90723ecb8e0e2e9d826775de9749df37a72d60703f86ed";
    private const string CiiUrl =
        "https://github.com/ConnectingEurope/eInvoicing-EN16931/archive/refs/tags/validation-1.3.17.zip";
    private const string CiiSha256 = "a646592f5ca31faa437f47c75b6dce585005d7ad191075a43a065d24acb89546";
    private const string CiiRoot =
        "eInvoicing-EN16931-validation-1.3.17/cii/schema/D16B SCRDM (Subset)/uncoupled clm/CII/uncefact/data/standard/";
    private const string CiiExample = "eInvoicing-EN16931-validation-1.3.17/cii/examples/CII_example1.xml";
    private const string Host = "http://benchmark.invalid/";

    private static readonly Lazy<Corpus> UblCorpus = new(() =>
        Load(UblUrl, UblSha256, "xsd/", "xsd/maindoc/UBL-Invoice-2.1.xsd", "xml/UBL-Invoice-2.1-Example.xml")
    );
    private static readonly Lazy<Corpus> CiiCorpus = new(() =>
        Load(CiiUrl, CiiSha256, CiiRoot, CiiRoot + "CrossIndustryInvoice_100pD16B.xsd", CiiExample)
    );

    /// <summary>Gets the UBL 2.1 invoice schema and example.</summary>
    public static Corpus Ubl => UblCorpus.Value;

    /// <summary>Gets the CII D16B cross industry invoice schema and example.</summary>
    public static Corpus Cii => CiiCorpus.Value;

    /// <summary>The documents of one schema (the root and everything it imports) and one valid instance.</summary>
    /// <param name="RootUri">The URI of the root schema document.</param>
    /// <param name="Schemas">The schema documents by absolute URI.</param>
    /// <param name="Instance">The instance, UTF-8.</param>
    internal sealed record Corpus(Uri RootUri, IReadOnlyDictionary<Uri, byte[]> Schemas, byte[] Instance);

    /// <summary>Repeats the last element named <paramref name="elementName"/> of the instance, so the document grows linearly.</summary>
    /// <param name="instance">The instance, UTF-8.</param>
    /// <param name="elementName">The qualified name of the repeated element, such as <c>cac:InvoiceLine</c>.</param>
    /// <param name="count">The number of copies to add.</param>
    /// <returns>The enlarged instance, UTF-8.</returns>
    public static byte[] Repeat(byte[] instance, string elementName, int count)
    {
        var text = Encoding.UTF8.GetString(instance);
        var start = text.LastIndexOf("<" + elementName, StringComparison.Ordinal);
        var closing = "</" + elementName + ">";
        var end = text.LastIndexOf(closing, StringComparison.Ordinal) + closing.Length;
        var block = text[start..end];
        var sb = new StringBuilder(text.Length + (block.Length + 1) * count).Append(text, 0, end);
        for (var i = 0; i < count; i++)
        {
            _ = sb.Append('\n').Append(block);
        }

        return Encoding.UTF8.GetBytes(sb.Append(text, end, text.Length - end).ToString());
    }

    private static Corpus Load(string url, string sha256, string schemaPrefix, string root, string example)
    {
        using var archive = new ZipArchive(new MemoryStream(Fetch(url, sha256)), ZipArchiveMode.Read);
        var all = archive
            .Entries.Where(e =>
                e.FullName.StartsWith(schemaPrefix, StringComparison.Ordinal)
                && e.FullName.EndsWith(".xsd", StringComparison.Ordinal)
            )
            .ToDictionary(e => ToUri(e.FullName), ReadSchema);

        // Only the root and the documents it imports transitively: both engines compile the same set.
        var rootUri = ToUri(root);
        var closure = new Dictionary<Uri, byte[]>();
        var pending = new Stack<Uri>([rootUri]);
        while (pending.Count > 0)
        {
            var uri = pending.Pop();
            if (closure.ContainsKey(uri))
            {
                continue;
            }

            closure[uri] = all[uri];
            using var stream = new MemoryStream(all[uri], writable: false);
            foreach (var element in XDocument.Load(stream).Descendants())
            {
                if (element.Attribute("schemaLocation")?.Value is { } location)
                {
                    pending.Push(new Uri(uri, location));
                }
            }
        }

        var instance = Read(archive.GetEntry(example) ?? throw new InvalidOperationException(example));

        return new Corpus(rootUri, closure, instance);
    }

    private static Uri ToUri(string entryName) =>
        new(Host + Uri.EscapeDataString(entryName).Replace("%2F", "/", StringComparison.Ordinal));

    // Limitation: the only schema document with a document type declaration (the XML Signature schema bundled with UBL)
    // is read without it. The XSD engine of this library reads such a declaration now (#988), but the stripped text is kept so
    // that the measured inputs do not change without CI-runner values. The declaration only repeats the xmlns:ds binding of
    // the root and defines entities that are never used; both engines read the same text.
    private static byte[] ReadSchema(ZipArchiveEntry entry)
    {
        var bytes = Read(entry);
        var text = Encoding.UTF8.GetString(bytes);
        var stripped = DocumentTypeDeclaration().Replace(text, string.Empty);

        return string.Equals(text, stripped, StringComparison.Ordinal) ? bytes : Encoding.UTF8.GetBytes(stripped);
    }

    [GeneratedRegex(@"<!DOCTYPE[^\[>]*\[.*?\]>", RegexOptions.Singleline)]
    private static partial Regex DocumentTypeDeclaration();

    private static byte[] Read(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return memory.ToArray();
    }

    private static byte[] Fetch(string url, string sha256)
    {
        var folder = Environment.GetEnvironmentVariable("SCHEMAS_BENCHMARK_CACHE")
            ?? Path.Combine(Path.GetTempPath(), "NetEvolve.Schemas.Benchmarks");
        var cached = Path.Combine(folder, sha256);
        if (File.Exists(cached))
        {
            var local = File.ReadAllBytes(cached);
            if (Verify(local, sha256))
            {
                return local;
            }
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
#pragma warning disable VSTHRD002 // Synchronous setup of a benchmark, no synchronization context
        var bytes = client.GetByteArrayAsync(new Uri(url)).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (!Verify(bytes, sha256))
        {
            throw new InvalidOperationException($"The archive {url} does not match the pinned checksum {sha256}.");
        }

        _ = Directory.CreateDirectory(folder);
        File.WriteAllBytes(cached, bytes);

        return bytes;
    }

    private static bool Verify(byte[] bytes, string sha256) =>
        string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), sha256, StringComparison.OrdinalIgnoreCase);
}
