namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;

using System;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;

public class CoreParserProblemTests
{
    private static async Task AssertSingle(XmlElem root, DiagnosticCode code, int line, int column)
    {
        var (schema, problems, result) = CoreParserFixture.Parse(root);

        await Assert.That(result).IsFalse();
        await Assert.That(schema).IsNull();
        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(code);
        await Assert.That(problems[0].Location.Source).IsEqualTo(CoreParserFixture.SystemId);
        await Assert.That(problems[0].Location.Line).IsEqualTo(line);
        await Assert.That(problems[0].Location.Column).IsEqualTo(column);
        await Assert.That(problems[0].Message).IsNotEmpty();
    }

    [Test]
    public async Task Misplaced_AssertInSchema_ReportsCodeAndLocation() =>
        await AssertSingle(
            E("schema", 1, 1, E("assert", 2, 3, A("test", "x")), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.ElementMisplaced,
            2,
            3
        );

    [Test]
    public async Task Misplaced_PatternInRule_ReportsCodeAndLocation() =>
        await AssertSingle(
            E("schema", 1, 1, E("pattern", 2, 3, E("rule", 3, 5, A("context", "a"), E("pattern", 4, 7)))),
            SchematronDiagnosticCodes.ElementMisplaced,
            4,
            7
        );

    [Test]
    public async Task Misplaced_NsInPattern_ReportsCodeAndLocation() =>
        await AssertSingle(
            E("schema", 1, 1, E("pattern", 2, 3, E("ns", 3, 5, A("prefix", "x"), A("uri", "u")))),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            5
        );

    [Test]
    public async Task Misplaced_RuleInSchema_ReportsCodeAndLocation() =>
        await AssertSingle(
            E("schema", 1, 1, E("rule", 2, 3, A("context", "a")), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.ElementMisplaced,
            2,
            3
        );

    [Test]
    public async Task Misplaced_ValueOfInTitle_ReportsCodeAndLocation() =>
        await AssertSingle(
            E("schema", 1, 1, E("title", 2, 3, E("value-of", 2, 10, A("select", "."))), E("pattern", 3, 3)),
            SchematronDiagnosticCodes.ElementMisplaced,
            2,
            10
        );

    [Test]
    public async Task Misplaced_SecondTitle_ReportsCodeAndLocation() =>
        await AssertSingle(
            E("schema", 1, 1, E("title", 2, 3), E("title", 3, 3), E("pattern", 4, 3)),
            SchematronDiagnosticCodes.ElementMisplaced,
            3,
            3
        );

    [Test]
    public async Task Unknown_ElementInSchematronNamespace_ReportsCodeAndLocation() =>
        await AssertSingle(
            E("schema", 1, 1, E("pattern", 2, 3, E("frobnicate", 3, 5))),
            SchematronDiagnosticCodes.ElementUnknown,
            3,
            5
        );

    // Flipped from Deferred_IncludeElement_ReportsUnsupported (#200): include is parsed since #202.
    [Test]
    public async Task Include_InSchema_IsParsedAndNoLongerReportsUnsupported()
    {
        var (schema, problems, result) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("include", 2, 3, A("href", "x.sch")), E("pattern", 3, 3))
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(schema!.Includes.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Root_NotSchema_ReportsRootInvalid() =>
        await AssertSingle(E("pattern", 1, 1), SchematronDiagnosticCodes.RootInvalid, 1, 1);

    [Test]
    public async Task Root_ForeignNamespace_ReportsRootInvalid() =>
        await AssertSingle(Foreign("schema", 1, 1), SchematronDiagnosticCodes.RootInvalid, 1, 1);

    [Test]
    public async Task Attribute_AssertWithoutTest_ReportsMissing() =>
        await AssertSingle(
            E("schema", 1, 1, E("pattern", 2, 3, E("rule", 3, 5, A("context", "a"), E("assert", 4, 7)))),
            SchematronDiagnosticCodes.AttributeMissing,
            4,
            7
        );

    [Test]
    public async Task Attribute_UnknownEdition_ReportsEditionUnknown() =>
        await AssertSingle(
            E("schema", 1, 1, A("schematronEdition", "1999"), E("pattern", 2, 3)),
            SchematronDiagnosticCodes.EditionUnknown,
            1,
            1
        );

    [Test]
    public async Task Attribute_InvalidAbstract_ReportsInvalid() =>
        await AssertSingle(
            E("schema", 1, 1, E("pattern", 2, 3, A("abstract", "maybe"))),
            SchematronDiagnosticCodes.AttributeInvalid,
            2,
            3
        );

    [Test]
    public async Task Problems_AreCollectedInDocumentOrder()
    {
        var (_, problems, result) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("assert", 2, 3), E("pattern", 3, 3, E("frobnicate", 4, 5)))
        );

        await Assert.That(result).IsFalse();
        await Assert.That(problems.Count).IsEqualTo(2);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
        await Assert.That(problems[1].Code).IsEqualTo(SchematronDiagnosticCodes.ElementUnknown);
    }

    [Test]
    public async Task TryParse_NullProblemSink_Throws()
    {
        var node = Build(E("schema", 1, 1));

        await Assert
            .That(() => SchematronCoreParser.TryParse(node, null, null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryParse_DefaultNode_Throws() =>
        await Assert
            .That(() => SchematronCoreParser.TryParse(default, null, new FakeProblemSink(), out _))
            .Throws<ArgumentNullException>();
}
