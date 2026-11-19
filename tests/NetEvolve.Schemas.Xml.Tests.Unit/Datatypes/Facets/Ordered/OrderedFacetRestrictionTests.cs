namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Ordered;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

public class OrderedFacetRestrictionTests
{
    private static XsdDecimal D(string lexical) =>
        OrderedFacetSupport.Value<XsdDecimal>("decimal", lexical, XsdVersion.V11);

    private static XsdDateTime T(string lexical) =>
        OrderedFacetSupport.Value<XsdDateTime>("dateTime", lexical, XsdVersion.V11);

    private static OrderedFacetRestrictionResult Decimal(
        OrderedFacetSet<XsdDecimal> baseSet,
        OrderedFacetSet<XsdDecimal> derived,
        PrimitiveKind primitive = PrimitiveKind.DecimalType,
        XsdVersion version = XsdVersion.V11
    ) =>
        OrderedFacetRestriction.Validate(
            OrderedFacetSupport.Base<XsdDecimal>("decimal", version),
            primitive,
            baseSet,
            derived
        );

    private static OrderedFacetRestrictionResult DateTime(
        OrderedFacetSet<XsdDateTime> baseSet,
        OrderedFacetSet<XsdDateTime> derived,
        XsdVersion version = XsdVersion.V11
    ) =>
        OrderedFacetRestriction.Validate(
            OrderedFacetSupport.Base<XsdDateTime>("dateTime", version),
            PrimitiveKind.DateTime,
            baseSet,
            derived
        );

    private static FacetKindSet Fixed(params FacetKind[] kinds)
    {
        var set = default(FacetKindSet);
        foreach (var kind in kinds)
        {
            set = set.With(kind);
        }

        return set;
    }

    [Test]
    public async Task EmptyStep_IsValid()
    {
        var result = Decimal(default, default);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.None);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.None);
    }

    [Test]
    public async Task MinInclusiveAboveTheBaseMaxInclusive_IsAnError()
    {
        var result = Decimal(new(maxInclusive: D("10")), new(minInclusive: D("11")));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.BoundOutsideBase);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.MinInclusive);
    }

    [Test]
    public async Task MaxInclusiveBelowTheBaseMinInclusive_IsAnError()
    {
        var result = Decimal(new(minInclusive: D("10")), new(maxInclusive: D("9")));

        await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.BoundOutsideBase);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.MaxInclusive);
    }

    // Rows: base facet, base value, derived facet, derived value, valid. The base value is 10 in every row.
    [Test]
    // The same side keeps the bound or tightens it.
    [Arguments(FacetKind.MaxInclusive, FacetKind.MaxInclusive, "10", true)]
    [Arguments(FacetKind.MaxInclusive, FacetKind.MaxInclusive, "9.99", true)]
    [Arguments(FacetKind.MaxInclusive, FacetKind.MaxInclusive, "10.01", false)]
    [Arguments(FacetKind.MaxExclusive, FacetKind.MaxExclusive, "10", true)]
    [Arguments(FacetKind.MaxExclusive, FacetKind.MaxExclusive, "10.01", false)]
    [Arguments(FacetKind.MinInclusive, FacetKind.MinInclusive, "10", true)]
    [Arguments(FacetKind.MinInclusive, FacetKind.MinInclusive, "10.01", true)]
    [Arguments(FacetKind.MinInclusive, FacetKind.MinInclusive, "9.99", false)]
    [Arguments(FacetKind.MinExclusive, FacetKind.MinExclusive, "10", true)]
    [Arguments(FacetKind.MinExclusive, FacetKind.MinExclusive, "9.99", false)]
    // Inclusive against exclusive on the same side: an inclusive bound may not reach the excluded value.
    [Arguments(FacetKind.MaxExclusive, FacetKind.MaxInclusive, "10", false)]
    [Arguments(FacetKind.MaxExclusive, FacetKind.MaxInclusive, "9.99", true)]
    [Arguments(FacetKind.MaxInclusive, FacetKind.MaxExclusive, "10", true)]
    [Arguments(FacetKind.MaxInclusive, FacetKind.MaxExclusive, "10.01", false)]
    [Arguments(FacetKind.MinExclusive, FacetKind.MinInclusive, "10", false)]
    [Arguments(FacetKind.MinExclusive, FacetKind.MinInclusive, "10.01", true)]
    [Arguments(FacetKind.MinInclusive, FacetKind.MinExclusive, "10", true)]
    [Arguments(FacetKind.MinInclusive, FacetKind.MinExclusive, "9.99", false)]
    // The opposite side: the derived range must stay above the base lower bound and below the base upper bound.
    [Arguments(FacetKind.MinInclusive, FacetKind.MaxInclusive, "10", true)]
    [Arguments(FacetKind.MinInclusive, FacetKind.MaxInclusive, "9.99", false)]
    [Arguments(FacetKind.MinInclusive, FacetKind.MaxExclusive, "10", false)]
    [Arguments(FacetKind.MinInclusive, FacetKind.MaxExclusive, "10.01", true)]
    [Arguments(FacetKind.MinExclusive, FacetKind.MaxInclusive, "10", false)]
    [Arguments(FacetKind.MinExclusive, FacetKind.MaxInclusive, "10.01", true)]
    [Arguments(FacetKind.MinExclusive, FacetKind.MaxExclusive, "10", false)]
    [Arguments(FacetKind.MinExclusive, FacetKind.MaxExclusive, "10.01", true)]
    [Arguments(FacetKind.MaxInclusive, FacetKind.MinInclusive, "10", true)]
    [Arguments(FacetKind.MaxInclusive, FacetKind.MinInclusive, "10.01", false)]
    [Arguments(FacetKind.MaxInclusive, FacetKind.MinExclusive, "10", false)]
    [Arguments(FacetKind.MaxInclusive, FacetKind.MinExclusive, "9.99", true)]
    [Arguments(FacetKind.MaxExclusive, FacetKind.MinInclusive, "10", false)]
    [Arguments(FacetKind.MaxExclusive, FacetKind.MinInclusive, "9.99", true)]
    [Arguments(FacetKind.MaxExclusive, FacetKind.MinExclusive, "10", false)]
    [Arguments(FacetKind.MaxExclusive, FacetKind.MinExclusive, "9.99", true)]
    public async Task BoundsOfAStep_MustStayInsideTheBoundsOfTheBase(
        FacetKind baseFacet,
        FacetKind derivedFacet,
        string derivedValue,
        bool valid
    )
    {
        var result = Decimal(Set(baseFacet, D("10")), Set(derivedFacet, D(derivedValue)));

        await Assert
            .That(result.IsValid)
            .IsEqualTo(valid)
            .Because($"base {baseFacet} 10, derived {derivedFacet} {derivedValue}");
        if (!valid)
        {
            await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.BoundOutsideBase);
            await Assert.That(result.Facet).IsEqualTo(derivedFacet);
        }
    }

    private static OrderedFacetSet<XsdDecimal> Set(FacetKind kind, XsdDecimal value) =>
        kind switch
        {
            FacetKind.MinInclusive => new(minInclusive: value),
            FacetKind.MinExclusive => new(minExclusive: value),
            FacetKind.MaxInclusive => new(maxInclusive: value),
            _ => new(maxExclusive: value),
        };

    [Test]
    public async Task DerivedBoundsAreComparedWithEveryBoundOfTheBase()
    {
        var baseSet = new OrderedFacetSet<XsdDecimal>(minInclusive: D("0"), maxExclusive: D("100"));

        await Assert.That(Decimal(baseSet, new(minInclusive: D("5"), maxInclusive: D("99"))).IsValid).IsTrue();
        await Assert.That(Decimal(baseSet, new(maxInclusive: D("100"))).IsValid).IsFalse();
        await Assert.That(Decimal(baseSet, new(minExclusive: D("-1"))).IsValid).IsFalse();
        await Assert.That(Decimal(baseSet, new(minInclusive: D("100"))).IsValid).IsFalse();
    }

    [Test]
    [Arguments("5", "5", true)]
    [Arguments("5", "6", true)]
    [Arguments("6", "5", false)]
    public async Task MinInclusiveAndMaxInclusiveOfOneStep_MustNotCross(string min, string max, bool valid)
    {
        var result = Decimal(default, new(minInclusive: D(min), maxInclusive: D(max)));

        await Assert.That(result.IsValid).IsEqualTo(valid);
        if (!valid)
        {
            await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.LowerBoundAboveUpperBound);
            await Assert.That(result.Facet).IsEqualTo(FacetKind.MinInclusive);
        }
    }

    [Test]
    public async Task AnEmptyValueSpaceInOneStep_IsAnError()
    {
        // The exclusive side makes equal bounds empty.
        await Assert.That(Decimal(default, new(minInclusive: D("5"), maxExclusive: D("5"))).IsValid).IsFalse();
        await Assert.That(Decimal(default, new(minExclusive: D("5"), maxInclusive: D("5"))).IsValid).IsFalse();
        await Assert.That(Decimal(default, new(minExclusive: D("5"), maxExclusive: D("5"))).IsValid).IsFalse();
        await Assert.That(Decimal(default, new(minExclusive: D("5"), maxExclusive: D("6"))).IsValid).IsTrue();
        await Assert.That(Decimal(default, new(minInclusive: D("5"), maxExclusive: D("6"))).IsValid).IsTrue();
    }

    [Test]
    public async Task InclusiveAndExclusiveOfTheSameSide_InOneStep_IsAnError()
    {
        var minimum = Decimal(default, new(minInclusive: D("1"), minExclusive: D("2")));
        var maximum = Decimal(default, new(maxInclusive: D("9"), maxExclusive: D("8")));

        await Assert.That(minimum.Error).IsEqualTo(OrderedFacetRestrictionError.InclusiveAndExclusive);
        await Assert.That(minimum.Facet).IsEqualTo(FacetKind.MinExclusive);
        await Assert.That(maximum.Error).IsEqualTo(OrderedFacetRestrictionError.InclusiveAndExclusive);
        await Assert.That(maximum.Facet).IsEqualTo(FacetKind.MaxExclusive);
    }

    [Test]
    public async Task ChangingAFixedBound_IsAnError()
    {
        var baseSet = new OrderedFacetSet<XsdDecimal>(
            minInclusive: D("5"),
            maxInclusive: D("50"),
            fixedFacets: Fixed(FacetKind.MinInclusive, FacetKind.MaxInclusive)
        );

        var changed = Decimal(baseSet, new(minInclusive: D("6")));

        await Assert.That(changed.Error).IsEqualTo(OrderedFacetRestrictionError.FixedFacetChanged);
        await Assert.That(changed.Facet).IsEqualTo(FacetKind.MinInclusive);
        await Assert.That(Decimal(baseSet, new(maxInclusive: D("49"))).Facet).IsEqualTo(FacetKind.MaxInclusive);
    }

    [Test]
    public async Task RepeatingAFixedBoundWithTheSameValue_IsValid()
    {
        var baseSet = new OrderedFacetSet<XsdDecimal>(minInclusive: D("5"), fixedFacets: Fixed(FacetKind.MinInclusive));

        // 5.0 and 5.00 are the value 5.
        await Assert.That(Decimal(baseSet, new(minInclusive: D("5.00"))).IsValid).IsTrue();
        await Assert.That(Decimal(baseSet, default).IsValid).IsTrue();
        await Assert.That(Decimal(baseSet, new(maxInclusive: D("9"))).IsValid).IsTrue();
    }

    [Test]
    public async Task DateTimeBounds_FollowTheTimelineOrder()
    {
        var baseSet = new OrderedFacetSet<XsdDateTime>(maxInclusive: T("2000-01-01T12:00:00Z"));

        // 13:00+01:00 is the same instant.
        await Assert.That(DateTime(baseSet, new(maxInclusive: T("2000-01-01T13:00:00+01:00"))).IsValid).IsTrue();
        await Assert.That(DateTime(baseSet, new(maxInclusive: T("2000-01-01T14:00:00+01:00"))).IsValid).IsFalse();
        await Assert.That(DateTime(baseSet, new(minInclusive: T("2000-01-01T12:00:01Z"))).IsValid).IsFalse();
        await Assert.That(DateTime(baseSet, new(minInclusive: T("2000-01-01T11:59:59Z"))).IsValid).IsTrue();
    }

    // Pins the decision: bounds that are not ordered against each other do not satisfy the relation a valid restriction needs.
    [Test]
    public async Task IncomparableBounds_AreNotAValidRestriction()
    {
        var baseSet = new OrderedFacetSet<XsdDateTime>(maxInclusive: T("2000-01-01T12:00:00Z"));

        var result = DateTime(baseSet, new(maxInclusive: T("2000-01-01T13:00:00")));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.BoundOutsideBase);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.MaxInclusive);
    }

    [Test]
    [Arguments(5, 5, true)]
    [Arguments(5, 4, true)]
    [Arguments(5, 6, false)]
    public async Task TotalDigits_MayOnlyDecrease(int baseValue, int derivedValue, bool valid)
    {
        var result = Decimal(new(totalDigits: baseValue), new(totalDigits: derivedValue));

        await Assert.That(result.IsValid).IsEqualTo(valid);
        if (!valid)
        {
            await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.TotalDigitsLoosened);
            await Assert.That(result.Facet).IsEqualTo(FacetKind.TotalDigits);
        }
    }

    [Test]
    [Arguments(3, 3, true)]
    [Arguments(3, 2, true)]
    [Arguments(3, 4, false)]
    public async Task FractionDigits_MayOnlyDecrease(int baseValue, int derivedValue, bool valid)
    {
        var result = Decimal(new(fractionDigits: baseValue), new(fractionDigits: derivedValue));

        await Assert.That(result.IsValid).IsEqualTo(valid);
        if (!valid)
        {
            await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.FractionDigitsLoosened);
            await Assert.That(result.Facet).IsEqualTo(FacetKind.FractionDigits);
        }
    }

    [Test]
    public async Task FractionDigitsAboveTheEffectiveTotalDigits_IsAnError()
    {
        var inOneStep = Decimal(default, new(totalDigits: 3, fractionDigits: 4));
        var againstTheBase = Decimal(new(totalDigits: 3), new(fractionDigits: 4));
        var reducedTotal = Decimal(new(fractionDigits: 4), new(totalDigits: 3));

        await Assert.That(inOneStep.Error).IsEqualTo(OrderedFacetRestrictionError.FractionDigitsGreaterThanTotalDigits);
        await Assert.That(inOneStep.Facet).IsEqualTo(FacetKind.FractionDigits);
        await Assert
            .That(againstTheBase.Error)
            .IsEqualTo(OrderedFacetRestrictionError.FractionDigitsGreaterThanTotalDigits);
        await Assert
            .That(reducedTotal.Error)
            .IsEqualTo(OrderedFacetRestrictionError.FractionDigitsGreaterThanTotalDigits);
        await Assert.That(Decimal(default, new(totalDigits: 3, fractionDigits: 3)).IsValid).IsTrue();
    }

    [Test]
    public async Task ChangingAFixedDigitsFacet_IsAnError()
    {
        var baseSet = new OrderedFacetSet<XsdDecimal>(
            totalDigits: 8,
            fractionDigits: 2,
            fixedFacets: Fixed(FacetKind.TotalDigits, FacetKind.FractionDigits)
        );

        var total = Decimal(baseSet, new(totalDigits: 7));
        var fraction = Decimal(baseSet, new(fractionDigits: 1));

        await Assert.That(total.Error).IsEqualTo(OrderedFacetRestrictionError.FixedFacetChanged);
        await Assert.That(total.Facet).IsEqualTo(FacetKind.TotalDigits);
        await Assert.That(fraction.Error).IsEqualTo(OrderedFacetRestrictionError.FixedFacetChanged);
        await Assert.That(fraction.Facet).IsEqualTo(FacetKind.FractionDigits);
        await Assert.That(Decimal(baseSet, new(totalDigits: 8, fractionDigits: 2)).IsValid).IsTrue();
    }

    [Test]
    [Arguments(ExplicitTimezoneMode.Optional, ExplicitTimezoneMode.Optional, true)]
    [Arguments(ExplicitTimezoneMode.Optional, ExplicitTimezoneMode.Required, true)]
    [Arguments(ExplicitTimezoneMode.Optional, ExplicitTimezoneMode.Prohibited, true)]
    [Arguments(ExplicitTimezoneMode.Required, ExplicitTimezoneMode.Required, true)]
    [Arguments(ExplicitTimezoneMode.Required, ExplicitTimezoneMode.Optional, false)]
    [Arguments(ExplicitTimezoneMode.Required, ExplicitTimezoneMode.Prohibited, false)]
    [Arguments(ExplicitTimezoneMode.Prohibited, ExplicitTimezoneMode.Prohibited, true)]
    [Arguments(ExplicitTimezoneMode.Prohibited, ExplicitTimezoneMode.Optional, false)]
    [Arguments(ExplicitTimezoneMode.Prohibited, ExplicitTimezoneMode.Required, false)]
    public async Task ExplicitTimezone_MayOnlyBeNarrowedFromOptional(
        ExplicitTimezoneMode baseMode,
        ExplicitTimezoneMode derivedMode,
        bool valid
    )
    {
        var result = DateTime(new(explicitTimezone: baseMode), new(explicitTimezone: derivedMode));

        await Assert.That(result.IsValid).IsEqualTo(valid);
        if (!valid)
        {
            await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.ExplicitTimezoneChanged);
            await Assert.That(result.Facet).IsEqualTo(FacetKind.ExplicitTimezone);
        }
    }

    [Test]
    public async Task ExplicitTimezone_IsRejectedInVersion10()
    {
        var result = DateTime(default, new(explicitTimezone: ExplicitTimezoneMode.Required), XsdVersion.V10);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsEqualTo(OrderedFacetRestrictionError.NotApplicable);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.ExplicitTimezone);
    }

    [Test]
    public async Task ExplicitTimezone_IsAcceptedInVersion11() =>
        await Assert
            .That(DateTime(default, new(explicitTimezone: ExplicitTimezoneMode.Prohibited), XsdVersion.V11).IsValid)
            .IsTrue();

    [Test]
    public async Task FacetsThatTheTypeDoesNotAllow_AreRejectedByTheApplicabilityTable()
    {
        var boundOnString = Decimal(default, new(minInclusive: D("1")), PrimitiveKind.StringType);
        var digitsOnFloat = Decimal(default, new(totalDigits: 3), PrimitiveKind.FloatType);
        var timezoneOnDecimal = Decimal(default, new(explicitTimezone: ExplicitTimezoneMode.Required));
        var bounds = Decimal(default, new(minInclusive: D("1")), PrimitiveKind.FloatType);

        await Assert.That(boundOnString.Error).IsEqualTo(OrderedFacetRestrictionError.NotApplicable);
        await Assert.That(boundOnString.Facet).IsEqualTo(FacetKind.MinInclusive);
        await Assert.That(digitsOnFloat.Error).IsEqualTo(OrderedFacetRestrictionError.NotApplicable);
        await Assert.That(digitsOnFloat.Facet).IsEqualTo(FacetKind.TotalDigits);
        await Assert.That(timezoneOnDecimal.Error).IsEqualTo(OrderedFacetRestrictionError.NotApplicable);
        await Assert.That(timezoneOnDecimal.Facet).IsEqualTo(FacetKind.ExplicitTimezone);
        await Assert.That(bounds.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_RejectsANullType() =>
        await Assert
            .That(() =>
                OrderedFacetRestriction.Validate<XsdDecimal>(null!, PrimitiveKind.DecimalType, default, default)
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task FacetSet_ReportsTheSpecifiedFacets()
    {
        var set = new OrderedFacetSet<XsdDecimal>(
            minInclusive: D("1"),
            maxExclusive: D("9"),
            totalDigits: 4,
            explicitTimezone: ExplicitTimezoneMode.Optional
        );

        await Assert.That(set.Specified.Contains(FacetKind.MinInclusive)).IsTrue();
        await Assert.That(set.Specified.Contains(FacetKind.MaxExclusive)).IsTrue();
        await Assert.That(set.Specified.Contains(FacetKind.TotalDigits)).IsTrue();
        await Assert.That(set.Specified.Contains(FacetKind.ExplicitTimezone)).IsTrue();
        await Assert.That(set.Specified.Contains(FacetKind.MinExclusive)).IsFalse();
        await Assert.That(set.Specified.Contains(FacetKind.MaxInclusive)).IsFalse();
        await Assert.That(set.Specified.Contains(FacetKind.FractionDigits)).IsFalse();
        await Assert.That(default(OrderedFacetSet<XsdDecimal>).Specified).IsEqualTo(default(FacetKindSet));
    }

    [Test]
    [Arguments(0, null)]
    [Arguments(-1, null)]
    [Arguments(null, -1)]
    public async Task FacetSet_RejectsOutOfRangeDigits(int? totalDigits, int? fractionDigits) =>
        await Assert
            .That(() => new OrderedFacetSet<XsdDecimal>(totalDigits: totalDigits, fractionDigits: fractionDigits))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task FacetSet_RejectsAnUndefinedTimezoneMode() =>
        await Assert
            .That(() => new OrderedFacetSet<XsdDateTime>(explicitTimezone: (ExplicitTimezoneMode)7))
            .Throws<ArgumentOutOfRangeException>();
}
