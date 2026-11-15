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
    public async Task PublicApi_WithSeededMember_DiffersFromVerifiedFile()
    {
        // The seeded type is public but absent from the verified file, so verification would fail.
        var seeded = PublicApiHarness.Generate(typeof(FixtureApi).Assembly, [ApprovedNamespace, SeededNamespace]);
        var verified = await File.ReadAllTextAsync(PublicApiHarness.VerifiedFilePath(AssemblyName));

        await Assert.That(seeded).Contains(nameof(SeededMember));
        await Assert.That(seeded.TrimEnd()).IsNotEqualTo(verified.TrimStart('﻿').TrimEnd());
    }
}
