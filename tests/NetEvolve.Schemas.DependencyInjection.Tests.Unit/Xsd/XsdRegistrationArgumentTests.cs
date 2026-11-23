namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Xsd;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using static NetEvolve.Schemas.DependencyInjection.Tests.Unit.Xsd.XsdRegistrationTestSupport;

public class XsdRegistrationArgumentTests
{
    [Test]
    public async Task Plain_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        _ = await Assert
            .That(() => services.AddXsdValidator(_ => Open(RootSchema)))
            .Throws<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Test]
    public async Task Plain_NullSchema_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddXsdValidator(null!))
            .Throws<ArgumentNullException>()
            .WithParameterName("schema");
    }

    [Test]
    public async Task Keyed_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        _ = await Assert
            .That(() => services.AddKeyedXsdValidator("k", (_, _) => Open(RootSchema)))
            .Throws<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Test]
    public async Task Keyed_NullServiceKey_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddKeyedXsdValidator(null!, (_, _) => Open(RootSchema)))
            .Throws<ArgumentNullException>()
            .WithParameterName("serviceKey");
    }

    [Test]
    public async Task Keyed_NullSchema_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddKeyedXsdValidator("k", null!))
            .Throws<ArgumentNullException>()
            .WithParameterName("schema");
    }

    [Test]
    public async Task Keyed_AnyKey_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        await Assert
            .That(() => services.AddKeyedXsdValidator(KeyedService.AnyKey, (_, _) => Open(RootSchema)))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Registration_ReturnsTheSameServiceCollection()
    {
        var services = new ServiceCollection();

        await Assert.That(services.AddXsdValidator(_ => Open(RootSchema))).IsSameReferenceAs(services);
        await Assert.That(services.AddKeyedXsdValidator("k", (_, _) => Open(RootSchema))).IsSameReferenceAs(services);
    }

    [Test]
    public async Task Extensions_AreStaticInPlatformNamespace_WithExactlyThePlainAndKeyedMethods()
    {
        var extensions = typeof(XsdValidatorServiceCollectionExtensions);
        var methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        await Assert.That(extensions.IsAbstract && extensions.IsSealed).IsTrue();
        await Assert.That(extensions.Namespace).IsEqualTo("Microsoft.Extensions.DependencyInjection");
        await Assert
            .That(methods.Select(m => m.Name).OrderBy(name => name, StringComparer.Ordinal))
            .IsEquivalentTo(["AddKeyedXsdValidator", "AddXsdValidator"]);
        foreach (var method in methods)
        {
            await Assert.That(method.ReturnType).IsEqualTo(typeof(IServiceCollection));
        }
    }
}
