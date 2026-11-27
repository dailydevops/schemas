namespace NetEvolve.Schemas.Xsd.Tests.Unit.Readme;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Testing.Readme;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;

// The regions between the sample markers are the code blocks of src/NetEvolve.Schemas.Xsd/README.md, character for character.
public class XsdReadmeTests
{
    private const string Readme = "src/NetEvolve.Schemas.Xsd/README.md";
    private const string Samples = "tests/NetEvolve.Schemas.Xsd.Tests.Unit/Readme/XsdReadmeTests.cs";
    private const string LegacySamples = "tests/NetEvolve.Schemas.Tests.LegacyCompiler/Readme/XsdReadmeLegacyTests.cs";

    [Test]
    public async Task QuickStartSample_ReportsTheInvalidQuantity(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="order">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="quantity" type="xs:positiveInteger"/>
                  </xs:sequence>
                  <xs:attribute name="id" type="xs:ID" use="required"/>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        var validator = await SchemaValidator.ForXsd().AddSchema(schema.AsMemory()).BuildAsync(cancellationToken);
        var result = await validator.ValidateAsync(
            """<order id="o1"><quantity>0</quantity></order>""".AsMemory(),
            "file:///orders/1.xml",
            cancellationToken
        );
        // </sample>

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues).HasSingleItem();
        await Assert.That(result.Issues[0].Code.ToString()).IsEqualTo("NES4625");
        await Assert.That(result.Issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(result.Issues[0].Location.Source).IsEqualTo("file:///orders/1.xml");
        await Assert.That(result.Issues[0].Location.Line).IsEqualTo(1);
        await Assert.That(validator.Version).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    public async Task StreamAndMemorySample_ValidateTheSameInstance(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schemaText = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="order" type="xs:int"/>
            </xs:schema>
            """;
        await using var schemaStream = new MemoryStream(Encoding.UTF8.GetBytes(schemaText));
        await using var instanceStream = new MemoryStream("<order>7</order>"u8.ToArray());

        // <sample>
        var validator = await SchemaValidator
            .ForXsd()
            .AddSchema(schemaStream, "file:///order.xsd")
            .BuildAsync(cancellationToken);

        var fromStream = await validator.ValidateAsync(instanceStream, "file:///order.xml", cancellationToken);
        var fromBytes = validator.Validate("<order>seven</order>"u8, "file:///order.xml", cancellationToken);
        // </sample>

        await Assert.That(fromStream.IsValid).IsTrue();
        await Assert.That(fromBytes.IsValid).IsFalse();
        await Assert.That(fromBytes.Issues[0].Code.ToString()).IsEqualTo("NES4616");
    }

    [Test]
    public async Task VersionSelectionSample_RejectsTheOpenContentInBothVersionsWithTheirCodes(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:simpleType name="price"><xs:restriction base="xs:precisionDecimal"/></xs:simpleType>
            </xs:schema>
            """;

        var codes = new List<string>();
        foreach (var version in new[] { XsdVersion.V11, XsdVersion.V10 })
        {
            try
            {
                _ = await SchemaValidator
                    .ForXsd()
                    .WithVersion(version)
                    .AddSchema(schema.AsMemory())
                    .BuildAsync(cancellationToken);
            }
            catch (SchemaCompilationException exception)
            {
                codes.Add($"{version}: {exception.Issues[0].Code}");
            }
        }
        // </sample>

        await Assert.That(codes).IsEquivalentTo(["V11: NES4076", "V10: NES4112"]);
        await Assert.That(codes[0]).IsEqualTo("V11: NES4076");
    }

    [Test]
    public async Task StrictVersionSample_ValidatesAnXsd10Schema(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="order" type="xs:int"/>
            </xs:schema>
            """;

        // <sample>
        var validator = await SchemaValidator
            .ForXsd()
            .WithVersion(XsdVersion.V10)
            .AddSchema(schema.AsMemory())
            .BuildAsync(cancellationToken);
        // </sample>
        var result = await validator.ValidateAsync("<order>1</order>".AsMemory(), null, cancellationToken);

        await Assert.That(validator.Version).IsEqualTo(XsdVersion.V10);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task TreeSample_GivesTheIssuesAndTheAnnotationsOfTheElement(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="order">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="quantity" type="xs:int"/>
                  </xs:sequence>
                  <xs:attribute name="priority" type="xs:int" default="3"/>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;
        var validator = await SchemaValidator.ForXsd().AddSchema(schema.AsMemory()).BuildAsync(cancellationToken);

        // <sample>
        var tree = XmlDocumentLoader.Load("<order><quantity>12</quantity></order>".AsSpan());

        var annotated = validator.ValidateTree(tree, cancellationToken);

        var order = annotated.Document.Document.FirstChild;
        var found = annotated.TryGetAnnotation(order, out var annotation);
        // </sample>

        await Assert.That(annotated.Result.IsValid).IsTrue();
        await Assert.That(found).IsTrue();
        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(annotation.TypeName.IsAnonymous).IsTrue();
        await Assert.That(order.FirstAttribute.IsSpecified).IsFalse();
        await Assert.That(order.FirstAttribute.StringValue).IsEqualTo("3");
    }

    [Test]
    public async Task IncludeSample_ReadsTheIncludedDocumentThroughTheResolver(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Directory
            .CreateDirectory(Path.Combine(Path.GetTempPath(), "xsd-readme-" + Guid.NewGuid().ToString("N")))
            .FullName;
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(directory, "types.xsd"),
                """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:element name="order" type="xs:int"/>
                </xs:schema>
                """,
                cancellationToken
            );
            await File.WriteAllTextAsync(
                Path.Combine(directory, "main.xsd"),
                """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:include schemaLocation="types.xsd"/>
                </xs:schema>
                """,
                cancellationToken
            );

            // <sample>
            var path = Path.Combine(directory, "main.xsd");
            var main = new ReadOnlyMemory<byte>(await File.ReadAllBytesAsync(path, cancellationToken));
            var systemId = new Uri(path).AbsoluteUri;

            string? deniedCode = null;
            try
            {
                _ = await SchemaValidator.ForXsd().AddSchema(main, systemId).BuildAsync(cancellationToken);
            }
            catch (SchemaCompilationException exception)
            {
                deniedCode = exception.Issues[0].Code.ToString();
            }

            var validator = await SchemaValidator
                .ForXsd()
                .WithResolver(new SandboxedFileSystemResourceResolver(directory))
                .AddSchema(main, systemId)
                .BuildAsync(cancellationToken);
            var result = validator.Validate("<order>7</order>", null, cancellationToken);
            // </sample>

            await Assert.That(deniedCode).IsEqualTo("NES0100");
            await Assert.That(result.IsValid).IsTrue();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public async Task HintSample_LoadsTheHintedSchemaThroughTheResolver(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Directory
            .CreateDirectory(Path.Combine(Path.GetTempPath(), "xsd-readme-" + Guid.NewGuid().ToString("N")))
            .FullName;
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(directory, "order.xsd"),
                """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:element name="order" type="xs:int"/>
                </xs:schema>
                """,
                cancellationToken
            );

            // <sample>
            var validator = await SchemaValidator
                .ForXsd()
                .WithSchemaLocationHints(true)
                .WithResolver(new SandboxedFileSystemResourceResolver(directory))
                .BuildAsync(cancellationToken);

            var result = await validator.ValidateAsync(
                """
                <order xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                       xsi:noNamespaceSchemaLocation="order.xsd">seven</order>
                """.AsMemory(),
                new Uri(Path.Combine(directory, "order.xml")).AbsoluteUri,
                cancellationToken
            );
            // </sample>

            await Assert.That(result.IsValid).IsFalse();
            await Assert.That(result.Issues[0].Code.ToString()).IsEqualTo("NES4616");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public async Task LimitsSample_KeepsTheFirstIssueOnly(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="list">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="n" type="xs:int" maxOccurs="unbounded"/>
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        // <sample>
        var validator = await SchemaValidator
            .ForXsd()
            .WithLimits(ValidationLimits.Default.WithMaxIssues(1).WithMaxDepth(32))
            .AddSchema(schema.AsMemory())
            .BuildAsync(cancellationToken);

        var result = validator.Validate("<list><n>a</n><n>b</n></list>", null, cancellationToken);
        // </sample>

        await Assert.That(result.Issues).HasSingleItem();
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Readme_NamesTheEntryPointsAndTheStatusOfTheDefaultVersion()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("XsdSchemaValidatorExtensions");
        await Assert.That(readme).Contains("C# 14");
        await Assert.That(readme).Contains("WithVersion(XsdVersion.V10)");
        await Assert.That(readme).Contains("NES4076");
        await Assert.That(readme).Contains("NES4020");
        await Assert.That(readme).Contains("#360");
        await Assert.That(readme).Contains("#318");
        await Assert.That(readme).Contains("#956");
        await Assert.That(readme).Contains("docs/xsd/system-xml-schema-deviations.md");
    }

    [Test]
    public async Task Readme_FollowsTheSectionOrderOfTheProjectTemplate()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var template = ReadmeSamples.ReadText("templates/readme-project.md");
        var wanted = template
            .Split('\n')
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(line => line.TrimEnd())
            .ToArray();
        var actual = readme
            .Split('\n')
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(line => line.TrimEnd())
            .ToArray();

        await Assert
            .That(actual)
            .IsEquivalentTo(wanted)
            .Because("the readme keeps the H2 sections of the template, in order.");
        await Assert.That(actual.SequenceEqual(wanted)).IsTrue();
        await Assert.That(readme).DoesNotContain("<!--");
        await Assert.That(readme).DoesNotContain("YourPackageName");
        await Assert.That(readme).DoesNotContain("YourRepo");
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
}
