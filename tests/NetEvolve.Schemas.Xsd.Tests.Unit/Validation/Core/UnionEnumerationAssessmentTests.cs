namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Linq;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

public class UnionEnumerationAssessmentTests
{
    private static SimpleTypeDefinition Restrict(
        FacetFixture fixture,
        SimpleTypeDefinition union,
        params string[] listed
    )
    {
        var facets = FacetFactory.Create(
            union,
            [.. listed.Select(v => FacetFixture.Decl(FacetKind.Enumeration, v))],
            XsdVersion.V11
        );
        return fixture.Derive(union, [.. facets]);
    }

    private static SimpleTypeDefinition Union(FacetFixture fixture, params IXsdComponent[] members) =>
        SimpleTypeDefinition.Union(
            default,
            FacetFixture.Where,
            XsdDerivationMethod.None,
            members,
            [],
            fixture.Reporter
        )!;

    [Test]
    [Arguments("1", true)]
    [Arguments("+01", true)]
    [Arguments("2", true)]
    [Arguments("3", false)]
    [Arguments("1.0", false)]
    [Arguments("x", false)]
    public async Task Union_AcceptsAValueOfTheSameMemberValueSpace(string lexical, bool valid)
    {
        var fixture = new FacetFixture();
        var type = Restrict(fixture, fixture.Base("union:integer+decimal"), "1", "2");

        var result = fixture.Assessor.Assess(type, lexical, out _);

        await Assert.That(result.IsValid).IsEqualTo(valid);
        if (!valid && lexical != "x")
        {
            await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.Enumeration);
        }
    }

    [Test]
    public async Task Union_OfNoAcceptingMember_FailsBeforeTheFacet()
    {
        var fixture = new FacetFixture();
        var type = Restrict(fixture, fixture.Base("union:integer+boolean"), "1");

        await Assert.That(fixture.Assessor.Assess(type, "x", out _).FailedFacet).IsNotEqualTo(FacetKind.Enumeration);
    }

    [Test]
    public async Task Union_WithADerivedMember_ComparesInTheValueSpaceOfThatMember()
    {
        var fixture = new FacetFixture();
        var small = fixture.Base("integer", fixture.Facet("integer", FacetKind.MaxInclusive, "5"));
        var type = Restrict(fixture, Union(fixture, small, fixture.Wrapped("decimal")), "1", "9");

        // 9 exceeds the derived integer member, so decimal accepts it and equality is decimal equality.
        await Assert.That(fixture.Assessor.Assess(type, "01", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "9", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "9.0", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "2", out _).FailedFacet).IsEqualTo(FacetKind.Enumeration);
    }

    [Test]
    public async Task Union_WithAListMember_ComparesTheLexicalForm()
    {
        var fixture = new FacetFixture();
        var type = Restrict(fixture, Union(fixture, fixture.Base("list:integer"), fixture.Wrapped("boolean")), "1 2");

        await Assert.That(fixture.Assessor.Assess(type, " 1  2 ", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "2 1", out _).FailedFacet).IsEqualTo(FacetKind.Enumeration);
    }

    [Test]
    public async Task Union_WithAnAnyUriMember_ComparesTheLexicalForm()
    {
        var fixture = new FacetFixture();
        var uri = fixture.Base("anyURI", fixture.Facet("anyURI", FacetKind.MaxLength, "5"));
        var type = Restrict(fixture, Union(fixture, uri, fixture.Wrapped("integer")), "urn:a");

        await Assert.That(fixture.Assessor.Assess(type, "urn:a", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "urn:b", out _).FailedFacet).IsEqualTo(FacetKind.Enumeration);
        await Assert.That(fixture.Assessor.Assess(type, "urn:abc", out _).IsValid).IsFalse();
    }

    [Test]
    public async Task Union_OfAUnionOfBuiltInTypes_CarriesTheEnumeration()
    {
        var fixture = new FacetFixture();
        var type = Restrict(
            fixture,
            Union(fixture, fixture.Base("union:integer+boolean"), fixture.Wrapped("string")),
            "1"
        );

        await Assert.That(fixture.Assessor.Assess(type, "1", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "2", out _).FailedFacet).IsEqualTo(FacetKind.Enumeration);
    }

    [Test]
    public async Task Union_WithoutAnyListedValueAcceptedByAMember_CarriesAFacetThatAcceptsNothing()
    {
        var fixture = new FacetFixture();
        var union = fixture.Base("union:integer+boolean");

        var facets = FacetFactory.Create(union, [FacetFixture.Decl(FacetKind.Enumeration, "x")], XsdVersion.V11);

        await Assert.That(facets).Count().IsEqualTo(1);
    }
}
