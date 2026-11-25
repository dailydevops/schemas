namespace NetEvolve.Schemas.Tests.PublicApi.Xsd;

using NetEvolve.Schemas.Tests.PublicApi.Shared;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Pins the public API of the Xsd assembly per library target framework; any unapproved change fails the verification.</summary>
public class XsdPublicApiTests
{
    private const string AssemblyName = "NetEvolve.Schemas.Xsd";

    [Test]
    public async Task PublicApi_Xsd_MatchesApprovedSnapshot() =>
        await PublicApiHarness.VerifyAsync(
            typeof(IXsdValidator).Assembly,
            AssemblyName,
            ["NetEvolve.Schemas", "Microsoft.Extensions.DependencyInjection"]
        );

    [Test]
    public async Task PublicApi_XsdVersion_IsPublicInXml()
    {
        var type = typeof(XsdVersion);

        await Assert.That(type.IsPublic).IsTrue();
        await Assert.That(type.Assembly.GetName().Name).IsEqualTo("NetEvolve.Schemas.Xml");
    }

    [Test]
    public async Task Surface_OnlyDocumentedNamespacesArePublic()
    {
        var undocumented = new List<string>();

        foreach (var type in typeof(IXsdValidator).Assembly.GetExportedTypes())
        {
            if (
                type.Namespace
                is not ("NetEvolve.Schemas" or "NetEvolve.Schemas.Xsd" or "Microsoft.Extensions.DependencyInjection")
            )
            {
                undocumented.Add(type.FullName!);
            }
        }

        await Assert.That(undocumented).IsEmpty();
    }

    [Test]
    public async Task Entry_Builder_Validator_LiveInXsdAssembly()
    {
        var assembly = typeof(IXsdValidator).Assembly;

        await Assert.That(assembly.GetName().Name).IsEqualTo(AssemblyName);
        await Assert.That(typeof(XsdValidatorBuilder).Assembly).IsEqualTo(assembly);
        await Assert.That(typeof(XsdSchemaValidatorExtensions).Assembly).IsEqualTo(assembly);
    }
}
