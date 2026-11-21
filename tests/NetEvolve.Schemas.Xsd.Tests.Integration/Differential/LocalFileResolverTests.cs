namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>Pins decision D9: the baseline reads local files only.</summary>
public class LocalFileResolverTests
{
    [Test]
    public async Task GetEntity_HttpUri_ThrowsNotSupported(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new LocalFileResolver();

        _ = await Assert
            .That(() => resolver.GetEntity(new Uri("http://127.0.0.1:9/xml.xsd"), null, null))
            .Throws<NotSupportedException>();
    }

    [Test]
    public async Task GetEntity_FileUri_ReturnsReadableStream(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var path = directory.Write("part.xsd", "<schema/>");

        await Assert.That(ReadThroughTheResolver(path)).IsEqualTo("<schema/>");
    }

    private static string ReadThroughTheResolver(string path)
    {
        using var stream = (Stream)new LocalFileResolver().GetEntity(new Uri(path), null, null)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
