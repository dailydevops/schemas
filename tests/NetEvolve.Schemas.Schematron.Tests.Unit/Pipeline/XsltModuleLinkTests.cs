namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Testing.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The XSLT modules of a schema are linked in the Resolve stage and compiled in the Compile stage (#832): <c>xsl:import</c> and
/// <c>xsl:include</c> are followed through the resolver, and the declarations of the modules no longer end the build.
/// </summary>
public class XsltModuleLinkTests
{
    private const string Xsl = "xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";

    private const string Library =
        "<xsl:stylesheet version=\"2.0\" "
        + Xsl
        + " xmlns:f=\"urn:f\">"
        + "<xsl:key name=\"k\" match=\"a\" use=\"@id\"/>"
        + "<xsl:variable name=\"v\" select=\"1\"/>"
        + "<xsl:function name=\"f:twice\"><xsl:param name=\"x\"/><xsl:sequence select=\"$x * 2\"/></xsl:function>"
        + "</xsl:stylesheet>";

    private const string Pattern =
        "<pattern id=\"p\"><rule context=\"a\"><assert test=\"true()\">Never fails.</assert></rule></pattern>";

    private static InMemoryResourceResolver Resolver(string name, string content) =>
        new InMemoryResourceResolver().Add("file:///" + name, Encoding.UTF8.GetBytes(content));

    [Test]
    [Arguments("import")]
    [Arguments("include")]
    public async Task SchemaThatLinksALibrary_BuildsWithoutAnUnsupportedDeclaration(string kind)
    {
        var validator = Builder(Wrap("<xsl:" + kind + " " + Xsl + " href=\"lib.xsl\"/>" + Pattern))
            .WithResolver(Resolver("lib.xsl", Library))
            .BuildNow();

        await Assert.That(validator.Schema.Patterns.Count).IsEqualTo(1);
    }

    [Test]
    public async Task LinkedModuleThatTheResolverCannotFind_IsNotFound()
    {
        var failure = Capture(() =>
            _ = Builder(Wrap("<xsl:import " + Xsl + " href=\"missing.xsl\"/>" + Pattern))
                .WithResolver(new InMemoryResourceResolver())
                .BuildNow()
        );

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
    }

    [Test]
    public async Task UnregisteredDeclarationInALinkedModule_IsUnsupportedAtTheModule()
    {
        var module = "<xsl:stylesheet version=\"2.0\" " + Xsl + "><xsl:template match=\"/\"/></xsl:stylesheet>";

        var failure = Capture(() =>
            _ = Builder(Wrap("<xsl:import " + Xsl + " href=\"lib.xsl\"/>" + Pattern))
                .WithResolver(Resolver("lib.xsl", module))
                .BuildNow()
        );

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(failure.Issues.Single().Location.Source).IsEqualTo("file:///lib.xsl");
    }
}
