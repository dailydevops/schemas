namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Readme;

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Testing.Readme;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

// The regions between the sample markers are the code blocks of src/NetEvolve.Schemas.DependencyInjection/README.md, character for character.
public class DependencyInjectionReadmeTests
{
    private const string Readme = "src/NetEvolve.Schemas.DependencyInjection/README.md";
    private const string Template = "templates/readme-project.md";
    private const string Samples =
        "tests/NetEvolve.Schemas.DependencyInjection.Tests.Unit/Readme/DependencyInjectionReadmeTests.cs";

    [Test]
    public async Task QuickStartSample_ValidatesAgainstTheRegisteredSchema(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="order" type="xs:int"/>
            </xs:schema>
            """;

        var services = new ServiceCollection();
        services.AddXsdValidator(_ => new MemoryStream(Encoding.UTF8.GetBytes(schema)));

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IXsdValidator>();
        var valid = validator.Validate("<order>42</order>".AsSpan(), null, cancellationToken).IsValid;
        var invalid = validator.Validate("<order>x</order>".AsSpan(), null, cancellationToken).IsValid;
        // </sample>

        await Assert.That(valid).IsTrue();
        await Assert.That(invalid).IsFalse();
        await Assert.That(provider.GetRequiredService<IXsdValidator>()).IsSameReferenceAs(validator);
        await Assert.That(provider.GetService<ISchemaValidator>()).IsNull();
    }

    [Test]
    public async Task KeyedSample_ResolvesOneValidatorPerKeyAndConfiguresTheVersion(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="order" type="xs:int"/>
            </xs:schema>
            """;

        var services = new ServiceCollection();
        services.AddKeyedXsdValidator(
            "strict",
            (_, _) => new MemoryStream(Encoding.UTF8.GetBytes(schema)),
            (_, _, builder) => builder.WithVersion(XsdVersion.V10)
        );
        services.AddKeyedXsdValidator("default", (_, _) => new MemoryStream(Encoding.UTF8.GetBytes(schema)));

        using var provider = services.BuildServiceProvider();
        var strict = provider.GetRequiredKeyedService<IXsdValidator>("strict");
        var other = provider.GetRequiredKeyedService<IXsdValidator>("default");
        var asInterface = provider.GetRequiredKeyedService<ISchemaValidator>("strict");
        // </sample>

        await Assert.That(strict).IsNotSameReferenceAs(other);
        await Assert.That(strict.Version).IsEqualTo(XsdVersion.V10);
        await Assert.That(other.Version).IsEqualTo(XsdVersion.V11);
        await Assert.That(asInterface).IsSameReferenceAs(strict);
        await Assert.That(provider.GetRequiredKeyedService<IXsdValidator>("strict")).IsSameReferenceAs(strict);
    }

    [Test]
    public async Task SchematronSample_AppliesThePhaseOfTheConfigureCallback(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        const string schema = """
            <schema xmlns="http://purl.oclc.org/dsdl/schematron">
              <ns prefix="i" uri="urn:invoice"/>
              <phase id="basic"><active pattern="p1"/></phase>
              <pattern id="p1">
                <rule context="i:invoice"><assert test="i:number">An invoice needs a number.</assert></rule>
              </pattern>
              <pattern id="p2">
                <rule context="i:invoice"><report test="i:total &gt; 1000">The invoice is large.</report></rule>
              </pattern>
            </schema>
            """;

        var services = new ServiceCollection();
        services.AddSchematronValidator(
            _ => new MemoryStream(Encoding.UTF8.GetBytes(schema)),
            (_, builder) => builder.WithPhase("basic")
        );

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<ISchematronValidator>();
        var invoice = """<invoice xmlns="urn:invoice"><total>2500</total></invoice>""";
        var result = validator.Validate(invoice.AsSpan(), null, cancellationToken);
        // </sample>

        await Assert.That(validator.Phase).IsEqualTo("basic");
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DtdSample_DeniesTheExternalSubsetUnlessTheCallbackAllowsIt(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await File.WriteAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "readme-order.dtd"),
            "<!ELEMENT order EMPTY>",
            cancellationToken
        );

        // <sample>
        var root = AppContext.BaseDirectory;
        var systemId = new Uri(Path.Combine(root, "order.xml")).AbsoluteUri;

        var services = new ServiceCollection();
        services.AddDtdValidator();
        services.AddKeyedDtdValidator("local", (_, _, builder) =>
            builder.WithResolver(new SandboxedFileSystemResourceResolver(root)).WithAllowExternalSubset(true)
        );

        using var provider = services.BuildServiceProvider();
        var document = "<!DOCTYPE order SYSTEM \"readme-order.dtd\"><order/>";
        var plain = provider.GetRequiredService<IDtdValidator>();
        var local = provider.GetRequiredKeyedService<IDtdValidator>("local");
        var denied = plain.Validate(document.AsSpan(), systemId, cancellationToken);
        var allowed = local.Validate(document.AsSpan(), systemId, cancellationToken);
        // </sample>

        await Assert.That(denied.IsValid).IsFalse();
        await Assert.That(allowed.IsValid).IsTrue();
    }

    [Test]
    public async Task CustomSample_BuildsLazilyAndSharesOneInstance(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        var builds = 0;
        var services = new ServiceCollection();
        services.AddSchemaValidator<ISchemaValidator>(_ =>
        {
            builds++;
            return DtdSchemaValidatorExtensions.ForDtd().Build();
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
    public async Task Readme_NamesEveryPublicRegistrationMethod()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        string[] methods =
        [
            "AddSchemaValidator",
            "AddKeyedSchemaValidator",
            "AddXsdValidator",
            "AddKeyedXsdValidator",
            "AddSchematronValidator",
            "AddKeyedSchematronValidator",
            "AddDtdValidator",
            "AddKeyedDtdValidator",
        ];

        foreach (var method in methods)
        {
            await Assert.That(readme).Contains(method).Because(method);
        }

        await Assert.That(readme).Contains("IKeyedServiceProvider");
        await Assert.That(readme).Contains("KeyedService.AnyKey");
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

        await Assert.That(readme[0]).IsEqualTo("# NetEvolve.Schemas.DependencyInjection");
    }

    [Test]
    public async Task Readme_HasNoTemplatePlaceholdersAndNoComments()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).DoesNotContain("<!--");
        await Assert.That(readme).DoesNotContain("YourPackageName");
        await Assert.That(readme).DoesNotContain("YourRepo");
        await Assert.That(readme).DoesNotContain("x.x.x");
        await Assert.That(readme).Contains("https://www.nuget.org/packages/NetEvolve.Schemas.DependencyInjection/");
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
        var compiled = ReadmeSamples.SamplesIn(Samples);
        var blocks = ReadmeSamples.CodeBlocks(Readme);

        await Assert.That(blocks).Count().IsEqualTo(compiled.Count);
        foreach (var block in blocks)
        {
            await Assert.That(compiled).Contains(block);
        }
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
}
