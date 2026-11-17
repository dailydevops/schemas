namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.CoreParserFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;

public class CoreParserInclusionTests
{
    private static async Task AssertSingle(XmlElem root, DiagnosticCode code, int line, int column)
    {
        var (schema, problems, result) = Parse(root);

        await Assert.That(result).IsFalse();
        await Assert.That(schema).IsNull();
        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(code);
        await Assert.That(problems[0].Location.Line).IsEqualTo(line);
        await Assert.That(problems[0].Location.Column).IsEqualTo(column);
    }

    [Test]
    public async Task Include_InSchema_IsParsedWithItsPositionAmongTheLists()
    {
        var (schema, problems, result) = Parse(
            E(
                "schema",
                1,
                1,
                E("pattern", 2, 3),
                E("let", 3, 3, A("name", "a"), A("value", "1")),
                E("include", 4, 3, A("href", "x.sch")),
                E("pattern", 5, 3)
            )
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
        var include = schema!.Includes[0];
        await Assert.That(include.Href).IsEqualTo("x.sch");
        await Assert.That(include.Index).IsEqualTo(1);
        await Assert.That(include.LetIndex).IsEqualTo(1);
        await Assert.That(include.ParameterIndex).IsEqualTo(0);
        await Assert.That(include.ForeignIndex).IsEqualTo(0);
        await Assert.That(include.Location.Line).IsEqualTo(4);
        await Assert.That(include.Location.Column).IsEqualTo(3);
        await Assert.That(include.Location.Source).IsEqualTo(SystemId);
    }

    [Test]
    public async Task Include_InPattern_IsParsedWithItsPositionAmongTheRules()
    {
        var (schema, _, result) = Parse(
            E(
                "schema",
                1,
                1,
                E("pattern", 2, 3, E("rule", 3, 5, A("context", "a")), E("include", 4, 5, A("href", "r.sch")))
            )
        );

        await Assert.That(result).IsTrue();
        var include = schema!.Patterns[0].Includes[0];
        await Assert.That(include.Href).IsEqualTo("r.sch");
        await Assert.That(include.Index).IsEqualTo(1);
    }

    [Test]
    public async Task Include_InRule_IsKeptInTheItems()
    {
        var (schema, _, result) = Parse(
            E(
                "schema",
                1,
                1,
                E(
                    "pattern",
                    2,
                    3,
                    E(
                        "rule",
                        3,
                        5,
                        A("context", "a"),
                        E("assert", 4, 7, A("test", "x")),
                        E("include", 5, 7, A("href", "a.sch"))
                    )
                )
            )
        );

        await Assert.That(result).IsTrue();
        var items = schema!.Patterns[0].Rules[0].Items;
        await Assert.That(items.Count).IsEqualTo(2);
        await Assert.That(((SchematronInclude)items[1]).Href).IsEqualTo("a.sch");
    }

    [Test]
    public async Task Include_InLibrary_IsParsed()
    {
        var node = Build(E("library", 1, 1, E("include", 2, 3, A("href", "x.sch"))));
        var problems = new FakeProblemSink();

        var ok = SchematronCoreParser.TryParseLibrary(
            node,
            SystemId,
            SchematronEdition.Iso2025,
            false,
            problems,
            out var library
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(library!.Includes[0].Href).IsEqualTo("x.sch");
    }

    [Test]
    public async Task Include_WithoutHref_ReportsAttributeMissing() =>
        await AssertSingle(
            E("schema", 1, 1, E("include", 2, 3), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.AttributeMissing,
            2,
            3
        );

    [Test]
    public async Task Include_WithSchematronChild_ReportsMisplaced() =>
        await AssertSingle(
            E("schema", 1, 1, E("include", 2, 3, A("href", "x"), E("p", 2, 20)), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.ElementMisplaced,
            2,
            20
        );

    [Test]
    public async Task Include_InDiagnostics_StaysUnsupported() =>
        await AssertSingle(
            E("schema", 1, 1, E("diagnostics", 2, 3, E("include", 3, 5, A("href", "x"))), E("pattern", 4, 3)),
            SchematronDiagnosticCodes.Unsupported,
            3,
            5
        );

    [Test]
    public async Task Include_InPhase_StaysUnsupported() =>
        await AssertSingle(
            E("schema", 1, 1, E("phase", 2, 3, A("id", "p"), E("include", 3, 5, A("href", "x"))), E("pattern", 4, 3)),
            SchematronDiagnosticCodes.Unsupported,
            3,
            5
        );

    [Test]
    public async Task Extends_TopLevel_StaysUnsupported() =>
        await AssertSingle(
            E("schema", 1, 1, E("extends", 2, 3, A("rule", "r")), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.Unsupported,
            2,
            3
        );

    [Test]
    public async Task Extends_InRule_IsParsedInBothForms()
    {
        var (schema, problems, result) = Parse(
            E(
                "schema",
                1,
                1,
                E(
                    "pattern",
                    2,
                    3,
                    E(
                        "rule",
                        3,
                        5,
                        A("context", "a"),
                        E("extends", 4, 7, A("rule", "r1")),
                        E("extends", 5, 7, A("href", "lib.sch"))
                    )
                )
            )
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
        var items = schema!.Patterns[0].Rules[0].Items;
        var byRule = (SchematronExtends)items[0];
        var byHref = (SchematronExtends)items[1];
        await Assert.That(byRule.Rule).IsEqualTo("r1");
        await Assert.That(byRule.Href).IsNull();
        await Assert.That(byRule.Location.Line).IsEqualTo(4);
        await Assert.That(byHref.Rule).IsNull();
        await Assert.That(byHref.Href).IsEqualTo("lib.sch");
    }

    [Test]
    public async Task Extends_WithNeitherRuleNorHref_ReportsAttributeMissing() =>
        await AssertSingle(
            E("schema", 1, 1, E("pattern", 2, 3, E("rule", 3, 5, A("context", "a"), E("extends", 4, 7)))),
            SchematronDiagnosticCodes.AttributeMissing,
            4,
            7
        );

    [Test]
    public async Task Extends_WithRuleAndHref_ReportsAttributeInvalid() =>
        await AssertSingle(
            E(
                "schema",
                1,
                1,
                E(
                    "pattern",
                    2,
                    3,
                    E("rule", 3, 5, A("context", "a"), E("extends", 4, 7, A("rule", "r"), A("href", "h")))
                )
            ),
            SchematronDiagnosticCodes.AttributeInvalid,
            4,
            7
        );

    [Test]
    [Arguments("Container", "library", SchematronEdition.Iso2025)]
    [Arguments("Container", "pattern", SchematronEdition.Iso2016)]
    [Arguments("Container", "group", SchematronEdition.Iso2025)]
    [Arguments("Pattern", "rule", SchematronEdition.Iso2016)]
    [Arguments("Rule", "assert", SchematronEdition.Iso2016)]
    [Arguments("Rule", "report", SchematronEdition.Iso2016)]
    [Arguments("Rule", "p", SchematronEdition.Iso2016)]
    [Arguments("Library", "library", SchematronEdition.Iso2025)]
    public async Task Fragment_RootAllowedAtThePlace_IsParsed(string targetName, string root, SchematronEdition edition)
    {
        var target = Enum.Parse<FragmentTarget>(targetName);
        var problems = new FakeProblemSink();
        var node = Build(E(root, 1, 1, A("test", "x"), A("context", "c"), A("id", "i")));

        var status = SchematronCoreParser.TryParseFragment(
            node,
            SystemId,
            edition,
            false,
            target,
            problems,
            out var fragment,
            out var name
        );

        await Assert.That(status).IsEqualTo(FragmentStatus.Parsed);
        await Assert.That(fragment).IsNotNull();
        await Assert.That(name).IsEqualTo(root);
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("Container", "phase", "Unsupported")]
    [Arguments("Container", "ns", "Unsupported")]
    [Arguments("Container", "extends", "Unsupported")]
    [Arguments("Container", "rule", "WrongRoot")]
    [Arguments("Container", "schema", "WrongRoot")]
    [Arguments("Pattern", "title", "Unsupported")]
    [Arguments("Pattern", "pattern", "WrongRoot")]
    [Arguments("Rule", "title", "Unsupported")]
    [Arguments("Rule", "let", "Unsupported")]
    [Arguments("Rule", "extends", "Unsupported")]
    [Arguments("Rule", "rule", "WrongRoot")]
    [Arguments("Library", "phase", "WrongRoot")]
    [Arguments("Library", "pattern", "WrongRoot")]
    public async Task Fragment_RootNotAllowedAtThePlace_ReportsNothingAndNamesTheStatus(
        string targetName,
        string root,
        string expectedName
    )
    {
        var target = Enum.Parse<FragmentTarget>(targetName);
        var expected = Enum.Parse<FragmentStatus>(expectedName);
        var problems = new FakeProblemSink();

        var status = SchematronCoreParser.TryParseFragment(
            Build(E(root, 1, 1)),
            SystemId,
            SchematronEdition.Iso2025,
            false,
            target,
            problems,
            out var fragment,
            out var name
        );

        await Assert.That(status).IsEqualTo(expected);
        await Assert.That(fragment).IsNull();
        await Assert.That(name).IsEqualTo(root);
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fragment_GroupInA2016Schema_ReportsEditionMismatch()
    {
        var problems = new FakeProblemSink();

        var status = SchematronCoreParser.TryParseFragment(
            Build(E("group", 1, 1)),
            SystemId,
            SchematronEdition.Iso2016,
            false,
            FragmentTarget.Container,
            problems,
            out var fragment,
            out _
        );

        await Assert.That(status).IsEqualTo(FragmentStatus.Failed);
        await Assert.That(fragment).IsNull();
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EditionMismatch);
    }

    [Test]
    public async Task Fragment_ForeignRoot_IsWrongRoot()
    {
        var status = SchematronCoreParser.TryParseFragment(
            Build(Foreign("x", 1, 1)),
            SystemId,
            SchematronEdition.Iso2025,
            false,
            FragmentTarget.Container,
            new FakeProblemSink(),
            out _,
            out var name
        );

        await Assert.That(status).IsEqualTo(FragmentStatus.WrongRoot);
        await Assert.That(name).IsEqualTo("x");
    }

    [Test]
    public async Task Fragment_DocumentWithoutRoot_ReportsRootInvalid()
    {
        var problems = new FakeProblemSink();

        var status = SchematronCoreParser.TryParseFragment(
            BuildWithoutRoot(),
            SystemId,
            SchematronEdition.Iso2025,
            false,
            FragmentTarget.Container,
            problems,
            out _,
            out _
        );

        await Assert.That(status).IsEqualTo(FragmentStatus.Failed);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.RootInvalid);
    }
}
