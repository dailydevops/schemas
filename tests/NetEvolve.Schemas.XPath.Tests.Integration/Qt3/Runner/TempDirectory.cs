namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Path = Directory.CreateTempSubdirectory("qt3-").FullName;

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
