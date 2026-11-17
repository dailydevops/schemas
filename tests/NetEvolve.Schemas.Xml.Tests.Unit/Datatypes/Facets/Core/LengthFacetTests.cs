namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Strings;

public class LengthFacetTests
{
    private const string Supplementary = "\U0001D11E"; // one code point, two UTF-16 code units

    [Test]
    [Arguments("abc", 3, true)]
    [Arguments("abc", 2, false)]
    [Arguments("ab\U0001D11E", 3, true)]
    [Arguments("ab\U0001D11E", 4, false)]
    [Arguments("", 0, true)]
    public async Task Length_OnStrings_CountsCodePoints(string lexical, int length, bool expected) =>
        await Assert
            .That(LengthFacet.Length(length, LengthUnit.Characters).IsSatisfiedBy(lexical.AsSpan()))
            .IsEqualTo(expected);

    [Test]
    public async Task SupplementaryCharacter_CountsOnce()
    {
        var max1 = LengthFacet.MaxLength(1, LengthUnit.Characters);
        var min2 = LengthFacet.MinLength(2, LengthUnit.Characters);

        await Assert.That(max1.IsSatisfiedBy(Supplementary.AsSpan())).IsTrue();
        await Assert.That(min2.IsSatisfiedBy(Supplementary.AsSpan())).IsFalse();
        await Assert.That(min2.IsSatisfiedBy((Supplementary + Supplementary).AsSpan())).IsTrue();
    }

    [Test]
    public async Task LoneSurrogate_CountsAsOneCharacter() =>
        await Assert
            .That(LengthFacet.Length(2, LengthUnit.Characters).IsSatisfiedBy("\uD834\uD834".AsSpan()))
            .IsTrue();

    [Test]
    [Arguments(2, "a b", true)]
    [Arguments(3, "a b", false)]
    [Arguments(0, "", true)]
    [Arguments(1, "a", true)]
    public async Task Length_OnLists_CountsItems(int length, string lexical, bool expected) =>
        await Assert
            .That(LengthFacet.Length(length, LengthUnit.Items).IsSatisfiedBy(lexical.AsSpan()))
            .IsEqualTo(expected);

    [Test]
    public async Task MinAndMaxLength_OnLists_CountItemsNotCharacters()
    {
        var min = LengthFacet.MinLength(2, LengthUnit.Items);
        var max = LengthFacet.MaxLength(2, LengthUnit.Items);

        await Assert.That(min.IsSatisfiedBy("longitem".AsSpan())).IsFalse();
        await Assert.That(min.IsSatisfiedBy("a b c".AsSpan())).IsTrue();
        await Assert.That(max.IsSatisfiedBy("a b c".AsSpan())).IsFalse();
        await Assert.That(max.IsSatisfiedBy("a b".AsSpan())).IsTrue();
    }

    [Test]
    public async Task Facets_ExposeKindValueAndUnit()
    {
        var facet = LengthFacet.MinLength(4, LengthUnit.Items);

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.MinLength);
        await Assert.That(facet.Value).IsEqualTo(4);
        await Assert.That(facet.Unit).IsEqualTo(LengthUnit.Items);
        await Assert.That(LengthFacet.Length(1, LengthUnit.Characters).Kind).IsEqualTo(FacetKind.Length);
        await Assert.That(LengthFacet.MaxLength(1, LengthUnit.Characters).Kind).IsEqualTo(FacetKind.MaxLength);
    }

    [Test]
    public async Task NegativeValue_IsRejected() =>
        await Assert
            .That(() => LengthFacet.Length(-1, LengthUnit.Characters))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task OnAtomicDescriptor_CountsCodePointsAfterWhitespaceNormalization()
    {
        var restricted = new AtomicDescriptor<XsdString>(
            "short",
            (AtomicDescriptor<XsdString>)BuiltInDatatypes.For(XsdVersion.V11).Get("token"),
            null,
            [LengthFacet.MaxLength(3, LengthUnit.Characters)]
        );

        await Assert.That(restricted.Validate("  ab\U0001D11E  ".AsSpan()).IsValid).IsTrue();
        var result = restricted.Validate("abcd".AsSpan());
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.MaxLength);
    }
}
