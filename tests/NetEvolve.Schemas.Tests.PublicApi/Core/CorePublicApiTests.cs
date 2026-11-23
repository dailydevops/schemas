namespace NetEvolve.Schemas.Tests.PublicApi.Core;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.Tests.PublicApi.Shared;
using TUnit.Assertions.Enums;

/// <summary>Locks the public API of the <c>NetEvolve.Schemas</c> core assembly per library target framework.</summary>
internal sealed class CorePublicApiTests
{
    private const string AssemblyName = "NetEvolve.Schemas";

    private static readonly string[] LibraryTargetFrameworks = ["netstandard2.0", "net10.0", "net11.0"];

    [Test]
    public async Task PublicApi_MatchesVerifiedFile() =>
        await PublicApiHarness.VerifyAsync(typeof(ISchemaValidator).Assembly, AssemblyName, [AssemblyName]);

    [Test]
    public async Task VerifiedFiles_ExistForEveryLibraryTargetFramework()
    {
        var missing = LibraryTargetFrameworks.Where(tfm => !File.Exists(VerifiedFile(tfm))).ToArray();

        await Assert.That(missing).IsEmpty();
    }

    [Test]
    [Arguments("netstandard2.0")]
    [Arguments("net11.0")]
    public async Task VerifiedFile_IsLineForLineIdenticalToNet10(
        string targetFramework,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baseline = await File.ReadAllLinesAsync(VerifiedFile("net10.0"), cancellationToken);
        var other = await File.ReadAllLinesAsync(VerifiedFile(targetFramework), cancellationToken);

        // No difference is required by the target framework today. A required difference becomes an
        // explicit per-framework allow-list entry here, each with its reason.
        await Assert.That(other).IsEquivalentTo(baseline, CollectionOrdering.Matching);
    }

    private static string VerifiedFile(string targetFramework, [CallerFilePath] string sourceFile = "") =>
        Path.Combine(
            Path.GetDirectoryName(sourceFile)!,
            PublicApiHarness.SnapshotName(AssemblyName, targetFramework) + ".verified.txt"
        );
}
