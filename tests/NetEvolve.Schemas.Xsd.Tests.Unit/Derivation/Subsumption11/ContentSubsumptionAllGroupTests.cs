namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Derivation.Subsumption11;
using NetEvolve.Schemas.Xsd.Elements;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11.SubsumptionKit;

/// <summary>All groups of XSD 1.1 as the restriction and as the base, against all groups, sequences, and choices.</summary>
public class ContentSubsumptionAllGroupTests
{
    private const long Unbounded = AutomatonParticle.Unbounded;

    [Test]
    public async Task AllGroup_AgainstItself_IsSubsumed() =>
        await AssertSubsumed(All([El("a"), El("b")]), All([El("a"), El("b")]));

    [Test]
    public async Task AllGroup_AgainstASequence_IsNotSubsumed_BecauseItAcceptsEveryOrder() =>
        await AssertNotSubsumed(All([El("a"), El("b")]), Seq([El("a"), El("b")]));

    [Test]
    public async Task Sequence_AgainstAnAllGroup_IsSubsumed() =>
        await AssertSubsumed(Seq([El("b"), El("a")]), All([El("a"), El("b")]));

    [Test]
    public async Task Sequence_ThatMissesAMember_AgainstAnAllGroup_IsNotSubsumed() =>
        await AssertNotSubsumed(Seq([El("a")]), All([El("a"), El("b")]));

    [Test]
    public async Task AllGroup_AgainstAChoiceOfBothOrders_IsSubsumed() =>
        await AssertSubsumed(All([El("a"), El("b")]), Cho([Seq([El("a"), El("b")]), Seq([El("b"), El("a")])]));

    [Test]
    public async Task AllGroup_AgainstAChoiceOfOneOrder_IsNotSubsumed() =>
        await AssertNotSubsumed(All([El("a"), El("b")]), Cho([Seq([El("a"), El("b")]), El("c")]));

    [Test]
    public async Task AllGroup_WithFewerMembers_IsNotSubsumedByTheLargerWhenRequired_ButByTheOptionalOne()
    {
        await AssertSubsumed(All([El("a"), El("b")]), All([El("a"), El("b"), El("c", 0, 1)]));
        await AssertNotSubsumed(All([El("a"), El("b")]), All([El("a"), El("b"), El("c")]));
        await AssertNotSubsumed(All([El("a"), El("b"), El("c", 0, 1, line: 4)]), All([El("a"), El("b")]), line: 4);
    }

    [Test]
    public async Task AllGroup_MakesAnOptionalMemberRequired_IsSubsumed_AndNotTheOtherWay()
    {
        await AssertSubsumed(All([El("a"), El("b")]), All([El("a"), El("b", 0, 1)]));
        await AssertNotSubsumed(All([El("a"), El("b", 0, 1)]), All([El("a"), El("b")]));
    }

    [Test]
    public async Task AllGroup_NarrowsTheOccurrenceRangeOfAMember_IsSubsumed_AndNotTheOtherWay()
    {
        await AssertSubsumed(All([El("a", 1, 2), El("b")]), All([El("a", 0, 3), El("b")]));
        await AssertNotSubsumed(All([El("a", 0, 3), El("b")]), All([El("a", 1, 2), El("b")]));
        await AssertSubsumed(All([El("a", 2, 5)]), All([El("a", 1, Unbounded)]));
        await AssertNotSubsumed(All([El("a", 1, Unbounded)]), All([El("a", 1, 5)]));
    }

    [Test]
    public async Task AllGroup_WithBoundsOfOneBillion_IsDecidedWithoutCountingThem()
    {
        await AssertSubsumed(All([El("a", 0, Billion)]), All([El("a", 0, Billion + 1)]));
        await AssertNotSubsumed(All([El("a", 0, Billion + 1)]), All([El("a", 0, Billion)]));
    }

    [Test]
    public async Task OptionalAllGroup_AcceptsTheEmptyContent()
    {
        await AssertSubsumed(All([El("a")], 0, 1), Seq([El("a", 0, 1)]));
        await AssertSubsumed(Seq([]), All([El("a")], 0, 1));
        await AssertNotSubsumed(Seq([], line: 9), All([El("a")]), line: 9);
        await AssertNotSubsumed(All([El("a")], 0, 1), All([El("a")]));
    }

    [Test]
    public async Task EmptyAllGroup_IsTheEmptyContent()
    {
        await AssertSubsumed(All([]), Seq([El("a", 0, 1)]));
        await AssertNotSubsumed(All([]), Seq([El("a")]));
        await AssertSubsumed(Seq([]), All([]));
    }

    [Test]
    public async Task AllGroup_WithAWildcardMember_IsCompared()
    {
        await AssertSubsumed(All([El("a"), El("b")]), All([El("a"), Any()]));
        await AssertNotSubsumed(All([El("a"), Any()]), All([El("a"), El("b")]));
        await AssertSubsumed(All([El("a", 0, 1), Any(min: 0, max: Unbounded)]), All([Any(min: 0, max: Unbounded)]));
    }

    [Test]
    public async Task AllGroup_WithUnboundedMembers_AgainstASequence_IsDecided()
    {
        await AssertSubsumed(Seq([El("a", 0, Unbounded)]), All([El("a", 0, Unbounded)]));
        await AssertNotSubsumed(All([El("a", 0, Unbounded)]), Seq([El("a", 0, 1)]));
    }

    [Test]
    public async Task AllGroup_ThroughAGroupReference_IsCompared()
    {
        await AssertSubsumed(Ref(ModelGroupCompositor.All, [El("a"), El("b")]), All([El("b", 0, 1), El("a")]));
        await AssertNotSubsumed(Ref(ModelGroupCompositor.All, [El("a"), El("b")]), Seq([El("a"), El("b")]));
    }

    [Test]
    public async Task AllGroup_WithAMemberThatCannotOccur_LeavesItOut()
    {
        await AssertSubsumed(All([El("a"), El("b", 0, 0)]), Seq([El("a")]));
        await AssertSubsumed(All([El("a"), El("b", 0, 0)]), All([El("a")]));
    }

    [Test]
    public async Task AllGroup_WithMoreMembersThanTheCountersAllow_IsUndecided()
    {
        var members = new Particle[SubsumptionSearch.MaxVariables + 1];
        for (var i = 0; i < members.Length; i++)
        {
            members[i] = El("m" + i, 0, 1);
        }

        var outcome = Check(All(members), All(members));

        await Assert.That(outcome.Verdict).IsEqualTo(SubsumptionVerdict.Undecided);
    }

    [Test]
    public async Task AllGroup_NestedInAnotherGroup_IsStillNotCompared()
    {
        var outcome = Check(Seq([El("a"), All([El("b")])]), Seq([El("a")]));

        await Assert.That(outcome.Verdict).IsEqualTo(SubsumptionVerdict.NotCompared);
    }

    [Test]
    public async Task AllGroup_ThatMayOccurMoreThanOnce_IsNotCompared()
    {
        var outcome = Check(All([El("a")], 1, 2), Seq([El("a")]));

        await Assert.That(outcome.Verdict).IsEqualTo(SubsumptionVerdict.NotCompared);
    }
}
