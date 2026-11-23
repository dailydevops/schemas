namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() =>
        Path = Directory
            .CreateDirectory(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qt3-" + Guid.NewGuid().ToString("N"))
            )
            .FullName;

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
