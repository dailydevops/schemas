namespace NetEvolve.Schemas.Tests.Unit.Readme;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Testing.Fakes;
using NetEvolve.Schemas.Testing.Readme;

// The regions between the sample markers are the code blocks of src/NetEvolve.Schemas/README.md, character for character.
public class CoreReadmeTests
{
    private const string Readme = "src/NetEvolve.Schemas/README.md";
    private const string Template = "templates/readme-project.md";
    private const string Samples = "tests/NetEvolve.Schemas.Tests.Unit/Readme/CoreReadmeTests.cs";
    private const string LegacySamples = "tests/NetEvolve.Schemas.Tests.LegacyCompiler/Readme/CoreReadmeLegacyTests.cs";

    [Test]
    public async Task EntryPointSample_BuildsValidatorThatReceivesTheDocument(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        var limits = ValidationLimits.Default.WithMaxDepth(64).WithMaxIssues(100);
        var validator = await SchemaValidator
            .ForFake()
            .WithLimits(limits)
            .WithResolver(new SandboxedFileSystemResourceResolver("schemas"))
            .BuildAsync(cancellationToken);

        await validator.ValidateAsync("<root/>".AsMemory(), "file:///documents/root.xml", cancellationToken);
        // </sample>

        await Assert.That(validator.Calls).Count().IsEqualTo(1);
        await Assert.That(validator.Calls[0].SystemId).IsEqualTo("file:///documents/root.xml");
    }

    [Test]
    public async Task ResultModelSample_DescribesAnInvalidResult()
    {
        // <sample>
        var issue = new ValidationIssue(
            CoreDiagnosticCodes.DepthLimitExceeded,
            IssueSeverity.Error,
            "The element depth exceeds the limit of 64.",
            new DocumentLocation("file:///documents/root.xml", 3, 14)
        );
        var result = new ValidationResult([issue]);

        var isValid = result.IsValid;
        var first = result.Issues[0].ToString();
        // </sample>

        await Assert.That(isValid).IsFalse();
        await Assert.That(result.IsTruncated).IsFalse();
        await Assert
            .That(first)
            .IsEqualTo("NES0002 Error file:///documents/root.xml(3,14): The element depth exceeds the limit of 64.");
    }

    [Test]
    public async Task DefaultResolverSample_RefusesExternalResources(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        var resolver = SchemaValidator.ForFake().Resolver;
        var request = new ResourceRequest(
            ResourceKind.SchemaDocument,
            "types.xsd",
            "file:///schemas/main.xsd",
            "file:///schemas/types.xsd"
        );

        using var resolved = resolver.Resolve(in request, cancellationToken);
        // </sample>

        await Assert.That(resolved.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(resolved.Message).StartsWith("NES0100");
    }

    [Test]
    public async Task Builder_DefaultsToDefaultLimitsAndDenyAllResolver()
    {
        var builder = SchemaValidator.ForFake();

        await Assert.That(builder.Limits).IsSameReferenceAs(ValidationLimits.Default);
        await Assert.That(builder.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
    }

    [Test]
    public async Task Readme_NamesTheDefaultLimits()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var d = ValidationLimits.Default;
        string[] rows =
        [
            Row("MaxIssues", Number(d.MaxIssues)),
            Row("MaxInputSize", Mebibytes(d.MaxInputSize)),
            Row("MaxDepth", Number(d.MaxDepth)),
            Row("MaxAttributesPerElement", Number(d.MaxAttributesPerElement)),
            Row("MaxEntityExpansion", Number(d.MaxEntityExpansion)),
            Row("MaxEntityDepth", Number(d.MaxEntityDepth)),
            Row("MaxExternalResources", Number(d.MaxExternalResources)),
            Row("MaxResourceSize", Mebibytes(d.MaxResourceSize)),
        ];

        foreach (var row in rows)
        {
            await Assert.That(readme).Contains(row);
        }
    }

    [Test]
    public async Task Readme_HasLimitationsSectionNamingThePreCSharp14Fallback()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("## Limitations");
        await Assert.That(readme).Contains("C# 14");
        await Assert.That(readme).Contains("SchemaValidatorExtensions");
    }

    [Test]
    public async Task Readme_StatesThatExternalResourcesAreRefusedWithoutAResolver()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("deny by default");
        await Assert.That(readme).Contains("DenyAllResourceResolver");
    }

    [Test]
    public async Task Readme_NamesTheDiagnosticRanges()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        (DiagnosticCodeRange Range, string Owner)[] rows =
        [
            (DiagnosticRanges.Core, "`NetEvolve.Schemas` (`DiagnosticRanges.Core`)"),
            (DiagnosticRanges.Xml, "`NetEvolve.Schemas.Xml`"),
            (DiagnosticRanges.XPath, "`NetEvolve.Schemas.XPath`"),
            (DiagnosticRanges.Schematron, "`NetEvolve.Schemas.Schematron`"),
            (DiagnosticRanges.Xsd, "`NetEvolve.Schemas.Xsd`"),
            (DiagnosticRanges.DependencyInjection, "`NetEvolve.Schemas.DependencyInjection`"),
            (DiagnosticRanges.Reserved, "Reserved"),
            (DiagnosticRanges.Consumer, "Consumers (`DiagnosticRanges.Consumer`)"),
        ];

        foreach (var (range, owner) in rows)
        {
            await Assert.That(readme).Contains($"| {range.First}-{range.Last} | {owner} |");
        }
    }

    [Test]
    public async Task Readme_FollowsTheSectionOrderOfTheProjectTemplate()
    {
        var template = Headings(ReadmeSamples.ReadText(Template));
        var readme = Headings(ReadmeSamples.ReadText(Readme));
        var position = 0;

        foreach (var heading in template)
        {
            if (heading.StartsWith("# Project Name", StringComparison.Ordinal))
            {
                continue;
            }

            var found = readme.IndexOf(heading, position);
            await Assert.That(found).IsGreaterThanOrEqualTo(0).Because(heading);
            position = found + 1;
        }

        await Assert.That(readme[0]).IsEqualTo("# NetEvolve.Schemas");
    }

    [Test]
    public async Task Readme_HasNoTemplatePlaceholdersAndNoComments()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).DoesNotContain("<!--");
        await Assert.That(readme).DoesNotContain("YourPackageName");
        await Assert.That(readme).DoesNotContain("YourRepo");
        await Assert.That(readme).DoesNotContain("x.x.x");
        await Assert.That(readme).Contains("https://www.nuget.org/packages/NetEvolve.Schemas/");
    }

    [Test]
    public async Task Readme_LinksToFilesThatExist()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        const string prefix = "https://github.com/dailydevops/schemas/blob/main/";
        var checkedLinks = 0;

        foreach (var line in readme.Split('\n'))
        {
            var from = 0;
            while (true)
            {
                var start = line.IndexOf(prefix, from, StringComparison.Ordinal);
                if (start < 0)
                {
                    break;
                }

                var end = line.IndexOfAny([')', ' ', '`', '>'], start);
                var path = line.Substring(start + prefix.Length, (end < 0 ? line.Length : end) - start - prefix.Length);
                await Assert.That(File.Exists(RepositoryPath(path))).IsTrue().Because(path);
                checkedLinks++;
                from = start + prefix.Length;
            }
        }

        await Assert.That(checkedLinks).IsGreaterThan(0);
    }

    [Test]
    public async Task EveryReadmeCodeBlock_IsACompiledSample()
    {
        var compiled = new List<string>(ReadmeSamples.SamplesIn(Samples));
        compiled.AddRange(ReadmeSamples.SamplesIn(LegacySamples));
        var blocks = ReadmeSamples.CodeBlocks(Readme);

        await Assert.That(blocks).Count().IsEqualTo(compiled.Count);
        foreach (var block in blocks)
        {
            await Assert.That(compiled).Contains(block);
        }
    }

    private static string RepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Schemas.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new InvalidOperationException("Schemas.slnx was not found."),
            relativePath
        );
    }

    private static List<string> Headings(string markdown)
    {
        var headings = new List<string>();
        var inFence = false;
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
            }
            else if (
                !inFence
                && (line.StartsWith("# ", StringComparison.Ordinal) || line.StartsWith("## ", StringComparison.Ordinal))
            )
            {
                headings.Add(line);
            }
        }

        return headings;
    }

    private static string Row(string name, string value) => $"| `{name}` | {value} |";

    private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string Mebibytes(long bytes) => $"{bytes / (1024 * 1024)} MiB";
}
