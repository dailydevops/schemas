namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Edition2025;

using System;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Edition2025.Edition2025Fixture;

public class Edition2025ProblemTests
{
    // Misplaced 2025 elements.
    [Test]
    public async Task Group_InRule_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("pattern", 2, 3, E("rule", 3, 5, A("context", "a"), E("group", 4, 7)))),
            SchematronDiagnosticCodes.ElementMisplaced,
            4,
            7
        );

    [Test]
    public async Task Group_InPattern_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("pattern", 2, 3, E("group", 3, 5))),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            5
        );

    [Test]
    public async Task Group_InGroup_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("group", 2, 3, E("group", 3, 5))),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            5
        );

    [Test]
    public async Task Rules_InPattern_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("pattern", 2, 3, E("rules", 3, 5))),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            5
        );

    [Test]
    public async Task Param_InRule_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("pattern", 2, 3, E("rule", 3, 5, A("context", "a"), E("param", 4, 7, A("name", "n"))))),
            SchematronDiagnosticCodes.ElementMisplaced,
            4,
            7
        );

    [Test]
    public async Task Property_InSchema_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("property", 2, 3, A("id", "p")), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.ElementMisplaced,
            2,
            3
        );

    [Test]
    public async Task Properties_InPattern_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("pattern", 2, 3, E("properties", 3, 5))),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            5
        );

    [Test]
    public async Task Properties_Twice_ReportsTheSecondAsMisplaced() =>
        await AssertSingle(
            Schema2025(E("properties", 2, 3), E("properties", 3, 3), E("pattern", 4, 3)),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            3
        );

    [Test]
    public async Task Library_InSchema_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("library", 2, 3), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.ElementMisplaced,
            2,
            3
        );

    [Test]
    public async Task Rule_InRules_AssertDirectly_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("rules", 2, 3, E("assert", 3, 5, A("test", "t")))),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            5
        );

    [Test]
    public async Task Name_InProperty_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("properties", 2, 3, E("property", 3, 5, A("id", "p"), E("name", 3, 20)))),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            20
        );

    [Test]
    public async Task Param_WithChildren_IsMisplaced() =>
        await AssertSingle(
            Schema2025(E("param", 2, 3, A("name", "n"), E("p", 2, 20))),
            SchematronDiagnosticCodes.ElementMisplaced,
            2,
            20
        );

    [Test]
    public async Task Property_InLibrary_IsMisplaced_WhenNotInProperties()
    {
        var (library, problems, result) = ParseLibrary(E("library", 1, 1, E("property", 2, 3, A("id", "p"))));

        await Assert.That(result).IsFalse();
        await Assert.That(library).IsNull();
        await AssertProblem(problems, SchematronDiagnosticCodes.ElementMisplaced, 2, 3);
    }

    [Test]
    [Arguments("ns")]
    [Arguments("phase")]
    public async Task SchemaOnlyElement_InLibrary_IsMisplaced(string local)
    {
        var (library, problems, result) = ParseLibrary(
            E("library", 1, 1, E(local, 2, 3, A("id", "x"), A("prefix", "p"), A("uri", "u")))
        );

        await Assert.That(result).IsFalse();
        await Assert.That(library).IsNull();
        await AssertProblem(problems, SchematronDiagnosticCodes.ElementMisplaced, 2, 3);
    }

    // Missing attributes.
    [Test]
    public async Task Param_WithoutName_ReportsMissing() =>
        await AssertSingle(
            Schema2025(E("param", 2, 3), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.AttributeMissing,
            2,
            3
        );

    [Test]
    public async Task Property_WithoutId_ReportsMissing() =>
        await AssertSingle(
            Schema2025(E("properties", 2, 3, E("property", 3, 5)), E("pattern", 4, 3)),
            SchematronDiagnosticCodes.AttributeMissing,
            3,
            5
        );

    [Test]
    public async Task Group_WithBadAbstract_ReportsInvalid() =>
        await AssertSingle(
            Schema2025(E("group", 2, 3, A("abstract", "maybe"))),
            SchematronDiagnosticCodes.AttributeInvalid,
            2,
            3
        );

    [Test]
    public async Task Rules_WithRuleWithoutContext_ReportsMissing() =>
        await AssertSingle(
            Schema2025(E("rules", 2, 3, E("rule", 3, 5))),
            SchematronDiagnosticCodes.AttributeMissing,
            3,
            5
        );

    // Edition gate.
    [Test]
    [Arguments("2016")]
    [Arguments(null)]
    public async Task Group_In2016Schema_ReportsEditionMismatch(string? edition) =>
        await AssertSingle(
            Schema(edition, E("group", 2, 3, E("rule", 3, 5, A("context", "a"))), E("pattern", 4, 3)),
            SchematronDiagnosticCodes.EditionMismatch,
            2,
            3
        );

    [Test]
    [Arguments("2016")]
    [Arguments(null)]
    public async Task Rules_In2016Schema_ReportsEditionMismatch(string? edition) =>
        await AssertSingle(
            Schema(edition, E("rules", 2, 3), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.EditionMismatch,
            2,
            3
        );

    [Test]
    [Arguments("2016")]
    [Arguments(null)]
    public async Task SchemaParam_In2016Schema_ReportsEditionMismatchAndIsNotParsed(string? edition)
    {
        var (schema, problems, result) = Parse(Schema(edition, E("param", 2, 3, A("name", "n")), E("pattern", 3, 3)));

        await Assert.That(result).IsFalse();
        await Assert.That(schema).IsNull();
        await AssertProblem(problems, SchematronDiagnosticCodes.EditionMismatch, 2, 3);
    }

    [Test]
    public async Task SchemaParam_In2016Schema_WithoutName_ReportsOnlyTheMismatch() =>
        await AssertSingle(
            Schema("2016", E("param", 2, 3), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.EditionMismatch,
            2,
            3
        );

    [Test]
    public async Task Library_In2016_ReportsEditionMismatchAtLibraryLocation()
    {
        var (library, problems, result) = ParseLibrary(
            E("library", 1, 1, E("pattern", 2, 3)),
            SchematronEdition.Iso2016
        );

        await Assert.That(result).IsFalse();
        await Assert.That(library).IsNull();
        await AssertProblem(problems, SchematronDiagnosticCodes.EditionMismatch, 1, 1);
    }

    [Test]
    public async Task Library_In2016_WithGroup_ReportsMismatchForLibraryAndGroup()
    {
        var (library, problems, result) = ParseLibrary(E("library", 1, 1, E("group", 2, 3)), SchematronEdition.Iso2016);

        await Assert.That(result).IsFalse();
        await Assert.That(library).IsNull();
        await Assert.That(problems.Count).IsEqualTo(2);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EditionMismatch);
        await Assert.That(problems[0].Location.Line).IsEqualTo(1);
        await Assert.That(problems[1].Code).IsEqualTo(SchematronDiagnosticCodes.EditionMismatch);
        await Assert.That(problems[1].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Properties_In2016Schema_AreNotGated()
    {
        var (_, problems, result) = Parse(
            Schema("2016", E("properties", 2, 3, E("property", 3, 5, A("id", "p"))), E("pattern", 4, 3))
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
    }

    // Library entry point.
    [Test]
    public async Task Library_RootIsSchema_ReportsRootInvalid()
    {
        var (library, problems, result) = ParseLibrary(E("schema", 1, 1));

        await Assert.That(result).IsFalse();
        await Assert.That(library).IsNull();
        await AssertProblem(problems, SchematronDiagnosticCodes.RootInvalid, 1, 1);
    }

    [Test]
    public async Task Library_DocumentWithoutRoot_ReportsRootInvalid()
    {
        var problems = new FakeProblemSink();

        var result = SchematronCoreParser.TryParseLibrary(
            BuildWithoutRoot(),
            null,
            SchematronEdition.Iso2025,
            true,
            problems,
            out var library
        );

        await Assert.That(result).IsFalse();
        await Assert.That(library).IsNull();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.RootInvalid);
    }

    [Test]
    public async Task Library_NullProblems_Throws() =>
        await Assert
            .That(() =>
                SchematronCoreParser.TryParseLibrary(
                    Build(E("library", 1, 1)),
                    null,
                    SchematronEdition.Iso2025,
                    true,
                    null!,
                    out _
                )
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Root_Library_StillReportsUnsupportedAsMainRoot()
    {
        var (schema, problems, result) = Parse(E("library", 1, 1));

        await Assert.That(result).IsFalse();
        await Assert.That(schema).IsNull();
        await AssertProblem(problems, SchematronDiagnosticCodes.Unsupported, 1, 1);
    }
}
