namespace NetEvolve.Schemas.Tests.PublicApi.Xml;

using NetEvolve.Schemas.Tests.PublicApi.Shared;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Pins the public API of the Xml assembly per library target framework; any unapproved change fails the verification.</summary>
public class XmlPublicApiTests
{
    private const string AssemblyName = "NetEvolve.Schemas.Xml";

    // The whole assembly: the DTD entry point lives in NetEvolve.Schemas (design #103, AP-2 and AP-3), so no narrower prefix selects it.
    [Test]
    public async Task PublicApi_MatchesVerifiedFile() =>
        await PublicApiHarness.VerifyAsync(typeof(XmlSymbol).Assembly, AssemblyName, ["NetEvolve.Schemas"]);

    [Test]
    public async Task Entry_Builder_Validator_LiveInXmlAssembly()
    {
        var assembly = typeof(XmlSymbol).Assembly;

        await Assert.That(assembly.GetName().Name).IsEqualTo(AssemblyName);
        await Assert.That(typeof(IDtdValidator).Assembly).IsEqualTo(assembly);
        await Assert.That(typeof(DtdValidatorBuilder).Assembly).IsEqualTo(assembly);
        await Assert.That(typeof(DtdSchemaValidatorExtensions).Assembly).IsEqualTo(assembly);
    }
}
