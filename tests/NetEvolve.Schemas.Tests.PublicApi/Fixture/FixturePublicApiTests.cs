namespace NetEvolve.Schemas.Tests.PublicApi.Fixture;

using NetEvolve.Schemas.Tests.PublicApi.Fixture.Approved;
using NetEvolve.Schemas.Tests.PublicApi.Fixture.Seeded;
using NetEvolve.Schemas.Tests.PublicApi.Shared;

/// <summary>Proves the harness on the fixture namespaces compiled into this test assembly.</summary>
internal sealed class FixturePublicApiTests
{
    private const string AssemblyName = "NetEvolve.Schemas.Fixture";
    private const string ApprovedNamespace = "NetEvolve.Schemas.Tests.PublicApi.Fixture.Approved";
    private const string SeededNamespace = "NetEvolve.Schemas.Tests.PublicApi.Fixture.Seeded";

    [Test]
    public async Task PublicApi_MatchesVerifiedFile() =>
        await PublicApiHarness.VerifyAsync(typeof(FixtureApi).Assembly, AssemblyName, [ApprovedNamespace]);

    [Test]
    public async Task PublicApi_WithSeededMember_FailsVerification(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // The seeded type is public but absent from the verified file, so Verify must reject it.
        var received = PublicApiHarness
            .VerifiedFilePath(AssemblyName)
            .Replace(".verified.", ".received.", StringComparison.Ordinal);
        Exception? failure = null;
        try
        {
            await PublicApiHarness.VerifyAsync(
                typeof(FixtureApi).Assembly,
                AssemblyName,
                [ApprovedNamespace, SeededNamespace],
                requireUniquePrefix: false
            );
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            File.Delete(received);
        }

        await Assert.That(failure?.Message).Contains(nameof(SeededMember));
    }
}
