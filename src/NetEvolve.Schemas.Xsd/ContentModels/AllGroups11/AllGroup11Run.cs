namespace NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The run state of one element frame over an <see cref="AllGroup11"/>: one bit per member that occurred, and a counter next
/// to the bits for each member that needs one. It offers the match step API of <c>CountingRun</c>, so the validator drives
/// both the same way.
/// </summary>
/// <remarks>
/// A step allocates nothing; the bitset and the counters are allocated once with the run. A rejected step leaves the run
/// unchanged. The occurrence range of a member bounds the children it takes: a child above <c>maxOccurs</c> is offered to the
/// wildcard members, and refused when none has room.
/// </remarks>
internal sealed class AllGroup11Run
{
    private readonly AllGroup11 _group;
    private readonly ulong[] _seen;
    private readonly long[] _counts;
    private bool _started;

    internal AllGroup11Run(AllGroup11 group)
    {
        Argument.ThrowIfNull(group);

        _group = group;
        _seen = new ulong[group.WordCount];
        _counts = new long[group.CounterSlots];
    }

    /// <summary>Returns to the state before the first child.</summary>
    public void Reset()
    {
        Array.Clear(_seen, 0, _seen.Length);
        Array.Clear(_counts, 0, _counts.Length);
        _started = false;
    }

    /// <summary>Determines whether a member has occurred at least once.</summary>
    /// <param name="member">The member index.</param>
    /// <returns><see langword="true"/> when it has been seen.</returns>
    public bool IsSeen(int member)
    {
        Argument.ThrowIfLessThan(member, 0);
        Argument.ThrowIfGreaterThanOrEqual(member, _group.MemberCount);

        return IsSeenCore(member);
    }

    private bool IsSeenCore(int member) => (_seen[member >> 6] & (1UL << (member & 63))) != 0;

    /// <summary>Tries to accept a child that is known by its schema name id only; wildcards never match.</summary>
    /// <param name="schemaNameId">The bound schema name id, <c>-1</c> when unknown.</param>
    /// <param name="matched">The matched member when accepted.</param>
    /// <returns><see langword="true"/> when the child is accepted.</returns>
    public bool TryStep(int schemaNameId, out MatchedParticle matched) => TryStep(schemaNameId, default, out matched);

    /// <summary>
    /// Tries to accept a child. A declared member takes it while it has room; then the first wildcard member that has room and
    /// allows the name does.
    /// </summary>
    /// <param name="schemaNameId">The bound schema name id, <c>-1</c> when the name is unknown to the schema.</param>
    /// <param name="name">The expanded name, used for wildcards; <see langword="default"/> reaches no wildcard.</param>
    /// <param name="matched">The matched member when accepted.</param>
    /// <returns><see langword="true"/> when the child is accepted.</returns>
    public bool TryStep(int schemaNameId, XPathQName name, out MatchedParticle matched)
    {
        matched = default;
        var member = _group.FindMember(schemaNameId);
        if (member >= 0 && HasRoom(member))
        {
            Accept(member);
            matched = new MatchedParticle(member, false);
            return true;
        }

        // An empty local name marks "no expanded name supplied"; real names never have one.
        if (name.LocalName.Length == 0)
        {
            return false;
        }

        foreach (var wildcard in _group.WildcardMembers)
        {
            if (HasRoom(wildcard) && _group.WildcardOf(wildcard)!.Allows(name))
            {
                Accept(wildcard);
                matched = new MatchedParticle(wildcard, true);
                return true;
            }
        }

        return false;
    }

    private bool HasRoom(int member)
    {
        var slot = _group.SlotOfMember(member);
        return slot >= 0 ? _counts[slot] < _group.MaxOf(member) : _group.MaxOf(member) != 1 || !IsSeenCore(member);
    }

    private void Accept(int member)
    {
        var slot = _group.SlotOfMember(member);
        if (slot >= 0)
        {
            _counts[slot]++;
        }

        _seen[member >> 6] |= 1UL << (member & 63);
        _started = true;
    }

    private bool IsMissing(int member)
    {
        var minimum = _group.MinOf(member);
        if (minimum == 0)
        {
            return false;
        }

        var slot = _group.SlotOfMember(member);
        return slot >= 0 ? _counts[slot] < minimum : !IsSeenCore(member);
    }

    /// <summary>Determines whether the content may end now.</summary>
    /// <returns>
    /// <see langword="true"/> when every member reached its minimum, or when nothing occurred and the group is optional.
    /// </returns>
    public bool CanEnd()
    {
        if (!_started && _group.IsOptional)
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

        foreach (var member in _group.CountedMembers)
        {
            if (_counts[_group.SlotOfMember(member)] < _group.MinOf(member))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Collects one schema name id per element member that is below its minimum, as many as fit.</summary>
    /// <param name="destination">The bounded destination.</param>
    /// <param name="wildcardMissing">Whether a wildcard member is below its minimum; it has no name to collect.</param>
    /// <returns>The number of ids written, the first name of each missing member.</returns>
    public int CollectMissing(Span<int> destination, out bool wildcardMissing)
    {
        wildcardMissing = false;
        var written = 0;
        for (var m = 0; m < _group.MemberCount; m++)
        {
            if (!IsMissing(m))
            {
                continue;
            }

            if (_group.WildcardOf(m) is not null)
            {
                wildcardMissing = true;
            }
            else if (written < destination.Length)
            {
                destination[written++] = _group.NamesOf(m)[0];
            }
        }

        return written;
    }

    /// <summary>Collects the schema name ids a child may have next, as many as fit.</summary>
    /// <param name="destination">The bounded destination.</param>
    /// <param name="wildcardExpected">Whether a wildcard member with room may take the next child.</param>
    /// <returns>The number of ids written, every name of each element member with room.</returns>
    public int CollectExpected(Span<int> destination, out bool wildcardExpected)
    {
        wildcardExpected = false;
        var written = 0;
        for (var m = 0; m < _group.MemberCount; m++)
        {
            if (!HasRoom(m))
            {
                continue;
            }

            if (_group.WildcardOf(m) is not null)
            {
                wildcardExpected = true;
                continue;
            }

            foreach (var id in _group.NamesOf(m))
            {
                if (written < destination.Length)
                {
                    destination[written++] = id;
                }
            }
        }

        return written;
    }
}
