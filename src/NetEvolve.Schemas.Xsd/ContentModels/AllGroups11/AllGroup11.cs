namespace NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The immutable program of one XSD 1.1 <c>all</c> group: its members, each an element particle or a wildcard particle with its
/// own occurrence range, and whether the group as a whole is optional (Structures 1.1, 3.8.4.1.3 and 3.9).
/// </summary>
/// <remarks>
/// <para>
/// The run state is a bitset of the members that occurred at least once, plus a counter for each member that needs more than
/// that: a bounded <c>maxOccurs</c> above 1, or a <c>minOccurs</c> above 1. A member with <c>maxOccurs</c> 1 or unbounded and a
/// <c>minOccurs</c> of 0 or 1 needs no counter, so a group of the XSD 1.0 shape costs what <c>AllGroup</c> costs.
/// </para>
/// <para>
/// Limitations: when an element is accepted by a declared member and by a wildcard member, the declared member takes it while it
/// has room; only then, and for a name no member declares, the first wildcard with room in document order takes it. That is the
/// validation path of 3.8.4.1.3 for the usual forms; a group in which two wildcards overlap breaks Unique Particle Attribution,
/// which is XSD-21, and is not detected here. A name id shared by two element members is a schema error that is not detected
/// here; the lookup then picks one of them. Nested <c>all</c> groups are flattened by <see cref="AllGroup11Compiler"/>. The
/// members are numbered by their order; <see cref="MatchedParticle.ParticleIndex"/> is that number.
/// </para>
/// </remarks>
internal sealed class AllGroup11 : ICompiledContentModel
{
    private readonly int[][] _memberNameIds;
    private readonly IWildcard?[] _wildcards;
    private readonly long[] _minOccurs;
    private readonly long[] _maxOccurs;
    private readonly int[] _slotOfMember;
    private readonly int[] _countedMembers;
    private readonly int[] _wildcardMembers;
    private readonly int[] _sortedNameIds;
    private readonly int[] _memberOfName;
    private readonly ulong[] _requiredMask;

    /// <summary>Initializes a new instance of the <see cref="AllGroup11"/> class.</summary>
    /// <param name="isOptional">Whether the group as a whole may be absent (<c>minOccurs</c> 0).</param>
    /// <param name="members">The element particles and wildcard particles, each with <c>maxOccurs</c> of at least 1.</param>
    public AllGroup11(bool isOptional, AutomatonParticle[] members)
    {
        ArgumentNullException.ThrowIfNull(members);

        IsOptional = isOptional;
        var count = members.Length;
        _memberNameIds = new int[count][];
        _wildcards = new IWildcard?[count];
        _minOccurs = new long[count];
        _maxOccurs = new long[count];
        _slotOfMember = new int[count];
        _requiredMask = new ulong[(count + 63) >> 6];

        var counted = 0;
        for (var m = 0; m < count; m++)
        {
            var member = members[m];
            ArgumentNullException.ThrowIfNull(member);
            ArgumentOutOfRangeException.ThrowIfGreaterThan((int)member.Kind, (int)AutomatonParticleKind.Wildcard);
            ArgumentOutOfRangeException.ThrowIfLessThan(member.MaxOccurs, 1L);

            _minOccurs[m] = member.MinOccurs;
            _maxOccurs[m] = member.MaxOccurs;
            _memberNameIds[m] = member.NameIds ?? [];
            if (member.Kind == AutomatonParticleKind.Wildcard)
            {
                _wildcards[m] = member.WildcardComponent;
            }

            // A bit says "at least once"; only a minimum above 1 or a bounded maximum above 1 needs a count.
            var needsCounter =
                member.MinOccurs > 1 || (member.MaxOccurs > 1 && member.MaxOccurs != AutomatonParticle.Unbounded);
            _slotOfMember[m] = needsCounter ? counted++ : -1;
            if (member.MinOccurs > 0)
            {
                _requiredMask[m >> 6] |= 1UL << (m & 63);
            }
        }

        _countedMembers = CountedIndexes(_slotOfMember, counted);
        _wildcardMembers = WildcardIndexes(_wildcards);
        _sortedNameIds = NameTable(_memberNameIds, out _memberOfName);
    }

    /// <summary>Gets a value indicating whether the group as a whole may be absent.</summary>
    public bool IsOptional { get; }

    /// <summary>Gets the number of members.</summary>
    public int MemberCount => _memberNameIds.Length;

    /// <inheritdoc />
    public int PositionCount => _memberNameIds.Length;

    /// <inheritdoc />
    /// <remarks>One counter for each member that a bit cannot describe.</remarks>
    public int CounterSlots => _countedMembers.Length;

    internal int WordCount => _requiredMask.Length;

    internal ulong RequiredWord(int word) => _requiredMask[word];

    internal int[] NamesOf(int member) => _memberNameIds[member];

    internal IWildcard? WildcardOf(int member) => _wildcards[member];

    internal long MinOf(int member) => _minOccurs[member];

    internal long MaxOf(int member) => _maxOccurs[member];

    /// <summary>Gets the counter slot of a member, <c>-1</c> when the bitset describes it.</summary>
    /// <param name="member">The member index.</param>
    /// <returns>The slot.</returns>
    internal int SlotOfMember(int member) => _slotOfMember[member];

    internal int[] CountedMembers => _countedMembers;

    internal int[] WildcardMembers => _wildcardMembers;

    /// <summary>Finds the element member that accepts a name id.</summary>
    /// <returns>The member index, <c>-1</c> when no element member accepts it.</returns>
    internal int FindMember(int schemaNameId)
    {
        if (schemaNameId < 0)
        {
            return -1;
        }

        var index = Array.BinarySearch(_sortedNameIds, schemaNameId);
        return index < 0 ? -1 : _memberOfName[index];
    }

    /// <summary>Creates the mutable run state for one element frame.</summary>
    /// <returns>The run.</returns>
    public AllGroup11Run CreateRun() => new(this);

    private static int[] CountedIndexes(int[] slots, int counted)
    {
        var members = new int[counted];
        for (var m = 0; m < slots.Length; m++)
        {
            if (slots[m] >= 0)
            {
                members[slots[m]] = m;
            }
        }

        return members;
    }

    private static int[] WildcardIndexes(IWildcard?[] wildcards)
    {
        var count = 0;
        foreach (var wildcard in wildcards)
        {
            count += wildcard is null ? 0 : 1;
        }

        var members = new int[count];
        count = 0;
        for (var m = 0; m < wildcards.Length; m++)
        {
            if (wildcards[m] is not null)
            {
                members[count++] = m;
            }
        }

        return members;
    }

    // The name ids of all element members in ascending order, and for each the member that owns it.
    private static int[] NameTable(int[][] names, out int[] memberOfName)
    {
        var total = 0;
        foreach (var ids in names)
        {
            total += ids.Length;
        }

        var sorted = new int[total];
        memberOfName = new int[total];
        var n = 0;
        for (var m = 0; m < names.Length; m++)
        {
            foreach (var id in names[m])
            {
                sorted[n] = id;
                memberOfName[n++] = m;
            }
        }

        Array.Sort(sorted, memberOfName);
        return sorted;
    }
}
