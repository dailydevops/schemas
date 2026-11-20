namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Subordinate;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Subordinate.SubordinateFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion.InclusionFixture;

/// <summary>Decision Q6 of #106: <c>visit-each</c> is rejected as unsupported until #387.</summary>
public class VisitEachTests
{
    private static readonly IResourceResolver Deny = DenyAllResourceResolver.Instance;

    [Test]
    public async Task Rule_VisitEach_IsRejectedAsUnsupported()
    {
        var schema = Load(Schema(Pattern("p1", Failing(" visit-each=\"b\""))));

        var (ok, problems) = Check(schema.Patterns, Deny);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
        await Assert.That(problems.Problems[0].Message).Contains("visit-each");
        await Assert.That(problems.Problems[0].Location).IsEqualTo(schema.Patterns[0].Rules[0].Location);
    }

    [Test]
    public async Task Rule_WithoutVisitEach_IsAccepted()
    {
        var (ok, problems) = Check(Schema(Pattern("p1", Failing())), Deny);

        await Assert.That(ok).IsTrue();
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Rule_VisitEach_EveryRuleIsReported_InDocumentOrder()
    {
        var xml = Schema(
            Pattern("p1", Failing(" visit-each=\"b\"") + Failing()) + Pattern("p2", Failing(" visit-each=\"c\""))
        );
        var schema = Load(xml);

        var (ok, problems) = Check(schema.Patterns, Deny);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(2);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(schema.Patterns[0].Rules[0].Location);
        await Assert.That(problems.Problems[1].Location).IsEqualTo(schema.Patterns[1].Rules[0].Location);
    }

    [Test]
    public async Task Rule_VisitEach_InAGroup_IsRejectedAsUnsupported()
    {
        var group = El("group", " id=\"g1\"", Failing(" visit-each=\"b\""));

        var (ok, problems) = Check(Schema(group, Edition2025), Deny);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
    }

    [Test]
    public async Task Rule_VisitEach_InAnAbstractPattern_IsReportedOnlyThroughItsInstance()
    {
        var abstractPattern = El("pattern", " id=\"abs\" abstract=\"true\"", Failing(" visit-each=\"b\""));
        var instance = El("pattern", " id=\"p1\" is-a=\"abs\"", string.Empty);
        var schema = Load(Schema(abstractPattern + instance));
        var expansion = new FakeProblemSink();
        _ = AbstractPatternExpander.TryExpand(schema.Patterns, expansion, out var patterns);

        var (ok, problems) = Check(patterns, Deny);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Rule_VisitEach_EmptyValue_IsTreatedAsAbsent()
    {
        var (ok, _) = Check(Schema(Pattern("p1", Failing(" visit-each=\"\""))), Deny);

        await Assert.That(ok).IsTrue();
    }
}
