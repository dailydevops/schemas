namespace NetEvolve.Schemas.Tests.PublicApi.Xml;

using NetEvolve.Schemas.Tests.PublicApi.Shared;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Pins the public API of the Xml assembly per library target framework; any unapproved change fails the verification.</summary>
public class XmlPublicApiTests
{
    private const string AssemblyName = "NetEvolve.Schemas.Xml";

    [Test]
    public async Task PublicApi_MatchesVerifiedFile() =>
        await PublicApiHarness.VerifyAsync(
            typeof(XmlSymbol).Assembly,
            AssemblyName,
            [
                "NetEvolve.Schemas.Xml.Contracts",
                "NetEvolve.Schemas.Xml.Datatypes",
                "NetEvolve.Schemas.Xml.Interop",
                "NetEvolve.Schemas.Xml.Regex",
                "NetEvolve.Schemas.Xml.Writer",
            ]
        );
}
