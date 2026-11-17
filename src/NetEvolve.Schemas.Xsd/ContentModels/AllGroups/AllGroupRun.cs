namespace NetEvolve.Schemas.Xsd.ContentModels.AllGroups;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The run state of one element frame over an <see cref="AllGroup"/>: one bit per seen member. It offers the match
/// step API of <c>CountingRun</c>, so the validator drives both the same way.
/// </summary>
/// <remarks>
/// A step allocates nothing; the bitset is allocated once with the run. A rejected step leaves the run unchanged, and
/// callers tell a duplicate from an unknown name by <see cref="IsSeen"/> of the member.
/// </remarks>
internal sealed class AllGroupRun
{
    private readonly AllGroup _group;
    private readonly ulong[] _seen;
    private int _seenCount;

    internal AllGroupRun(AllGroup group)
    {
        Argument.ThrowIfNull(group);

        _group = group;
        _seen = new ulong[group.WordCount];
    }

    /// <summary>Returns to the state before the first child.</summary>
    public void Reset()
    {
        Array.Clear(_seen, 0, _seen.Length);
        _seenCount = 0;
    }

    /// <summary>Determines whether a member has occurred.</summary>
    /// <param name="member">The member index.</param>
    /// <returns><see langword="true"/> when it has been seen.</returns>
    public bool IsSeen(int member) => (_seen[member >> 6] & (1UL << (member & 63))) != 0;

    /// <summary>Tries to accept a child. A second occurrence of a member and an unknown name are rejected.</summary>
    /// <param name="schemaNameId">The bound schema name id, <c>-1</c> when unknown.</param>
    /// <param name="matched">The matched member when accepted.</param>
    /// <returns><see langword="true"/> when the child is accepted.</returns>
    public bool TryStep(int schemaNameId, out MatchedParticle matched)
    {
        matched = default;
        var member = _group.FindMember(schemaNameId);
        if (member < 0 || IsSeen(member))
        {
            return false;
        }

        _seen[member >> 6] |= 1UL << (member & 63);
        _seenCount++;
        matched = new MatchedParticle(member, false);
        return true;
    }

    /// <summary>Determines whether the content may end now.</summary>
    /// <returns>
    /// <see langword="true"/> when every required member occurred, or when nothing occurred and the group is optional.
    /// </returns>
    public bool CanEnd()
    {
        if (_seenCount == 0 && _group.IsOptional)
        {
            return true;
        }

        for (var w = 0; w < _seen.Length; w++)
        {
            if ((_group.RequiredWord(w) & ~_seen[w]) != 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Collects one schema name id per required member that has not occurred, as many as fit.</summary>
    /// <param name="destination">The bounded destination.</param>
    /// <returns>The number of ids written, the first name of each missing member.</returns>
    public int CollectMissing(Span<int> destination)
    {
        var written = 0;
        for (var m = 0; m < _group.MemberCount && written < destination.Length; m++)
        {
            if ((_group.RequiredWord(m >> 6) & (1UL << (m & 63))) != 0 && !IsSeen(m))
            {
                destination[written++] = _group.NamesOf(m)[0];
            }
        }

        return written;
    }

    /// <summary>Collects the schema name ids a child may have next, as many as fit.</summary>
    /// <param name="destination">The bounded destination.</param>
    /// <param name="wildcardExpected">Always <see langword="false"/>; wildcards are not part of a 1.0 all group.</param>
    /// <returns>The number of ids written, every name of each unseen member.</returns>
    public int CollectExpected(Span<int> destination, out bool wildcardExpected)
    {
        wildcardExpected = false;
        var written = 0;
        for (var m = 0; m < _group.MemberCount; m++)
        {
            if (IsSeen(m))
            {
                continue;
            }

            foreach (var id in _group.NamesOf(m))
            {
                if (written == destination.Length)
                {
                    return written;
                }

                destination[written++] = id;
            }
        }

        return written;
    }
}
