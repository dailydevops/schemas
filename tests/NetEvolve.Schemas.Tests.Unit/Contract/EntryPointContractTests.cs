namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System.Reflection;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Fakes;

public class EntryPointContractTests
{
    [Test]
    public async Task SchemaValidator_IsStaticClass()
    {
        var type = typeof(SchemaValidator);

        await Assert.That(type.IsAbstract && type.IsSealed).IsTrue();
        await Assert.That(type.IsPublic).IsTrue();
        await Assert.That(type.Namespace).IsEqualTo("NetEvolve.Schemas");
    }

    [Test]
    public async Task ExtensionMember_And_NamedExtensionClass_ReturnSameBuilderType()
    {
        var viaMember = SchemaValidator.ForFake();
        var viaClass = FakeSchemaValidatorExtensions.ForFake();
        var method = typeof(FakeSchemaValidatorExtensions).GetMethod(
            "ForFake",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [],
            modifiers: null
        );

        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(FakeBuilder));
        await Assert.That(viaMember.GetType()).IsEqualTo(viaClass.GetType());
    }
}
