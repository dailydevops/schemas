namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding.EmbeddingNodes;

/// <summary>
/// The parser keeps <c>xsl:copy-of</c> in the content of a <c>property</c> under an XSLT binding, skips it under an XPath binding,
/// and still rejects every other XSLT element and every other place.
/// </summary>
public class PropertyCopyOfParseTests
{
    private static (SchematronSchema? Schema, System.Collections.Generic.List<SchematronProblem> Problems) Parse(
        string? binding,
        params object[] propertyContent
    )
    {
        object[] root = binding is null
            ? [E("properties", 2, 3, E("property", 3, 5, [A("id", "p1"), .. propertyContent])), E("pattern", 4, 3)]
            :
            [
                A("queryBinding", binding),
                E("properties", 2, 3, E("property", 3, 5, [A("id", "p1"), .. propertyContent])),
                E("pattern", 4, 3),
            ];
        var (schema, problems, _) = CoreParserFixture.Parse(E("schema", 1, 1, root));
        return (schema, problems);
    }

    private static SchematronMessagePart[] Parts(SchematronSchema? schema)
    {
        var parts = schema!.Properties!.Items[0].Message.Parts;
        var result = new SchematronMessagePart[parts.Count];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = parts[i];
        }

        return result;
    }

    [Test]
    public async Task CopyOf_InAProperty_IsKeptWithItsSelect()
    {
        var (schema, problems) = Parse(null, Xsl("copy-of", 3, 20, A("select", "@attribute")));

        await Assert.That(problems.Count).IsEqualTo(0);
        var parts = Parts(schema);
        await Assert.That(parts.Length).IsEqualTo(1);
        var copyOf = (SchematronCopyOf)parts[0];
        await Assert.That(copyOf.Select).IsEqualTo("@attribute");
        await Assert.That(copyOf.Location.Line).IsEqualTo(3);
        await Assert.That(copyOf.Location.Column).IsEqualTo(20);
    }

    [Test]
    public async Task CopyOf_BetweenText_KeepsTheOrderOfTheParts()
    {
        var (schema, problems) = Parse(
            null,
            "before ",
            Xsl("copy-of", 3, 20, A("select", "a")),
            E("value-of", 3, 40, A("select", "b")),
            " after"
        );

        var parts = Parts(schema);
        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(parts.Length).IsEqualTo(4);
        await Assert.That(parts[0]).IsTypeOf<SchematronText>();
        await Assert.That(parts[1]).IsTypeOf<SchematronCopyOf>();
        await Assert.That(parts[2]).IsTypeOf<SchematronValueOf>();
        await Assert.That(parts[3]).IsTypeOf<SchematronText>();
    }

    [Test]
    public async Task CopyOf_WithFormattingWhiteSpaceAround_DropsTheWhiteSpaceParts()
    {
        var (schema, problems) = Parse(null, "\n  ", Xsl("copy-of", 3, 20, A("select", "@id")), "\n");

        await Assert.That(problems.Count).IsEqualTo(0);
        var parts = Parts(schema);
        await Assert.That(parts.Length).IsEqualTo(1);
        await Assert.That(parts[0]).IsTypeOf<SchematronCopyOf>();
    }

    [Test]
    public async Task CopyOf_WithFormattingWhiteSpaceBetweenMixedParts_KeepsTheTextThatHasContent()
    {
        var (schema, problems) = Parse(
            null,
            "\n  ",
            Xsl("copy-of", 3, 20, A("select", "a")),
            "\n  ",
            E("value-of", 4, 5, A("select", "b")),
            "\n  tail \n"
        );

        await Assert.That(problems.Count).IsEqualTo(0);
        var parts = Parts(schema);
        await Assert.That(parts.Length).IsEqualTo(3);
        await Assert.That(parts[0]).IsTypeOf<SchematronCopyOf>();
        await Assert.That(parts[1]).IsTypeOf<SchematronValueOf>();
        await Assert.That(((SchematronText)parts[2]).Text).IsEqualTo("\n  tail \n");
    }

    [Test]
    public async Task Property_WithoutCopyOf_KeepsItsWhiteSpaceText()
    {
        var (schema, problems) = Parse(null, "\n  ", E("value-of", 4, 5, A("select", "b")), "\n");

        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(Parts(schema).Length).IsEqualTo(3);
    }

    [Test]
    [Arguments("xslt")]
    [Arguments("xslt1")]
    [Arguments("xslt2")]
    [Arguments("xslt3")]
    public async Task CopyOf_UnderAnXsltBinding_IsKept(string binding)
    {
        var (schema, problems) = Parse(binding, Xsl("copy-of", 3, 20, A("select", "a")));

        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(Parts(schema).Length).IsEqualTo(1);
    }

    [Test]
    [Arguments("xpath")]
    [Arguments("xpath2")]
    public async Task CopyOf_UnderAnXPathBinding_IsSkipped(string binding)
    {
        var (schema, problems) = Parse(binding, "text", Xsl("copy-of", 3, 20, A("select", "a")));

        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(Parts(schema).Length).IsEqualTo(1);
        await Assert.That(Parts(schema)[0]).IsTypeOf<SchematronText>();
    }

    [Test]
    public async Task CopyOf_WithoutSelect_ReportsTheMissingAttributeAtTheElement()
    {
        var (_, problems) = Parse(null, Xsl("copy-of", 3, 20));

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.AttributeMissing);
        await Assert.That(problems[0].Location.Line).IsEqualTo(3);
        await Assert.That(problems[0].Location.Column).IsEqualTo(20);
    }

    [Test]
    [Arguments("type", "xs:string")]
    [Arguments("validation", "strict")]
    [Arguments("copy-namespaces", "no")]
    [Arguments("copy-namespaces", "false")]
    [Arguments("copy-namespaces", "0")]
    public async Task CopyOf_WithAnUnsupportedAttribute_IsTheUnsupportedConstructError(string name, string value)
    {
        var (_, problems) = Parse(null, Xsl("copy-of", 3, 20, A("select", "a"), A(name, value)));

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(problems[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    [Arguments("yes")]
    [Arguments("true")]
    [Arguments("1")]
    public async Task CopyOf_WithCopyNamespacesYes_IsKept(string value)
    {
        var (schema, problems) = Parse(null, Xsl("copy-of", 3, 20, A("select", "a"), A("copy-namespaces", value)));

        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(Parts(schema).Length).IsEqualTo(1);
    }

    [Test]
    public async Task CopyOf_WithAnInvalidCopyNamespacesValue_IsAnInvalidAttribute()
    {
        var (_, problems) = Parse(null, Xsl("copy-of", 3, 20, A("select", "a"), A("copy-namespaces", "maybe")));

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.AttributeInvalid);
    }

    [Test]
    public async Task OtherXsltElement_InAProperty_IsStillMisplaced()
    {
        var (_, problems) = Parse(null, Xsl("value-of", 3, 20, A("select", "a")));

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task CopyOf_InsideAnEmphasisOfAProperty_IsMisplaced()
    {
        var (_, problems) = Parse(null, E("emph", 3, 20, Xsl("copy-of", 3, 30, A("select", "a"))));

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task CopyOf_InADiagnostic_IsMisplaced()
    {
        var (_, problems, _) = CoreParserFixture.Parse(
            E(
                "schema",
                1,
                1,
                E("diagnostics", 2, 3, E("diagnostic", 3, 5, A("id", "d1"), Xsl("copy-of", 3, 20, A("select", "a")))),
                E("pattern", 4, 3)
            )
        );

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task CopyOf_InAnAssert_IsMisplaced()
    {
        var (_, problems, _) = CoreParserFixture.Parse(
            E(
                "schema",
                1,
                1,
                E(
                    "pattern",
                    2,
                    3,
                    E("rule", 3, 5, A("context", "a"), E("assert", 4, 7, A("test", "x"), Xsl("copy-of", 4, 20)))
                )
            )
        );

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }
}
