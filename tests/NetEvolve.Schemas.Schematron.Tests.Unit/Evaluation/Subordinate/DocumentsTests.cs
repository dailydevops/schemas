namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Subordinate;

using System;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Evaluation.Subordinate;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Subordinate.SubordinateFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion.InclusionFixture;

/// <summary>Decision Q7 of #106 and design #137 AP-22: <c>documents</c> is denied or unsupported, and never reaches a resolver.</summary>
public class DocumentsTests
{
    private static string Documents(string attributes = " documents=\"other.xml\"") =>
        Schema(El("pattern", " id=\"p1\"" + attributes, Failing()));

    [Test]
    public async Task Pattern_Documents_DefaultResolver_DeniesAll()
    {
        var schema = Load(Documents());

        var (ok, problems) = Check(schema.Patterns, DenyAllResourceResolver.Instance);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.DocumentDenied);
        await Assert.That(problems.Problems[0].Message).Contains("documents");
        await Assert.That(problems.Problems[0].Location).IsEqualTo(schema.Patterns[0].Location);
    }

    [Test]
    public async Task Pattern_Documents_AllowingResolver_IsUnsupported_AndNeverCallsTheResolver()
    {
        var resources = new FakeResources().Add("other.xml", "<a/>");

        var (ok, problems) = Check(Documents(), resources);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
        await Assert.That(problems.Problems[0].Message).Contains("documents");
        await Assert.That(resources.Requests.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Pattern_WithoutDocuments_IsAccepted_AndNeverCallsTheResolver()
    {
        var resources = new FakeResources();

        var (ok, problems) = Check(Documents(string.Empty), resources);

        await Assert.That(ok).IsTrue();
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
        await Assert.That(resources.Requests.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Group_Documents_DefaultResolver_DeniesAll()
    {
        var group = El("group", " id=\"g1\" documents=\"other.xml\"", Failing());

        var (ok, problems) = Check(Schema(group, Edition2025), DenyAllResourceResolver.Instance);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.DocumentDenied);
    }

    [Test]
    public async Task Pattern_Documents_InAnAbstractPattern_IsNotReported()
    {
        var xml = Schema(El("pattern", " id=\"abs\" abstract=\"true\" documents=\"other.xml\"", Failing()));

        var (ok, _) = Check(xml, DenyAllResourceResolver.Instance);

        await Assert.That(ok).IsTrue();
    }

    [Test]
    public async Task Pattern_DocumentsAndVisitEach_ReportEveryProblem()
    {
        var xml = Schema(El("pattern", " id=\"p1\" documents=\"other.xml\"", Failing(" visit-each=\"b\"")));

        var (ok, problems) = Check(xml, DenyAllResourceResolver.Instance);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(2);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.DocumentDenied);
        await Assert.That(problems.Problems[1].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
    }

    [Test]
    public async Task TryCheck_NullArguments_Throw()
    {
        var sink = new FakeProblemSink();

        await Assert
            .That(() => SubordinateConstructs.TryCheck(null!, DenyAllResourceResolver.Instance, sink))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SubordinateConstructs.TryCheck([], null!, sink)).Throws<ArgumentNullException>();
        await Assert
            .That(() => SubordinateConstructs.TryCheck([], DenyAllResourceResolver.Instance, null!))
            .Throws<ArgumentNullException>();
    }
}
