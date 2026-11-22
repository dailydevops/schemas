namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Xsd;

using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using static NetEvolve.Schemas.DependencyInjection.Tests.Unit.Xsd.XsdRegistrationTestSupport;

public class XsdRegistrationTests
{
    [Test]
    public async Task Plain_ResolvesOneSingletonValidator_ThatAcceptsValidAndRejectsInvalidInstances()
    {
        var services = new ServiceCollection().AddXsdValidator(_ => Open(RootSchema));
        using var provider = services.BuildServiceProvider();

        var validator = provider.GetRequiredService<IXsdValidator>();

        await Assert.That(services.Count).IsEqualTo(1);
        await Assert.That(services.Single().Lifetime).IsEqualTo(ServiceLifetime.Singleton);
        await Assert.That(provider.GetRequiredService<IXsdValidator>()).IsSameReferenceAs(validator);
        await Assert.That(provider.GetService<ISchemaValidator>()).IsNull();
        await Assert.That(IsValid(validator, ValidRoot)).IsTrue();
        await Assert.That(IsValid(validator, InvalidRoot)).IsFalse();
    }

    [Test]
    public async Task Keyed_ResolvesOneSingletonValidatorPerKey_ThatAcceptsValidAndRejectsInvalidInstances()
    {
        using var provider = new ServiceCollection()
            .AddKeyedXsdValidator("root", (_, _) => Open(RootSchema))
            .AddKeyedXsdValidator("other", (_, _) => Open(OtherSchema))
            .BuildServiceProvider();

        var root = provider.GetRequiredKeyedService<IXsdValidator>("root");
        var other = provider.GetRequiredKeyedService<IXsdValidator>("other");

        await Assert.That(root).IsNotSameReferenceAs(other);
        await Assert.That(provider.GetRequiredKeyedService<IXsdValidator>("root")).IsSameReferenceAs(root);
        await Assert.That(provider.GetRequiredKeyedService<IXsdValidator>("other")).IsSameReferenceAs(other);
        await Assert.That(IsValid(root, ValidRoot)).IsTrue();
        await Assert.That(IsValid(root, InvalidRoot)).IsFalse();
        await Assert.That(IsValid(other, ValidOther)).IsTrue();
        await Assert.That(IsValid(other, InvalidOther)).IsFalse();
    }

    [Test]
    public async Task Keyed_AlsoRegistersSchemaValidatorUnderTheKey_AndTheUnkeyedLookupIsIndependent()
    {
        using var provider = new ServiceCollection()
            .AddKeyedXsdValidator("root", (_, _) => Open(RootSchema))
            .BuildServiceProvider();

        await Assert
            .That(provider.GetRequiredKeyedService<ISchemaValidator>("root"))
            .IsSameReferenceAs(provider.GetRequiredKeyedService<IXsdValidator>("root"));
        await Assert.That(provider.GetService<IXsdValidator>()).IsNull();
        await Assert.That(provider.GetService<ISchemaValidator>()).IsNull();
    }

    [Test]
    public async Task ConfigureCallback_AddsFurtherSchemas_ToFormOneSchemaSet()
    {
        using var provider = new ServiceCollection()
            .AddXsdValidator(_ => Open(RootSchema), (_, builder) => builder.AddSchema(OtherSchema.AsMemory()))
            .AddKeyedXsdValidator(
                "set",
                (_, _) => Open(RootSchema),
                (_, _, builder) => builder.AddSchema(OtherSchema.AsMemory())
            )
            .BuildServiceProvider();

        IXsdValidator[] validators =
        [
            provider.GetRequiredService<IXsdValidator>(),
            provider.GetRequiredKeyedService<IXsdValidator>("set"),
        ];
        foreach (var validator in validators)
        {
            await Assert.That(IsValid(validator, ValidRoot)).IsTrue();
            await Assert.That(IsValid(validator, ValidOther)).IsTrue();
            await Assert.That(IsValid(validator, InvalidOther)).IsFalse();
        }
    }

    [Test]
    public async Task WithoutAVersionChoice_TheBuiltValidatorReportsVersion11()
    {
        using var provider = new ServiceCollection()
            .AddXsdValidator(_ => Open(RootSchema))
            .AddKeyedXsdValidator("k", (_, _) => Open(RootSchema))
            .BuildServiceProvider();

        await Assert.That(provider.GetRequiredService<IXsdValidator>().Version).IsEqualTo(XsdVersion.V11);
        await Assert.That(provider.GetRequiredKeyedService<IXsdValidator>("k").Version).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    public async Task VersionChosenInTheConfigureCallback_IsReportedByTheBuiltValidator()
    {
        using var provider = new ServiceCollection()
            .AddXsdValidator(_ => Open(RootSchema), (_, builder) => builder.WithVersion(XsdVersion.V10))
            .AddKeyedXsdValidator(
                "k",
                (_, _) => Open(RootSchema),
                (_, _, builder) => builder.WithVersion(XsdVersion.V10)
            )
            .BuildServiceProvider();

        await Assert.That(provider.GetRequiredService<IXsdValidator>().Version).IsEqualTo(XsdVersion.V10);
        await Assert.That(provider.GetRequiredKeyedService<IXsdValidator>("k").Version).IsEqualTo(XsdVersion.V10);
    }

    [Test]
    public async Task Callbacks_ReceiveTheProviderAndTheKey()
    {
        var marker = new List<string>();
        List<string>? seenBySchema = null;
        List<string>? seenByConfigure = null;
        object? keyForSchema = null;
        object? keyForConfigure = null;
        using var provider = new ServiceCollection()
            .AddSingleton(marker)
            .AddKeyedXsdValidator(
                "rules",
                (sp, key) =>
                {
                    seenBySchema = sp.GetRequiredService<List<string>>();
                    keyForSchema = key;
                    return Open(RootSchema);
                },
                (sp, key, _) =>
                {
                    seenByConfigure = sp.GetRequiredService<List<string>>();
                    keyForConfigure = key;
                }
            )
            .BuildServiceProvider();

        _ = provider.GetRequiredKeyedService<IXsdValidator>("rules");

        await Assert.That(seenBySchema).IsSameReferenceAs(marker);
        await Assert.That(seenByConfigure).IsSameReferenceAs(marker);
        await Assert.That(keyForSchema).IsEqualTo("rules");
        await Assert.That(keyForConfigure).IsEqualTo("rules");
    }

    [Test]
    public async Task PlainCallbacks_ReceiveTheProvider()
    {
        var marker = new List<string>();
        List<string>? seenBySchema = null;
        List<string>? seenByConfigure = null;
        using var provider = new ServiceCollection()
            .AddSingleton(marker)
            .AddXsdValidator(
                sp =>
                {
                    seenBySchema = sp.GetRequiredService<List<string>>();
                    return Open(RootSchema);
                },
                (sp, _) => seenByConfigure = sp.GetRequiredService<List<string>>()
            )
            .BuildServiceProvider();

        _ = provider.GetRequiredService<IXsdValidator>();

        await Assert.That(seenBySchema).IsSameReferenceAs(marker);
        await Assert.That(seenByConfigure).IsSameReferenceAs(marker);
    }

    [Test]
    public async Task SystemId_ResolvesRelativeIncludes_ForPlainAndKeyedRegistrations()
    {
        var resolver = new CountingResolver().Add(IncludedSystemId, IncludedSchema);
        using var provider = new ServiceCollection()
            .AddXsdValidator(
                _ => Open(IncludingSchema),
                (_, builder) => builder.WithResolver(resolver),
                IncludingSystemId
            )
            .AddKeyedXsdValidator(
                "k",
                (_, _) => Open(IncludingSchema),
                (_, _, builder) => builder.WithResolver(resolver),
                IncludingSystemId
            )
            .BuildServiceProvider();

        await Assert.That(IsValid(provider.GetRequiredService<IXsdValidator>(), ValidIncludedRoot)).IsTrue();
        await Assert.That(IsValid(provider.GetRequiredKeyedService<IXsdValidator>("k"), ValidIncludedRoot)).IsTrue();
    }

    [Test]
    public async Task SecondRegistration_DoesNotReplaceTheFirst()
    {
        var calls = 0;
        var services = new ServiceCollection()
            .AddXsdValidator(_ => Open(RootSchema))
            .AddXsdValidator(_ =>
            {
                calls++;
                return Open(OtherSchema);
            })
            .AddKeyedXsdValidator("k", (_, _) => Open(RootSchema))
            .AddKeyedXsdValidator("k", (_, _) =>
            {
                calls++;
                return Open(OtherSchema);
            });
        using var provider = services.BuildServiceProvider();

        await Assert.That(IsValid(provider.GetRequiredService<IXsdValidator>(), ValidRoot)).IsTrue();
        await Assert.That(IsValid(provider.GetRequiredKeyedService<IXsdValidator>("k"), ValidRoot)).IsTrue();
        await Assert.That(calls).IsEqualTo(0);
        await Assert.That(services.Count).IsEqualTo(3);
    }
}
