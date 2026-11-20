namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism11;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Wildcards.Negation;
using NetEvolve.Schemas.Xsd.Wildcards.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton.AutomatonTestKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism.DeterminismKit;
using Outcome = NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism.DeterminismKit.Outcome;

public class DeterminismCheck11Tests
{
    /// <summary>A wildcard particle of XSD 1.1; the spec names the namespaces as in <see cref="NameConstraintTests.Spec"/> and <c>test</c> is the namespace of the elements.</summary>
    private static AutomatonParticle Neg(int line, string spec, long min = 1, long max = 1) =>
        AutomatonParticle.Wildcard(
            min,
            max,
            NegatedWildcard.Create(NameConstraintTests.Spec(spec), XsdProcessContents.Lax, Locate(line))
        );

    private static AutomatonParticle CoreWild(int line, string namespaceValue) => Wild(line, namespaceValue);

    private static Outcome Run(IDeterminismCheck check, AutomatonParticle root)
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var deterministic = check.Check(
            Compile(root),
            Terms(root),
            NameOf,
            new XsdIssueReporter(collector),
            Locate(99)
        );
        return new Outcome(deterministic, collector.ToResult().Issues);
    }

    private static Outcome Run10(AutomatonParticle root) => Run(new DeterminismChecker(), root);

    private static Outcome Run11(AutomatonParticle root) => Run(new DeterminismChecker11(), root);

    private static readonly Func<AutomatonParticle>[] ElementBesideAWildcard =
    [
        () => Cho(El(A), Neg(1, "any")),
        () => Cho(Neg(1, "any"), El(A)),
        () => Seq(El(A, 0, 1), Neg(1, "any")),
        () => Seq(Neg(1, "any", 0, 1), El(A)),
        () => Seq(El(A, 0, Unbounded), Neg(1, "not other")),
        () => Cho(El(A), Neg(1, "set test")),
        () => Cho(Seq(El(B), El(A)), Neg(1, "any;defined;sibling")),
    ];

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    public async Task ElementBesideAnOverlappingWildcard_IsAnErrorIn10_AndAcceptedIn11(int index)
    {
        var root = ElementBesideAWildcard[index]();

        var old = Run10(root);
        var current = Run11(root);

        await Assert.That(old.Deterministic).IsFalse();
        await Assert.That(old.Issues).Count().IsGreaterThanOrEqualTo(1);
        await Assert.That(old.Issues[0].Code).IsEqualTo(XsdRuleCodes.CosNonambig);
        await Assert.That(current.Deterministic).IsTrue();
        await Assert.That(current.Issues).IsEmpty();
    }

    [Test]
    public async Task ElementBesideAWildcard_WithCoreWildcard_IsAcceptedIn11()
    {
        var root = Cho(El(A), CoreWild(1, "##any"));

        await Assert.That(Run11(root).Deterministic).IsTrue();
        await Assert.That(Run10(root).Deterministic).IsFalse();
    }

    [Test]
    public async Task ElementBesideAWildcard_ThatDoesNotAllowIt_IsAcceptedInBoth()
    {
        var models = new[]
        {
            Cho(El(A), Neg(1, "not test")),
            Cho(El(A), Neg(1, "set other")),
            Cho(El(A), Neg(1, "any;q test#e1")),
            Seq(El(A, 0, 1), Neg(1, "any;q test#e1 test#e2"), El(B)),
        };

        foreach (var model in models)
        {
            await Assert.That(Run10(model).Deterministic).IsTrue();
            await Assert.That(Run11(model).Deterministic).IsTrue();
        }
    }

    [Test]
    public async Task ElementsWithTheSameName_AreStillAmbiguousIn11_ThoughAWildcardIsPresent()
    {
        var outcome = Run11(Cho(El(A), El(A), Neg(3, "not other")));

        await Assert.That(outcome.Deterministic).IsFalse();
        await Assert.That(outcome.Issues).Count().IsEqualTo(1);
        await Assert.That(outcome.Issues[0].Code).IsEqualTo(XsdRuleCodes.CosNonambig);
        await Assert.That(outcome.Issues[0].Message).Contains("element");
    }

    [Test]
    [Arguments("any", "any")]
    [Arguments("any", "not a")]
    [Arguments("not a", "not b")]
    [Arguments("set a b", "set b c")]
    [Arguments("set a", "not b")]
    [Arguments("any;q test#e1", "any;q test#e2")]
    [Arguments("any;defined", "any;sibling")]
    public async Task OverlappingWildcards_AreAmbiguousIn11(string first, string second)
    {
        var outcome = Run11(Cho(Neg(1, first), Neg(2, second)));

        await Assert.That(outcome.Deterministic).IsFalse();
        await Assert.That(outcome.Issues).Count().IsEqualTo(1);
        var issue = outcome.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(XsdRuleCodes.CosNonambig);
        await Assert.That(issue.Location).IsEqualTo(Locate(1));
        await Assert.That(issue.Message).Contains("wildcard");
        await Assert.That(issue.Message).Contains(Locate(2).ToString());
    }

    [Test]
    [Arguments("set a", "set b")]
    [Arguments("set a", "not a")]
    [Arguments("set L", "not L")]
    [Arguments("set a b", "not a b")]
    [Arguments("set", "any")]
    public async Task WildcardsWithDisjointNamespaces_AreAcceptedIn11(string first, string second)
    {
        var outcome = Run11(Cho(Neg(1, first), Neg(2, second)));

        await Assert.That(outcome.Deterministic).IsTrue();
        await Assert.That(outcome.Issues).IsEmpty();
    }

    [Test]
    public async Task WildcardsOfBothImplementations_AreDecidedByTheirNamespaces()
    {
        var disjoint = Run11(Cho(CoreWild(1, "##targetNamespace"), Neg(2, "not t L")));
        var overlapping = Run11(Cho(CoreWild(1, "##other"), Neg(2, "not x")));

        await Assert.That(disjoint.Deterministic).IsTrue();
        await Assert.That(overlapping.Deterministic).IsFalse();
        await Assert.That(overlapping.Issues).Count().IsEqualTo(1);
    }

    [Test]
    public async Task WildcardsThatCannotFollowEachOther_AreNotCompared()
    {
        var outcome = Run11(Seq(Neg(1, "any"), El(A), Neg(3, "any")));

        await Assert.That(outcome.Deterministic).IsTrue();
        await Assert.That(outcome.Issues).IsEmpty();
    }

    [Test]
    public async Task Wildcard_WithCounters_IsCheckedLikeAnyOtherParticle()
    {
        var ambiguous = Run11(Seq(Neg(1, "any", 0, 3), Neg(2, "any")));
        var fine = Run11(Seq(Neg(1, "any", 2, 2), Neg(2, "any")));

        await Assert.That(ambiguous.Deterministic).IsFalse();
        await Assert.That(fine.Deterministic).IsTrue();
    }

    [Test]
    public async Task DeeplyNestedCounters_AboveTheStateCeiling_ReportTheEngineCode()
    {
        var nested = El(A, 2, 9);
        for (var i = 0; i < 12; i++)
        {
            nested = Seq(2, 9, nested);
        }

        var outcome = Run11(nested);

        await Assert.That(outcome.Deterministic).IsFalse();
        await Assert.That(outcome.Issues).Count().IsEqualTo(1);
        await Assert.That(outcome.Issues[0].Code).IsEqualTo(XsdRuleCodes.DeterminismAnalysisCeilingExceeded);
        await Assert.That(outcome.Issues[0].Location).IsEqualTo(Locate(99));
    }

    [Test]
    public async Task ModelsWithoutWildcards_GiveTheVerdictOfTheXsd10Check()
    {
        var models = new[]
        {
            Seq(El(A), El(B)),
            Cho(El(A), El(A)),
            Seq(El(A, 0, 1), El(A)),
            Cho(Seq(El(A), El(B)), Seq(El(A), El(C))),
            Seq(El(A, 2, 9), El(B)),
        };

        foreach (var model in models)
        {
            var old = Run10(model);
            var current = Run11(model);

            await Assert.That(current.Deterministic).IsEqualTo(old.Deterministic);
            await Assert.That(current.Issues).Count().IsEqualTo(old.Issues.Count);
        }
    }

    [Test]
    public async Task Matcher_PrefersTheElementDeclaration_OverTheWildcardThatAllowsItToo()
    {
        var root = Cho(El(A), Neg(2, "any"));
        var automaton = Compile(root);

        var element = automaton.CreateRun();
        var matchedElement = element.TryStep(A, NameOf(A), out var viaName);
        var other = automaton.CreateRun();
        var matchedOther = other.TryStep(-1, new XPathQName("urn:other", "z"), out var viaWildcard);

        await Assert.That(matchedElement).IsTrue();
        await Assert.That(viaName.IsWildcard).IsFalse();
        await Assert.That(element.CanEnd()).IsTrue();
        await Assert.That(matchedOther).IsTrue();
        await Assert.That(viaWildcard.IsWildcard).IsTrue();
    }

    [Test]
    public async Task Matcher_WildcardWithNotQName_DoesNotMatchTheExcludedName()
    {
        var automaton = Compile(Seq(El(A), Neg(2, "any;q other#z")));
        var run = automaton.CreateRun();

        _ = run.TryStep(A, NameOf(A), out _);
        var excluded = run.TryStep(-1, new XPathQName("urn:other", "z"), out _);
        var allowed = run.TryStep(-1, new XPathQName("urn:other", "y"), out _);

        await Assert.That(excluded).IsFalse();
        await Assert.That(allowed).IsTrue();
    }

    [Test]
    public async Task Seam_Checker_IsAnIDeterminismCheck_AndReportsThroughTheReporter()
    {
        var check = new DeterminismChecker11();
        var ambiguous = Cho(El(A), El(A));
        var accepted = Cho(El(A), Neg(2, "any"));

        var first = Run(check, ambiguous);
        var second = Run(check, accepted);

        await Assert.That(check).IsAssignableTo<IDeterminismCheck>();
        await Assert.That(first.Deterministic).IsFalse();
        await Assert.That(first.Issues).Count().IsEqualTo(1);
        await Assert.That(second.Deterministic).IsTrue();
    }

    [Test]
    public async Task Check_RejectsNullAndMismatchedArguments()
    {
        var root = Seq(El(A), Neg(2, "any"));
        var model = Compile(root);
        var terms = Terms(root);
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var withNull = new List<IXsdComponent> { terms[0], null! };

        await Assert
            .That(() => DeterminismCheck11.Check(null!, terms, NameOf, reporter, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DeterminismCheck11.Check(model, null!, NameOf, reporter, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DeterminismCheck11.Check(model, terms, null!, reporter, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DeterminismCheck11.Check(model, terms, NameOf, null!, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DeterminismCheck11.Check(model, withNull, NameOf, reporter, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DeterminismCheck11.Check(model, terms.GetRange(0, 1), NameOf, reporter, default))
            .Throws<ArgumentOutOfRangeException>();
    }
}
