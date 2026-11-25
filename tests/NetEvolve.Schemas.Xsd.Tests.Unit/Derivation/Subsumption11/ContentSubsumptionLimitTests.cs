namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Derivation.Subsumption11;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11.SubsumptionKit;

/// <summary>Inputs that are not compared, ceilings and the work limit, reporting, guards, and the reading of models that are not deterministic.</summary>
public class ContentSubsumptionLimitTests
{
    private const string Ceiling = "cos-content-act-restrict.ceiling";

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private IReadOnlyList<ValidationIssue> Issues => _collector.ToResult().Issues;

    private static Particle Unresolved(int line = 1) =>
        Particle.CreateReference(At(line), 1, 1, XsdSymbolSpace.Elements, new XPathQName(Tns, "ref"));

    private static Particle Circular()
    {
        var reference = Particle.CreateReference(At(1), 1, 1, XsdSymbolSpace.Groups, new XPathQName(Tns, "g"));
        var group = ModelGroup.Create(ModelGroupCompositor.Sequence, At(1), [reference, El("a")]);
        var definition = ModelGroupDefinition.Create(new XPathQName(Tns, "g"), At(1), group);
        reference.Bind(0, definition);
        return Particle.Create(At(1), 1, 1, definition);
    }

    private static async Task AssertVerdict(Particle restriction, Particle baseParticle, SubsumptionVerdict verdict)
    {
        var outcome = Check(restriction, baseParticle);

        await Assert.That(outcome.Verdict).IsEqualTo(verdict);
    }

    [Test]
    public async Task AllGroup_OnEitherSide_IsNotCompared()
    {
        await AssertVerdict(All([El("a"), El("b")]), Seq([El("a"), El("b")]), SubsumptionVerdict.NotCompared);
        await AssertVerdict(Seq([El("a")]), All([El("a"), El("b")]), SubsumptionVerdict.NotCompared);
        await AssertVerdict(Seq([El("a"), All([El("b")])]), Seq([El("a")]), SubsumptionVerdict.NotCompared);
    }

    [Test]
    public async Task UnresolvedTerm_IsNotCompared_BecauseTheResolverReportsIt()
    {
        await AssertVerdict(Unresolved(), El("a"), SubsumptionVerdict.NotCompared);
        await AssertVerdict(El("a"), Unresolved(), SubsumptionVerdict.NotCompared);
        await AssertVerdict(Seq([El("a"), Unresolved()]), Seq([El("a")]), SubsumptionVerdict.NotCompared);
    }

    [Test]
    public async Task CircularGroupDefinition_IsNotCompared_BecauseTheResolverReportsIt()
    {
        await AssertVerdict(Circular(), Circular(), SubsumptionVerdict.NotCompared);
        await AssertVerdict(Circular(), El("a"), SubsumptionVerdict.NotCompared);
        await AssertVerdict(El("a"), Circular(), SubsumptionVerdict.NotCompared);
        await Assert.That(Check(Circular(), El("a")).Violation.IsViolation).IsFalse();
    }

    [Test]
    public async Task WorkLimit_IsUndecided_AndNamesTheCeilingOfTheCheck()
    {
        var outcome = Subsumption(1).Check(Seq([El("a"), El("b"), El("c")], line: 4), Seq([El("a"), El("b"), El("c")]));

        await Assert.That(outcome.Verdict).IsEqualTo(SubsumptionVerdict.Undecided);
        await Assert.That(outcome.Violation.Code).IsEqualTo(XsdRuleCodes.CosContentActRestrict);
        await Assert.That(outcome.Violation.Clause).IsEqualTo(Ceiling);
        await Assert.That(outcome.Violation.Location.Line).IsEqualTo(4);
        await Assert.That(outcome.Work).IsGreaterThan(0);
    }

    [Test]
    public async Task WorkLimit_ThatIsNotReached_IsDecided()
    {
        var outcome = Subsumption(100).Check(Seq([El("a"), El("b")]), Seq([El("a"), El("b")]));

        await Assert.That(outcome.Verdict).IsEqualTo(SubsumptionVerdict.Subsumed);
    }

    [Test]
    public async Task OccurrenceBound_OfTwoToTheSixtyOrMore_IsUndecided()
    {
        await AssertVerdict(El("a", 1, 1L << 61), El("a", 1, 1L << 61), SubsumptionVerdict.Undecided);
        await AssertVerdict(El("a", 1L << 60, Unbounded), El("a", 1, Unbounded), SubsumptionVerdict.Undecided);
        await AssertVerdict(El("a", 2, 3), El("a", 1, 1L << 61), SubsumptionVerdict.Undecided);
    }

    [Test]
    public async Task ContentModel_WithMoreCountersThanTheCeiling_IsUndecided()
    {
        Particle Counted(int count)
        {
            var members = new Particle[count];
            for (var i = 0; i < count; i++)
            {
                members[i] = Seq([El("e" + i)], 2, 3);
            }

            return Seq(members);
        }

        await AssertVerdict(Counted(13), Counted(12), SubsumptionVerdict.Undecided);
        await AssertVerdict(Counted(3), Counted(3), SubsumptionVerdict.Subsumed);
    }

    [Test]
    public async Task ContentModel_WithMoreTransitionsThanTheAutomatonAccepts_IsUndecided()
    {
        var members = new Particle[1500];
        for (var i = 0; i < members.Length; i++)
        {
            members[i] = El("e" + i, 0, 1);
        }

        await AssertVerdict(Seq(members), El("a"), SubsumptionVerdict.Undecided);
    }

    private static Particle Doubled(Particle leaf)
    {
        // Each level uses the one below twice: 2^20 positions if they were expanded.
        var level = ModelGroupDefinition.Create(
            new XPathQName(Tns, "g0"),
            At(1),
            ModelGroup.Create(ModelGroupCompositor.Sequence, At(1), [leaf])
        );
        for (var i = 1; i <= 20; i++)
        {
            var below = level;
            level = ModelGroupDefinition.Create(
                new XPathQName(Tns, "g" + i),
                At(1),
                ModelGroup.Create(
                    ModelGroupCompositor.Sequence,
                    At(1),
                    [Particle.Create(At(1), 1, 1, below), Particle.Create(At(1), 1, 1, below)]
                )
            );
        }

        return Particle.Create(At(1), 1, 1, level);
    }

    [Test]
    public async Task ZonesOfManyCounters_AreLimitedByTheMemoryOfTheCheck()
    {
        // Twelve counted groups and then a long sequence: every position of the sequence holds a zone of twenty-five counters.
        Particle Model()
        {
            var members = new List<Particle>();
            for (var i = 0; i < 12; i++)
            {
                members.Add(Seq([El("c" + i)], 2, 3));
            }

            for (var i = 0; i < 14_000; i++)
            {
                members.Add(El("e" + i));
            }

            return Seq(members.ToArray());
        }

        await AssertVerdict(Model(), Model(), SubsumptionVerdict.Undecided);
    }

    [Test]
    public async Task Wildcards_AreChargedToTheWorkLimit_WhenTheyAreAskedAboutEverySymbol()
    {
        var wildcards = new Particle[400];
        for (var i = 0; i < wildcards.Length; i++)
        {
            wildcards[i] = Any(Only("urn:" + i));
        }

        var outcome = Subsumption(10).Check(Seq([El("a")]), Cho(wildcards));

        await Assert.That(outcome.Verdict).IsEqualTo(SubsumptionVerdict.Undecided);
        await Assert
            .That(Subsumption().Check(Seq([El("a", ns: "urn:3")]), Cho(wildcards)).Verdict)
            .IsEqualTo(SubsumptionVerdict.Subsumed);
    }

    [Test]
    public async Task GroupDefinition_UsedManyTimes_IsRefusedBeforeItIsExpandedFurther()
    {
        var huge = Doubled(El("a"));
        var hugeWildcards = Doubled(Any());

        await AssertVerdict(huge, huge, SubsumptionVerdict.Undecided);
        await AssertVerdict(El("a"), huge, SubsumptionVerdict.Undecided);
        await AssertVerdict(hugeWildcards, El("a"), SubsumptionVerdict.Undecided);
    }

    [Test]
    public async Task Restriction_ThatIsNotDeterministic_IsReadAsAllItsMoves() =>
        await AssertSubsumed(
            Cho([Seq([El("a"), El("b")]), Seq([El("a"), El("c")])]),
            Seq([El("a"), Cho([El("b"), El("c")])])
        );

    [Test]
    public async Task Base_ThatIsNotDeterministic_IsReadAsItsFirstMove() =>
        await AssertNotSubsumed(
            Seq([El("a"), El("c", line: 5)]),
            Cho([Seq([El("a"), El("b")]), Seq([El("a"), El("c")])]),
            line: 5
        );

    [Test]
    public async Task Report_OfAValidRestriction_AddsNothing()
    {
        Subsumption().Report(Seq([El("a")]), Seq([El("a"), El("b", 0, 1)]), new XsdIssueReporter(_collector));

        await Assert.That(Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Report_OfAViolation_AddsALocatedIssueWithTheRuleAndTheClause()
    {
        Subsumption()
            .Report(Seq([El("a"), El("x", line: 12)], line: 10), Seq([El("a")]), new XsdIssueReporter(_collector));

        await Assert.That(Issues).Count().IsEqualTo(1);
        await Assert.That(Issues[0].Code).IsEqualTo(XsdRuleCodes.CosContentActRestrict);
        await Assert.That(FakeRules.Find(Issues[0].Code).Id).IsEqualTo("cos-content-act-restrict");
        await Assert.That(Issues[0].Constraint).IsEqualTo("cos-content-act-restrict.1");
        await Assert.That(Issues[0].Location.Line).IsEqualTo(12);
        await Assert.That(Issues[0].Message).IsNotEmpty();
    }

    [Test]
    public async Task Report_OfAnUndecidedCheck_AddsAnIssueAtTheRestriction()
    {
        Subsumption(1).Report(Seq([El("a")], line: 10), Seq([El("a")]), new XsdIssueReporter(_collector));

        await Assert.That(Issues).Count().IsEqualTo(1);
        await Assert.That(Issues[0].Constraint).IsEqualTo(Ceiling);
        await Assert.That(Issues[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task Report_OfAContentThatIsNotCompared_AddsNothing()
    {
        Subsumption().Report(All([El("a")]), Seq([El("b")]), new XsdIssueReporter(_collector));

        await Assert.That(Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Guards_RejectInvalidArguments()
    {
        var check = Subsumption();

        await Assert.That(() => new ContentSubsumption(null!, Values(XsdVersion.V11))).Throws<ArgumentNullException>();
        await Assert
            .That(() => new ContentSubsumption(new TypeDerivationOk(XsdVersion.V11), null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ContentSubsumption(new TypeDerivationOk(XsdVersion.V11), Values(XsdVersion.V11), 0))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => check.Check(null!, El("a"))).Throws<ArgumentNullException>();
        await Assert.That(() => check.Check(El("a"), null!)).Throws<ArgumentNullException>();
        await Assert.That(() => check.Report(El("a"), El("a"), null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Guards_OfTheInternalTypes_RejectInvalidArguments()
    {
        var ids = new Dictionary<XPathQName, int>();
        var names = new List<XPathQName>();
        var side = SubsumptionSide.Build(El("a"), ids, names);
        XPathQName[] symbols = [];

        await Assert.That(() => SubsumptionSide.Build(null!, ids, names)).Throws<ArgumentNullException>();
        await Assert.That(() => SubsumptionSide.Build(El("a"), null!, names)).Throws<ArgumentNullException>();
        await Assert.That(() => SubsumptionSide.Build(El("a"), ids, null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new SubsumptionSearch(null!, side, symbols, (_, _) => default, 1))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new SubsumptionSearch(side, null!, symbols, (_, _) => default, 1))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new SubsumptionSearch(side, side, null!, (_, _) => default, 1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new SubsumptionSearch(side, side, symbols, null!, 1)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new SubsumptionSearch(side, side, symbols, (_, _) => default, 0))
            .Throws<ArgumentOutOfRangeException>();
    }
}
