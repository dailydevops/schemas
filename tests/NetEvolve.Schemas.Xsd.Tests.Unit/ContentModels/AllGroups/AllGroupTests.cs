namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.AllGroups;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;

public class AllGroupTests
{
    private const int A = 1;
    private const int B = 2;
    private const int C = 3;

    private static AllGroup Group(bool optionalGroup, params bool[] memberOptional)
    {
        // Member i accepts the single name id i + 1.
        var ids = new int[memberOptional.Length][];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = [i + 1];
        }

        return new AllGroup(optionalGroup, ids, memberOptional);
    }

    private static bool Accepts(AllGroup group, params int[] ids)
    {
        var run = group.CreateRun();
        foreach (var id in ids)
        {
            if (!run.TryStep(id, out _))
            {
                return false;
            }
        }

        return run.CanEnd();
    }

    [Test]
    public async Task ThreeMembers_AcceptEveryPermutation()
    {
        var g = Group(false, false, false, false);
        int[][] permutations =
        [
            [A, B, C],
            [A, C, B],
            [B, A, C],
            [B, C, A],
            [C, A, B],
            [C, B, A],
        ];

        foreach (var permutation in permutations)
        {
            await Assert.That(Accepts(g, permutation)).IsTrue();
        }
    }

    [Test]
    public async Task OptionalMembers_MayBeAbsent()
    {
        var g = Group(false, false, true, true);

        await Assert.That(Accepts(g, A)).IsTrue();
        await Assert.That(Accepts(g, C, A)).IsTrue();
        await Assert.That(Accepts(g, B, A, C)).IsTrue();
        await Assert.That(Accepts(g, B)).IsFalse();
    }

    [Test]
    public async Task MissingRequiredMember_FailsAtTheEnd_AndNamesTheMissingElements()
    {
        var g = Group(false, false, false, true);
        var run = g.CreateRun();
        await Assert.That(run.TryStep(B, out _)).IsTrue();

        var missing = new int[4];
        var count = run.CollectMissing(missing);

        await Assert.That(run.CanEnd()).IsFalse();
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(missing[0]).IsEqualTo(A);
    }

    [Test]
    public async Task SecondOccurrence_FailsAtThatElement_AndKeepsTheState()
    {
        var g = Group(false, false, false);
        var run = g.CreateRun();
        await Assert.That(run.TryStep(A, out _)).IsTrue();

        await Assert.That(run.TryStep(A, out _)).IsFalse();
        await Assert.That(run.TryStep(B, out _)).IsTrue();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task UnknownName_IsRejected()
    {
        var g = Group(false, false);
        var run = g.CreateRun();

        await Assert.That(run.TryStep(99, out _)).IsFalse();
        await Assert.That(run.TryStep(-1, out _)).IsFalse();
    }

    [Test]
    public async Task OptionalGroup_AcceptsEmptyContent_ButOnceStartedNeedsItsRequiredMembers()
    {
        var g = Group(true, false, false);

        await Assert.That(Accepts(g)).IsTrue();
        await Assert.That(Accepts(g, A)).IsFalse();
        await Assert.That(Accepts(g, B, A)).IsTrue();
    }

    [Test]
    public async Task RequiredGroup_RejectsEmptyContent_UnlessAllMembersAreOptional()
    {
        await Assert.That(Accepts(Group(false, false, true))).IsFalse();
        await Assert.That(Accepts(Group(false, true, true))).IsTrue();
    }

    [Test]
    public async Task MemberWithSeveralNames_MatchesAnyOfThem_AsOneMember()
    {
        var g = new AllGroup(
            false,
            [
                [A, B],
                [C],
            ],
            [false, false]
        );

        await Assert.That(Accepts(g, B, C)).IsTrue();
        await Assert.That(Accepts(g, C, A)).IsTrue();
        await Assert.That(Accepts(g, A, B, C)).IsFalse();
    }

    [Test]
    public async Task MoreThan64Members_MatchCorrectly()
    {
        const int count = 130;
        var g = Group(false, new bool[count]);
        var run = g.CreateRun();

        for (var id = count; id >= 1; id--)
        {
            await Assert.That(run.TryStep(id, out var matched)).IsTrue();
            await Assert.That(matched.ParticleIndex).IsEqualTo(id - 1);
            await Assert.That(run.TryStep(id, out _)).IsFalse();
            await Assert.That(run.CanEnd()).IsEqualTo(id == 1);
        }

        run.Reset();
        _ = run.TryStep(70, out _);
        var missing = new int[count];
        await Assert.That(run.CollectMissing(missing)).IsEqualTo(count - 1);
        await Assert.That(run.CollectExpected(missing, out var wildcard)).IsEqualTo(count - 1);
        await Assert.That(wildcard).IsFalse();
    }

    [Test]
    public async Task CollectExpected_ListsTheUnseenNames_AndRespectsTheBound()
    {
        var g = Group(false, false, false, false);
        var run = g.CreateRun();
        _ = run.TryStep(B, out _);

        var all = new int[3];
        var written = run.CollectExpected(all, out _);
        var bounded = run.CollectExpected(new int[1], out _);

        await Assert.That(written).IsEqualTo(2);
        await Assert.That(all[0]).IsEqualTo(A);
        await Assert.That(all[1]).IsEqualTo(C);
        await Assert.That(bounded).IsEqualTo(1);
    }

    [Test]
    public async Task Reset_ReturnsToTheStart()
    {
        var g = Group(false, false);
        var run = g.CreateRun();
        _ = run.TryStep(A, out _);

        run.Reset();

        await Assert.That(run.TryStep(A, out _)).IsTrue();
    }

    [Test]
    public async Task MatchStep_AllocatesZeroBytes()
    {
        var g = Group(false, new bool[70]);
        var run = g.CreateRun();
        var destination = new int[8];

        AllocationAssert.NoAllocation(() =>
        {
            run.Reset();
            for (var id = 70; id >= 1; id--)
            {
                _ = run.TryStep(id, out _);
                _ = run.TryStep(id, out _);
            }

            _ = run.CanEnd();
            _ = run.CollectExpected(destination, out _);
            _ = run.CollectMissing(destination);
        });

        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task Constructor_RejectsInvalidArguments()
    {
        await Assert.That(() => new AllGroup(false, null!, [])).Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new AllGroup(
                    false,
                    [
                        [A],
                    ],
                    null!
                )
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new AllGroup(
                    false,
                    [
                        [A],
                    ],
                    []
                )
            )
            .Throws<ArgumentException>();
        await Assert.That(() => new AllGroup(false, [null!], [false])).Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new AllGroup(
                    false,
                    [
                        [],
                    ],
                    [false]
                )
            )
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() =>
                new AllGroup(
                    false,
                    [
                        [-1],
                    ],
                    [false]
                )
            )
            .Throws<ArgumentOutOfRangeException>();
    }
}
