namespace NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

using NetEvolve.Schemas.Conformance.Fetch;

public class ConformanceCacheTests
{
    [Test]
    public async Task ResolveRoot_OverrideValue_Wins(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();

        await Assert.That(ConformanceCache.ResolveRoot(temp.Path)).IsEqualTo(temp.Path);
    }

    [Test]
    public async Task ResolveRoot_WithoutOverride_UsesArtifactsFolderInRepository(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = ConformanceCache.ResolveRoot(null);
        var repository = Path.GetDirectoryName(Path.GetDirectoryName(root))!;

        await Assert.That(root).EndsWith(Path.Combine("artifacts", "conformance-cache"));
        await Assert.That(File.Exists(Path.Combine(repository, "Schemas.slnx"))).IsTrue();
    }

    [Test]
    public async Task ResolveRoot_Default_ReadsEnvironmentVariable(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var previous = Environment.GetEnvironmentVariable(ConformanceCache.EnvironmentVariable);
        using var temp = new TempDirectory();
        try
        {
            Environment.SetEnvironmentVariable(ConformanceCache.EnvironmentVariable, temp.Path);

            await Assert.That(ConformanceCache.ResolveRoot()).IsEqualTo(temp.Path);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConformanceCache.EnvironmentVariable, previous);
        }
    }
}
