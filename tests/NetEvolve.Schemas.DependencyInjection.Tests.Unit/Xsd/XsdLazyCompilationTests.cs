namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Xsd;

using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using static NetEvolve.Schemas.DependencyInjection.Tests.Unit.Xsd.XsdRegistrationTestSupport;

public class XsdLazyCompilationTests
{
    [Test]
    public async Task Plain_SchemaIsNotLoadedAtRegistration_AndLoadedExactlyOnceAfterSeveralResolves()
    {
        var resolver = new CountingResolver().Add(IncludedSystemId, IncludedSchema);
        var opened = 0;
        var configured = 0;
        var services = new ServiceCollection().AddXsdValidator(
            sp =>
            {
                _ = Interlocked.Increment(ref opened);
                return Open(IncludingSchema);
            },
            (sp, builder) =>
            {
                _ = Interlocked.Increment(ref configured);
                _ = builder.WithResolver(resolver);
            },
            IncludingSystemId
        );
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        await Assert.That(opened).IsEqualTo(0);
        await Assert.That(configured).IsEqualTo(0);
        await Assert.That(resolver.Calls).IsEqualTo(0);

        for (var i = 0; i < 5; i++)
        {
            _ = provider.GetRequiredService<IXsdValidator>();
        }

        await Assert.That(opened).IsEqualTo(1);
        await Assert.That(configured).IsEqualTo(1);
        await Assert.That(resolver.Calls).IsEqualTo(1);
        await Assert.That(IsValid(provider.GetRequiredService<IXsdValidator>(), ValidIncludedRoot)).IsTrue();
    }

    [Test]
    public async Task Keyed_SchemaIsNotLoadedAtRegistration_AndLoadedExactlyOnceAfterSeveralResolves()
    {
        var resolver = new CountingResolver().Add(IncludedSystemId, IncludedSchema);
        var opened = 0;
        var configured = 0;
        var services = new ServiceCollection().AddKeyedXsdValidator(
            "k",
            (_, _) =>
            {
                _ = Interlocked.Increment(ref opened);
                return Open(IncludingSchema);
            },
            (_, _, builder) =>
            {
                _ = Interlocked.Increment(ref configured);
                _ = builder.WithResolver(resolver);
            },
            IncludingSystemId
        );
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        await Assert.That(opened).IsEqualTo(0);
        await Assert.That(configured).IsEqualTo(0);
        await Assert.That(resolver.Calls).IsEqualTo(0);

        for (var i = 0; i < 5; i++)
        {
            _ = provider.GetRequiredKeyedService<IXsdValidator>("k");
            _ = provider.GetRequiredKeyedService<ISchemaValidator>("k");
        }

        await Assert.That(opened).IsEqualTo(1);
        await Assert.That(configured).IsEqualTo(1);
        await Assert.That(resolver.Calls).IsEqualTo(1);
        await Assert.That(IsValid(provider.GetRequiredKeyedService<IXsdValidator>("k"), ValidIncludedRoot)).IsTrue();
    }

    [Test]
    public async Task Stream_IsDisposedAfterTheBuild()
    {
        using var stream = Open(RootSchema);
        using var provider = new ServiceCollection().AddXsdValidator(_ => stream).BuildServiceProvider();

        await Assert.That(stream.IsDisposed).IsFalse();

        _ = provider.GetRequiredService<IXsdValidator>();

        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Strict10_SchemaWithAnXsd11Construct_FailsOnFirstResolveWithASchemaError_AndTheStreamIsDisposed()
    {
        using var stream = Open(Xsd11Schema);
        var services = new ServiceCollection()
            .AddXsdValidator(_ => stream, (_, builder) => builder.WithVersion(XsdVersion.V10))
            .AddKeyedXsdValidator(
                "k",
                (_, _) => Open(Xsd11Schema),
                (_, _, builder) => builder.WithVersion(XsdVersion.V10)
            );
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        var plain = Capture(() => provider.GetRequiredService<IXsdValidator>());
        var keyed = Capture(() => provider.GetRequiredKeyedService<IXsdValidator>("k"));
        var forwarded = Capture(() => provider.GetRequiredKeyedService<ISchemaValidator>("k"));

        foreach (var exception in new[] { plain, keyed, forwarded })
        {
            await Assert.That(exception).IsNotNull();
            await Assert.That(exception!.Issues.Select(i => i.Code.ToString())).Contains(Strict10AssertCode);
        }

        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task InvalidSchemaDocument_RegistrationSucceeds_AndTheFailureSurfacesOnFirstResolve_WithTheStreamDisposed()
    {
        using var stream = Open("<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"><xs:");
        var services = new ServiceCollection().AddXsdValidator(_ => stream);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        await Assert.That(stream.IsDisposed).IsFalse();
        await Assert
            .That(() => provider.GetRequiredService<IXsdValidator>())
            .ThrowsExactly<SchemaCompilationException>();
        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task ConfigureFailure_SurfacesOnFirstResolve_AndTheStreamIsDisposed()
    {
        using var stream = Open(RootSchema);
        using var provider = new ServiceCollection()
            .AddXsdValidator(_ => stream, (_, _) => throw new InvalidOperationException("configure failed"))
            .BuildServiceProvider();

        await Assert
            .That(() => provider.GetRequiredService<IXsdValidator>())
            .ThrowsExactly<InvalidOperationException>();
        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task StreamAddedInTheConfigureCallback_BelongsToTheCallback_AndIsNotDisposedByTheRegistration()
    {
        using var extra = Open(OtherSchema);
        using var provider = new ServiceCollection()
            .AddXsdValidator(_ => Open(RootSchema), (_, builder) => builder.AddSchema(extra))
            .BuildServiceProvider();

        var validator = provider.GetRequiredService<IXsdValidator>();

        await Assert.That(IsValid(validator, ValidOther)).IsTrue();
        await Assert.That(extra.IsDisposed).IsFalse();
    }

    private static SchemaCompilationException? Capture(Func<object> resolve)
    {
        try
        {
            _ = resolve();
            return null;
        }
        catch (SchemaCompilationException exception)
        {
            return exception;
        }
    }
}
