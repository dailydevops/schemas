namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;

/// <summary>Reporting, guards, and the inputs a particle restriction check must survive: unresolved terms and circular group definitions.</summary>
public class ParticleRestrictionReportTests
{
    private readonly ParticleRestriction _check = Checker();
    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private IReadOnlyList<ValidationIssue> Issues => _collector.ToResult().Issues;

    private static Particle Unresolved(int line = 1) =>
        Particle.CreateReference(At(line), 1, 1, XsdSymbolSpace.Elements, new XPathQName(Tns, "ref"));

    // A group whose definition contains a reference to itself, which the resolver reports as mg-props-correct.2.
    private static Particle Circular()
    {
        var reference = Particle.CreateReference(At(1), 1, 1, XsdSymbolSpace.Groups, new XPathQName(Tns, "g"));
        var group = ModelGroup.Create(ModelGroupCompositor.Sequence, At(1), [reference, El("a")]);
        var definition = ModelGroupDefinition.Create(new XPathQName(Tns, "g"), At(1), group);
        reference.Bind(0, definition);
        return Particle.Create(At(1), 1, 1, definition);
    }

    [Test]
    public async Task Report_OfAValidRestriction_AddsNothing()
    {
        _check.Report(El("a"), El("a", 0, 1), new XsdIssueReporter(_collector));

        await Assert.That(Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Report_OfAViolation_AddsALocatedIssueWithTheRuleAndTheClause()
    {
        _check.Report(
            Seq([El("a", line: 11), El("x", line: 12)], line: 10),
            Seq([El("a"), El("b")]),
            new XsdIssueReporter(_collector)
        );

        await Assert.That(Issues).Count().IsEqualTo(1);
        await Assert.That(Issues[0].Code).IsEqualTo(XsdRuleCodes.RcaseRecurse);
        await Assert.That(FakeRules.Find(Issues[0].Code).Id).IsEqualTo("rcase-Recurse");
        await Assert.That(Issues[0].Constraint).IsEqualTo("rcase-Recurse.2.2");
        await Assert.That(Issues[0].Location.Line).IsEqualTo(12);
        await Assert.That(Issues[0].Message).IsNotEmpty();
    }

    [Test]
    public async Task EveryRuleOfTheTable_IsInTheCatalog()
    {
        DiagnosticCode[] codes =
        [
            XsdRuleCodes.CosParticleRestrict,
            XsdRuleCodes.RangeOk,
            XsdRuleCodes.RcaseNameAndTypeOK,
            XsdRuleCodes.RcaseNSCompat,
            XsdRuleCodes.RcaseRecurseAsIfGroup,
            XsdRuleCodes.RcaseNSSubset,
            XsdRuleCodes.RcaseNSRecurseCheckCardinality,
            XsdRuleCodes.RcaseRecurse,
            XsdRuleCodes.RcaseRecurseLax,
            XsdRuleCodes.RcaseRecurseUnordered,
            XsdRuleCodes.RcaseMapAndSum,
        ];

        foreach (var code in codes)
        {
            await Assert.That(FakeRules.Find(code).Id).IsNotEmpty();
        }
    }

    [Test]
    public async Task Guards_RejectNullArguments()
    {
        await Assert.That(() => new ParticleRestriction(null!, Values(XsdVersion.V10))).Throws<ArgumentNullException>();
        await Assert
            .That(() => new ParticleRestriction(new TypeDerivationOk(XsdVersion.V10), null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => _check.Check(null!, El("a"))).Throws<ArgumentNullException>();
        await Assert.That(() => _check.Check(El("a"), null!)).Throws<ArgumentNullException>();
        await Assert.That(() => _check.Report(El("a"), El("a"), null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new ParticleViolation(XsdRuleCodes.RangeOk, null!, "m", At(1)))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ParticleViolation(XsdRuleCodes.RangeOk, "c", null!, At(1)))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Violation_WithoutAViolation_IsNone()
    {
        await Assert.That(ParticleViolation.None.IsViolation).IsFalse();
        await Assert.That(ParticleViolation.None.Clause).IsNull();
    }

    [Test]
    public async Task UnresolvedTerm_IsSkipped_BecauseTheResolverReportsIt()
    {
        await AssertValid(_check.Check(Unresolved(), El("a")));
        await AssertValid(_check.Check(El("a"), Unresolved()));
        await AssertValid(_check.Check(Seq([El("a"), Unresolved()]), Seq([El("a"), El("b", 0, 1)])));
    }

    [Test]
    public async Task CircularGroupDefinition_IsSkipped_BecauseTheResolverReportsIt()
    {
        await AssertValid(_check.Check(Circular(), Circular()));
        await AssertValid(_check.Check(Circular(), El("a")));
        await AssertValid(_check.Check(El("a"), Circular()));
        await AssertValid(_check.Check(Seq([El("a"), Circular()]), Seq([El("a"), El("b", 0, 1)])));
    }

    [Test]
    public async Task GroupDefinition_UsedTwice_IsNotCircular()
    {
        var shared = ModelGroupDefinition.Create(
            new XPathQName(Tns, "shared"),
            At(1),
            ModelGroup.Create(ModelGroupCompositor.Sequence, At(1), [El("a"), El("b")])
        );
        Particle Use() => Particle.Create(At(1), 1, 1, shared);

        await AssertValid(_check.Check(Seq([Use(), Use()]), Seq([Use(), Use()])));
        await AssertViolation(
            _check.Check(Seq([Use(), Use()], line: 10), Seq([Use(), Use(), Use()])),
            XsdRuleCodes.RcaseRecurse,
            "rcase-Recurse.2.2",
            10
        );
    }
}
