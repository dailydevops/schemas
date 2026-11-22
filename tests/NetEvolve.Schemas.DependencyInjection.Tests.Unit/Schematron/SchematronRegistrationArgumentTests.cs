namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using static NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron.SchematronRegistrationTestSupport;

public class SchematronRegistrationArgumentTests
{
    [Test]
    public async Task Plain_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        _ = await Assert
            .That(() => services.AddSchematronValidator(_ => Open(NumberSchema)))
            .Throws<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Test]
    public async Task Plain_NullSchema_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddSchematronValidator(null!))
            .Throws<ArgumentNullException>()
            .WithParameterName("schema");
    }

    [Test]
    public async Task Keyed_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        _ = await Assert
            .That(() => services.AddKeyedSchematronValidator("k", (_, _) => Open(NumberSchema)))
            .Throws<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Test]
    public async Task Keyed_NullServiceKey_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddKeyedSchematronValidator(null!, (_, _) => Open(NumberSchema)))
            .Throws<ArgumentNullException>()
            .WithParameterName("serviceKey");
    }

    [Test]
    public async Task Keyed_NullSchema_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddKeyedSchematronValidator("k", null!))
            .Throws<ArgumentNullException>()
            .WithParameterName("schema");
    }

    [Test]
    public async Task Keyed_AnyKey_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        await Assert
            .That(() => services.AddKeyedSchematronValidator(KeyedService.AnyKey, (_, _) => Open(NumberSchema)))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Registration_ReturnsTheSameServiceCollection()
    {
        var services = new ServiceCollection();

        await Assert.That(services.AddSchematronValidator(_ => Open(NumberSchema))).IsSameReferenceAs(services);
        await Assert
            .That(services.AddKeyedSchematronValidator("k", (_, _) => Open(NumberSchema)))
            .IsSameReferenceAs(services);
    }

    [Test]
    public async Task Extensions_AreStaticInPlatformNamespace_WithExactlyThePlainAndKeyedMethods()
    {
        var extensions = typeof(SchematronValidatorServiceCollectionExtensions);
        var methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        await Assert.That(extensions.IsAbstract && extensions.IsSealed).IsTrue();
        await Assert.That(extensions.Namespace).IsEqualTo("Microsoft.Extensions.DependencyInjection");
        await Assert
            .That(methods.Select(m => m.Name).Order(StringComparer.Ordinal))
            .IsEquivalentTo(["AddKeyedSchematronValidator", "AddSchematronValidator"]);
        foreach (var method in methods)
        {
            await Assert.That(method.ReturnType).IsEqualTo(typeof(IServiceCollection));
        }
    }
}
