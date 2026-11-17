namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static AutomatonTestKit;

public class CountingAutomatonTests
{
    [Test]
    public async Task Sequence_AcceptsTheOrderAndRejectsTheRest()
    {
        var m = Compile(Seq(El(A), El(B)));

        await Assert.That(Accepts(m, A, B)).IsTrue();
        await Assert.That(Accepts(m, A)).IsFalse();
        await Assert.That(Accepts(m)).IsFalse();
        await Assert.That(Accepts(m, B, A)).IsFalse();
        await Assert.That(Accepts(m, A, B, B)).IsFalse();
    }

    [Test]
    public async Task Choice_AcceptsExactlyOneAlternative()
    {
        var m = Compile(Cho(El(A), El(B)));

        await Assert.That(Accepts(m, A)).IsTrue();
        await Assert.That(Accepts(m, B)).IsTrue();
        await Assert.That(Accepts(m, A, B)).IsFalse();
        await Assert.That(Accepts(m)).IsFalse();
    }

    [Test]
    public async Task NestedGroups_AcceptTheExpectedSequences()
    {
        // (a, (b | c)*, d)
        var m = Compile(Seq(El(A), Cho(0, Unbounded, El(B), El(C)), El(D)));

        await Assert.That(Accepts(m, A, D)).IsTrue();
        await Assert.That(Accepts(m, A, B, C, B, D)).IsTrue();
        await Assert.That(Accepts(m, A, B, D, D)).IsFalse();
        await Assert.That(Accepts(m, A, B)).IsFalse();
        await Assert.That(Accepts(m, D)).IsFalse();
    }

    [Test]
    public async Task EmptiableGroups_AcceptTheEmptyContentAndSkips()
    {
        // (a?, (b, c)?, d?)
        var m = Compile(Seq(El(A, 0, 1), Seq(0, 1, El(B), El(C)), El(D, 0, 1)));

        await Assert.That(Accepts(m)).IsTrue();
        await Assert.That(Accepts(m, A)).IsTrue();
        await Assert.That(Accepts(m, B, C)).IsTrue();
        await Assert.That(Accepts(m, A, B, C, D)).IsTrue();
        await Assert.That(Accepts(m, D)).IsTrue();
        await Assert.That(Accepts(m, B)).IsFalse();
        await Assert.That(Accepts(m, D, A)).IsFalse();
    }

    [Test]
    public async Task EmptiableBodyUnderACounter_AcceptsFewerIterations()
    {
        // (a?){2}: empty iterations pad the minimum.
        var m = Compile(Seq(2, 2, El(A, 0, 1)));

        await Assert.That(Accepts(m)).IsTrue();
        await Assert.That(Accepts(m, A)).IsTrue();
        await Assert.That(Accepts(m, A, A)).IsTrue();
        await Assert.That(Accepts(m, A, A, A)).IsFalse();
    }

    [Test]
    public async Task MaxOccursZero_RemovesTheParticle()
    {
        var m = Compile(Seq(El(A), El(B, 0, 0), El(C)));

        await Assert.That(m.PositionCount).IsEqualTo(2);
        await Assert.That(Accepts(m, A, C)).IsTrue();
        await Assert.That(Accepts(m, A, B, C)).IsFalse();
    }

    [Test]
    [Arguments(0, false)]
    [Arguments(1, false)]
    [Arguments(2, true)]
    [Arguments(4, true)]
    [Arguments(5, false)]
    public async Task CounterBoundaries_LeafRange(int count, bool expected)
    {
        var m = Compile(El(A, 2, 4));

        await Assert.That(Accepts(m, Rep(A, count))).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1, false)]
    [Arguments(2, false)]
    [Arguments(3, false)]
    [Arguments(4, true)]
    [Arguments(5, true)]
    [Arguments(6, true)]
    [Arguments(7, false)]
    [Arguments(8, false)]
    public async Task CounterBoundaries_NestedCountedGroups(int count, bool expected)
    {
        // (a{2,3}){2}: 4 = 2+2, 5 = 2+3, 6 = 3+3; 3 is one iteration short and 7 one over.
        var m = Compile(Seq(2, 2, El(A, 2, 3)));

        await Assert.That(Accepts(m, Rep(A, count))).IsEqualTo(expected);
    }

    [Test]
    public async Task CounterBoundaries_CountedGroupOfSequence()
    {
        // (a, b{1,2}){2,3}
        var m = Compile(Seq(2, 3, El(A), El(B, 1, 2)));

        await Assert.That(Accepts(m, A, B)).IsFalse();
        await Assert.That(Accepts(m, A, B, A, B)).IsTrue();
        await Assert.That(Accepts(m, A, B, B, A, B)).IsTrue();
        await Assert.That(Accepts(m, A, B, A, B, A, B)).IsTrue();
        await Assert.That(Accepts(m, A, B, A, B, A, B, A, B)).IsFalse();
        await Assert.That(Accepts(m, A, B, B, B, A, B)).IsFalse();
    }

    [Test]
    public async Task UnboundedWithMinimum_SaturatesTheCounter()
    {
        var m = Compile(El(A, 3, Unbounded));

        await Assert.That(Accepts(m, Rep(A, 2))).IsFalse();
        await Assert.That(Accepts(m, Rep(A, 3))).IsTrue();
        await Assert.That(Accepts(m, Rep(A, 1000))).IsTrue();
    }

    [Test]
    public async Task OverlappingCounterPaths_KeepEveryConfiguration()
    {
        // (a{1,2}){1,2}: after one a, one or two iterations are live.
        var m = Compile(Seq(1, 2, El(A, 1, 2)));

        await Assert.That(Accepts(m, Rep(A, 4))).IsTrue();
        await Assert.That(Accepts(m, Rep(A, 5))).IsFalse();
    }

    [Test]
    public async Task MaxOccursOfOneBillion_CompilesToTheSizeOfMaxOccursTwo()
    {
        var small = Compile(Seq(El(A, 1, 2), El(B)));
        var big = Compile(Seq(El(A, 1, 1_000_000_000), El(B)));
        var nestedBig = Compile(Seq(1, 1_000_000_000, El(A, 1, 1_000_000_000)));
        var nestedSmall = Compile(Seq(1, 2, El(A, 1, 2)));

        await Assert.That(big.PositionCount).IsEqualTo(small.PositionCount);
        await Assert.That(big.TransitionCount).IsEqualTo(small.TransitionCount);
        await Assert.That(big.CounterSlots).IsEqualTo(small.CounterSlots);
        await Assert.That(nestedBig.PositionCount).IsEqualTo(nestedSmall.PositionCount);
        await Assert.That(nestedBig.TransitionCount).IsEqualTo(nestedSmall.TransitionCount);
        await Assert.That(nestedBig.CounterSlots).IsEqualTo(nestedSmall.CounterSlots);
        await Assert.That(Accepts(big, A, A, A, B)).IsTrue();
    }

    [Test]
    public async Task MinOccursOfOneBillion_IsUnsatisfiedByAShortRun()
    {
        var m = Compile(El(A, 1_000_000_000, 1_000_000_000));

        await Assert.That(Accepts(m, Rep(A, 100))).IsFalse();
    }

    [Test]
    public async Task MatchStep_AllocatesNothing()
    {
        var m = Compile(Seq(2, 3, El(A), El(B, 1, 2), Cho(El(C), El(D, 0, Unbounded))));
        var run = m.CreateRun();
        int[] input = [A, B, C, A, B, B, D, D];

        AllocationAssert.NoAllocation(() =>
        {
            run.Reset();
            foreach (var id in input)
            {
                _ = run.TryStep(id, out _);
            }

            _ = run.CanEnd();
        });

        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task FailedStep_ReportsTheExpectedNames_AndKeepsTheState()
    {
        var m = Compile(Seq(El(A), Cho(El(B), El(C)), El(D)));
        var run = m.CreateRun();
        _ = run.TryStep(A, out _);
        var expected = new int[8];

        var rejected = run.TryStep(D, out var matched);
        var written = run.CollectExpected(expected, out var wildcard);
        var list = new List<int>(expected[..written]);
        list.Sort();
        var recovers = run.TryStep(C, out _);

        await Assert.That(rejected).IsFalse();
        await Assert.That(matched).IsEqualTo(default(MatchedParticle));
        await Assert.That(list).IsEquivalentTo([B, C]);
        await Assert.That(wildcard).IsFalse();
        await Assert.That(recovers).IsTrue();
    }

    [Test]
    public async Task CollectExpected_IsBoundedByTheDestination_AndHonorsCounters()
    {
        var m = Compile(Seq(El(A, 2, 2), El(B)));
        var run = m.CreateRun();
        _ = run.TryStep(A, out _);
        var one = new int[1];

        var written = run.CollectExpected(one, out _);

        // After one of two required a, only a may follow, never b.
        await Assert.That(written).IsEqualTo(1);
        await Assert.That(one[0]).IsEqualTo(A);
    }

    [Test]
    public async Task Wildcard_MatchesByNameThroughTheSeam_AndReportsMatchedParticle()
    {
        var log = new List<string>();
        var any = new FakeWildcard(log, XsdProcessContents.Lax, "urn:x");
        var m = Compile(Seq(El(A), AutomatonParticle.Wildcard(0, Unbounded, any)));
        var run = m.CreateRun();

        var first = run.TryStep(A, new XPathQName("", "a"), out var matchedA);
        var second = run.TryStep(-1, new XPathQName("urn:x", "other"), out var matchedWildcard);
        var third = run.TryStep(-1, new XPathQName("urn:y", "other"), out _);

        await Assert.That(first).IsTrue();
        await Assert.That(matchedA.IsWildcard).IsFalse();
        await Assert.That(second).IsTrue();
        await Assert.That(matchedWildcard.IsWildcard).IsTrue();
        await Assert.That(matchedWildcard.ParticleIndex).IsEqualTo(1);
        await Assert.That(third).IsFalse();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task Wildcard_ExpectedReportsTheWildcardFlag()
    {
        var any = new FakeWildcard([], XsdProcessContents.Skip, "urn:x");
        var m = Compile(Seq(El(A), AutomatonParticle.Wildcard(1, 1, any)));
        var run = m.CreateRun();
        _ = run.TryStep(A, out _);
        var expected = new int[4];

        var written = run.CollectExpected(expected, out var wildcard);

        await Assert.That(written).IsEqualTo(0);
        await Assert.That(wildcard).IsTrue();
    }

    [Test]
    public async Task ElementPriorityOverWildcard_PicksTheNamedParticle()
    {
        var any = new FakeWildcard([], XsdProcessContents.Lax, "");
        var m = Compile(Cho(AutomatonParticle.Wildcard(1, 1, any), El(A)));
        var run = m.CreateRun();

        _ = run.TryStep(A, new XPathQName("", "a"), out var matched);

        await Assert.That(matched.IsWildcard).IsFalse();
    }

    [Test]
    public async Task SubstitutionClosure_IsOnePositionWithManyNames()
    {
        var m = Compile(Seq(AutomatonParticle.Element(1, 1, A, B, C), El(D)));

        await Assert.That(m.PositionCount).IsEqualTo(2);
        await Assert.That(Accepts(m, B, D)).IsTrue();
        await Assert.That(Accepts(m, C, D)).IsTrue();
        await Assert.That(Accepts(m, D, D)).IsFalse();
    }

    [Test]
    public async Task Compile_AboveThePositionCeiling_ReportsTheEngineCode()
    {
        var leaves = new AutomatonParticle[CountingAutomatonCompiler.MaxPositions + 1];
        for (var i = 0; i < leaves.Length; i++)
        {
            leaves[i] = El(i + 1);
        }

        var result = Compile(Cho(leaves), out var collector);
        var issues = collector.ToResult().Issues;

        await Assert.That(result).IsNull();
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.ContentModelPositionCeilingExceeded);
    }

    [Test]
    public async Task Compile_AboveTheTransitionCeiling_ReportsTheEngineCode()
    {
        // (a1 | ... | a2000)*: every position follows every position, 4 million pairs.
        var leaves = new AutomatonParticle[2000];
        for (var i = 0; i < leaves.Length; i++)
        {
            leaves[i] = El(i + 1);
        }

        var result = Compile(Cho(0, Unbounded, leaves), out var collector);
        var issues = collector.ToResult().Issues;

        await Assert.That(result).IsNull();
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.ContentModelTransitionCeilingExceeded);
    }

    [Test]
    public async Task ConfigurationCeiling_StopsARunWithoutCorruptingIt()
    {
        // (a{1,70}){1,70} keeps well over 1,024 counter configurations alive after about 46 a.
        var m = Compile(Seq(1, 70, El(A, 1, 70)));
        var run = m.CreateRun();
        var accepted = 0;
        while (accepted < 70 && run.TryStep(A, out _))
        {
            accepted++;
        }

        await Assert.That(run.CeilingExceeded).IsTrue();
        await Assert.That(accepted).IsLessThan(70);
    }

    [Test]
    public async Task Reset_RestartsTheRun()
    {
        var m = Compile(Seq(El(A), El(B)));
        var run = m.CreateRun();
        _ = run.TryStep(A, out _);
        _ = run.TryStep(B, out _);

        run.Reset();

        await Assert.That(run.CanEnd()).IsFalse();
        await Assert.That(run.TryStep(A, out _)).IsTrue();
    }

    [Test]
    public async Task EmptyContentModel_AcceptsOnlyEmptyContent()
    {
        var m = Compile(Seq());

        await Assert.That(m.PositionCount).IsEqualTo(0);
        await Assert.That(Accepts(m)).IsTrue();
        await Assert.That(Accepts(m, A)).IsFalse();
    }

    [Test]
    public async Task Particle_InvalidRange_Throws()
    {
        await Assert.That(() => El(A, 3, 2)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => El(A, -1, 2)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => AutomatonParticle.Sequence(1, 1, null!)).Throws<ArgumentNullException>();
    }
}
