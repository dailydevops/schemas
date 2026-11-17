namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Core;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

public class RegistrationShapeTests
{
    private static readonly Type _extensions = typeof(SchemaValidatorServiceCollectionExtensions);

    [Test]
    public async Task Extensions_AreStaticInPlatformNamespace()
    {
        await Assert.That(_extensions.IsAbstract && _extensions.IsSealed).IsTrue();
        await Assert.That(_extensions.Namespace).IsEqualTo("Microsoft.Extensions.DependencyInjection");
        await Assert.That(_extensions.Assembly.GetName().Name).IsEqualTo("NetEvolve.Schemas.DependencyInjection");
    }

    [Test]
    public async Task Extensions_ExposeExactlyThePlainAndKeyedMethods()
    {
        var methods = _extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        await Assert
            .That(methods.Select(m => m.Name).Order(StringComparer.Ordinal))
            .IsEquivalentTo(["AddKeyedSchemaValidator", "AddSchemaValidator"]);
        foreach (var method in methods)
        {
            var constraints = method.GetGenericArguments().Single().GetGenericParameterConstraints();
            await Assert.That(constraints).Contains(typeof(NetEvolve.Schemas.ISchemaValidator));
            await Assert.That(method.ReturnType).IsEqualTo(typeof(IServiceCollection));
        }
    }

    [Test]
    public async Task Assembly_DoesNotReferenceSystemXml()
    {
        var references = _extensions.Assembly.GetReferencedAssemblies().Select(a => a.Name);

        await Assert.That(references).DoesNotContain("System.Xml");
    }
}
