namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

internal sealed class FixtureDirectory : IDisposable
{
    public FixtureDirectory() =>
        Path = FixtureSuite.Create(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "xsts-runner-" + Guid.NewGuid().ToString("N"))
        );

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
