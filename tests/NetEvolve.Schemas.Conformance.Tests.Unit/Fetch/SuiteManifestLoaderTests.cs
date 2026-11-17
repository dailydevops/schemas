#if NET10_0_OR_GREATER
using NetEvolve.Schemas.Conformance.Manifest;

namespace NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

public class SuiteManifestLoaderTests
{
    private static readonly string ValidHash = new('a', 64);

    private static string Json(string key, string hash, string archive = "zip", string extra = "") =>
        $$"""{ "key": "{{key}}", "url": "https://example.invalid/{{key}}.zip", "sha256": "{{hash}}", "archive": "{{archive}}"{{extra}} }""";

    [Test]
    public async Task LoadDirectory_ReadsOneManifestPerFile()
    {
        using var temp = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "one.json"), Json("one", ValidHash));
        await File.WriteAllTextAsync(
            Path.Combine(temp.Path, "two.json"),
            Json("two", ValidHash.ToUpperInvariant(), "tar.gz", ", \"subPath\": \"sub/dir\"")
        );
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "README.md"), "ignored");

        var manifests = SuiteManifestLoader.LoadDirectory(temp.Path);

        await Assert.That(manifests.Count).IsEqualTo(2);
        await Assert.That(manifests[0].Key).IsEqualTo("one");
        await Assert.That(manifests[0].ArchiveType).IsEqualTo(ArchiveType.Zip);
        await Assert.That(manifests[0].SubPath).IsNull();
        await Assert.That(manifests[1].Key).IsEqualTo("two");
        await Assert.That(manifests[1].ArchiveType).IsEqualTo(ArchiveType.TarGz);
        await Assert.That(manifests[1].Sha256).IsEqualTo(ValidHash);
        await Assert.That(manifests[1].SubPath).IsEqualTo("sub/dir");
        await Assert.That(manifests[1].SourceUrl).IsEqualTo(new Uri("https://example.invalid/two.zip"));
    }

    [Test]
    public async Task LoadDirectory_DuplicateSuiteKey_Fails()
    {
        using var temp = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "a.json"), Json("same", ValidHash));
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "b.json"), Json("same", ValidHash));

        var ex = await Assert.That(() => SuiteManifestLoader.LoadDirectory(temp.Path)).Throws<SuiteManifestException>();

        await Assert.That(ex!.Message).Contains("same");
    }

    [Test]
    public async Task LoadDirectory_MissingDirectory_Fails()
    {
        using var temp = new TempDirectory();

        await Assert
            .That(() => SuiteManifestLoader.LoadDirectory(Path.Combine(temp.Path, "none")))
            .Throws<SuiteManifestException>();
    }

    [Test]
    [Arguments("")]
    [Arguments("abc")]
    [Arguments("zz00000000000000000000000000000000000000000000000000000000000000")]
    [Arguments("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Parse_MalformedChecksum_IsRejected(string hash) =>
        await Assert.That(() => SuiteManifestLoader.Parse(Json("s", hash), "s.json")).Throws<SuiteManifestException>();

    [Test]
    public async Task Parse_MissingChecksum_IsRejected()
    {
        const string json = """{ "key": "s", "url": "https://example.invalid/s.zip", "archive": "zip" }""";

        var ex = await Assert.That(() => SuiteManifestLoader.Parse(json, "s.json")).Throws<SuiteManifestException>();

        await Assert.That(ex!.Message).Contains("sha256");
    }

    [Test]
    [Arguments("""{ "url": "https://e.invalid/s.zip", "sha256": "HASH", "archive": "zip" }""")]
    [Arguments("""{ "key": "s", "sha256": "HASH", "archive": "zip" }""")]
    [Arguments("""{ "key": "s", "url": "not a url", "sha256": "HASH", "archive": "zip" }""")]
    [Arguments("""{ "key": "s", "url": "ftp://e.invalid/s.zip", "sha256": "HASH", "archive": "zip" }""")]
    [Arguments("""{ "key": "s", "url": "https://e.invalid/s.zip", "sha256": "HASH", "archive": "rar" }""")]
    [Arguments("""{ "key": "s", "url": "https://e.invalid/s.zip", "sha256": "HASH" }""")]
    [Arguments("""{ "key": "s", "url": "https://e.invalid/s.zip", "sha256": "HASH", "archive": "zip", "subPath": "../x" }""")]
    [Arguments("""{ "key": "s", "url": "https://e.invalid/s.zip", "sha256": "HASH", "archive": "zip", "subPath": "/x" }""")]
    [Arguments("""{ "key": "s", "url": 5, "sha256": "HASH", "archive": "zip" }""")]
    [Arguments("""{ "key": "a/b", "url": "https://e.invalid/s.zip", "sha256": "HASH", "archive": "zip" }""")]
    [Arguments("[]")]
    [Arguments("{ not json")]
    public async Task Parse_InvalidManifest_IsRejected(string template)
    {
        var json = template.Replace("HASH", ValidHash, StringComparison.Ordinal);

        await Assert.That(() => SuiteManifestLoader.Parse(json, "s.json")).Throws<SuiteManifestException>();
    }

    [Test]
    public async Task Parse_NullArguments_Throw()
    {
        await Assert.That(() => SuiteManifestLoader.Parse(null!, "s.json")).Throws<ArgumentNullException>();
        await Assert.That(() => SuiteManifestLoader.LoadDirectory(null!)).Throws<ArgumentNullException>();
    }
}
#endif
