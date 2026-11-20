namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude;

using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Loading.XInclude;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using NetEvolve.Schemas.Xml.Contracts;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.XIncludeFixture;

public class XIncludeSchemaTests
{
    private const string Sch = "http://purl.oclc.org/dsdl/schematron";
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    private const string Schema = $"""
        <schema xmlns="{Sch}" xmlns:xi="{Xi}" schematronEdition="2025" queryBinding="xslt2">
          <xi:include href="lib/functions.xml"/>
          <pattern id="p"><rule context="a"><assert test="f:x(.)">m</assert></rule></pattern>
        </schema>
        """;

    private const string Functions = $"""
        <xsl:function xmlns:xsl="{Xsl}" xmlns:f="urn:f" name="f:x" as="xs:boolean">
          <xsl:param name="a"/>
          <xsl:sequence select="true()"/>
        </xsl:function>
        """;

    [Test]
    public async Task FunctionLibraryPart_LoadsIntoTheObjectModelWithTheFunctionsPresent(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var files = new FakeResources().Add("file:///s/lib/functions.xml", Functions);
        var problems = new FakeProblemSink();
        var resolver = new XIncludeResolver(files, ValidationLimits.Default);

        var resolved = resolver.TryResolve(
            InclusionFixture.Document(Schema),
            "file:///s/schema.sch",
            problems,
            cancellationToken,
            out var document
        );
        var parsed = SchematronCoreParser.TryParse(document, "file:///s/schema.sch", problems, out var schema);

        await Assert.That(resolved).IsTrue();
        await Assert.That(parsed).IsTrue();
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
        await Assert.That(schema!.ForeignElements.Count).IsEqualTo(1);
        await Assert.That(schema.ForeignElements[0].NamespaceUri).IsEqualTo(Xsl);
        await Assert.That(schema.ForeignElements[0].LocalName).IsEqualTo("function");
        await Assert.That(schema.ForeignElements[0].Children.Count).IsGreaterThan(1);
        await Assert.That(schema.Patterns.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Resolver_ThatDenies_StopsTheSchemaFromLoadingWithTheResolverDeniedCode(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var problems = new FakeProblemSink();
        var resolver = new XIncludeResolver(DenyAllResourceResolver.Instance, ValidationLimits.Default);

        var resolved = resolver.TryResolve(
            InclusionFixture.Document(Schema),
            "file:///s/schema.sch",
            problems,
            cancellationToken,
            out _
        );

        await Assert.That(resolved).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(problems.Problems[0].Location.Line).IsEqualTo(2);
        await Assert.That(problems.Problems[0].Location.Column).IsEqualTo(3);
    }

    [Test]
    public async Task Resolver_CountsAcrossDocuments_OfOneInstance(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var files = new FakeResources().Add("file:///a/p.xml", "<p/>");
        var resolver = new XIncludeResolver(files, ValidationLimits.Default.WithMaxExternalResources(1));
        var first = new FakeProblemSink();
        var second = new FakeProblemSink();

        var one = resolver.TryResolve(Load(Doc(Inc("p.xml"))), RootUri, first, cancellationToken, out _);
        var two = resolver.TryResolve(Load(Doc(Inc("p.xml"))), RootUri, second, cancellationToken, out _);

        await Assert.That(one).IsTrue();
        await Assert.That(two).IsFalse();
        await Assert.That(second.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
    }

    [Test]
    public async Task Guards_RejectNullArguments()
    {
        var document = Load("<root/>");
        var problems = new FakeProblemSink();
        var resolver = new XIncludeResolver(new FakeResources(), ValidationLimits.Default);

        await Assert.That(() => new XIncludeResolver(null!, ValidationLimits.Default)).Throws<ArgumentNullException>();
        await Assert.That(() => new XIncludeResolver(new FakeResources(), null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => resolver.TryResolve(default, RootUri, problems, CancellationToken.None, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => resolver.TryResolve(document, RootUri, null!, CancellationToken.None, out _))
            .Throws<ArgumentNullException>();
    }
}
