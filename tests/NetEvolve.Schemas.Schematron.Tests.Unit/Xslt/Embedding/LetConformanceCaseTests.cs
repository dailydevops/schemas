namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Embedding;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding.EmbeddingNodes;

/// <summary>
/// The case of the Schematron conformance suite <c>iso-rule-based-validation/let-value-element-content.xml</c>, as far as this package
/// reaches it: a <c>let</c> whose content is an <c>xsl:message</c> with <c>terminate="yes"</c> and the text after it. The case
/// parses, the value is a document node that holds the message as an element, nothing terminates, and the string value of the
/// variable contains the text of the message (the test of the second schema of the case, <c>contains($foobar, 'NOT terminate')</c>).
/// The end-to-end run of the case needs the XPath evaluator and the compile pipeline, which are not on main (#297), so the conformance
/// baseline is not changed. The test of the first schema, <c>count($foobar[self::xsl:message]) = 1</c>, holds for a sequence of the
/// content nodes and not for a document node; the document node is what the issue requires (design #150 EP-5, EP-8), see the
/// decisions on #289.
/// </summary>
public class LetConformanceCaseTests
{
    private static SchematronLet ParseCase()
    {
        var (schema, problems, _) = CoreParserFixture.Parse(
            E(
                "schema",
                1,
                1,
                E("ns", 2, 3, A("prefix", "xsl"), A("uri", XsltNamespace)),
                E(
                    "let",
                    3,
                    3,
                    A("name", "foobar"),
                    Xsl("message", 4, 7, A("terminate", "yes"), "This should NOT terminate the program"),
                    "ß\n      "
                ),
                E("pattern", 5, 3, E("rule", 6, 5, A("context", "/"), E("report", 7, 7, A("test", "true()"))))
            )
        );

        return problems.Count == 0 && schema is not null
            ? schema.Lets[0]
            : throw new System.InvalidOperationException("The case must parse.");
    }

    [Test]
    public async Task LetValueElementContent_ParsesWithoutAProblem()
    {
        var let = ParseCase();

        await Assert.That(let.Name).IsEqualTo("foobar");
        await Assert.That(let.Content!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task LetValueElementContent_IsADocumentThatHoldsTheMessageAsAnElement_AndDoesNotTerminate()
    {
        var run = new MiniRun();

        var status = new EmbeddedContent().TryBindLetContent(run, 0, ParseCase().Content!, out var problem);

        _ = run.TryGetValue(0, out var value);
        var document = value[0];
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(problem.Code).IsEqualTo(default(DiagnosticCode));
        await Assert.That(PathQuery.Select(in document, "/*").Count).IsEqualTo(1);
        await Assert
            .That(PathQuery.Describe(PathQuery.Select(in document, "/*")[0]))
            .IsEqualTo("Element:message=This should NOT terminate the program");
    }

    [Test]
    public async Task LetValueElementContent_StringValue_ContainsTheTextOfTheMessage()
    {
        var run = new MiniRun();
        _ = new EmbeddedContent().TryBindLetContent(run, 0, ParseCase().Content!, out _);

        _ = run.TryGetValue(0, out var value);
        _ = value[0].TryGetNode(out var document);

        await Assert.That(document.StringValue()).Contains("NOT terminate");
        await Assert.That(document.StringValue()).Contains("ß");
    }
}
