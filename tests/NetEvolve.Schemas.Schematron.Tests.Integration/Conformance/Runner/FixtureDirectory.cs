namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

internal sealed class FixtureDirectory : IDisposable
{
    public FixtureDirectory() =>
        Path = FixtureSuite.Create(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "schematron-runner-" + Guid.NewGuid().ToString("N"))
        );

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
