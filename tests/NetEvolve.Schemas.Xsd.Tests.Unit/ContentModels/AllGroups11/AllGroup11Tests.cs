namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.AllGroups11;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class AllGroup11Tests
{
    private const int A = 1;
    private const int B = 2;
    private const int C = 3;

    // Children: a positive value is a declared element with that schema name id (in no namespace); Local is an unknown name in
    // no namespace, Foreign an unknown name in the namespace urn:w, Other an unknown name in the namespace urn:o.
    private const int Local = -1;
    private const int Foreign = -2;
    private const int Other = -3;

    private static AutomatonParticle E(int id, long min = 1, long max = 1) => AutomatonParticle.Element(min, max, id);

    private static AutomatonParticle W(string namespaces, long min = 1, long max = 1) =>
        AutomatonParticle.Wildcard(min, max, SchemaFixture.Wild(namespaces, XsdProcessContents.Lax));

    private static AllGroup11 Group(params AutomatonParticle[] members) => new(false, members);

    private static bool Step(AllGroup11Run run, int child) => Step(run, child, out _);

    private static bool Step(AllGroup11Run run, int child, out MatchedParticle matched) =>
        child switch
        {
            > 0 => run.TryStep(child, new XPathQName(string.Empty, "e" + child), out matched),
            Local => run.TryStep(-1, new XPathQName(string.Empty, "x"), out matched),
            Foreign => run.TryStep(-1, new XPathQName("urn:w", "x"), out matched),
            _ => run.TryStep(-1, new XPathQName("urn:o", "x"), out matched),
        };

    private static bool Accepts(AllGroup11 group, params int[] children)
    {
        var run = group.CreateRun();
        foreach (var child in children)
        {
            if (!Step(run, child))
            {
                return false;
            }
        }

        return run.CanEnd();
    }

    [Test]
    public async Task MaxOccursAboveOne_AcceptsEveryInterleavingUpToTheBound_AndRejectsAbove()
    {
        var group = Group(E(A, 1, 2), E(B, 1, 2));

        // Every sequence over {a, b} up to length 6, in every interleaving, is checked against the counts.
        for (var length = 0; length <= 6; length++)
        {
            for (var mask = 0; mask < 1 << length; mask++)
            {
                var children = new int[length];
                var a = 0;
                for (var i = 0; i < length; i++)
                {
                    children[i] = (mask >> i & 1) == 0 ? A : B;
                    a += children[i] == A ? 1 : 0;
                }

                var b = length - a;
                var expected = a is >= 1 and <= 2 && b is >= 1 and <= 2;
                await Assert.That(Accepts(group, children)).IsEqualTo(expected);
            }
        }
    }

    [Test]
    public async Task ExceedingTheBound_IsRejectedAtThatElement_AndKeepsTheState()
    {
        var run = Group(E(A, 0, 2), E(B)).CreateRun();
        await Assert.That(Step(run, A)).IsTrue();
        await Assert.That(Step(run, A)).IsTrue();

        await Assert.That(Step(run, A)).IsFalse();
        await Assert.That(Step(run, B)).IsTrue();
        await Assert.That(Step(run, B)).IsFalse();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task MinOccursAboveOne_NeedsThatManyOccurrences()
    {
        var group = Group(E(A, 3, 5), E(B, 0, 1));

        await Assert.That(Accepts(group, A, A)).IsFalse();
        await Assert.That(Accepts(group, A, B, A)).IsFalse();
        await Assert.That(Accepts(group, A, B, A, A)).IsTrue();
        await Assert.That(Accepts(group, A, A, A, A, A)).IsTrue();
        await Assert.That(Accepts(group, A, A, A, A, A, A)).IsFalse();
    }

    [Test]
    public async Task UnboundedMembers_AcceptAnyNumberAtOrAboveTheMinimum()
    {
        var group = Group(
            E(A, 2, AutomatonParticle.Unbounded),
            E(B, 0, AutomatonParticle.Unbounded),
            E(C, 1, AutomatonParticle.Unbounded)
        );

        await Assert.That(Accepts(group, A, C)).IsFalse();
        await Assert.That(Accepts(group, A, C, A)).IsTrue();
        await Assert.That(Accepts(group, C, B, A, B, C, A, B, A, A, A, A, A, A)).IsTrue();
        await Assert.That(Accepts(group, A, A, B)).IsFalse();
    }

    [Test]
    public async Task ZeroMinimumWithBound_NeedsNoOccurrence()
    {
        var group = Group(E(A, 0, 3));

        await Assert.That(Accepts(group)).IsTrue();
        await Assert.That(Accepts(group, A, A, A)).IsTrue();
        await Assert.That(Accepts(group, A, A, A, A)).IsFalse();
    }

    [Test]
    public async Task OptionalGroup_AcceptsEmptyContent_ButOnceStartedNeedsItsMinimums()
    {
        var group = new AllGroup11(true, [E(A, 2, 3), E(B)]);

        await Assert.That(Accepts(group)).IsTrue();
        await Assert.That(Accepts(group, A)).IsFalse();
        await Assert.That(Accepts(group, A, B, A)).IsTrue();
    }

    [Test]
    public async Task RequiredGroup_RejectsEmptyContent_UnlessEveryMemberIsOptional()
    {
        await Assert.That(Accepts(Group(E(A, 0, 2), E(B, 1, 2)))).IsFalse();
        await Assert.That(Accepts(Group(E(A, 0, 2), E(B, 0, 2)))).IsTrue();
        await Assert.That(Accepts(Group())).IsTrue();
    }

    [Test]
    public async Task Wildcard_MatchesAnElementThatNoDeclaredMemberMatches()
    {
        var group = Group(E(A), W("##any"));

        await Assert.That(Accepts(group, A, Foreign)).IsTrue();
        await Assert.That(Accepts(group, Foreign, A)).IsTrue();
        await Assert.That(Accepts(group, Local, A)).IsTrue();
        await Assert.That(Accepts(group, A)).IsFalse();
        await Assert.That(Accepts(group, Foreign)).IsFalse();
        await Assert.That(Accepts(group, Foreign, Other, A)).IsFalse();
    }

    [Test]
    public async Task Wildcard_RespectsItsNamespaceConstraint()
    {
        var group = Group(E(A), W("urn:w"));

        await Assert.That(Accepts(group, Foreign, A)).IsTrue();
        await Assert.That(Accepts(group, Other, A)).IsFalse();
        await Assert.That(Accepts(group, Local, A)).IsFalse();
    }

    [Test]
    public async Task DeclaredElement_IsPreferredOverTheWildcard_WhileItHasRoom()
    {
        var run = Group(E(A), W("##any")).CreateRun();

        await Assert.That(Step(run, A, out var first)).IsTrue();
        await Assert.That(first.IsWildcard).IsFalse();
        await Assert.That(first.ParticleIndex).IsEqualTo(0);

        // The element no longer competes, so the wildcard takes the second one (Structures 1.1, 3.8.4.1.3).
        await Assert.That(Step(run, A, out var second)).IsTrue();
        await Assert.That(second.IsWildcard).IsTrue();
        await Assert.That(second.ParticleIndex).IsEqualTo(1);
        await Assert.That(Step(run, A)).IsFalse();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task DeclaredElement_WinsOverARequiredWildcard_SoALoneElementLeavesTheWildcardUnmet()
    {
        // Decision: the validation path never attributes an item to a wildcard that a declared member can take.
        var group = Group(E(A, 0, 1), W("##any"));

        await Assert.That(Accepts(group, A)).IsFalse();
        await Assert.That(Accepts(group, A, A)).IsTrue();
        await Assert.That(Accepts(group, Foreign)).IsTrue();
    }

    [Test]
    public async Task Wildcard_MaxOccurs_BoundsTheElementsItTakes()
    {
        var group = Group(W("##any", 0, 2), E(A, 0, 1));

        await Assert.That(Accepts(group, Foreign, Local)).IsTrue();
        await Assert.That(Accepts(group, Foreign, Local, Other)).IsFalse();
        await Assert.That(Accepts(group, A, A, Foreign)).IsTrue();
        await Assert.That(Accepts(group, A, A, Foreign, Local)).IsFalse();
    }

    [Test]
    public async Task Wildcard_MinOccurs_NeedsThatManyElements()
    {
        var group = Group(W("##any", 2, AutomatonParticle.Unbounded), E(A, 0, 1));

        await Assert.That(Accepts(group, Foreign)).IsFalse();
        await Assert.That(Accepts(group, Foreign, Local)).IsTrue();
        await Assert.That(Accepts(group, Foreign, Local, Other, Foreign, Local)).IsTrue();
    }

    [Test]
    public async Task SeveralWildcards_TheFirstWithRoomInDocumentOrderTakesTheChild()
    {
        // Limitation: overlapping wildcards violate Unique Particle Attribution (XSD-21); the first one with room wins.
        var run = Group(W("##any", 0, 1), W("##any", 0, 1)).CreateRun();

        await Assert.That(Step(run, Foreign, out var first)).IsTrue();
        await Assert.That(Step(run, Foreign, out var second)).IsTrue();
        await Assert.That(Step(run, Foreign)).IsFalse();
        await Assert.That(first.ParticleIndex).IsEqualTo(0);
        await Assert.That(second.ParticleIndex).IsEqualTo(1);
    }

    [Test]
    public async Task IdOnlyStep_NeverReachesAWildcard()
    {
        var run = Group(E(A, 0, 1), W("##any", 0, 1)).CreateRun();

        await Assert.That(run.TryStep(A, out _)).IsTrue();
        await Assert.That(run.TryStep(A, out _)).IsFalse();
        await Assert.That(run.TryStep(-1, out _)).IsFalse();
        await Assert.That(run.TryStep(99, out _)).IsFalse();
        await Assert.That(run.TryStep(A, new XPathQName(string.Empty, "a"), out var viaName)).IsTrue();
        await Assert.That(viaName.IsWildcard).IsTrue();
    }

    [Test]
    public async Task UnknownName_WithoutAWildcard_IsRejected()
    {
        var run = Group(E(A)).CreateRun();

        await Assert.That(Step(run, Local)).IsFalse();
        await Assert.That(Step(run, 99)).IsFalse();
        await Assert.That(run.TryStep(-5, out _)).IsFalse();
    }

    [Test]
    public async Task MemberWithSeveralNames_MatchesAnyOfThem_AsOneMemberWithOneCount()
    {
        var group = Group(AutomatonParticle.Element(1, 2, A, B), E(C));

        await Assert.That(Accepts(group, B, C, A)).IsTrue();
        await Assert.That(Accepts(group, A, B, C)).IsTrue();
        await Assert.That(Accepts(group, A, B, A, C)).IsFalse();
    }

    [Test]
    public async Task MoreThan64Members_MixOfBitAndCounterMembers()
    {
        const int count = 130;
        var members = new AutomatonParticle[count];
        for (var i = 0; i < count; i++)
        {
            // Member i accepts the name id i + 1; every third member needs two to three occurrences.
            members[i] = i % 3 == 0 ? E(i + 1, 2, 3) : E(i + 1);
        }

        var run = Group(members).CreateRun();
        for (var id = count; id >= 1; id--)
        {
            var counted = (id - 1) % 3 == 0;
            await Assert.That(Step(run, id, out var matched)).IsTrue();
            await Assert.That(matched.ParticleIndex).IsEqualTo(id - 1);
            await Assert.That(Step(run, id)).IsEqualTo(counted);
        }

        await Assert.That(run.CanEnd()).IsTrue();
        await Assert.That(Step(run, 1)).IsTrue();
        await Assert.That(Step(run, 1)).IsFalse();

        run.Reset();
        _ = Step(run, 71);
        var missing = new int[count];
        await Assert.That(run.CollectMissing(missing, out _)).IsEqualTo(count - 1);
        await Assert.That(run.CollectExpected(missing, out _)).IsEqualTo(count - 1);
        await Assert.That(run.IsSeen(70)).IsTrue();
        await Assert.That(run.IsSeen(0)).IsFalse();
    }

    [Test]
    public async Task CollectMissing_NamesTheMembersBelowTheirMinimum_AndFlagsAWildcard()
    {
        var run = Group(E(A, 2, 3), E(B, 0, 1), E(C), W("##any", 1, 2)).CreateRun();
        _ = Step(run, A);
        var destination = new int[4];

        var written = run.CollectMissing(destination, out var wildcardMissing);

        await Assert.That(written).IsEqualTo(2);
        await Assert.That(destination[0]).IsEqualTo(A);
        await Assert.That(destination[1]).IsEqualTo(C);
        await Assert.That(wildcardMissing).IsTrue();
        await Assert.That(run.CanEnd()).IsFalse();

        _ = Step(run, A);
        _ = Step(run, C);
        _ = Step(run, Foreign);
        await Assert.That(run.CollectMissing(destination, out wildcardMissing)).IsEqualTo(0);
        await Assert.That(wildcardMissing).IsFalse();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task CollectMissing_RespectsTheBound()
    {
        var run = Group(E(A), E(B), E(C)).CreateRun();

        await Assert.That(run.CollectMissing(new int[2], out _)).IsEqualTo(2);
        await Assert.That(run.CollectMissing([], out _)).IsEqualTo(0);
    }

    [Test]
    public async Task CollectExpected_ListsTheNamesWithRoomLeft_AndFlagsAWildcardWithRoom()
    {
        var run = Group(E(A), E(B, 0, 2), E(C, 0, AutomatonParticle.Unbounded), W("##any", 0, 1)).CreateRun();
        _ = Step(run, A);
        _ = Step(run, B);
        _ = Step(run, B);
        _ = Step(run, C);
        var all = new int[4];

        var written = run.CollectExpected(all, out var wildcardExpected);

        await Assert.That(written).IsEqualTo(1);
        await Assert.That(all[0]).IsEqualTo(C);
        await Assert.That(wildcardExpected).IsTrue();

        _ = Step(run, Foreign);
        _ = run.CollectExpected(all, out wildcardExpected);
        await Assert.That(wildcardExpected).IsFalse();
    }

    [Test]
    public async Task CollectExpected_RespectsTheBound_AndListsEveryNameOfAMember()
    {
        var run = Group(AutomatonParticle.Element(0, 1, A, B), E(C, 0, 1)).CreateRun();

        await Assert.That(run.CollectExpected(new int[3], out _)).IsEqualTo(3);
        await Assert.That(run.CollectExpected(new int[2], out _)).IsEqualTo(2);
        await Assert.That(run.CollectExpected(new int[1], out _)).IsEqualTo(1);
        await Assert.That(run.CollectExpected([], out _)).IsEqualTo(0);
    }

    [Test]
    public async Task Reset_ReturnsToTheStart_IncludingTheCounters()
    {
        var group = Group(E(A, 2, 2), E(B));
        var run = group.CreateRun();
        _ = Step(run, A);
        _ = Step(run, A);
        _ = Step(run, B);

        run.Reset();

        await Assert.That(run.CanEnd()).IsFalse();
        await Assert.That(Step(run, A)).IsTrue();
        await Assert.That(Step(run, A)).IsTrue();
        await Assert.That(Step(run, A)).IsFalse();
        await Assert.That(Step(run, B)).IsTrue();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task Program_ReportsItsShape()
    {
        var group = new AllGroup11(true, [E(A), E(B, 0, 3), E(C, 2, 4), W("##any")]);

        await Assert.That(group.IsOptional).IsTrue();
        await Assert.That(group.MemberCount).IsEqualTo(4);
        await Assert.That(group.PositionCount).IsEqualTo(4);
        await Assert.That(group.CounterSlots).IsEqualTo(2);
    }

    [Test]
    public async Task MatchStep_AllocatesZeroBytes()
    {
        var members = new AutomatonParticle[72];
        for (var i = 0; i < 70; i++)
        {
            members[i] = i % 2 == 0 ? E(i + 1, 1, 3) : E(i + 1, 0, AutomatonParticle.Unbounded);
        }

        members[70] = E(71, 2, 4);
        members[71] = W("urn:w", 0, 5);
        var run = Group(members).CreateRun();
        var destination = new int[8];
        var local = new XPathQName(string.Empty, "x");
        var foreign = new XPathQName("urn:w", "x");

        AllocationAssert.NoAllocation(() =>
        {
            run.Reset();
            for (var id = 71; id >= 1; id--)
            {
                _ = run.TryStep(id, local, out _);
                _ = run.TryStep(id, local, out _);
                _ = run.TryStep(id, local, out _);
                _ = run.TryStep(id, local, out _);
                _ = run.TryStep(-1, foreign, out _);
            }

            _ = run.CanEnd();
            _ = run.CollectExpected(destination, out _);
            _ = run.CollectMissing(destination, out _);
        });

        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task Constructor_RejectsInvalidArguments()
    {
        await Assert.That(() => new AllGroup11(false, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new AllGroup11(false, [null!])).Throws<ArgumentNullException>();
        await Assert
            .That(() => new AllGroup11(false, [AutomatonParticle.Sequence(1, 1)]))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => new AllGroup11(false, [AutomatonParticle.Choice(1, 1)]))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new AllGroup11(false, [E(A, 0, 0)])).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task IsSeen_OutOfRangeMember_Throws()
    {
        var run = Group(E(A), E(B)).CreateRun();

        await Assert.That(() => run.IsSeen(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => run.IsSeen(2)).Throws<ArgumentOutOfRangeException>();
    }
}
