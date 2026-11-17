namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;

public class CoreParserEdgeTests
{
    [Test]
    public async Task Root_LibraryElement_ReportsUnsupported()
    {
        var (schema, problems, result) = CoreParserFixture.Parse(E("library", 1, 1));

        await Assert.That(result).IsFalse();
        await Assert.That(schema).IsNull();
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
    }

    [Test]
    public async Task Root_DocumentWithoutElement_ReportsRootInvalid()
    {
        var problems = new FakeProblemSink();

        var result = SchematronCoreParser.TryParse(BuildWithoutRoot(), null, problems, out var schema);

        await Assert.That(result).IsFalse();
        await Assert.That(schema).IsNull();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.RootInvalid);
    }

    [Test]
    public async Task TryParse_RootElementNode_Parses()
    {
        var problems = new FakeProblemSink();
        var root = Build(E("schema", 1, 1, E("pattern", 2, 2))).FirstChild;

        var result = SchematronCoreParser.TryParse(root, null, problems, out var schema);

        await Assert.That(result).IsTrue();
        await Assert.That(schema!.Patterns.Count).IsEqualTo(1);
        await Assert.That(schema.Location.Source).IsNull();
    }

    [Test]
    public async Task Location_TreeWithoutPositions_IsZero()
    {
        var problems = new FakeProblemSink();

        _ = SchematronCoreParser.TryParse(
            Build(E("schema", 4, 4, E("pattern", 5, 5)), trackPositions: false),
            CoreParserFixture.SystemId,
            problems,
            out var schema
        );

        await Assert.That(schema!.Location.Line).IsEqualTo(0);
        await Assert.That(schema.Location.Column).IsEqualTo(0);
        await Assert.That(schema.Location.Source).IsEqualTo(CoreParserFixture.SystemId);
    }

    [Test]
    public async Task Pattern_AbstractAndIsA_AreModeled()
    {
        var (schema, _, result) = CoreParserFixture.Parse(
            E(
                "schema",
                1,
                1,
                E("pattern", 2, 2, A("abstract", " true "), A("id", "ap"), A("documents", "doc")),
                E("pattern", 3, 2, A("is-a", "ap"), A("abstract", "0"))
            )
        );

        var abstractPattern = (SchematronPattern)schema!.Patterns[0];
        var instance = (SchematronPattern)schema.Patterns[1];

        await Assert.That(result).IsTrue();
        await Assert.That(abstractPattern.IsAbstract).IsTrue();
        await Assert.That(abstractPattern.Documents).IsEqualTo("doc");
        await Assert.That(instance.IsA).IsEqualTo("ap");
        await Assert.That(instance.IsAbstract).IsFalse();
    }

    [Test]
    public async Task Rule_TitleParagraphAndRawSlots_AreModeled()
    {
        var (schema, _, result) = CoreParserFixture.Parse(
            E(
                "schema",
                1,
                1,
                E(
                    "pattern",
                    2,
                    2,
                    E("title", 3, 3, "PT"),
                    E("p", 4, 3, "PP"),
                    E("rule", 5, 3, A("context", "a"), A("visit-each", "b"), E("title", 6, 5, "RT"), E("p", 7, 5, "RP"))
                )
            )
        );

        var pattern = (SchematronPattern)schema!.Patterns[0];
        var rule = pattern.Rules[0];

        await Assert.That(result).IsTrue();
        await Assert.That(pattern.Title).IsNotNull();
        await Assert.That(pattern.Paragraphs.Count).IsEqualTo(1);
        await Assert.That(rule.VisitEach).IsEqualTo("b");
        await Assert.That(rule.Title).IsNotNull();
        await Assert.That(rule.Items[0]).IsTypeOf<SchematronParagraph>();
    }

    [Test]
    public async Task Phase_ParagraphLetAndRawSlots_AreModeled()
    {
        var (schema, _, result) = CoreParserFixture.Parse(
            E(
                "schema",
                1,
                1,
                E(
                    "phase",
                    2,
                    2,
                    A("id", "ph"),
                    A("from", "f"),
                    A("when", "w"),
                    E("p", 3, 3, "x"),
                    E("let", 4, 3, A("name", "n"), A("value", "v"))
                ),
                E("pattern", 5, 2)
            )
        );

        var phase = schema!.Phases[0];

        await Assert.That(result).IsTrue();
        await Assert.That(phase.From).IsEqualTo("f");
        await Assert.That(phase.When).IsEqualTo("w");
        await Assert.That(phase.Paragraphs.Count).IsEqualTo(1);
        await Assert.That(phase.Lets.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Parts_DirectionAndSpan_AreModeled()
    {
        var (schema, _, result) = CoreParserFixture.Parse(
            E(
                "schema",
                1,
                1,
                E(
                    "pattern",
                    2,
                    2,
                    E(
                        "rule",
                        3,
                        3,
                        A("context", "a"),
                        E(
                            "report",
                            4,
                            5,
                            A("test", "t"),
                            E("dir", 5, 7, A("value", "rtl"), "d"),
                            E("span", 6, 7, A("class", "k"), "s"),
                            Foreign("skipped", 7, 7)
                        )
                    )
                )
            )
        );

        var parts = ((SchematronReport)((SchematronPattern)schema!.Patterns[0]).Rules[0].Items[0]).Message.Parts;

        await Assert.That(result).IsTrue();
        await Assert.That(parts.Count).IsEqualTo(2);
        await Assert.That(((SchematronDirection)parts[0]).Value).IsEqualTo("rtl");
        await Assert.That(((SchematronSpan)parts[1]).StyleClass).IsEqualTo("k");
    }

    [Test]
    public async Task Parts_NameInParagraph_IsMisplaced()
    {
        var (_, problems, result) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("p", 2, 3, E("name", 2, 9)), E("pattern", 3, 3))
        );

        await Assert.That(result).IsFalse();
        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task Parts_ValueOfWithoutSelect_ReportsMissing()
    {
        var (_, problems, _) = CoreParserFixture.Parse(
            E(
                "schema",
                1,
                1,
                E(
                    "pattern",
                    2,
                    2,
                    E("rule", 3, 3, A("context", "a"), E("assert", 4, 5, A("test", "t"), E("value-of", 4, 9)))
                )
            )
        );

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.AttributeMissing);
    }

    [Test]
    public async Task Let_WithoutValueOrContent_ReportsMissing()
    {
        var (_, problems, _) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("let", 2, 3, A("name", "v")), E("pattern", 3, 3))
        );

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.AttributeMissing);
    }

    [Test]
    public async Task Let_WithValueAndSchematronChild_IsMisplaced()
    {
        var (_, problems, _) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("let", 2, 3, A("name", "v"), A("value", "1"), E("p", 2, 30)), E("pattern", 3, 3))
        );

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task Diagnostics_NonDiagnosticChild_IsMisplaced()
    {
        var (_, problems, _) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("pattern", 2, 2), E("diagnostics", 3, 2, E("p", 4, 3)))
        );

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task Phase_PatternChild_IsMisplaced()
    {
        var (_, problems, _) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("phase", 2, 2, A("id", "x"), E("pattern", 3, 3)), E("pattern", 4, 2))
        );

        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task Flags_TabsAndNewlines_SplitIntoTokens()
    {
        var (schema, _, _) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("pattern", 2, 2, E("rule", 3, 3, A("context", "a"), A("flag", "\ta\r\nb  "))))
        );

        await Assert.That(((SchematronPattern)schema!.Patterns[0]).Rules[0].Flags).IsEquivalentTo(["a", "b"]);
    }

    [Test]
    public async Task Flags_WhitespaceOnly_IsEmpty()
    {
        var (schema, _, _) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("pattern", 2, 2, E("rule", 3, 3, A("context", "a"), A("flag", "  "))))
        );

        await Assert.That(((SchematronPattern)schema!.Patterns[0]).Rules[0].Flags.Count).IsEqualTo(0);
    }
}
