namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Readme;

using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.Testing.Readme;

// The regions between the sample markers are the code blocks of src/NetEvolve.Schemas.DependencyInjection/README.md, character for character.
public class DependencyInjectionReadmeTests
{
    private const string Readme = "src/NetEvolve.Schemas.DependencyInjection/README.md";
    private const string Samples =
        "tests/NetEvolve.Schemas.DependencyInjection.Tests.Unit/Readme/DependencyInjectionReadmeTests.cs";

    [Test]
    public async Task PlainSample_BuildsLazilyAndSharesOneInstance()
    {
        // <sample>
        var builds = 0;
        var services = new ServiceCollection();
        services.AddSchemaValidator<ISchemaValidator>(_ =>
        {
            builds++;
            return SchemaValidator.ForFake().Build();
        });

        using var provider = services.BuildServiceProvider();
        var notBuiltYet = builds == 0;

        var first = provider.GetRequiredService<ISchemaValidator>();
        var second = provider.GetRequiredService<ISchemaValidator>();
        // </sample>

        await Assert.That(notBuiltYet).IsTrue();
        await Assert.That(builds).IsEqualTo(1);
        await Assert.That(second).IsSameReferenceAs(first);
    }

    [Test]
    public async Task KeyedSample_ResolvesOneValidatorPerKey()
    {
        // <sample>
        var services = new ServiceCollection();
        services.AddKeyedSchemaValidator<ISchemaValidator>("strict", (_, _) =>
            SchemaValidator.ForFake().WithLimits(ValidationLimits.Default.WithMaxDepth(16)).Build()
        );
        services.AddKeyedSchemaValidator<ISchemaValidator>("default", (_, _) => SchemaValidator.ForFake().Build());

        using var provider = services.BuildServiceProvider();
        var strict = provider.GetRequiredKeyedService<ISchemaValidator>("strict");
        var other = provider.GetRequiredKeyedService<ISchemaValidator>("default");
        // </sample>

        await Assert.That(strict).IsNotSameReferenceAs(other);
        await Assert.That(provider.GetRequiredKeyedService<ISchemaValidator>("strict")).IsSameReferenceAs(strict);
    }

    [Test]
    public async Task Readme_NamesTheRegistrationRules()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("AddSchemaValidator");
        await Assert.That(readme).Contains("AddKeyedSchemaValidator");
        await Assert.That(readme).Contains("singleton");
        await Assert.That(readme).Contains("lazily");
        await Assert.That(readme).Contains("## Limitations");
        await Assert.That(readme).Contains("IKeyedServiceProvider");
    }

    [Test]
    public async Task EveryReadmeCodeBlock_IsACompiledSample()
    {
        var compiled = ReadmeSamples.SamplesIn(Samples);
        var blocks = ReadmeSamples.CodeBlocks(Readme);

        await Assert.That(blocks).Count().IsEqualTo(compiled.Count);
        foreach (var block in blocks)
        {
            await Assert.That(compiled).Contains(block);
        }
    }
}
