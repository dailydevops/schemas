namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

/// <summary>
/// The ordered and numeric facets that one derivation step specifies: the four bounds, <c>totalDigits</c>, <c>fractionDigits</c>,
/// and <c>explicitTimezone</c>, with the fixed flags. <see langword="default"/> is a step that specifies nothing.
/// </summary>
/// <typeparam name="TValue">The value type of the bounds, always a non-ref struct.</typeparam>
/// <remarks>
/// The length facets of the binary types are not part of this set: they are the integers of <c>FacetSet</c> in the core facets.
/// <c>enumeration</c>, <c>pattern</c>, and <c>assertions</c> are other facets.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct OrderedFacetSet<TValue>
    where TValue : struct
{
    /// <summary>Initializes a new instance of the <see cref="OrderedFacetSet{TValue}"/> struct.</summary>
    /// <param name="minInclusive">The <c>minInclusive</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="minExclusive">The <c>minExclusive</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="maxInclusive">The <c>maxInclusive</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="maxExclusive">The <c>maxExclusive</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="totalDigits">The <c>totalDigits</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="fractionDigits">The <c>fractionDigits</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="explicitTimezone">The <c>explicitTimezone</c> facet, <see langword="null"/> when not specified.</param>
    /// <param name="fixedFacets">The facets that further restrictions may not change.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="totalDigits"/> is less than one, <paramref name="fractionDigits"/> is negative, or <paramref name="explicitTimezone"/> is not a defined value.</exception>
    public OrderedFacetSet(
        TValue? minInclusive = null,
        TValue? minExclusive = null,
        TValue? maxInclusive = null,
        TValue? maxExclusive = null,
        int? totalDigits = null,
        int? fractionDigits = null,
        ExplicitTimezoneMode? explicitTimezone = null,
        FacetKindSet fixedFacets = default
    )
    {
        if (totalDigits.HasValue)
        {
            Argument.ThrowIfLessThan(totalDigits.Value, 1);
        }

        if (fractionDigits.HasValue)
        {
            Argument.ThrowIfLessThan(fractionDigits.Value, 0);
        }

        if (explicitTimezone.HasValue)
        {
            Argument.ThrowIfLessThan((int)explicitTimezone.Value, (int)ExplicitTimezoneMode.Optional);
            Argument.ThrowIfGreaterThan((int)explicitTimezone.Value, (int)ExplicitTimezoneMode.Prohibited);
        }

        MinInclusive = minInclusive;
        MinExclusive = minExclusive;
        MaxInclusive = maxInclusive;
        MaxExclusive = maxExclusive;
        TotalDigits = totalDigits;
        FractionDigits = fractionDigits;
        ExplicitTimezone = explicitTimezone;
        FixedFacets = fixedFacets;
    }

    /// <summary>Gets the <c>minInclusive</c> facet.</summary>
    public TValue? MinInclusive { get; }

    /// <summary>Gets the <c>minExclusive</c> facet.</summary>
    public TValue? MinExclusive { get; }

    /// <summary>Gets the <c>maxInclusive</c> facet.</summary>
    public TValue? MaxInclusive { get; }

    /// <summary>Gets the <c>maxExclusive</c> facet.</summary>
    public TValue? MaxExclusive { get; }

    /// <summary>Gets the <c>totalDigits</c> facet.</summary>
    public int? TotalDigits { get; }

    /// <summary>Gets the <c>fractionDigits</c> facet.</summary>
    public int? FractionDigits { get; }

    /// <summary>Gets the <c>explicitTimezone</c> facet.</summary>
    public ExplicitTimezoneMode? ExplicitTimezone { get; }

    /// <summary>Gets the facets that further restrictions may not change.</summary>
    public FacetKindSet FixedFacets { get; }

    /// <summary>Gets the facets that the step specifies.</summary>
    public FacetKindSet Specified
    {
        get
        {
            var set = default(FacetKindSet);
            if (MinInclusive.HasValue)
            {
                set = set.With(FacetKind.MinInclusive);
            }

            if (MinExclusive.HasValue)
            {
                set = set.With(FacetKind.MinExclusive);
            }

            if (MaxInclusive.HasValue)
            {
                set = set.With(FacetKind.MaxInclusive);
            }

            if (MaxExclusive.HasValue)
            {
                set = set.With(FacetKind.MaxExclusive);
            }

            if (TotalDigits.HasValue)
            {
                set = set.With(FacetKind.TotalDigits);
            }

            if (FractionDigits.HasValue)
            {
                set = set.With(FacetKind.FractionDigits);
            }

            return ExplicitTimezone.HasValue ? set.With(FacetKind.ExplicitTimezone) : set;
        }
    }
}
