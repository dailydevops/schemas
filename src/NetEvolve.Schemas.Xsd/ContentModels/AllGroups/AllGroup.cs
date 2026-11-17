namespace NetEvolve.Schemas.Xsd.ContentModels.AllGroups;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The immutable program of one <c>all</c> group: its members, whether each is required, and whether the group as a
/// whole is optional (Structures 3.8, 3.8.4, 3.8.6).
/// </summary>
/// <remarks>
/// Limitations: this is the XSD 1.0 shape (design #146 DP-7). Every member occurs at most once; <c>maxOccurs</c> above 1,
/// wildcards, and group references (XSD 1.1) are XSD-19, and schema errors for groups outside the 1.0 limits are XSD-60.
/// A name id shared by two members is a schema error that is not detected here; the lookup then picks one of them.
/// The members are numbered by their order; <see cref="MatchedParticle.ParticleIndex"/> is that number.
/// </remarks>
internal sealed class AllGroup : ICompiledContentModel
{
    private readonly int[] _sortedNameIds;
    private readonly int[] _memberOfName;
    private readonly int[][] _memberNameIds;
    private readonly ulong[] _requiredMask;

    /// <summary>Initializes a new instance of the <see cref="AllGroup"/> class.</summary>
    /// <param name="isOptional">Whether the group as a whole may be absent (<c>minOccurs</c> 0).</param>
    /// <param name="memberNameIds">The schema name ids each member accepts (a substitution group closure has several).</param>
    /// <param name="memberOptional">Whether each member may be absent (<c>minOccurs</c> 0).</param>
    public AllGroup(bool isOptional, int[][] memberNameIds, bool[] memberOptional)
    {
        Argument.ThrowIfNull(memberNameIds);
        Argument.ThrowIfNull(memberOptional);
        Argument.ThrowIfNotEqual(memberOptional.Length, memberNameIds.Length);

        IsOptional = isOptional;
        _memberNameIds = new int[memberNameIds.Length][];
        _requiredMask = new ulong[(memberNameIds.Length + 63) >> 6];

        var total = 0;
        for (var m = 0; m < memberNameIds.Length; m++)
        {
            var ids = memberNameIds[m];
            Argument.ThrowIfNull(ids);
            Argument.ThrowIfLessThan(ids.Length, 1);

            _memberNameIds[m] = (int[])ids.Clone();
            total += ids.Length;
            if (!memberOptional[m])
            {
                _requiredMask[m >> 6] |= 1UL << (m & 63);
            }
        }

        _sortedNameIds = new int[total];
        _memberOfName = new int[total];
        var n = 0;
        for (var m = 0; m < _memberNameIds.Length; m++)
        {
            foreach (var id in _memberNameIds[m])
            {
                Argument.ThrowIfLessThan(id, 0);

                _sortedNameIds[n] = id;
                _memberOfName[n++] = m;
            }
        }

        Array.Sort(_sortedNameIds, _memberOfName);
    }

    /// <summary>Gets a value indicating whether the group as a whole may be absent.</summary>
    public bool IsOptional { get; }

    /// <summary>Gets the number of members.</summary>
    public int MemberCount => _memberNameIds.Length;

    /// <inheritdoc />
    public int PositionCount => _memberNameIds.Length;

    /// <inheritdoc />
    /// <remarks>The seen members are a bitset in the run, not counters.</remarks>
    public int CounterSlots => 0;

    internal int WordCount => _requiredMask.Length;

    internal ulong RequiredWord(int word) => _requiredMask[word];

    internal int[] NamesOf(int member) => _memberNameIds[member];

    /// <summary>Finds the member that accepts a name id.</summary>
    /// <returns>The member index, <c>-1</c> when no member accepts it.</returns>
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
    public AllGroupRun CreateRun() => new(this);
}
