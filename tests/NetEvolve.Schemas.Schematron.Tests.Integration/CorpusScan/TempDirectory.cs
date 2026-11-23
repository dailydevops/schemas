namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() =>
        Path = Directory
            .CreateDirectory(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "corpus-scan-" + Guid.NewGuid().ToString("N"))
            )
            .FullName;

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
