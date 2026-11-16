namespace NetEvolve.Schemas.Tests.PublicApi.Shared;

internal sealed class PublicApiHarnessTests
{
    [Test]
    public async Task LibraryTargetFramework_NamesTheBuildTheLegLoads()
    {
        var expected = Environment.Version.Major switch
        {
            8 => "netstandard2.1",
            9 => "netstandard2.1",
            var major => $"net{major}.0",
        };

        await Assert.That(PublicApiHarness.LibraryTargetFramework).IsEqualTo(expected);
    }

    [Test]
    [Arguments(".NETStandard,Version=v2.0", "netstandard2.0")]
    [Arguments(".NETStandard,Version=v2.1", "netstandard2.1")]
    [Arguments(".NETCoreApp,Version=v10.0", "net10.0")]
    public async Task ToMoniker_MapsFrameworkName(string frameworkName, string expected) =>
        await Assert.That(PublicApiHarness.ToMoniker(frameworkName)).IsEqualTo(expected);

    [Test]
    public async Task SnapshotName_CombinesAssemblyAndFramework() =>
        await Assert
            .That(PublicApiHarness.SnapshotName("NetEvolve.Schemas.Xml", "net10.0"))
            .IsEqualTo("NetEvolve.Schemas.Xml.net10.0");

    [Test]
    public async Task VerifiedFiles_OfDifferentAssemblies_NeverShareAFolder()
    {
        var projectDirectory = PublicApiHarness.ProjectDirectory();

        var conflicts = PublicApiHarness.FindSharedFolders(projectDirectory);

        await Assert.That(conflicts).IsEmpty();
    }

    [Test]
    public async Task FindSharedFolders_ReportsFolderWithTwoAssemblies(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _ = Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(directory, "A.Core.net10.0.verified.txt"),
                string.Empty,
                cancellationToken
            );
            await File.WriteAllTextAsync(
                Path.Combine(directory, "B.Core.net10.0.verified.txt"),
                string.Empty,
                cancellationToken
            );

            var conflicts = PublicApiHarness.FindSharedFolders(directory);

            await Assert.That(conflicts).Count().IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
