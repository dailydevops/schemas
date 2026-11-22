namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The operator mapping of XPath 2.0, B.2, for the comparisons: the implementation of a relation by the types of its operands. The
/// table is open: the registration file of this folder fills it with the numeric, string, and boolean entries, and the folder of
/// another type family (dates, times, and durations) adds its entries through a registration of its own, without a shared list or a
/// switch. A lookup finds the entry of the exact types first and then applies subtype substitution: an operand of a derived type
/// uses the entry of the base type it derives from, the most specific entry winning. A relation that a type family does not
/// register, such as <c>lt</c> on <c>xs:duration</c>, has no entry for it.
/// </summary>
/// <remarks>
/// <para>
/// Not thread-safe for registration; fill it before compiling. Reading is safe for concurrent evaluations. Limitation: a missing
/// entry is not reported here; the caller raises XPTY0004, which XPath 2.0, 3.5.1 defines for operand types without an operator.
/// </para>
/// <para>
/// This table mirrors the shape of the table of the arithmetic operators, which is keyed by the arithmetic operator enumeration
/// and lives in the folder of another work package; the two tables share no code, and unifying them is a change to that folder.
/// </para>
/// <para>
/// An entry is registered with <see cref="ComparisonImplementation"/> when it needs the operands only, and with
/// <see cref="ContextComparisonImplementation"/> when it reads the dynamic context, as the comparisons of dates and times do for the
/// implicit timezone. The evaluation looks entries up with <c>TryGetContextual</c>, which finds both kinds; <c>TryGet</c> finds the
/// entries registered without the context only.
/// </para>
/// </remarks>
internal sealed class ComparisonOperatorTable
{
    private readonly Dictionary<(int Relation, int Left, int Right), Entry> _entries = [];
    private readonly List<Entry>[] _byRelation;

    /// <summary>Initializes a new instance of the <see cref="ComparisonOperatorTable"/> class, empty.</summary>
    public ComparisonOperatorTable()
    {
        _byRelation = new List<Entry>[(int)ComparisonRelation.GreaterThanOrEqual + 1];
        for (var i = 0; i < _byRelation.Length; i++)
        {
            _byRelation[i] = [];
        }
    }

    /// <summary>Registers the implementation of a relation for a pair of operand types.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The type of the left operand.</param>
    /// <param name="right">The type of the right operand.</param>
    /// <param name="implementation">The implementation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    /// <exception cref="InvalidOperationException">The relation and the pair of types already have an implementation.</exception>
    public void Register(
        ComparisonRelation relation,
        XdmTypeId left,
        XdmTypeId right,
        ComparisonImplementation implementation
    )
    {
        ComparisonRelations.Check(relation);
        ArgumentNullException.ThrowIfNull(implementation);

        Add(
            relation,
            new Entry(
                left,
                right,
                implementation,
                (ComparisonRelation r, in XdmItem a, in XdmItem b, XPathEvaluationContext _) => implementation(r, a, b)
            )
        );
    }

    /// <summary>Registers the implementation of a relation for a pair of operand types that reads the dynamic context.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The type of the left operand.</param>
    /// <param name="right">The type of the right operand.</param>
    /// <param name="implementation">The implementation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    /// <exception cref="InvalidOperationException">The relation and the pair of types already have an implementation.</exception>
    public void RegisterContextual(
        ComparisonRelation relation,
        XdmTypeId left,
        XdmTypeId right,
        ContextComparisonImplementation implementation
    )
    {
        ComparisonRelations.Check(relation);
        ArgumentNullException.ThrowIfNull(implementation);

        Add(relation, new Entry(left, right, null, implementation));
    }

    /// <summary>Finds the implementation of a relation for a pair of operand types.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The type of the left operand.</param>
    /// <param name="right">The type of the right operand.</param>
    /// <param name="implementation">The implementation, <see langword="null"/> when there is none.</param>
    /// <returns><see langword="true"/> when an entry matches the types exactly or by subtype substitution.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    /// <remarks>
    /// Allocates nothing. The exact entry is one dictionary lookup; substitution scans the entries of the relation. When two entries
    /// match and the types of neither derive from the types of the other, the one registered first wins. It returns <see langword="false"/> when
    /// the most specific match is a contextual entry, even if a less specific plain entry also matches; <c>TryGetContextual</c> finds both kinds.
    /// </remarks>
    public bool TryGet(
        ComparisonRelation relation,
        XdmTypeId left,
        XdmTypeId right,
        out ComparisonImplementation? implementation
    )
    {
        var found = TryFind(relation, left, right, out var entry);
        implementation = found ? entry.Plain : null;
        return implementation is not null;
    }

    /// <summary>Finds the implementation of a relation for a pair of operand types, whether it reads the dynamic context or not.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The type of the left operand.</param>
    /// <param name="right">The type of the right operand.</param>
    /// <param name="implementation">The implementation, <see langword="null"/> when there is none; an entry registered without the context is wrapped once, at registration.</param>
    /// <returns><see langword="true"/> when an entry matches the types exactly or by subtype substitution.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    /// <remarks>Allocates nothing; the lookup is the one of <c>TryGet</c>.</remarks>
    public bool TryGetContextual(
        ComparisonRelation relation,
        XdmTypeId left,
        XdmTypeId right,
        out ContextComparisonImplementation? implementation
    )
    {
        var found = TryFind(relation, left, right, out var entry);
        implementation = found ? entry.Contextual : null;
        return found;
    }

    private void Add(ComparisonRelation relation, in Entry entry)
    {
        var key = ((int)relation, entry.Left.Value, entry.Right.Value);
        if (_entries.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"An implementation of '{relation}' for the operand types {entry.Left.Value} and {entry.Right.Value} is already registered."
            );
        }

        _entries.Add(key, entry);
        _byRelation[(int)relation].Add(entry);
    }

    private bool TryFind(ComparisonRelation relation, XdmTypeId left, XdmTypeId right, out Entry entry)
    {
        ComparisonRelations.Check(relation);

        if (_entries.TryGetValue(((int)relation, left.Value, right.Value), out entry))
        {
            return true;
        }

        // Substitution: the entry whose types both operands derive from; of two such entries, the one whose types derive from the other's.
        var entries = _byRelation[(int)relation];
        var best = -1;
        for (var i = 0; i < entries.Count; i++)
        {
            var candidate = entries[i];
            if (!XdmTypes.IsDerivedFrom(left, candidate.Left) || !XdmTypes.IsDerivedFrom(right, candidate.Right))
            {
                continue;
            }

            if (
                best < 0
                || (
                    XdmTypes.IsDerivedFrom(candidate.Left, entries[best].Left)
                    && XdmTypes.IsDerivedFrom(candidate.Right, entries[best].Right)
                )
            )
            {
                best = i;
            }
        }

        entry = best < 0 ? default : entries[best];
        return best >= 0;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Entry(
        XdmTypeId left,
        XdmTypeId right,
        ComparisonImplementation? plain,
        ContextComparisonImplementation contextual
    )
    {
        public XdmTypeId Left { get; } = left;

        public XdmTypeId Right { get; } = right;

        public ComparisonImplementation? Plain { get; } = plain;

        public ContextComparisonImplementation Contextual { get; } = contextual;
    }
}
