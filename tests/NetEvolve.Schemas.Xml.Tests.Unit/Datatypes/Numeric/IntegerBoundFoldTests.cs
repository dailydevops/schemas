namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Numeric;

using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;

public class IntegerBoundFoldTests
{
    private static IntegerBound Min(long bound) => new(FacetKind.MinInclusive, bound);

    private static IntegerBound Max(long bound) => new(FacetKind.MaxInclusive, bound);

    private static IValueFacet<XsdInteger>[] Fold(params IValueFacet<XsdInteger>[] facets) =>
        AtomicDescriptor<XsdInteger>.FoldBounds(facets);

    [Test]
    public async Task FoldBounds_KeepsTheLargestMinimumAndTheSmallestMaximum()
    {
        var innerMin = Min(-10);
        var innerMax = Max(10);

        var folded = Fold(Min(-1000), Max(1000), innerMin, innerMax);

        await Assert.That(folded.Length).IsEqualTo(2);
        await Assert.That(folded[0]).IsSameReferenceAs(innerMin);
        await Assert.That(folded[1]).IsSameReferenceAs(innerMax);
    }

    [Test]
    public async Task FoldBounds_KeepsOneOfTwoEqualBounds()
    {
        var first = Max(5);

        var folded = Fold(first, Max(5));

        await Assert.That(folded.Length).IsEqualTo(1);
        await Assert.That(folded[0]).IsSameReferenceAs(first);
    }

    [Test]
    public async Task FoldBounds_LeavesOtherFacetsAndTheirOrderUntouched()
    {
        var other = new BoundFacetStub();
        var inner = Max(1);

        var folded = Fold(Max(100), other, inner);

        await Assert.That(folded.Length).IsEqualTo(2);
        await Assert.That(folded[0]).IsSameReferenceAs(other);
        await Assert.That(folded[1]).IsSameReferenceAs(inner);
    }

    [Test]
    [Arguments("int", "-2147483649", FacetKind.MinInclusive)]
    [Arguments("int", "2147483648", FacetKind.MaxInclusive)]
    [Arguments("byte", "-99999999999999999999999999", FacetKind.MinInclusive)]
    [Arguments("byte", "99999999999999999999999999", FacetKind.MaxInclusive)]
    [Arguments("unsignedByte", "-1", FacetKind.MinInclusive)]
    [Arguments("unsignedByte", "256", FacetKind.MaxInclusive)]
    public async Task Validate_ReportsTheSameFailedFacetAfterFolding(string name, string lexical, FacetKind expected)
    {
        var result = BuiltInDatatypes.For(XsdVersion.V11).Get(name).Validate(lexical);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailedFacet).IsEqualTo(expected);
    }

    private sealed class BoundFacetStub : IValueFacet<XsdInteger>
    {
        public FacetKind Kind => FacetKind.MinExclusive;

        public bool IsSatisfiedBy(in XsdInteger value) => true;
    }
}
