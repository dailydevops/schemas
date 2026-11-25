namespace NetEvolve.Schemas.Tests.PublicApi.Schematron;

using NetEvolve.Schemas.Tests.PublicApi.Shared;

/// <summary>Pins the public API of the Schematron assembly per library target framework; any unapproved change fails the verification.</summary>
public class SchematronPublicApiTests
{
    private const string AssemblyName = "NetEvolve.Schemas.Schematron";

    [Test]
    public async Task PublicApi_Schematron_MatchesApprovedSnapshot() =>
        await PublicApiHarness.VerifyAsync(
            typeof(ISchematronValidator).Assembly,
            AssemblyName,
            ["NetEvolve.Schemas", "Microsoft.Extensions.DependencyInjection"]
        );

    [Test]
    public async Task Surface_OnlyDocumentedNamespacesArePublic()
    {
        var undocumented = new List<string>();

        foreach (var type in typeof(ISchematronValidator).Assembly.GetExportedTypes())
        {
            if (
                type.Namespace
                is not (
                    "NetEvolve.Schemas"
                    or "NetEvolve.Schemas.Schematron"
                    or "NetEvolve.Schemas.Schematron.Model"
                    or "NetEvolve.Schemas.Schematron.Svrl"
                    or "Microsoft.Extensions.DependencyInjection"
                )
            )
            {
                undocumented.Add(type.FullName!);
            }
        }

        await Assert.That(undocumented).IsEmpty();
    }

    [Test]
    public async Task Entry_Builder_Validator_LiveInSchematronAssembly()
    {
        var assembly = typeof(ISchematronValidator).Assembly;

        await Assert.That(assembly.GetName().Name).IsEqualTo(AssemblyName);
        await Assert.That(typeof(SchematronValidatorBuilder).Assembly).IsEqualTo(assembly);
        await Assert.That(typeof(SchematronSchemaValidatorExtensions).Assembly).IsEqualTo(assembly);
    }
}
