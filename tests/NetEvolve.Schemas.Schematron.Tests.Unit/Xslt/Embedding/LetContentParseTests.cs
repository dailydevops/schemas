namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding.EmbeddingNodes;

/// <summary>The parser keeps the element content of a <see langword="let"/> as data and reports a <see langword="let"/> that has both a value and content.</summary>
public class LetContentParseTests
{
    private static (SchematronLet? Let, int Problems, DiagnosticCode? Code) ParseLet(params object[] content)
    {
        var (schema, problems, _) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("let", 2, 3, content), E("pattern", 3, 3))
        );
        return (
            schema is { Lets.Count: > 0 } ? schema.Lets[0] : null,
            problems.Count,
            problems.Count > 0 ? problems[0].Code : null
        );
    }

    [Test]
    public async Task Let_WithElementContent_KeepsTheContentAsData()
    {
        var (let, problems, _) = ParseLet(A("name", "v"), Plain("item", 2, 20, A("n", "1"), "one"));

        await Assert.That(problems).IsEqualTo(0);
        await Assert.That(let!.Content).IsNotNull();
        await Assert.That(let.Content!.Count).IsEqualTo(1);
        var item = (SchematronForeignElement)let.Content[0];
        await Assert.That(item.LocalName).IsEqualTo("item");
        await Assert.That(item.Attributes.Single().Key).IsEqualTo("n");
        await Assert.That(((SchematronText)item.Children.Single()).Text).IsEqualTo("one");
    }

    [Test]
    public async Task Let_WithElementContent_HasNoValueExpression()
    {
        var (let, _, _) = ParseLet(A("name", "v"), Plain("item", 2, 20));

        await Assert.That(let!.Value).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Let_WithTextContent_IsContent()
    {
        var (let, problems, _) = ParseLet(A("name", "v"), "some text");

        await Assert.That(problems).IsEqualTo(0);
        await Assert.That(((SchematronText)let!.Content!.Single()).Text).IsEqualTo("some text");
    }

    [Test]
    public async Task Let_WithXsltElementContent_KeepsTheElementAsData()
    {
        var (let, problems, _) = ParseLet(A("name", "v"), Xsl("message", 2, 20, A("terminate", "yes"), "text"));

        await Assert.That(problems).IsEqualTo(0);
        var message = (SchematronForeignElement)let!.Content!.Single();
        await Assert.That(message.NamespaceUri).IsEqualTo(XsltNamespace);
        await Assert.That(message.LocalName).IsEqualTo("message");
    }

    [Test]
    public async Task Let_WithValue_HasNoContent()
    {
        var (let, problems, _) = ParseLet(A("name", "v"), A("value", "1"));

        await Assert.That(problems).IsEqualTo(0);
        await Assert.That(let!.Content).IsNull();
    }

    [Test]
    public async Task Let_WithValueAndWhiteSpace_HasNoContent()
    {
        var (let, problems, _) = ParseLet(A("name", "v"), A("value", "1"), "\n   ");

        await Assert.That(problems).IsEqualTo(0);
        await Assert.That(let!.Content).IsNull();
    }

    [Test]
    public async Task Let_WithValueAndElementContent_ReportsTheCodedErrorAtTheLet()
    {
        var (schema, problems, _) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("let", 2, 3, A("name", "v"), A("value", "1"), Plain("item", 2, 30)), E("pattern", 3, 3))
        );

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
        await Assert.That(problems[0].Location.Line).IsEqualTo(2);
        await Assert.That(problems[0].Location.Column).IsEqualTo(3);
        await Assert.That(problems[0].Message).Contains("not both");
        await Assert.That(schema).IsNull();
    }

    [Test]
    public async Task Let_WithValueAndTextContent_ReportsTheCodedError()
    {
        var (_, problems, code) = ParseLet(A("name", "v"), A("value", "1"), "text");

        await Assert.That(problems).IsEqualTo(1);
        await Assert.That(code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task Let_WithValueAndXsltContent_ReportsTheCodedErrorOnce()
    {
        var (_, problems, code) = ParseLet(A("name", "v"), A("value", "1"), Xsl("message", 2, 30), Plain("a", 2, 40));

        await Assert.That(problems).IsEqualTo(1);
        await Assert.That(code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task Let_WithoutValueAndWithoutContent_StillReportsTheMissingValue()
    {
        var (_, problems, code) = ParseLet(A("name", "v"), "  ");

        await Assert.That(problems).IsEqualTo(1);
        await Assert.That(code).IsEqualTo(SchematronDiagnosticCodes.AttributeMissing);
    }

    [Test]
    public async Task Let_InARuleAndAPhase_KeepsItsContentToo()
    {
        var (schema, problems, _) = CoreParserFixture.Parse(
            E(
                "schema",
                1,
                1,
                E("phase", 2, 3, A("id", "ph"), E("let", 2, 20, A("name", "p"), Plain("x", 2, 30))),
                E(
                    "pattern",
                    3,
                    3,
                    E("let", 3, 10, A("name", "q"), Plain("y", 3, 20)),
                    E("rule", 4, 5, A("context", "a"), E("let", 4, 20, A("name", "r"), Plain("z", 4, 30)))
                )
            )
        );

        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(schema!.Phases[0].Lets[0].Content).IsNotNull();
        await Assert.That(schema.Patterns[0].Lets[0].Content).IsNotNull();
        await Assert.That(schema.Patterns[0].Rules[0].Lets[0].Content).IsNotNull();
    }
}
