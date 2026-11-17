namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Core;

using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.Testing.Fakes;

public class KeyedResolutionTests
{
    [Test]
    public async Task TwoKeys_ResolveDistinctSingletons()
    {
        using var provider = new ServiceCollection()
            .AddKeyedSchemaValidator("a", (_, _) => new FakeValidator())
            .AddKeyedSchemaValidator("b", (_, _) => new FakeValidator())
            .BuildServiceProvider();

        var a = provider.GetRequiredKeyedService<FakeValidator>("a");
        var b = provider.GetRequiredKeyedService<FakeValidator>("b");

        await Assert.That(a).IsNotSameReferenceAs(b);
        await Assert.That(provider.GetRequiredKeyedService<FakeValidator>("a")).IsSameReferenceAs(a);
    }

    [Test]
    public async Task UnkeyedLookup_IsIndependent()
    {
        using var provider = new ServiceCollection()
            .AddKeyedSchemaValidator("a", (_, _) => new FakeValidator())
            .BuildServiceProvider();

        await Assert.That(provider.GetService<FakeValidator>()).IsNull();
        await Assert.That(provider.GetService<ISchemaValidator>()).IsNull();
    }

    [Test]
    public async Task KeyedSchemaValidator_ForwardsToKeyedInstance()
    {
        using var provider = new ServiceCollection()
            .AddKeyedSchemaValidator("a", (_, _) => new FakeValidator())
            .BuildServiceProvider();

        await Assert
            .That(provider.GetRequiredKeyedService<ISchemaValidator>("a"))
            .IsSameReferenceAs(provider.GetRequiredKeyedService<FakeValidator>("a"));
    }

    [Test]
    public async Task KeyedRegistrationOfSchemaValidator_AddsOneDescriptor()
    {
        var services = new ServiceCollection().AddKeyedSchemaValidator<ISchemaValidator>("a", (_, _) =>
            new FakeValidator()
        );

        await Assert.That(services.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Key_ReachesFactory()
    {
        object? seen = null;
        using var provider = new ServiceCollection()
            .AddKeyedSchemaValidator("rules", (_, key) =>
            {
                seen = key;
                return new FakeValidator();
            })
            .BuildServiceProvider();

        _ = provider.GetRequiredKeyedService<FakeValidator>("rules");

        await Assert.That(seen).IsEqualTo("rules");
    }
}
