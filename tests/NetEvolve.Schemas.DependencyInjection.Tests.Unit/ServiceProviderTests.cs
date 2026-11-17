namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit;

using Microsoft.Extensions.DependencyInjection;

public class ServiceProviderTests
{
    [Test]
    public async Task BuildServiceProvider_ResolvesRegisteredService()
    {
        var services = new ServiceCollection();
        _ = services.AddSingleton("registered");

        using var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetRequiredService<string>()).IsEqualTo("registered");
    }
}
