namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Testing.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// Everything that is wrong with a schema surfaces at <c>Build</c> as one <see cref="SchemaCompilationException"/> (SCH-55, #311, design
/// #137 AP-26): the problems of a stage are all reported, in document order, and a stage with problems stops the later ones.
/// </summary>
public class CompilationFailureTests
{
    private const string Pattern =
        "<pattern id=\"p\"><rule context=\"a\"><assert test=\"true()\">Never fails.</assert></rule></pattern>";

    [Test]
    public async Task SchemaWithTwoDefects_RaisesOneExceptionWithBothIssuesAndLocations()
    {
        var failure = BuildFailure(TwoDefects);

        await Assert.That(failure.Issues.Count).IsEqualTo(2);
        await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
        await Assert.That(failure.Issues[0].Constraint).IsEqualTo("XPST0003");
        await Assert.That(failure.Issues[1].Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
        await Assert.That(failure.Issues[1].Constraint).IsEqualTo("XPST0008");
        foreach (var issue in failure.Issues)
        {
            await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
            await Assert.That(issue.Location.Source).IsEqualTo(SchemaSystemId);
            await Assert.That(issue.Location.Line).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task Issues_AreBoundedByMaxIssues()
    {
        var builder = Builder(TwoDefects).WithLimits(ValidationLimits.Default.WithMaxIssues(1));

        var failure = Capture(() => _ = builder.Build());

        await Assert.That(failure.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TwoBuildsOfOneSchema_GiveTheSameIssues()
    {
        var first = BuildFailure(TwoDefects);
        var second = BuildFailure(TwoDefects);

        await Assert
            .That(string.Join("\n", second.Issues.Select(i => i.ToString())))
            .IsEqualTo(string.Join("\n", first.Issues.Select(i => i.ToString())));
    }

    [Test]
    public async Task SchemaThatIsNotWellFormed_ReportsTheXmlIssueOnly()
    {
        var failure = BuildFailure("<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\"><pattern>");

        await Assert.That(failure.Issues.Count).IsEqualTo(1);
        await Assert.That(failure.Issues[0].Location.Source).IsEqualTo(SchemaSystemId);
        await Assert.That(failure.Issues[0].Location.Line).IsGreaterThan(0);
        await Assert.That(failure.Issues[0].Code.Number).IsLessThan(3000);
    }

    [Test]
    public async Task RootThatIsNotASchema_IsRootInvalid()
    {
        var failure = BuildFailure("<foo/>");

        await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.RootInvalid);
    }

    [Test]
    public async Task UnsupportedQueryBinding_IsReportedWithTheSupportedNames()
    {
        var failure = BuildFailure(Wrap(Pattern, " queryBinding=\"xquery\""));

        await Assert.That(failure.Issues.Count).IsEqualTo(1);
        await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.BindingUnsupported);
        await Assert.That(failure.Issues[0].Message).Contains("xpath2");
    }

    [Test]
    public async Task UnknownPhase_IsPhaseUnknown()
    {
        var builder = Builder(Wrap(Pattern)).WithPhase("nope");

        var failure = Capture(() => _ = builder.Build());

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.PhaseUnknown);
    }

    [Test]
    public async Task UnknownParameter_IsParamUnknown()
    {
        var builder = Builder(Wrap(Pattern)).WithParameter("nope", "x");

        var failure = Capture(() => _ = builder.Build());

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.ParamUnknown);
        await Assert.That(failure.Issues.Single().Message).Contains("nope");
    }

    [Test]
    public async Task ParameterWithoutADefault_AndWithoutAValue_IsParamMissing()
    {
        var schema = Wrap("<param name=\"p\"/>" + Pattern, " schematronEdition=\"2025\"");

        var failure = BuildFailure(schema);

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.ParamMissing);
        await Assert.That(failure.Issues.Single().Message).Contains("'p'");
        await Assert.That(Builder(schema).WithParameter("p", "x").BuildNow()).IsNotNull();
    }

    [Test]
    public async Task IncludeWithTheDefaultResolver_IsDenied()
    {
        var failure = BuildFailure(Wrap("<pattern id=\"p\"><include href=\"rule.sch\"/></pattern>"));

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
    }

    [Test]
    public async Task IncludeThatTheResolverCannotFind_IsNotFound()
    {
        var builder = Builder(Wrap("<pattern id=\"p\"><include href=\"rule.sch\"/></pattern>"))
            .WithResolver(new InMemoryResourceResolver());

        var failure = Capture(() => _ = builder.Build());

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
    }

    [Test]
    public async Task AbstractPatternInstanceWithoutATarget_IsIdrefUnresolved()
    {
        var failure = BuildFailure(Wrap("<pattern id=\"p\" is-a=\"missing\"/>"));

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.IdrefUnresolved);
    }

    [Test]
    public async Task XsltDeclaration_ThatNoInstructionHandles_IsReportedAsUnsupported()
    {
        var failure = BuildFailure(
            Wrap("<xsl:template xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" match=\"/\"/>" + Pattern)
        );

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(failure.Issues.Single().Message).Contains("xsl:template");
    }

    [Test]
    public async Task XsltVariable_InASchema_Builds()
    {
        var validator = Build(
            Wrap("<xsl:variable xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" name=\"v\" select=\"1\"/>" + Pattern)
        );

        await Assert.That(validator.Schema.Patterns.Count).IsEqualTo(1);
    }

    [Test]
    public async Task XsltDeclaration_UnderAnXpathBinding_IsADataElement()
    {
        var validator = Build(
            Wrap(
                "<xsl:variable xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" name=\"v\" select=\"1\"/>" + Pattern,
                " queryBinding=\"xpath2\""
            )
        );

        await Assert.That(validator.Schema.Patterns.Count).IsEqualTo(1);
    }

    [Test]
    public async Task XIncludeThatTheResolverDenies_IsReportedByTheParse()
    {
        var failure = BuildFailure(
            Wrap("<xi:include xmlns:xi=\"http://www.w3.org/2001/XInclude\" href=\"part.xml\"/>" + Pattern)
        );

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
    }

    [Test]
    public async Task ExtendsOfAnUnknownRule_AndAnUnknownAbstractPattern_AreBothReported()
    {
        var failure = BuildFailure(
            Wrap(
                "<pattern id=\"p\"><rule context=\"a\"><extends rule=\"missing\"/></rule></pattern><pattern id=\"q\" is-a=\"none\"/>"
            )
        );

        await Assert.That(failure.Issues.Count).IsEqualTo(2);
        await Assert
            .That(failure.Issues.All(issue => issue.Code == SchematronDiagnosticCodes.IdrefUnresolved))
            .IsTrue();
    }

    [Test]
    public async Task ElementOfAnotherNamespace_IsIgnored()
    {
        var validator = Build(Wrap("<x:meta xmlns:x=\"urn:x\"/>" + Pattern));

        await Assert.That(validator.Schema.Patterns.Count).IsEqualTo(1);
    }

    [Test]
    public async Task SchemaWithAnUnknownEdition_IsEditionUnknown()
    {
        var failure = BuildFailure(Wrap(Pattern, " schematronEdition=\"2031\""));

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.EditionUnknown);
    }

    [Test]
    public async Task IncludedRule_IsPartOfTheCompiledSchema()
    {
        var resolver = new InMemoryResourceResolver().Add(
            "file:///rule.sch",
            Encoding.UTF8.GetBytes(
                "<rule xmlns=\"http://purl.oclc.org/dsdl/schematron\" context=\"a\"><assert test=\"b\">A b.</assert></rule>"
            )
        );
        var validator = Builder(Wrap("<pattern id=\"p\"><include href=\"rule.sch\"/></pattern>"))
            .WithResolver(resolver)
            .BuildNow();

        var result = Validate(validator, "<a/>");

        await Assert.That(result.Issues.Single().Message).IsEqualTo("A b.");
    }

    [Test]
    public async Task VisitEachOnAnEvaluatedRule_IsUnsupported()
    {
        var schema = Wrap(
            "<pattern id=\"p\"><rule context=\"@a\" visit-each=\"tokenize(.)\"><assert test=\"true()\">m</assert></rule></pattern>",
            " schematronEdition=\"2025\""
        );

        var failure = BuildFailure(schema);

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
        await Assert.That(failure.Issues.Single().Message).Contains("visit-each");
    }

    [Test]
    public async Task DocumentsOnAnEvaluatedPattern_IsDocumentDenied()
    {
        var schema = Wrap(
            "<pattern id=\"p\" documents=\"'other.xml'\"><rule context=\"a\"><assert test=\"true()\">m</assert></rule></pattern>",
            " schematronEdition=\"2025\""
        );

        var failure = BuildFailure(schema);

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.DocumentDenied);
    }

    [Test]
    public async Task VisitEachInAPatternOutsideTheSelectedPhase_Builds()
    {
        var schema = Wrap(
            "<phase id=\"on\"><active pattern=\"p1\"/></phase>"
                + "<pattern id=\"p1\"><rule context=\"a\"><assert test=\"true()\">m</assert></rule></pattern>"
                + "<pattern id=\"p2\"><rule context=\"@a\" visit-each=\"tokenize(.)\"><assert test=\"true()\">m</assert></rule></pattern>",
            " schematronEdition=\"2025\""
        );

        var validator = Builder(schema).WithPhase("on").BuildNow();

        await Assert.That(validator).IsNotNull();
    }

    [Test]
    public async Task VisitEachInAPatternOfTheSelectedPhase_IsUnsupported()
    {
        var schema = Wrap(
            "<phase id=\"on\"><active pattern=\"p2\"/></phase>"
                + "<pattern id=\"p1\"><rule context=\"a\"><assert test=\"true()\">m</assert></rule></pattern>"
                + "<pattern id=\"p2\"><rule context=\"@a\" visit-each=\"tokenize(.)\"><assert test=\"true()\">m</assert></rule></pattern>",
            " schematronEdition=\"2025\""
        );

        var failure = Capture(() => _ = Builder(schema).WithPhase("on").Build());

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
    }
}
