namespace NetEvolve.Schemas.Tests.LegacyCompiler.Readme;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Readme;

/// <summary>Compiled with C# 13: the region between the sample markers is a code block of src/NetEvolve.Schemas.Schematron/README.md.</summary>
public class SchematronReadmeLegacyTests
{
    private const string Schema = """
        <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron">
          <sch:pattern><sch:rule context="order"><sch:assert test="@total">An order needs a total.</sch:assert></sch:rule></sch:pattern>
        </sch:schema>
        """;

    [Test]
    public async Task NamedExtensionClassSample_CompilesWithoutExtensionMembers(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = Schema;

        // <sample>
        var builder = SchematronSchemaValidatorExtensions.ForSchematron(schema);
        // </sample>

        var validator = await builder.BuildAsync(cancellationToken);
        var result = await validator.ValidateAsync("<order/>".AsMemory(), cancellationToken: cancellationToken);

        await Assert.That(builder).IsTypeOf<SchematronValidatorBuilder>();
        await Assert.That(builder.Phase).IsEqualTo("#DEFAULT");
        await Assert.That(result.Issues).HasSingleItem();
        await Assert.That(result.Issues[0].Message).IsEqualTo("An order needs a total.");
    }

    [Test]
    public async Task Sample_IsInTheSchematronReadme()
    {
        var sample = ReadmeSamples.SamplesIn(
            "tests/NetEvolve.Schemas.Tests.LegacyCompiler/Readme/SchematronReadmeLegacyTests.cs"
        );
        var blocks = ReadmeSamples.CodeBlocks("src/NetEvolve.Schemas.Schematron/README.md");

        await Assert.That(sample).Count().IsEqualTo(1);
        await Assert.That(blocks).Contains(sample[0]);
    }
}
