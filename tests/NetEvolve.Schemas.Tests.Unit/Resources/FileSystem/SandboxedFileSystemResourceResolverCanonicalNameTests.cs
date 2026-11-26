namespace NetEvolve.Schemas.Tests.Unit.Resources.FileSystem;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.FileSystem;

internal sealed class SandboxedFileSystemResourceResolverCanonicalNameTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "nes-canon-" + Guid.NewGuid().ToString("N"));

    public SandboxedFileSystemResourceResolverCanonicalNameTests() =>
        _ = Directory.CreateDirectory(Path.Combine(_root, "Mixed Dir"));

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of the temporary root.
        }
    }

    [Test]
    [Arguments("Name.xml", new[] { "Name.xml" }, null)]
    [Arguments("name.xml", new[] { "Name.xml" }, "Name.xml")]
    [Arguments("name.xml", new[] { "NAME.xml", "Name.xml" }, "NAME.xml")]
    [Arguments("name.xml", new[] { "NAME.xml", "name.xml" }, null)]
    [Arguments("name.xml", new[] { "other.xml" }, null)]
    [Arguments("name.xml", new string[0], null)]
    public async Task StoredName_NamesAndSegment_ReturnsOnlyADifferentlyCasedStoredName(
        string segment,
        string[] names,
        string? expected
    ) => await Assert.That(SandboxedFileSystemResourceResolver.StoredName(names, segment)).IsEqualTo(expected);

    [Test]
    public async Task BuildSystemId_SegmentsBelowRoot_AppendsEncodedSegmentsToRootUri()
    {
        var id = SandboxedFileSystemResourceResolver.BuildSystemId("file:///r/", 1, ["r", "Mixed Dir", "File.xml"]);

        await Assert.That(id).IsEqualTo("file:///r/Mixed%20Dir/File.xml");
    }

    [Test]
    public async Task Resolve_DirectoryAndFileInOtherLetterCase_ReturnsOnDiskCasingOrNotFound(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        await File.WriteAllTextAsync(Path.Combine(_root, "Mixed Dir", "File.xml"), "<f/>", cancellationToken);
        var caseInsensitive = File.Exists(Path.Combine(_root, "mixed dir", "FILE.XML"));
        var request = new ResourceRequest(ResourceKind.SchemaDocument, "mixed%20dir/FILE.XML", null, null);

        using var result = new SandboxedFileSystemResourceResolver(_root).Resolve(in request, cancellationToken);

        await Assert.That(result.Status).IsEqualTo(caseInsensitive ? ResourceStatus.Found : ResourceStatus.NotFound);
        if (caseInsensitive)
        {
            await Assert.That(result.SystemId!).EndsWith("/Mixed%20Dir/File.xml");
        }
    }
}
