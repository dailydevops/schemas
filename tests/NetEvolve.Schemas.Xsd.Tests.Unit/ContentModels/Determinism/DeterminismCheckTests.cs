namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton.AutomatonTestKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism.DeterminismKit;

public class DeterminismCheckTests
{
    [Test]
    public async Task Choice_TwoParticlesWithTheSameName_IsAmbiguous()
    {
        var outcome = Check(Cho(El(A), El(A)));

        await Assert.That(outcome.Deterministic).IsFalse();
        await Assert.That(outcome.Issues).Count().IsEqualTo(1);
        await Assert.That(outcome.Issues[0].Code).IsEqualTo(XsdRuleCodes.CosNonambig);
    }

    [Test]
    public async Task Choice_OfSequencesWithTheSameFirstName_IsAmbiguous()
    {
        var outcome = Check(Cho(Seq(El(A), El(B)), Seq(El(A), El(C))));

        await Assert.That(outcome.Deterministic).IsFalse();
        await Assert.That(outcome.Issues).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Sequence_OptionalFirstParticleBeforeTheSameName_IsAmbiguous()
    {
        var outcome = Check(Seq(El(A, 0, 1), El(A)));

        await Assert.That(outcome.Deterministic).IsFalse();
        await Assert.That(outcome.Issues).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Sequence_RequiredFirstParticleBeforeTheSameName_IsDeterministic()
    {
        var outcome = Check(Seq(El(A), El(A)));

        await Assert.That(outcome.Deterministic).IsTrue();
        await Assert.That(outcome.Issues).IsEmpty();
    }

    [Test]
    public async Task Sequence_StarredParticleBeforeTheSameName_IsAmbiguous()
    {
        // (a*, a): after any a the next a stays in the loop or leaves it.
        var outcome = Check(Seq(El(A, 0, Unbounded), El(A)));

        await Assert.That(outcome.Deterministic).IsFalse();
    }

    [Test]
    public async Task DistinctNames_AreDeterministic()
    {
        var models = new[]
        {
            Seq(El(A), El(B)),
            Seq(El(A, 0, 1), El(B, 0, 1), El(C, 0, 1)),
            Seq(El(A), Cho(0, Unbounded, El(B), El(C)), El(D)),
            Cho(Seq(El(A), El(B)), Seq(El(C), El(D))),
            Seq(),
        };

        foreach (var model in models)
        {
            var outcome = Check(model);

            await Assert.That(outcome.Deterministic).IsTrue();
            await Assert.That(outcome.Issues).IsEmpty();
        }
    }

    [Test]
    public async Task ParticleThatCannotOccur_HasNoPosition()
    {
        // The compiler removes a{0,0}, so the other a has nothing to compete with.
        var removed = Check(Cho(El(A), El(A, 0, 0)));
        var kept = Check(Cho(El(A), El(A, 0, 1)));

        await Assert.That(removed.Deterministic).IsTrue();
        await Assert.That(kept.Deterministic).IsFalse();
    }

    [Test]
    [Arguments("urn:test", true)]
    [Arguments("##any", true)]
    [Arguments("##other", true)]
    [Arguments("urn:test urn:other", true)]
    [Arguments("urn:other", false)]
    [Arguments("##local", false)]
    [Arguments("##targetNamespace", false)]
    [Arguments("", false)]
    public async Task ElementVersusWildcard_IsAmbiguousWhenTheWildcardAllowsTheElementName(
        string wildcard,
        bool ambiguous
    )
    {
        // The element lives in urn:test; ##other excludes the target namespace urn:t and no namespace.
        var outcome = Check(Cho(El(A), Wild(2, wildcard)));

        await Assert.That(outcome.Deterministic).IsEqualTo(!ambiguous);
        await Assert.That(outcome.Issues).Count().IsEqualTo(ambiguous ? 1 : 0);
    }

    [Test]
    public async Task ElementVersusWildcard_InASequenceWithAnOptionalFirstParticle_IsAmbiguous()
    {
        var outcome = Check(Seq(El(A, 0, 1), Wild(2, "##any")));

        await Assert.That(outcome.Deterministic).IsFalse();
    }

    [Test]
    [Arguments("##any", "urn:x", true)]
    [Arguments("##any", "##any", true)]
    [Arguments("##any", "", false)]
    [Arguments("urn:a", "urn:b", false)]
    [Arguments("urn:a urn:b", "urn:b urn:c", true)]
    [Arguments("##other", "##other", true)]
    [Arguments("##other", "urn:x", true)]
    [Arguments("##other", "urn:t", false)]
    [Arguments("##other", "##local", false)]
    [Arguments("##other", "urn:t urn:x", true)]
    [Arguments("##other", "##any", true)]
    public async Task WildcardVersusWildcard_IsAmbiguousWhenSomeNamespaceIsAllowedByBoth(
        string first,
        string second,
        bool ambiguous
    )
    {
        var outcome = Check(Cho(Wild(1, first), Wild(2, second)));

        await Assert.That(outcome.Deterministic).IsEqualTo(!ambiguous);
        await Assert.That(outcome.Issues).Count().IsEqualTo(ambiguous ? 1 : 0);
    }

    [Test]
    public async Task WildcardVersusWildcard_InASequenceWithAnOptionalFirstParticle_IsAmbiguous()
    {
        var outcome = Check(Seq(Wild(1, "##any", 0, 1), Wild(2, "##any")));

        await Assert.That(outcome.Deterministic).IsFalse();
    }

    [Test]
    public async Task WildcardLoop_IsDeterministic()
    {
        // Two iterations of one particle are the same particle, however often the wildcard repeats.
        var alone = Check(Wild(1, "##any", 0, Unbounded));
        var afterAnElement = Check(Seq(El(A), Wild(2, "##any", 0, Unbounded)));
        var beforeAnElement = Check(Seq(Wild(1, "##any", 0, Unbounded), El(A)));

        await Assert.That(alone.Deterministic).IsTrue();
        await Assert.That(afterAnElement.Deterministic).IsTrue();
        await Assert.That(beforeAnElement.Deterministic).IsFalse();
    }

    [Test]
    [Arguments(1, 2, false)]
    [Arguments(2, 2, true)]
    [Arguments(2, 3, false)]
    [Arguments(3, 3, true)]
    [Arguments(0, 1, false)]
    [Arguments(999_999, 1_000_000, false)]
    [Arguments(1_000_000, 1_000_000, true)]
    [Arguments(1_000_000, 1_000_000_000, false)]
    public async Task CounterDependentAmbiguity_IsDecidedWithoutUnfolding(long min, long max, bool deterministic)
    {
        // (a{min,max}, a?): another a competes with the loop for as long as the loop may still repeat after the minimum.
        var outcome = Check(Seq(El(A, min, max), El(A, 0, 1)));

        await Assert.That(outcome.Deterministic).IsEqualTo(deterministic);
        await Assert.That(outcome.Issues).Count().IsEqualTo(deterministic ? 0 : 1);
    }

    [Test]
    public async Task CounterDependentAmbiguity_ForCertainCounterValuesOnly_IsDetected()
    {
        // (a{2,3}, a): at two a both may follow, at three only the sibling can, and at one neither.
        var ambiguous = Check(Seq(El(A, 2, 3), El(A)));
        var similar = Check(Seq(El(A, 3, 3), El(A)));
        var unbounded = Check(Seq(El(A, 2, Unbounded), El(A)));

        await Assert.That(ambiguous.Deterministic).IsFalse();
        await Assert.That(similar.Deterministic).IsTrue();
        await Assert.That(unbounded.Deterministic).IsFalse();
    }

    [Test]
    public async Task CounterDependentAmbiguity_OfANestedCountedGroup_IsDetected()
    {
        // ((a, b){2,3}, a): after the second b the loop may iterate again or the sequence may go on, and both start with a.
        var ambiguous = Check(Seq(Seq(2, 3, El(A), El(B)), El(C, 0, 1), El(A)));
        var similar = Check(Seq(Seq(3, 3, El(A), El(B)), El(A)));

        await Assert.That(ambiguous.Deterministic).IsFalse();
        await Assert.That(similar.Deterministic).IsTrue();
    }

    [Test]
    public async Task TwoPathsToTheSameParticle_WithDifferentCounters_AreNotAmbiguous()
    {
        // Design #146 DP-8: ambiguity is between distinct particles, never between iteration states of one.
        var twoThree = Check(Seq(2, 2, El(A, 2, 3)));
        var oneTwo = Check(Seq(1, 2, El(A, 1, 2)));

        await Assert.That(twoThree.Deterministic).IsTrue();
        await Assert.That(oneTwo.Deterministic).IsTrue();
    }

    [Test]
    public async Task SubstitutionGroupMember_CollidingWithASibling_IsAmbiguous()
    {
        // The particle for head a accepts a and its member b; the sibling particle b competes with the member.
        var outcome = Check(Cho(AutomatonParticle.Element(1, 1, A, B), El(B)));
        var distinct = Check(Cho(AutomatonParticle.Element(1, 1, A, B), El(C)));

        await Assert.That(outcome.Deterministic).IsFalse();
        await Assert.That(outcome.Issues).Count().IsEqualTo(1);
        await Assert.That(distinct.Deterministic).IsTrue();
    }

    [Test]
    public async Task SubstitutionGroupMember_CollidingWithAWildcard_IsAmbiguous()
    {
        // Only the member b lives in urn:x.
        XPathQName NameOfMember(int id) => id == B ? new XPathQName("urn:x", "member") : NameOf(id);

        var outcome = Check(Cho(AutomatonParticle.Element(1, 1, A, B), Wild(2, "urn:x")), NameOfMember);
        var head = Check(Cho(AutomatonParticle.Element(1, 1, A), Wild(2, "urn:x")), NameOfMember);

        await Assert.That(outcome.Deterministic).IsFalse();
        await Assert.That(head.Deterministic).IsTrue();
    }

    [Test]
    public async Task Issue_CarriesBothParticleLocationsAndTheRuleCode()
    {
        var outcome = Check(Seq(El(C), Cho(El(A), El(B), El(A))));

        var issue = outcome.Issues[0];

        await Assert.That(outcome.Issues).Count().IsEqualTo(1);
        await Assert.That(issue.Code).IsEqualTo(XsdRuleCodes.CosNonambig);
        await Assert.That(issue.Constraint).IsEqualTo("cos-nonambig");
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);

        // The first particle is the location of the issue, the second is named in the message, and both carry the name.
        await Assert.That(issue.Location).IsEqualTo(Locate(2));
        await Assert.That(issue.Message).Contains(Locate(2).ToString());
        await Assert.That(issue.Message).Contains(Locate(4).ToString());
        await Assert.That(issue.Message).Contains("{urn:test}e1");
    }

    [Test]
    public async Task Issue_OfAWildcardPair_NamesTheNamespaceConstraints()
    {
        var outcome = Check(Cho(Wild(1, "##any"), Wild(2, "urn:x")));

        var issue = outcome.Issues[0];

        await Assert.That(issue.Location).IsEqualTo(Locate(1));
        await Assert.That(issue.Message).Contains(Locate(2).ToString());
        await Assert.That(issue.Message).Contains("##any");
        await Assert.That(issue.Message).Contains("{urn:x}");
    }

    [Test]
    public async Task Issues_AreReportedOncePerPairInPositionOrder()
    {
        // (a{1,3}, a, b{1,3}, b): the pair of a and the pair of b are reached in several counter states each.
        var outcome = Check(Seq(El(A, 1, 3), El(A), El(B, 1, 3), El(B)));

        await Assert.That(outcome.Issues).Count().IsEqualTo(2);
        await Assert.That(outcome.Issues[0].Location).IsEqualTo(Locate(1));
        await Assert.That(outcome.Issues[1].Location).IsEqualTo(Locate(3));
    }

    [Test]
    public async Task ManySiblingCounters_WithHugeBounds_StayWithinTheCeiling()
    {
        // Counters of finished siblings are dead; keeping them would multiply the states by the regions per sibling.
        var children = new AutomatonParticle[60];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = El(i + 1, 2, 1_000_000_000);
        }

        var outcome = Check(Seq(children));

        await Assert.That(outcome.Deterministic).IsTrue();
        await Assert.That(outcome.Issues).IsEmpty();
    }

    [Test]
    public async Task DeeplyNestedCounters_AboveTheStateCeiling_ReportTheEngineCode()
    {
        var nested = El(A, 2, 9);
        for (var i = 0; i < 12; i++)
        {
            nested = Seq(2, 9, nested);
        }

        var outcome = Check(nested);

        await Assert.That(outcome.Deterministic).IsFalse();
        await Assert.That(outcome.Issues).Count().IsEqualTo(1);
        await Assert.That(outcome.Issues[0].Code).IsEqualTo(XsdRuleCodes.DeterminismAnalysisCeilingExceeded);
        await Assert.That(outcome.Issues[0].Location).IsEqualTo(Locate(99));
    }

    [Test]
    public async Task NestedCounters_BelowTheStateCeiling_AreChecked()
    {
        var nested = El(A, 2, 9);
        for (var i = 0; i < 3; i++)
        {
            nested = Seq(2, 9, nested);
        }

        var outcome = Check(nested);

        await Assert.That(outcome.Deterministic).IsTrue();
        await Assert.That(outcome.Issues).IsEmpty();
    }

    [Test]
    public async Task Check_RejectsNullAndMismatchedArguments()
    {
        var root = Seq(El(A), El(B));
        var model = Compile(root);
        var terms = Terms(root);
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));

        await Assert
            .That(() => DeterminismCheck.Check(null!, terms, NameOf, reporter, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DeterminismCheck.Check(model, null!, NameOf, reporter, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DeterminismCheck.Check(model, terms, null!, reporter, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DeterminismCheck.Check(model, terms, NameOf, null!, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DeterminismCheck.Check(model, terms.GetRange(0, 1), NameOf, reporter, default))
            .Throws<ArgumentOutOfRangeException>();
    }
}
