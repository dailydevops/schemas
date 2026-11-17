namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Path = Directory.CreateTempSubdirectory("corpus-scan-").FullName;

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
