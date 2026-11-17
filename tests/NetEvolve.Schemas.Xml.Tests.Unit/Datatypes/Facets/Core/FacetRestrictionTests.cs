namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

public class FacetRestrictionTests
{
    private static FacetRestrictionResult Strings(FacetSet baseSet, FacetSet derived) =>
        FacetRestriction.Validate(baseSet, derived, DatatypeVariety.Atomic, PrimitiveKind.StringType, XsdVersion.V11);

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
        var result = Strings(default, default);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Error).IsEqualTo(FacetRestrictionError.None);
    }

    [Test]
    public async Task ChangingAFixedFacet_IsAnError()
    {
        var baseSet = new FacetSet(minLength: 2, fixedFacets: Fixed(FacetKind.MinLength));

        var result = Strings(baseSet, new FacetSet(minLength: 3));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsEqualTo(FacetRestrictionError.FixedFacetChanged);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.MinLength);
    }

    [Test]
    public async Task RepeatingAFixedFacetWithTheSameValue_IsValid()
    {
        var baseSet = new FacetSet(maxLength: 5, fixedFacets: Fixed(FacetKind.MaxLength));

        await Assert.That(Strings(baseSet, new FacetSet(maxLength: 5)).IsValid).IsTrue();
        await Assert.That(Strings(baseSet, default).IsValid).IsTrue();
    }

    [Test]
    public async Task ChangingAFixedWhiteSpace_IsAnError()
    {
        var baseSet = new FacetSet(whiteSpace: WhitespaceMode.Replace, fixedFacets: Fixed(FacetKind.WhiteSpace));

        var result = Strings(baseSet, new FacetSet(whiteSpace: WhitespaceMode.Collapse));

        await Assert.That(result.Error).IsEqualTo(FacetRestrictionError.FixedFacetChanged);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.WhiteSpace);
    }

    [Test]
    [Arguments(WhitespaceMode.Collapse, WhitespaceMode.Preserve, false)]
    [Arguments(WhitespaceMode.Collapse, WhitespaceMode.Replace, false)]
    [Arguments(WhitespaceMode.Replace, WhitespaceMode.Preserve, false)]
    [Arguments(WhitespaceMode.Preserve, WhitespaceMode.Replace, true)]
    [Arguments(WhitespaceMode.Preserve, WhitespaceMode.Collapse, true)]
    [Arguments(WhitespaceMode.Replace, WhitespaceMode.Collapse, true)]
    [Arguments(WhitespaceMode.Collapse, WhitespaceMode.Collapse, true)]
    public async Task WhiteSpace_MayOnlyTighten(WhitespaceMode from, WhitespaceMode to, bool valid)
    {
        var result = Strings(new FacetSet(whiteSpace: from), new FacetSet(whiteSpace: to));

        await Assert.That(result.IsValid).IsEqualTo(valid);
        if (!valid)
        {
            await Assert.That(result.Error).IsEqualTo(FacetRestrictionError.WhiteSpaceLoosened);
            await Assert.That(result.Facet).IsEqualTo(FacetKind.WhiteSpace);
        }
    }

    [Test]
    public async Task Length_WithConflictingMinLength_IsAnError()
    {
        var result = Strings(default, new FacetSet(length: 3, minLength: 4));

        await Assert.That(result.Error).IsEqualTo(FacetRestrictionError.LengthConflict);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.MinLength);
    }

    [Test]
    public async Task Length_WithConflictingMaxLength_IsAnError()
    {
        var result = Strings(default, new FacetSet(length: 5, maxLength: 4));

        await Assert.That(result.Error).IsEqualTo(FacetRestrictionError.LengthConflict);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.MaxLength);
    }

    [Test]
    public async Task Length_WithConsistentMinAndMax_IsValid() =>
        await Assert.That(Strings(default, new FacetSet(length: 4, minLength: 2, maxLength: 4)).IsValid).IsTrue();

    [Test]
    public async Task MinLength_GreaterThanMaxLength_IsAnError()
    {
        var result = Strings(default, new FacetSet(minLength: 5, maxLength: 4));

        await Assert.That(result.Error).IsEqualTo(FacetRestrictionError.MinLengthGreaterThanMaxLength);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.MinLength);
    }

    [Test]
    public async Task MinLength_AgainstTheInheritedMaxLength_IsChecked()
    {
        var baseSet = new FacetSet(maxLength: 4);

        await Assert
            .That(Strings(baseSet, new FacetSet(minLength: 5)).Error)
            .IsEqualTo(FacetRestrictionError.MinLengthGreaterThanMaxLength);
        await Assert.That(Strings(baseSet, new FacetSet(minLength: 4)).IsValid).IsTrue();
    }

    [Test]
    public async Task Length_AgainstInheritedBounds_IsChecked()
    {
        var baseSet = new FacetSet(minLength: 2, maxLength: 6);

        await Assert
            .That(Strings(baseSet, new FacetSet(length: 1)).Error)
            .IsEqualTo(FacetRestrictionError.LengthConflict);
        await Assert
            .That(Strings(baseSet, new FacetSet(length: 7)).Error)
            .IsEqualTo(FacetRestrictionError.LengthConflict);
        await Assert.That(Strings(baseSet, new FacetSet(length: 3)).IsValid).IsTrue();
    }

    [Test]
    public async Task Length_MayNotChangeFromTheBase()
    {
        var baseSet = new FacetSet(length: 3);

        var result = Strings(baseSet, new FacetSet(length: 4));

        await Assert.That(result.Error).IsEqualTo(FacetRestrictionError.LengthChanged);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.Length);
        await Assert.That(Strings(baseSet, new FacetSet(length: 3)).IsValid).IsTrue();
    }

    [Test]
    public async Task MinLength_MayNotDecrease_AndMaxLength_MayNotIncrease()
    {
        var baseSet = new FacetSet(minLength: 2, maxLength: 6);

        await Assert
            .That(Strings(baseSet, new FacetSet(minLength: 1)).Error)
            .IsEqualTo(FacetRestrictionError.MinLengthLoosened);
        await Assert
            .That(Strings(baseSet, new FacetSet(maxLength: 7)).Error)
            .IsEqualTo(FacetRestrictionError.MaxLengthLoosened);
        await Assert.That(Strings(baseSet, new FacetSet(minLength: 3, maxLength: 5)).IsValid).IsTrue();
    }

    [Test]
    public async Task InapplicableFacet_IsRejectedByTheTable()
    {
        var result = FacetRestriction.Validate(
            default,
            new FacetSet(length: 2),
            DatatypeVariety.Atomic,
            PrimitiveKind.DecimalType,
            XsdVersion.V11
        );

        await Assert.That(result.Error).IsEqualTo(FacetRestrictionError.NotApplicable);
        await Assert.That(result.Facet).IsEqualTo(FacetKind.Length);
    }

    [Test]
    public async Task Union_RejectsLengthAndWhiteSpace()
    {
        var length = FacetRestriction.Validate(
            default,
            new FacetSet(minLength: 1),
            DatatypeVariety.Union,
            PrimitiveKind.None,
            XsdVersion.V11
        );
        var space = FacetRestriction.Validate(
            default,
            new FacetSet(whiteSpace: WhitespaceMode.Collapse),
            DatatypeVariety.Union,
            PrimitiveKind.None,
            XsdVersion.V11
        );

        await Assert.That(length.Facet).IsEqualTo(FacetKind.MinLength);
        await Assert.That(space.Facet).IsEqualTo(FacetKind.WhiteSpace);
    }

    [Test]
    public async Task Apply_OverlaysTheStepAndUnionsTheFixedFlags()
    {
        var baseSet = new FacetSet(
            minLength: 2,
            whiteSpace: WhitespaceMode.Replace,
            fixedFacets: Fixed(FacetKind.MinLength)
        );
        var derived = new FacetSet(
            maxLength: 9,
            whiteSpace: WhitespaceMode.Collapse,
            fixedFacets: Fixed(FacetKind.MaxLength)
        );

        var effective = FacetRestriction.Apply(baseSet, derived);

        await Assert.That(effective.MinLength).IsEqualTo(2);
        await Assert.That(effective.MaxLength).IsEqualTo(9);
        await Assert.That(effective.Length).IsNull();
        await Assert.That(effective.WhiteSpace).IsEqualTo(WhitespaceMode.Collapse);
        await Assert.That(effective.FixedFacets.Contains(FacetKind.MinLength)).IsTrue();
        await Assert.That(effective.FixedFacets.Contains(FacetKind.MaxLength)).IsTrue();
        await Assert.That(effective.FixedFacets.Contains(FacetKind.Length)).IsFalse();
    }

    [Test]
    public async Task FacetSet_Specified_ListsTheFacetsOfTheStep()
    {
        var set = new FacetSet(length: 1, whiteSpace: WhitespaceMode.Preserve);

        await Assert.That(set.Specified.Contains(FacetKind.Length)).IsTrue();
        await Assert.That(set.Specified.Contains(FacetKind.WhiteSpace)).IsTrue();
        await Assert.That(set.Specified.Contains(FacetKind.MinLength)).IsFalse();
        await Assert.That(default(FacetSet).Specified.Contains(FacetKind.Length)).IsFalse();
        await Assert.That(() => new FacetSet(maxLength: -1)).Throws<ArgumentOutOfRangeException>();
    }
}
