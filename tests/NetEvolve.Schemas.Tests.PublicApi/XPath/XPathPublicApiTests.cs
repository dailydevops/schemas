namespace NetEvolve.Schemas.Tests.PublicApi.XPath;

using NetEvolve.Schemas.Tests.PublicApi.Shared;
using NetEvolve.Schemas.XPath.Api;

/// <summary>Pins the public API of the XPath assembly per library target framework; any unapproved change fails the verification.</summary>
public class XPathPublicApiTests
{
    private const string AssemblyName = "NetEvolve.Schemas.XPath";

    [Test]
    public async Task PublicApi_XPath_MatchesApprovedSnapshot() =>
        await PublicApiHarness.VerifyAsync(typeof(XPathExpression).Assembly, AssemblyName, [AssemblyName]);
}
