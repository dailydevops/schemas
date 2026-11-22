namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron;

using Microsoft.Extensions.DependencyInjection;
using static NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron.SchematronRegistrationTestSupport;

public class SchematronRegistrationTests
{
    [Test]
    public async Task TwoKeys_ResolveDistinctSingletonValidators_ThatFireTheirOwnAssertions()
    {
        using var provider = new ServiceCollection()
            .AddKeyedSchematronValidator("number", (_, _) => Open(NumberSchema))
            .AddKeyedSchematronValidator("total", (_, _) => Open(TotalSchema))
            .BuildServiceProvider();

        var number = provider.GetRequiredKeyedService<ISchematronValidator>("number");
        var total = provider.GetRequiredKeyedService<ISchematronValidator>("total");

        await Assert.That(number).IsNotSameReferenceAs(total);
        await Assert.That(provider.GetRequiredKeyedService<ISchematronValidator>("number")).IsSameReferenceAs(number);
        await Assert.That(provider.GetRequiredKeyedService<ISchematronValidator>("total")).IsSameReferenceAs(total);
        await Assert.That(Messages(Validate(number))).IsEqualTo("An invoice needs a number.");
        await Assert.That(Messages(Validate(total))).IsEqualTo("The invoice is large.");
    }

    [Test]
    public async Task Keyed_AlsoRegistersSchemaValidatorUnderTheKey_AndTheUnkeyedLookupIsIndependent()
    {
        using var provider = new ServiceCollection()
            .AddKeyedSchematronValidator("number", (_, _) => Open(NumberSchema))
            .BuildServiceProvider();

        await Assert
            .That(provider.GetRequiredKeyedService<ISchemaValidator>("number"))
            .IsSameReferenceAs(provider.GetRequiredKeyedService<ISchematronValidator>("number"));
        await Assert.That(provider.GetService<ISchematronValidator>()).IsNull();
        await Assert.That(provider.GetService<ISchemaValidator>()).IsNull();
    }

    [Test]
    public async Task Plain_ResolvesOneSingletonValidator_AndRegistersNothingElse()
    {
        var services = new ServiceCollection().AddSchematronValidator(_ => Open(NumberSchema));
        using var provider = services.BuildServiceProvider();

        var validator = provider.GetRequiredService<ISchematronValidator>();

        await Assert.That(services.Count).IsEqualTo(1);
        await Assert.That(services.Single().Lifetime).IsEqualTo(ServiceLifetime.Singleton);
        await Assert.That(provider.GetRequiredService<ISchematronValidator>()).IsSameReferenceAs(validator);
        await Assert.That(provider.GetService<ISchemaValidator>()).IsNull();
        await Assert.That(Messages(Validate(validator))).IsEqualTo("An invoice needs a number.");
    }

    [Test]
    public async Task ConfigureCallback_SetsThePhase_ForPlainAndKeyedRegistrations()
    {
        using var provider = new ServiceCollection()
            .AddSchematronValidator(_ => Open(PhaseSchema), (_, builder) => builder.WithPhase("basic"))
            .AddKeyedSchematronValidator(
                "all",
                (_, _) => Open(PhaseSchema),
                (_, _, builder) => builder.WithPhase("#ALL")
            )
            .BuildServiceProvider();

        var basic = provider.GetRequiredService<ISchematronValidator>();
        var all = provider.GetRequiredKeyedService<ISchematronValidator>("all");

        await Assert.That(basic.Phase).IsEqualTo("basic");
        await Assert.That(Messages(Validate(basic))).IsEqualTo("No number.");
        await Assert.That(all.Phase).IsEqualTo("#ALL");
        await Assert.That(Messages(Validate(all))).IsEqualTo("No number.|Large.");
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
            .AddKeyedSchematronValidator(
                "rules",
                (sp, key) =>
                {
                    seenBySchema = sp.GetRequiredService<List<string>>();
                    keyForSchema = key;
                    return Open(NumberSchema);
                },
                (sp, key, _) =>
                {
                    seenByConfigure = sp.GetRequiredService<List<string>>();
                    keyForConfigure = key;
                }
            )
            .BuildServiceProvider();

        _ = provider.GetRequiredKeyedService<ISchematronValidator>("rules");

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
            .AddSchematronValidator(
                sp =>
                {
                    seenBySchema = sp.GetRequiredService<List<string>>();
                    return Open(NumberSchema);
                },
                (sp, _) => seenByConfigure = sp.GetRequiredService<List<string>>()
            )
            .BuildServiceProvider();

        _ = provider.GetRequiredService<ISchematronValidator>();

        await Assert.That(seenBySchema).IsSameReferenceAs(marker);
        await Assert.That(seenByConfigure).IsSameReferenceAs(marker);
    }

    [Test]
    public async Task SystemId_ResolvesRelativeIncludes_ForPlainAndKeyedRegistrations()
    {
        var resolver = new CountingResolver().Add("file:///rules/rule.sch", IncludedRule);
        using var provider = new ServiceCollection()
            .AddSchematronValidator(
                _ => Open(IncludingSchema),
                (_, builder) => builder.WithResolver(resolver),
                "file:///rules/main.sch"
            )
            .AddKeyedSchematronValidator(
                "k",
                (_, _) => Open(IncludingSchema),
                (_, _, builder) => builder.WithResolver(resolver),
                "file:///rules/main.sch"
            )
            .BuildServiceProvider();

        await Assert
            .That(Messages(Validate(provider.GetRequiredService<ISchematronValidator>())))
            .IsEqualTo("Included number.");
        await Assert
            .That(Messages(Validate(provider.GetRequiredKeyedService<ISchematronValidator>("k"))))
            .IsEqualTo("Included number.");
    }

    [Test]
    public async Task SecondRegistration_DoesNotReplaceTheFirst()
    {
        var calls = 0;
        var services = new ServiceCollection()
            .AddSchematronValidator(_ => Open(NumberSchema))
            .AddSchematronValidator(_ =>
            {
                calls++;
                return Open(TotalSchema);
            })
            .AddKeyedSchematronValidator("k", (_, _) => Open(NumberSchema))
            .AddKeyedSchematronValidator("k", (_, _) =>
            {
                calls++;
                return Open(TotalSchema);
            });
        using var provider = services.BuildServiceProvider();

        await Assert
            .That(Messages(Validate(provider.GetRequiredService<ISchematronValidator>())))
            .IsEqualTo("An invoice needs a number.");
        await Assert
            .That(Messages(Validate(provider.GetRequiredKeyedService<ISchematronValidator>("k"))))
            .IsEqualTo("An invoice needs a number.");
        await Assert.That(calls).IsEqualTo(0);
        await Assert.That(services.Count).IsEqualTo(3);
    }
}
