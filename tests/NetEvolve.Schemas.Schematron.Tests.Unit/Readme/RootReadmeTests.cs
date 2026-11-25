namespace NetEvolve.Schemas.Schematron.Tests.Unit.Readme;

using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Readme;

// The usage example of the root readme is the compiled quick start of the Schematron package readme, character for character.
public class RootReadmeTests
{
    private const string Readme = "README.md";
    private const string Samples = "tests/NetEvolve.Schemas.Schematron.Tests.Unit/Readme/SchematronReadmeTests.cs";

    [Test]
    public async Task UsageExample_IsTheCompiledSchematronQuickStart()
    {
        var blocks = ReadmeSamples.CodeBlocks(Readme);
        var compiled = ReadmeSamples.SamplesIn(Samples);

        await Assert.That(blocks).HasSingleItem();
        await Assert.That(blocks[0]).IsEqualTo(compiled[0]);
    }

    [Test]
    public async Task Readme_FollowsTheSolutionTemplateAndLeavesNoPlaceholder()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).DoesNotContain("<!--");
        await Assert.That(readme).DoesNotContain("YourRepo");
        await Assert.That(readme).DoesNotContain("ProjectName");

        var last = -1;
        foreach (
            var heading in new[]
            {
                "## Overview",
                "## Projects",
                "## Features",
                "## Getting Started",
                "## Development",
                "## Architecture",
                "## Conformance",
                "## Security Defaults",
                "## Known Limitations",
                "## Contributing",
                "## Code of Conduct",
                "## Documentation",
                "## Versioning",
                "## Support",
                "## License",
            }
        )
        {
            var index = readme.IndexOf(heading, System.StringComparison.Ordinal);
            await Assert.That(index).IsGreaterThan(last).Because(heading);
            last = index;
        }
    }
}
