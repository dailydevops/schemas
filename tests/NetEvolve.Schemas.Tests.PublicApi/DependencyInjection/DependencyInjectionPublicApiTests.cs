namespace NetEvolve.Schemas.Tests.PublicApi.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.Tests.PublicApi.Fixture.Seeded;
using NetEvolve.Schemas.Tests.PublicApi.Shared;

internal sealed class DependencyInjectionPublicApiTests
{
    private const string AssemblyName = "NetEvolve.Schemas.DependencyInjection";
    private const string SeededNamespace = "NetEvolve.Schemas.Tests.PublicApi.Fixture.Seeded";

    [Test]
    public async Task PublicApi_MatchesVerifiedFile() =>
        await PublicApiHarness.VerifyAsync(
            typeof(SchemaValidatorServiceCollectionExtensions).Assembly,
            AssemblyName,
            ["Microsoft.Extensions.DependencyInjection"]
        );

    [Test]
    public async Task PublicApi_WithSeededMember_FailsVerification()
    {
        // The seeded type is public but absent from the verified file, so Verify must reject it.
        var received = PublicApiHarness
            .VerifiedFilePath(AssemblyName)
            .Replace(".verified.", ".received.", StringComparison.Ordinal);
        Exception? failure = null;
        try
        {
            await PublicApiHarness.VerifyAsync(
                typeof(SeededMember).Assembly,
                AssemblyName,
                [SeededNamespace],
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
