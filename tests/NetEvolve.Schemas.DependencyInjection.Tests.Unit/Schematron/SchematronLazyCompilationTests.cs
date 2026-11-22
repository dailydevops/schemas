namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron;

using Microsoft.Extensions.DependencyInjection;
using static NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron.SchematronRegistrationTestSupport;

public class SchematronLazyCompilationTests
{
    [Test]
    public async Task Keyed_SchemaIsNotLoadedAtRegistration_AndLoadedExactlyOnceAfterSeveralResolves()
    {
        var resolver = new CountingResolver().Add("file:///rules/rule.sch", IncludedRule);
        var opened = 0;
        var services = new ServiceCollection().AddKeyedSchematronValidator(
            "k",
            (_, _) =>
            {
                _ = Interlocked.Increment(ref opened);
                return Open(IncludingSchema);
            },
            (_, _, builder) => builder.WithResolver(resolver),
            "file:///rules/main.sch"
        );
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        await Assert.That(opened).IsEqualTo(0);
        await Assert.That(resolver.Calls).IsEqualTo(0);

        for (var i = 0; i < 5; i++)
        {
            _ = provider.GetRequiredKeyedService<ISchematronValidator>("k");
            _ = provider.GetRequiredKeyedService<ISchemaValidator>("k");
        }

        await Assert.That(opened).IsEqualTo(1);
        await Assert.That(resolver.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task Plain_SchemaIsNotLoadedAtRegistration_AndLoadedExactlyOnceAfterSeveralResolves()
    {
        var resolver = new CountingResolver().Add("file:///rules/rule.sch", IncludedRule);
        var opened = 0;
        var services = new ServiceCollection().AddSchematronValidator(
            sp =>
            {
                _ = Interlocked.Increment(ref opened);
                return Open(IncludingSchema);
            },
            (_, builder) => builder.WithResolver(resolver),
            "file:///rules/main.sch"
        );
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        await Assert.That(opened).IsEqualTo(0);
        await Assert.That(resolver.Calls).IsEqualTo(0);

        for (var i = 0; i < 5; i++)
        {
            _ = provider.GetRequiredService<ISchematronValidator>();
        }

        await Assert.That(opened).IsEqualTo(1);
        await Assert.That(resolver.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task Stream_IsDisposedAfterTheBuild()
    {
        using var stream = Open(NumberSchema);
        using var provider = new ServiceCollection().AddSchematronValidator(_ => stream).BuildServiceProvider();

        _ = provider.GetRequiredService<ISchematronValidator>();

        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task InvalidSchema_RegistrationSucceeds_AndTheFailureSurfacesOnFirstResolve()
    {
        using var stream = Open(InvalidSchema);
        var services = new ServiceCollection()
            .AddSchematronValidator(_ => stream)
            .AddKeyedSchematronValidator("k", (_, _) => Open(InvalidSchema));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        await Assert.That(stream.IsDisposed).IsFalse();
        await Assert
            .That(() => provider.GetRequiredService<ISchematronValidator>())
            .ThrowsExactly<SchemaCompilationException>();
        await Assert.That(stream.IsDisposed).IsTrue();
        await Assert
            .That(() => provider.GetRequiredKeyedService<ISchematronValidator>("k"))
            .ThrowsExactly<SchemaCompilationException>();
        await Assert
            .That(() => provider.GetRequiredKeyedService<ISchemaValidator>("k"))
            .ThrowsExactly<SchemaCompilationException>();
    }

    [Test]
    public async Task ConfigureFailure_SurfacesOnFirstResolve_AndTheStreamIsDisposed()
    {
        using var stream = Open(NumberSchema);
        using var provider = new ServiceCollection()
            .AddSchematronValidator(_ => stream, (_, builder) => builder.WithPhase("missing"))
            .BuildServiceProvider();

        await Assert
            .That(() => provider.GetRequiredService<ISchematronValidator>())
            .ThrowsExactly<SchemaCompilationException>();
        await Assert.That(stream.IsDisposed).IsTrue();
    }
}
