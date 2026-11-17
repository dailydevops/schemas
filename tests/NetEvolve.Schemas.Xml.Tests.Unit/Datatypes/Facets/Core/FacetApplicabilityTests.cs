namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Core;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

public class FacetApplicabilityTests
{
    private const DatatypeVariety Atomic = DatatypeVariety.Atomic;

    [Test]
    [Arguments(FacetKind.Length, PrimitiveKind.StringType, true)]
    [Arguments(FacetKind.MinLength, PrimitiveKind.AnyUri, true)]
    [Arguments(FacetKind.MaxLength, PrimitiveKind.HexBinary, true)]
    [Arguments(FacetKind.Length, PrimitiveKind.DecimalType, false)]
    [Arguments(FacetKind.Length, PrimitiveKind.Boolean, false)]
    [Arguments(FacetKind.MaxInclusive, PrimitiveKind.StringType, false)]
    [Arguments(FacetKind.MaxInclusive, PrimitiveKind.DecimalType, true)]
    [Arguments(FacetKind.MinExclusive, PrimitiveKind.GYear, true)]
    [Arguments(FacetKind.MinInclusive, PrimitiveKind.Duration, true)]
    [Arguments(FacetKind.MaxExclusive, PrimitiveKind.Boolean, false)]
    [Arguments(FacetKind.TotalDigits, PrimitiveKind.DecimalType, true)]
    [Arguments(FacetKind.FractionDigits, PrimitiveKind.DoubleType, false)]
    [Arguments(FacetKind.Pattern, PrimitiveKind.Boolean, true)]
    [Arguments(FacetKind.Enumeration, PrimitiveKind.QName, true)]
    [Arguments(FacetKind.WhiteSpace, PrimitiveKind.StringType, true)]
    [Arguments(FacetKind.None, PrimitiveKind.StringType, false)]
    public async Task Atomic_FollowsTheSpecificationTable(FacetKind facet, PrimitiveKind primitive, bool expected) =>
        await Assert
            .That(FacetApplicability.IsApplicable(facet, Atomic, primitive, XsdVersion.V11))
            .IsEqualTo(expected);

    [Test]
    [Arguments(FacetKind.Length, true)]
    [Arguments(FacetKind.MinLength, true)]
    [Arguments(FacetKind.MaxLength, true)]
    [Arguments(FacetKind.Enumeration, true)]
    [Arguments(FacetKind.Pattern, true)]
    [Arguments(FacetKind.WhiteSpace, true)]
    [Arguments(FacetKind.MaxInclusive, false)]
    [Arguments(FacetKind.TotalDigits, false)]
    public async Task List_AllowsLengthFacetsAndTheCommonOnes(FacetKind facet, bool expected) =>
        await Assert
            .That(FacetApplicability.IsApplicable(facet, DatatypeVariety.List, PrimitiveKind.None, XsdVersion.V11))
            .IsEqualTo(expected);

    [Test]
    [Arguments(FacetKind.Pattern, true)]
    [Arguments(FacetKind.Enumeration, true)]
    [Arguments(FacetKind.Length, false)]
    [Arguments(FacetKind.WhiteSpace, false)]
    [Arguments(FacetKind.MaxInclusive, false)]
    public async Task Union_AllowsOnlyPatternEnumerationAndAssertions(FacetKind facet, bool expected) =>
        await Assert
            .That(FacetApplicability.IsApplicable(facet, DatatypeVariety.Union, PrimitiveKind.None, XsdVersion.V11))
            .IsEqualTo(expected);

    [Test]
    public async Task Version11Facets_AreRejectedInVersion10()
    {
        await Assert
            .That(
                FacetApplicability.IsApplicable(FacetKind.Assertions, Atomic, PrimitiveKind.StringType, XsdVersion.V10)
            )
            .IsFalse();
        await Assert
            .That(
                FacetApplicability.IsApplicable(FacetKind.Assertions, Atomic, PrimitiveKind.StringType, XsdVersion.V11)
            )
            .IsTrue();
        await Assert
            .That(
                FacetApplicability.IsApplicable(FacetKind.ExplicitTimezone, Atomic, PrimitiveKind.Date, XsdVersion.V10)
            )
            .IsFalse();
        await Assert
            .That(
                FacetApplicability.IsApplicable(FacetKind.ExplicitTimezone, Atomic, PrimitiveKind.Date, XsdVersion.V11)
            )
            .IsTrue();
        await Assert
            .That(
                FacetApplicability.IsApplicable(
                    FacetKind.ExplicitTimezone,
                    Atomic,
                    PrimitiveKind.StringType,
                    XsdVersion.V11
                )
            )
            .IsFalse();
    }
}
