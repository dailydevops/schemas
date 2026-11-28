namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Core;

using System;
using System.Globalization;
using System.Linq;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Core.Fakes;

public class EnumerationFacetTests
{
    private static AtomicDescriptor<int> TwoForms() => new("twoForms", XsdVersion.V11, new FakeTwoFormsMapping());

    [Test]
    [Arguments("one")]
    [Arguments("uno")]
    public async Task Enumeration_MatchesByValue_NotByLexicalForm(string lexical)
    {
        var type = TwoForms();
        var facet = new EnumerationFacet<int>(type, [type.Parse("one".AsSpan()).Value]);

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.Enumeration);
        await Assert.That(facet.IsSatisfiedBy(type.Parse(lexical.AsSpan()).Value)).IsTrue();
        await Assert.That(facet.IsSatisfiedBy(type.Parse("dos".AsSpan()).Value)).IsFalse();
    }

    [Test]
    public async Task OnDescriptor_AcceptsEveryFormOfAnEnumeratedValue()
    {
        var type = TwoForms();
        var restricted = new AtomicDescriptor<int>(
            "onlyOne",
            type,
            null,
            [new EnumerationFacet<int>(type, [type.Parse("uno".AsSpan()).Value])]
        );

        await Assert.That(restricted.Validate("one".AsSpan()).IsValid).IsTrue();
        await Assert.That(restricted.Validate(" uno ".AsSpan()).IsValid).IsTrue();
        var result = restricted.Validate("two".AsSpan());
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.Enumeration);
    }

    [Test]
    public async Task OrderedType_MatchesByCompareEqual()
    {
        var digits = new AtomicDescriptor<int>("digits", XsdVersion.V11, new FakeDigitsMapping());
        var facet = new EnumerationFacet<int>(digits, [7, 12]);

        await Assert.That(facet.IsSatisfiedBy(12)).IsTrue();
        await Assert.That(facet.IsSatisfiedBy(8)).IsFalse();
    }

    [Test]
    public async Task EmptyEnumeration_AcceptsNothing()
    {
        var type = TwoForms();

        await Assert.That(new EnumerationFacet<int>(type, []).IsSatisfiedBy(1)).IsFalse();
    }

    [Test]
    public async Task Constructor_RejectsNull()
    {
        var type = TwoForms();

        await Assert.That(() => new EnumerationFacet<int>(null!, [1])).Throws<ArgumentNullException>();
        await Assert.That(() => new EnumerationFacet<int>(type, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new LexicalEnumerationFacet(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task LexicalEnumeration_ComparesNormalizedStringsOrdinally()
    {
        var facet = new LexicalEnumerationFacet(["red", "green"]);

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.Enumeration);
        await Assert.That(facet.IsSatisfiedBy("green".AsSpan())).IsTrue();
        await Assert.That(facet.IsSatisfiedBy("Green".AsSpan())).IsFalse();
        await Assert.That(facet.IsSatisfiedBy("".AsSpan())).IsFalse();
    }

    [Test]
    public async Task LexicalEnumeration_LargeList_MatchesEveryValueAndNothingElse()
    {
        var values = Enumerable.Range(0, 1000).Select(i => "code" + i.ToString(CultureInfo.InvariantCulture)).ToArray();
        var facet = new LexicalEnumerationFacet(values);

        foreach (var value in values)
        {
            await Assert.That(facet.IsSatisfiedBy(value.AsSpan())).IsTrue();
        }

        await Assert.That(facet.IsSatisfiedBy("code1000".AsSpan())).IsFalse();
        await Assert.That(facet.IsSatisfiedBy("CODE7".AsSpan())).IsFalse();
        await Assert.That(facet.IsSatisfiedBy("code".AsSpan())).IsFalse();
        await Assert.That(facet.IsSatisfiedBy("code70 ".AsSpan())).IsFalse();
        await Assert.That(facet.IsSatisfiedBy("".AsSpan())).IsFalse();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task LexicalEnumeration_LargeList_HandlesTheEmptyString(bool listed)
    {
        var values = Enumerable.Range(0, 20).Select(i => "v" + i.ToString(CultureInfo.InvariantCulture)).ToList();
        if (listed)
        {
            values.Add("");
        }

        var facet = new LexicalEnumerationFacet(values);

        await Assert.That(facet.IsSatisfiedBy("".AsSpan())).IsEqualTo(listed);
        await Assert.That(facet.IsSatisfiedBy("v19".AsSpan())).IsTrue();
    }

    [Test]
    public async Task LexicalEnumeration_LargeList_AllowsDuplicateValues()
    {
        var values = Enumerable
            .Range(0, 20)
            .Select(i => "v" + (i % 5).ToString(CultureInfo.InvariantCulture))
            .ToArray();
        var facet = new LexicalEnumerationFacet(values);

        await Assert.That(facet.IsSatisfiedBy("v4".AsSpan())).IsTrue();
        await Assert.That(facet.IsSatisfiedBy("v5".AsSpan())).IsFalse();
    }
}
