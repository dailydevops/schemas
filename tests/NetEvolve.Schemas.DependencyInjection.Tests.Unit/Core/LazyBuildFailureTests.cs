namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Core;

using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.Testing.Fakes;

public class LazyBuildFailureTests
{
    [Test]
    public async Task Plain_RegistrationSucceeds_FailureSurfacesOnFirstResolve()
    {
        var services = new ServiceCollection().AddSchemaValidator(_ => RegistrationTestSupport.Throw());
        using var provider = services.BuildServiceProvider();

        await Assert
            .That(() => provider.GetRequiredService<FakeValidator>())
            .ThrowsExactly<SchemaCompilationException>();
    }

    [Test]
    public async Task Keyed_RegistrationSucceeds_FailureSurfacesOnFirstResolve()
    {
        var services = new ServiceCollection().AddKeyedSchemaValidator("a", (_, _) => RegistrationTestSupport.Throw());
        using var provider = services.BuildServiceProvider();

        await Assert
            .That(() => provider.GetRequiredKeyedService<FakeValidator>("a"))
            .ThrowsExactly<SchemaCompilationException>();
        await Assert
            .That(() => provider.GetRequiredKeyedService<ISchemaValidator>("a"))
            .ThrowsExactly<SchemaCompilationException>();
    }

    [Test]
    public async Task FailedBuild_IsNotCached()
    {
        var calls = 0;
        using var provider = new ServiceCollection()
            .AddSchemaValidator(provider =>
            {
                _ = Interlocked.Increment(ref calls);
                return RegistrationTestSupport.Throw();
            })
            .BuildServiceProvider();

        await Assert
            .That(() => provider.GetRequiredService<FakeValidator>())
            .ThrowsExactly<SchemaCompilationException>();
        await Assert
            .That(() => provider.GetRequiredService<FakeValidator>())
            .ThrowsExactly<SchemaCompilationException>();

        await Assert.That(calls).IsEqualTo(2);
    }
}
