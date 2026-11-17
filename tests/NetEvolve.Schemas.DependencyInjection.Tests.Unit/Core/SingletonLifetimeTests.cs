namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Core;

using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.Testing.Fakes;

public class SingletonLifetimeTests
{
    [Test]
    public async Task Resolve_Twice_ReturnsSameReference()
    {
        using var provider = new ServiceCollection()
            .AddSchemaValidator(_ => new FakeValidator())
            .BuildServiceProvider();

        await Assert
            .That(provider.GetRequiredService<FakeValidator>())
            .IsSameReferenceAs(provider.GetRequiredService<FakeValidator>());
    }

    [Test]
    public async Task Descriptor_HasSingletonLifetime()
    {
        var services = new ServiceCollection().AddSchemaValidator(_ => new FakeValidator());

        await Assert.That(services.Single().Lifetime).IsEqualTo(ServiceLifetime.Singleton);
    }

    [Test]
    public async Task Factory_IsNotCalledAtRegistrationOrBuild()
    {
        var calls = 0;
        var services = new ServiceCollection().AddSchemaValidator(provider =>
        {
            _ = Interlocked.Increment(ref calls);
            return new FakeValidator();
        });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        await Assert.That(calls).IsEqualTo(0);
    }

    [Test]
    public async Task Factory_RunsOnceAcrossConcurrentFirstResolves()
    {
        var calls = 0;
        using var provider = new ServiceCollection()
            .AddSchemaValidator(provider =>
            {
                _ = Interlocked.Increment(ref calls);
                return new FakeValidator();
            })
            .BuildServiceProvider();

        var resolved = await Task.WhenAll(
            Enumerable.Range(0, 64).Select(_ => Task.Run(() => provider.GetRequiredService<FakeValidator>()))
        );

        await Assert.That(calls).IsEqualTo(1);
        await Assert.That(resolved.Distinct().Count()).IsEqualTo(1);
    }
}
