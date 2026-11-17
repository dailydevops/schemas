namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Core;

using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.Testing.Fakes;

public class TryAddSemanticsTests
{
    [Test]
    public async Task Plain_SecondRegistration_IsNoOp()
    {
        var first = new FakeValidator();
        var secondCalls = 0;
        var services = new ServiceCollection().AddSchemaValidator(_ => first);
        var count = services.Count;

        _ = services.AddSchemaValidator(provider =>
        {
            _ = Interlocked.Increment(ref secondCalls);
            return new FakeValidator();
        });
        using var provider = services.BuildServiceProvider();

        await Assert.That(services.Count).IsEqualTo(count);
        await Assert.That(provider.GetRequiredService<FakeValidator>()).IsSameReferenceAs(first);
        await Assert.That(secondCalls).IsEqualTo(0);
    }

    [Test]
    public async Task Keyed_SecondRegistrationWithSameKey_IsNoOp()
    {
        var first = new FakeValidator();
        var services = new ServiceCollection().AddKeyedSchemaValidator("a", (_, _) => first);
        var count = services.Count;

        _ = services.AddKeyedSchemaValidator("a", (_, _) => new FakeValidator());
        using var provider = services.BuildServiceProvider();

        await Assert.That(services.Count).IsEqualTo(count);
        await Assert.That(provider.GetRequiredKeyedService<FakeValidator>("a")).IsSameReferenceAs(first);
        await Assert.That(provider.GetRequiredKeyedService<ISchemaValidator>("a")).IsSameReferenceAs(first);
    }

    [Test]
    public async Task SameTypeWithDifferentKey_AndDifferentTypeWithSameKey_BothRegister()
    {
        using var provider = new ServiceCollection()
            .AddKeyedSchemaValidator("a", (_, _) => new FakeValidator())
            .AddKeyedSchemaValidator("b", (_, _) => new FakeValidator())
            .AddKeyedSchemaValidator<ISchemaValidator>("a", (_, _) => new FakeValidator())
            .BuildServiceProvider();

        await Assert.That(provider.GetRequiredKeyedService<FakeValidator>("a")).IsNotNull();
        await Assert.That(provider.GetRequiredKeyedService<FakeValidator>("b")).IsNotNull();
    }

    [Test]
    public async Task PlainAndKeyedRegistration_AreIndependent()
    {
        using var provider = new ServiceCollection()
            .AddSchemaValidator(_ => new FakeValidator())
            .AddKeyedSchemaValidator("a", (_, _) => new FakeValidator())
            .BuildServiceProvider();

        await Assert
            .That(provider.GetRequiredService<FakeValidator>())
            .IsNotSameReferenceAs(provider.GetRequiredKeyedService<FakeValidator>("a"));
    }
}
