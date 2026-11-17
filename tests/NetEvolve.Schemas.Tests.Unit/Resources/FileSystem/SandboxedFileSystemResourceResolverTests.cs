namespace NetEvolve.Schemas.Tests.Unit.Resources.FileSystem;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.FileSystem;

internal sealed class SandboxedFileSystemResourceResolverTests : IDisposable
{
    private readonly string _workspace = Path.Combine(
        Path.GetTempPath(),
        "nes-sandbox-" + Guid.NewGuid().ToString("N")
    );
    private readonly string _root;

    public SandboxedFileSystemResourceResolverTests()
    {
        _root = Path.Combine(_workspace, "root");
        _ = Directory.CreateDirectory(Path.Combine(_root, "sub"));
        File.WriteAllText(Path.Combine(_root, "a.xml"), "<a/>");
        File.WriteAllText(Path.Combine(_root, "sub", "b.xml"), "<b/>");
        File.WriteAllText(Path.Combine(_workspace, "outside.xml"), "<outside/>");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_workspace, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of the temporary workspace.
        }
    }

    private ResourceResult Resolve(string reference, string? baseUri = null, string? resolvedUri = null)
    {
        var request = new ResourceRequest(ResourceKind.SchemaDocument, reference, baseUri, resolvedUri);
        return new SandboxedFileSystemResourceResolver(_root).Resolve(in request, CancellationToken.None);
    }

    private static async Task AssertDenied(ResourceResult result)
    {
        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(result.Stream).IsNull();
        await Assert.That(result.Message!).Contains(CoreDiagnosticCodes.ResourceDenied.ToString());
    }

    [Test]
    [Arguments("a.xml", "<a/>")]
    [Arguments("./a.xml", "<a/>")]
    [Arguments("sub/b.xml", "<b/>")]
    [Arguments("sub/../a.xml", "<a/>")]
    [Arguments("sub/b.xml#fragment", "<b/>")]
    public async Task Resolve_RelativeReferenceInsideRoot_ReturnsFileContent(
        string reference,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var result = Resolve(reference);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Found);
        using var reader = new StreamReader(result.Stream!);
        await Assert.That(await reader.ReadToEndAsync(cancellationToken)).IsEqualTo(expected);
    }

    [Test]
    public async Task Resolve_ResolvedUriInsideRoot_ReturnsFileContent(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var uri = new Uri(Path.Combine(_root, "a.xml")).AbsoluteUri;

        using var result = Resolve("ignored.xml", resolvedUri: uri);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Found);
    }

    [Test]
    [Arguments("../outside.xml")]
    [Arguments("sub/../../outside.xml")]
    [Arguments("/outside.xml")]
    [Arguments("/etc/passwd")]
    [Arguments("C:/Windows/win.ini")]
    [Arguments("C:\\Windows\\win.ini")]
    [Arguments("//server/share/a.xml")]
    [Arguments("\\\\server\\share\\a.xml")]
    [Arguments("\\\\.\\C:\\a.xml")]
    [Arguments("//./C:/a.xml")]
    [Arguments("http://example.org/a.xml")]
    [Arguments("https://example.org/a.xml")]
    [Arguments("ftp://example.org/a.xml")]
    [Arguments("file://example.org/a.xml")]
    [Arguments("file://example.org/etc/passwd")]
    public async Task Resolve_EscapingReference_IsDenied(string reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var result = Resolve(reference);

        await AssertDenied(result);
    }

    [Test]
    [Arguments("%2e%2e/outside.xml")]
    [Arguments("%2E%2E/outside.xml")]
    [Arguments("sub/%2e%2e/%2e%2e/outside.xml")]
    [Arguments("..%2foutside.xml")]
    [Arguments("sub%2f..%2f..%2foutside.xml")]
    [Arguments("..%5coutside.xml")]
    [Arguments("%2e%2e%5coutside.xml")]
    [Arguments("..\\outside.xml")]
    [Arguments("sub\\..\\..\\outside.xml")]
    [Arguments("sub/..\\outside.xml")]
    [Arguments("sub%5c..%5c..%5coutside.xml")]
    [Arguments("a%2fb.xml")]
    [Arguments("sub%00/b.xml")]
    [Arguments("%C3%28.xml")]
    [Arguments("a.xml%3Astream")]
    [Arguments("a%zz.xml")]
    public async Task Resolve_EncodedOrAlternateSeparatorForm_IsDenied(
        string reference,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var result = Resolve(reference);

        await AssertDenied(result);
    }

    [Test]
    [Arguments("")]
    [Arguments("sub/")]
    [Arguments("sub//b.xml")]
    public async Task Resolve_ReferenceWithoutFile_IsDenied(string reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var result = Resolve(reference);

        await AssertDenied(result);
    }

    [Test]
    public async Task Resolve_BaseUriOutsideRoot_IsDenied(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var result = Resolve("a.xml", baseUri: new Uri(_workspace + Path.DirectorySeparatorChar).AbsoluteUri);

        await AssertDenied(result);
    }

    [Test]
    public async Task Resolve_SiblingDirectoryWithRootAsPrefix_IsDenied(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = Directory.CreateDirectory(_root + "-sibling");
        await File.WriteAllTextAsync(Path.Combine(_root + "-sibling", "a.xml"), "<x/>", cancellationToken);

        using var result = Resolve("../root-sibling/a.xml");

        await AssertDenied(result);
    }

    [Test]
    public async Task Resolve_DefaultRequest_IsDeniedWithoutThrowing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = default(ResourceRequest);

        using var result = new SandboxedFileSystemResourceResolver(_root).Resolve(in request, CancellationToken.None);

        await AssertDenied(result);
    }

    [Test]
    public async Task Resolve_MissingFileInsideRoot_IsNotFoundNotDenied(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var result = Resolve("missing.xml");

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.NotFound);
        await Assert.That(result.Message!).Contains(CoreDiagnosticCodes.ResourceNotFound.ToString());
        await Assert.That(result.Message!).DoesNotContain(CoreDiagnosticCodes.ResourceDenied.ToString());
    }

    [Test]
    public async Task Resolve_DirectoryInsideRoot_IsNotFound(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var result = Resolve("sub");

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.NotFound);
    }

    [Test]
    public async Task Resolve_LockedFile_IsDeniedWithoutThrowing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var locked = new FileStream(
            Path.Combine(_root, "a.xml"),
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None
        );

        using var result = Resolve("a.xml");

        await AssertDenied(result);
    }

    [Test]
    public async Task Resolve_FoundStream_IsReadOnly(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var result = Resolve("a.xml");

        await Assert.That(result.Stream!.CanRead).IsTrue();
        await Assert.That(result.Stream!.CanWrite).IsFalse();
    }

    [Test]
    public async Task Resolve_FoundStream_IsDisposedWithTheResult(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Resolve("a.xml");
        var stream = result.Stream!;

        result.Dispose();

        await Assert.That(stream.CanRead).IsFalse();
    }

    [Test]
    public async Task Resolve_SystemId_LetsNestedReferencesResolveInsideRoot(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var first = Resolve("sub/b.xml");

        await Assert.That(first.SystemId).IsNotNull();
        using var nested = Resolve("../a.xml", baseUri: first.SystemId);

        await Assert.That(nested.Status).IsEqualTo(ResourceStatus.Found);

        using var escaping = Resolve("../../outside.xml", baseUri: first.SystemId);

        await AssertDenied(escaping);
    }

    [Test]
    public async Task Resolve_FileThatIsASymbolicLinkInsideRoot_IsServed(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryCreateFileLink(Path.Combine(_root, "link-in.xml"), Path.Combine(_root, "sub", "b.xml")))
        {
            Skip.Test("The operating system denies the creation of symbolic links.");
        }

        using var result = Resolve("link-in.xml");

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Found);
    }

    [Test]
    public async Task Resolve_FileThatIsASymbolicLinkOutsideRoot_IsDenied(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryCreateFileLink(Path.Combine(_root, "link-out.xml"), Path.Combine(_workspace, "outside.xml")))
        {
            Skip.Test("The operating system denies the creation of symbolic links.");
        }

        using var result = Resolve("link-out.xml");

        await AssertDenied(result);
    }

    [Test]
    public async Task Resolve_DirectoryThatIsASymbolicLinkOutsideRoot_IsDenied(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryCreateDirectoryLink(Path.Combine(_root, "dir-out"), _workspace))
        {
            Skip.Test("The operating system denies the creation of symbolic links.");
        }

        using var result = Resolve("dir-out/outside.xml");

        await AssertDenied(result);
    }

    [Test]
    public async Task Resolve_DirectoryThatIsASymbolicLinkInsideRoot_IsServed(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryCreateDirectoryLink(Path.Combine(_root, "dir-in"), Path.Combine(_root, "sub")))
        {
            Skip.Test("The operating system denies the creation of symbolic links.");
        }

        using var result = Resolve("dir-in/b.xml");

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Found);
    }

    [Test]
    public async Task Resolve_SymbolicLinkCycle_IsDenied(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (
            !TryCreateFileLink(Path.Combine(_root, "loop-a.xml"), Path.Combine(_root, "loop-b.xml"))
            || !TryCreateFileLink(Path.Combine(_root, "loop-b.xml"), Path.Combine(_root, "loop-a.xml"))
        )
        {
            Skip.Test("The operating system denies the creation of symbolic links.");
        }

        using var result = Resolve("loop-a.xml");

        await Assert.That(result.Status).IsNotEqualTo(ResourceStatus.Found);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("  ")]
    public async Task Constructor_NullOrWhiteSpaceRoot_Throws(string? root, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(() => new SandboxedFileSystemResourceResolver(root!)).Throws<ArgumentException>();
    }

    private static bool TryCreateFileLink(string link, string target)
    {
        try
        {
            _ = File.CreateSymbolicLink(link, target);
            return true;
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static bool TryCreateDirectoryLink(string link, string target)
    {
        try
        {
            _ = Directory.CreateSymbolicLink(link, target);
            return true;
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}
