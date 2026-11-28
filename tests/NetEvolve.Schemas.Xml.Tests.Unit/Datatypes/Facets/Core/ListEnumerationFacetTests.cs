namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core.Fakes;

public class ListEnumerationFacetTests
{
    private static AtomicDescriptor<int> Digits() => new("digits", XsdVersion.V11, new FakeDigitsMapping());

    [Test]
    [Arguments("7 12", true)]
    [Arguments("  7   12 ", true)]
    [Arguments("3", true)]
    [Arguments("12 7", false)]
    [Arguments("7", false)]
    [Arguments("7 12 3", false)]
    [Arguments("", false)]
    [Arguments("7 x", false)]
    public async Task Enumeration_ComparesItemWise(string lexical, bool expected)
    {
        var facet = new ListEnumerationFacet<int>(
            Digits(),
            [
                [7, 12],
                [3],
            ]
        );

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.Enumeration);
        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    public async Task EmptyEnumeration_AcceptsNothing()
    {
        var facet = new ListEnumerationFacet<int>(Digits(), []);

        await Assert.That(facet.IsSatisfiedBy("7".AsSpan())).IsFalse();
    }

    [Test]
    public async Task EmptySequence_MatchesTheEmptyList()
    {
        var facet = new ListEnumerationFacet<int>(
            Digits(),
            [
                [],
            ]
        );

        await Assert.That(facet.IsSatisfiedBy("  ".AsSpan())).IsTrue();
        await Assert.That(facet.IsSatisfiedBy("7".AsSpan())).IsFalse();
    }

    [Test]
    [Arguments("red green", true)]
    [Arguments(" red   green ", true)]
    [Arguments("blue", true)]
    [Arguments("green red", false)]
    [Arguments("red", false)]
    [Arguments("Red green", false)]
    [Arguments("red green blue", false)]
    [Arguments("", false)]
    public async Task LexicalEnumeration_ComparesItemsOrdinally(string lexical, bool expected)
    {
        var facet = new ListLexicalEnumerationFacet(["red green", "blue"]);

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.Enumeration);
        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    public async Task Constructor_RejectsNull()
    {
        await Assert
            .That(() =>
                new ListEnumerationFacet<int>(
                    null!,
                    [
                        [1],
                    ]
                )
            )
            .Throws<ArgumentNullException>();
        await Assert.That(() => new ListEnumerationFacet<int>(Digits(), null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new ListLexicalEnumerationFacet(null!)).Throws<ArgumentNullException>();
    }
}
