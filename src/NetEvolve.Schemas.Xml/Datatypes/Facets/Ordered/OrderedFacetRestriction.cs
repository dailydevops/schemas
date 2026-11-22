namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

/// <summary>
/// Checks a derivation step over the bounds, digits, and <c>explicitTimezone</c> facets of the base type (XSD 1.1 Part 2, sections
/// 4.3.7 to 4.3.12 and 4.3.14, and the facet-specific schema component constraints).
/// </summary>
/// <remarks>
/// <para>
/// Order of the checks: applicability, fixed flags, inclusive with exclusive bound of one side, bounds against the bounds of the base
/// type, lower against upper bound of the step, digits, then <c>explicitTimezone</c>. The first violation is reported.
/// </para>
/// <para>
/// Limitation: bounds that are not ordered against each other (<see cref="XsdOrder.Incomparable"/>, for example a dateTime
/// without timezone inside the 14 hour window) do not satisfy the relation that a valid restriction needs, so such a step is an
/// error. The length facets of the binary types are checked by <c>FacetRestriction</c> in the core facets. The integer family fixes
/// <c>fractionDigits</c> at zero in the specification; the built-in integer descriptors carry no facet set, so that fixed flag is not
/// part of this check.
/// </para>
/// </remarks>
public static class OrderedFacetRestriction
{
    private static readonly FacetKind[] BoundKinds =
    [
        FacetKind.MinInclusive,
        FacetKind.MinExclusive,
        FacetKind.MaxInclusive,
        FacetKind.MaxExclusive,
    ];

    private static readonly FacetKind[] Checked =
    [
        FacetKind.MinInclusive,
        FacetKind.MinExclusive,
        FacetKind.MaxInclusive,
        FacetKind.MaxExclusive,
        FacetKind.TotalDigits,
        FacetKind.FractionDigits,
        FacetKind.ExplicitTimezone,
    ];

    /// <summary>Determines whether a step is a valid restriction of its base type.</summary>
    /// <typeparam name="TValue">The value type of the bounds, always a non-ref struct.</typeparam>
    /// <param name="type">The type whose value space orders the bounds; its version selects the applicable facets.</param>
    /// <param name="primitive">The primitive ancestor of the type.</param>
    /// <param name="baseSet">The effective facets of the base type.</param>
    /// <param name="derived">The facets the step specifies.</param>
    /// <returns>The first violation, valid otherwise.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    public static OrderedFacetRestrictionResult Validate<TValue>(
        IDatatypeDescriptor<TValue> type,
        PrimitiveKind primitive,
        OrderedFacetSet<TValue> baseSet,
        OrderedFacetSet<TValue> derived
    )
        where TValue : struct
    {
        ArgumentNullException.ThrowIfNull(type);

        var specified = derived.Specified;
        foreach (var kind in Checked)
        {
            if (
                specified.Contains(kind)
                && !FacetApplicability.IsApplicable(kind, DatatypeVariety.Atomic, primitive, type.LexicalMode)
            )
            {
                return new(OrderedFacetRestrictionError.NotApplicable, kind);
            }
        }

        var fixedChange = FixedChange(type, baseSet, derived);
        if (fixedChange != FacetKind.None)
        {
            return new(OrderedFacetRestrictionError.FixedFacetChanged, fixedChange);
        }

        if (derived.MinInclusive.HasValue && derived.MinExclusive.HasValue)
        {
            return new(OrderedFacetRestrictionError.InclusiveAndExclusive, FacetKind.MinExclusive);
        }

        if (derived.MaxInclusive.HasValue && derived.MaxExclusive.HasValue)
        {
            return new(OrderedFacetRestrictionError.InclusiveAndExclusive, FacetKind.MaxExclusive);
        }

        var bound = CheckBounds(type, baseSet, derived);
        if (!bound.IsValid)
        {
            return bound;
        }

        return CheckDigitsAndTimezone(baseSet, derived);
    }

    private static OrderedFacetRestrictionResult CheckBounds<TValue>(
        IDatatypeDescriptor<TValue> type,
        OrderedFacetSet<TValue> baseSet,
        OrderedFacetSet<TValue> derived
    )
        where TValue : struct
    {
        foreach (var derivedKind in BoundKinds)
        {
            if (Get(derived, derivedKind) is not { } derivedValue)
            {
                continue;
            }

            foreach (var baseKind in BoundKinds)
            {
                if (
                    Get(baseSet, baseKind) is { } baseValue
                    && !Holds(type, derivedKind, derivedValue, baseKind, baseValue)
                )
                {
                    return new(OrderedFacetRestrictionError.BoundOutsideBase, derivedKind);
                }
            }
        }

        // The lower bound of the step against its own upper bound: the value space must not be empty.
        foreach (var lowerKind in BoundKinds)
        {
            if (IsUpper(lowerKind) || Get(derived, lowerKind) is not { } lower)
            {
                continue;
            }

            foreach (var upperKind in BoundKinds)
            {
                if (
                    IsUpper(upperKind)
                    && Get(derived, upperKind) is { } upper
                    && !Holds(type, lowerKind, lower, upperKind, upper)
                )
                {
                    return new(OrderedFacetRestrictionError.LowerBoundAboveUpperBound, lowerKind);
                }
            }
        }

        return default;
    }

    private static OrderedFacetRestrictionResult CheckDigitsAndTimezone<TValue>(
        OrderedFacetSet<TValue> baseSet,
        OrderedFacetSet<TValue> derived
    )
        where TValue : struct
    {
        if (derived.TotalDigits > baseSet.TotalDigits)
        {
            return new(OrderedFacetRestrictionError.TotalDigitsLoosened, FacetKind.TotalDigits);
        }

        if (derived.FractionDigits > baseSet.FractionDigits)
        {
            return new(OrderedFacetRestrictionError.FractionDigitsLoosened, FacetKind.FractionDigits);
        }

        var totalDigits = derived.TotalDigits ?? baseSet.TotalDigits;
        var fractionDigits = derived.FractionDigits ?? baseSet.FractionDigits;
        if ((derived.TotalDigits.HasValue || derived.FractionDigits.HasValue) && fractionDigits > totalDigits)
        {
            return new(OrderedFacetRestrictionError.FractionDigitsGreaterThanTotalDigits, FacetKind.FractionDigits);
        }

        // A base value of required or prohibited cannot be changed; optional can become either.
        return
            baseSet.ExplicitTimezone is { } baseMode
            && baseMode != ExplicitTimezoneMode.Optional
            && derived.ExplicitTimezone.HasValue
            && derived.ExplicitTimezone != baseMode
            ? new(OrderedFacetRestrictionError.ExplicitTimezoneChanged, FacetKind.ExplicitTimezone)
            : default;
    }

    private static FacetKind FixedChange<TValue>(
        IDatatypeDescriptor<TValue> type,
        OrderedFacetSet<TValue> baseSet,
        OrderedFacetSet<TValue> derived
    )
        where TValue : struct
    {
        var fixedFacets = baseSet.FixedFacets;
        foreach (var kind in BoundKinds)
        {
            if (
                fixedFacets.Contains(kind)
                && Get(baseSet, kind) is { } baseValue
                && Get(derived, kind) is { } derivedValue
                && !SameValue(type, derivedValue, baseValue)
            )
            {
                return kind;
            }
        }

        if (fixedFacets.Contains(FacetKind.TotalDigits) && Differs(baseSet.TotalDigits, derived.TotalDigits))
        {
            return FacetKind.TotalDigits;
        }

        if (fixedFacets.Contains(FacetKind.FractionDigits) && Differs(baseSet.FractionDigits, derived.FractionDigits))
        {
            return FacetKind.FractionDigits;
        }

        return
            fixedFacets.Contains(FacetKind.ExplicitTimezone)
            && baseSet.ExplicitTimezone.HasValue
            && derived.ExplicitTimezone.HasValue
            && baseSet.ExplicitTimezone != derived.ExplicitTimezone
            ? FacetKind.ExplicitTimezone
            : FacetKind.None;
    }

    // The bound of one facet kind, or null when the step does not specify it.
    private static TValue? Get<TValue>(in OrderedFacetSet<TValue> set, FacetKind kind)
        where TValue : struct =>
        kind switch
        {
            FacetKind.MinInclusive => set.MinInclusive,
            FacetKind.MinExclusive => set.MinExclusive,
            FacetKind.MaxInclusive => set.MaxInclusive,
            _ => set.MaxExclusive,
        };

    private static bool IsUpper(FacetKind kind) => kind is FacetKind.MaxInclusive or FacetKind.MaxExclusive;

    private static bool IsInclusive(FacetKind kind) => kind is FacetKind.MinInclusive or FacetKind.MaxInclusive;

    private static bool Differs(int? baseValue, int? derivedValue) =>
        baseValue.HasValue && derivedValue.HasValue && baseValue != derivedValue;

    private static bool SameValue<TValue>(IDatatypeDescriptor<TValue> type, TValue a, TValue b)
    {
        var order = type.Compare(in a, in b);
        return order == XsdOrder.Equal || (order == XsdOrder.Incomparable && type.IsIdentical(in a, in b));
    }

    // Whether the bound of facet kind <first> may stand next to the bound of facet kind <second>. On the same side the first
    // bound must be at least as tight as the second; on opposite sides the lower bound must lie below the upper bound. A strict
    // relation is needed whenever an exclusive bound or the loosening of an exclusive bound by an inclusive one would admit a value.
    private static bool Holds<TValue>(
        IDatatypeDescriptor<TValue> type,
        FacetKind first,
        TValue firstValue,
        FacetKind second,
        TValue secondValue
    )
    {
        var order = type.Compare(in firstValue, in secondValue);
        if (order is XsdOrder.Incomparable)
        {
            return false;
        }

        var firstUpper = IsUpper(first);
        if (firstUpper == IsUpper(second))
        {
            var strict = IsInclusive(first) && !IsInclusive(second);
            return firstUpper ? AtMost(order, strict) : AtLeast(order, strict);
        }

        var strictOpposite = !(IsInclusive(first) && IsInclusive(second));
        return firstUpper ? AtLeast(order, strictOpposite) : AtMost(order, strictOpposite);
    }

    private static bool AtMost(XsdOrder order, bool strict) =>
        order == XsdOrder.Less || (!strict && order == XsdOrder.Equal);

    private static bool AtLeast(XsdOrder order, bool strict) =>
        order == XsdOrder.Greater || (!strict && order == XsdOrder.Equal);
}
