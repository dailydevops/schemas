namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Edition2025;

using NetEvolve.Schemas.Schematron.Model;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Edition2025.Edition2025Fixture;

public class Edition2025ForeignTests
{
    [Test]
    public async Task XsltElement_UnderSchema_IsPreserved()
    {
        var schema = ParseValid(
            Schema2025(
                Xsl(
                    "function",
                    2,
                    3,
                    A("name", "f:x"),
                    A("as", "xs:string"),
                    "lead",
                    Xsl("param", 3, 5, A("name", "a")),
                    Xsl("sequence", 4, 5)
                ),
                E("pattern", 5, 3)
            )
        );

        var element = schema.ForeignElements[0];

        await Assert.That(schema.ForeignElements.Count).IsEqualTo(1);
        await Assert.That(element.NamespaceUri).IsEqualTo(XslNamespace);
        await Assert.That(element.LocalName).IsEqualTo("function");
        await Assert.That(element.Attributes.Count).IsEqualTo(2);
        await Assert.That(element.Attributes[0].Key).IsEqualTo("name");
        await Assert.That(element.Attributes[0].Value).IsEqualTo("f:x");
        await Assert.That(element.Attributes[1].Key).IsEqualTo("as");
        await Assert.That(element.Children.Count).IsEqualTo(3);
        await Assert.That(((SchematronText)element.Children[0]).Text).IsEqualTo("lead");
        await Assert.That(((SchematronForeignElement)element.Children[1]).LocalName).IsEqualTo("param");
        await Assert.That(((SchematronForeignElement)element.Children[1]).Attributes[0].Value).IsEqualTo("a");
        await Assert.That(((SchematronForeignElement)element.Children[2]).LocalName).IsEqualTo("sequence");
        await AssertLocation(element, 2, 3);
        await AssertLocation(element.Children[1], 3, 5);
        await AssertLocation(element.Children[2], 4, 5);
        await Assert.That(schema.Patterns.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ForeignElements_UnderSchema_KeepDocumentOrder()
    {
        var schema = ParseValid(
            Schema2025(Xsl("key", 2, 3), E("pattern", 3, 3), Foreign("other", 4, 3), Xsl("variable", 5, 3))
        );

        await Assert.That(schema.ForeignElements.Count).IsEqualTo(3);
        await Assert.That(schema.ForeignElements[0].LocalName).IsEqualTo("key");
        await Assert.That(schema.ForeignElements[1].LocalName).IsEqualTo("other");
        await Assert.That(schema.ForeignElements[1].NamespaceUri).IsEqualTo(ForeignNamespace);
        await Assert.That(schema.ForeignElements[2].LocalName).IsEqualTo("variable");
        await Assert.That(schema.Patterns.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ForeignElement_WithoutContent_HasNoAttributesAndNoChildren()
    {
        var schema = ParseValid(Schema2025(Xsl("import", 2, 3), E("pattern", 3, 3)));

        await Assert.That(schema.ForeignElements[0].Attributes.Count).IsEqualTo(0);
        await Assert.That(schema.ForeignElements[0].Children.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ForeignElement_UnderLibrary_IsPreserved()
    {
        var (library, _, result) = ParseLibrary(E("library", 1, 1, Xsl("function", 2, 3, A("name", "f"))));

        await Assert.That(result).IsTrue();
        await Assert.That(library!.ForeignElements.Count).IsEqualTo(1);
        await Assert.That(library.ForeignElements[0].LocalName).IsEqualTo("function");
        await AssertLocation(library.ForeignElements[0], 2, 3);
    }

    [Test]
    public async Task ForeignElement_UnderPattern_IsSkipped()
    {
        var schema = ParseValid(Schema2025(E("pattern", 2, 3, Foreign("note", 3, 5))));

        await Assert.That(schema.ForeignElements.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(null)]
    [Arguments("xslt")]
    [Arguments("xslt1")]
    [Arguments("XSLT2")]
    [Arguments("xslt3")]
    public async Task XsltElement_InPattern_IsMisplaced_UnderXsltBinding(string? binding)
    {
        var root = binding is null
            ? Schema2025(E("pattern", 2, 3, Xsl("variable", 3, 5)))
            : SchemaWithBinding(binding, E("pattern", 2, 3, Xsl("variable", 3, 5)));

        await AssertSingle(root, SchematronDiagnosticCodes.ElementMisplaced, 3, 5);
    }

    [Test]
    public async Task XsltElement_InRule_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("pattern", 2, 3, E("rule", 3, 5, A("context", "a"), Xsl("variable", 4, 7)))),
            SchematronDiagnosticCodes.ElementMisplaced,
            4,
            7
        );

    [Test]
    public async Task XsltElement_InMessage_IsMisplaced() =>
        await AssertSingle(
            Schema2025(
                E(
                    "pattern",
                    2,
                    3,
                    E("rule", 3, 5, A("context", "a"), E("assert", 4, 7, A("test", "t"), "m", Xsl("value-of", 4, 20)))
                )
            ),
            SchematronDiagnosticCodes.ElementMisplaced,
            4,
            20
        );

    [Test]
    public async Task XsltElement_InGroup_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("group", 2, 3, Xsl("variable", 3, 5))),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            5
        );

    [Test]
    public async Task XsltElement_InPhase_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("phase", 2, 3, A("id", "p"), Xsl("variable", 3, 5)), E("pattern", 4, 3)),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            5
        );

    [Test]
    [Arguments("xpath")]
    [Arguments("xpath2")]
    public async Task XsltElement_InPattern_IsSkipped_UnderXpathBinding(string binding)
    {
        var (schema, problems, result) = Parse(
            SchemaWithBinding(
                binding,
                Xsl("function", 2, 3),
                E("pattern", 3, 3, Xsl("variable", 4, 5), E("rule", 5, 5, A("context", "a"), Xsl("variable", 6, 7)))
            )
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(schema!.ForeignElements.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ForeignElement_InPatternOfOtherNamespace_IsSkippedUnderXsltBinding()
    {
        var (_, problems, result) = Parse(Schema2025(E("pattern", 2, 3, Foreign("note", 3, 5))));

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task XsltElement_InLet_IsNotMisplaced()
    {
        var (_, problems, result) = Parse(
            Schema2025(E("let", 2, 3, A("name", "v"), A("value", "1"), Xsl("sequence", 2, 30)), E("pattern", 3, 3))
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task XsltElement_InLetWithoutValue_ReportsUnsupported() =>
        await AssertSingle(
            Schema2025(E("let", 2, 3, A("name", "v"), Xsl("sequence", 2, 30)), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.Unsupported,
            2,
            3
        );

    [Test]
    public async Task XsltElement_InLibraryPattern_IsMisplaced_WhenXsltBinding()
    {
        var (library, problems, result) = ParseLibrary(E("library", 1, 1, E("pattern", 2, 3, Xsl("variable", 3, 5))));

        await Assert.That(result).IsFalse();
        await Assert.That(library).IsNull();
        await AssertProblem(problems, SchematronDiagnosticCodes.ElementMisplaced, 3, 5);
    }

    [Test]
    public async Task XsltElement_InLibraryPattern_IsSkipped_WhenXpathBinding()
    {
        var (library, problems, result) = ParseLibrary(
            E("library", 1, 1, E("pattern", 2, 3, Xsl("variable", 3, 5))),
            xsltBinding: false
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(library!.Patterns.Count).IsEqualTo(1);
    }
}
