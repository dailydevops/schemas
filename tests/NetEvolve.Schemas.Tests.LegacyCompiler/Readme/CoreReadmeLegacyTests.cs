namespace NetEvolve.Schemas.Tests.LegacyCompiler.Readme;

using NetEvolve.Schemas.Testing.Readme;

/// <summary>Compiled with C# 13: the region between the sample markers is a code block of src/NetEvolve.Schemas/README.md.</summary>
public class CoreReadmeLegacyTests
{
    [Test]
    public async Task NamedExtensionClassSample_CompilesWithoutExtensionMembers()
    {
        // <sample>
        var builder = FakeSchemaValidatorExtensions.ForFake();
        // </sample>

        await Assert.That(builder.Limits).IsSameReferenceAs(ValidationLimits.Default);
    }

    [Test]
    public async Task Sample_IsInTheCoreReadme()
    {
        var sample = ReadmeSamples.SamplesIn(
            "tests/NetEvolve.Schemas.Tests.LegacyCompiler/Readme/CoreReadmeLegacyTests.cs"
        );
        var blocks = ReadmeSamples.CodeBlocks("src/NetEvolve.Schemas/README.md");

        await Assert.That(sample).Count().IsEqualTo(1);
        await Assert.That(blocks).Contains(sample[0]);
    }
}
