namespace NetEvolve.Schemas.Tests.PublicApi.Fixture;

using NetEvolve.Schemas.Tests.PublicApi.Fixture.Approved;
using NetEvolve.Schemas.Tests.PublicApi.Fixture.Seeded;
using NetEvolve.Schemas.Tests.PublicApi.Harness;

/// <summary>Proves the harness on the fixture namespaces compiled into this test assembly.</summary>
public class FixturePublicApiTests
{
    private const string AssemblyName = "NetEvolve.Schemas.Fixture";
    private const string ApprovedNamespace = "NetEvolve.Schemas.Tests.PublicApi.Fixture.Approved";
    private const string SeededNamespace = "NetEvolve.Schemas.Tests.PublicApi.Fixture.Seeded";

    [Test]
    public async Task PublicApi_MatchesVerifiedFile() =>
        await PublicApiHarness.VerifyAsync(typeof(FixtureApi).Assembly, AssemblyName, [ApprovedNamespace]);

    [Test]
    public async Task PublicApi_WithSeededMember_FailsVerification()
    {
        // The seeded type is public but absent from the verified file, so verification must fail.
        _ = typeof(SeededMember);

        await Assert
            .That(async () =>
                await PublicApiHarness.VerifyAsync(
                    typeof(FixtureApi).Assembly,
                    AssemblyName,
                    [ApprovedNamespace, SeededNamespace]
                )
            )
            .ThrowsException();

        PublicApiHarness.DeleteReceivedFiles();
    }
}
