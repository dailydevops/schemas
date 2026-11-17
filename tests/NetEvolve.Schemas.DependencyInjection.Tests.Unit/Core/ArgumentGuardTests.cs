namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Core;

using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.Testing.Fakes;

public class ArgumentGuardTests
{
    [Test]
    public async Task Plain_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        _ = await Assert
            .That(() => services.AddSchemaValidator(_ => new FakeValidator()))
            .Throws<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Test]
    public async Task Plain_NullFactory_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddSchemaValidator<FakeValidator>(null!))
            .Throws<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Test]
    public async Task Keyed_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        _ = await Assert
            .That(() => services.AddKeyedSchemaValidator("a", (_, _) => new FakeValidator()))
            .Throws<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Test]
    public async Task Keyed_NullServiceKey_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddKeyedSchemaValidator(null!, (_, _) => new FakeValidator()))
            .Throws<ArgumentNullException>()
            .WithParameterName("serviceKey");
    }

    [Test]
    public async Task Keyed_NullFactory_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddKeyedSchemaValidator<FakeValidator>("a", null!))
            .Throws<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Test]
    public async Task Keyed_AnyKey_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = await Assert
            .That(() => services.AddKeyedSchemaValidator(KeyedService.AnyKey, (_, _) => new FakeValidator()))
            .Throws<ArgumentException>()
            .WithParameterName("serviceKey");
        await Assert.That(services).IsEmpty();
    }
}
