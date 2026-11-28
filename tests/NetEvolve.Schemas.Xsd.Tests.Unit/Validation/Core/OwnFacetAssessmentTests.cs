namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Linq;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Pattern;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Definitions;

public class OwnFacetAssessmentTests
{
    private static PatternFacet Pattern(params string[] patterns)
    {
        _ = PatternFacet.TryCreate(patterns, XsdVersion.V11, null, out var facet, out _);
        return facet!;
    }

    [Test]
    [Arguments("abc", true, FacetKind.None)]
    [Arguments("abcd", false, FacetKind.MaxLength)]
    [Arguments("a", false, FacetKind.MinLength)]
    public async Task Restriction_EvaluatesLengthFacets(string lexical, bool valid, FacetKind failed)
    {
        var fixture = new FacetFixture();
        var type = fixture.Base(
            "string",
            fixture.Facet("string", FacetKind.MaxLength, "3"),
            fixture.Facet("string", FacetKind.MinLength, "2")
        );

        var result = fixture.Assessor.Assess(type, lexical, out _);

        await Assert.That(result.IsValid).IsEqualTo(valid);
        await Assert.That(result.FailedFacet).IsEqualTo(failed);
    }

    [Test]
    public async Task Restriction_EvaluatesTheLengthFacet()
    {
        var fixture = new FacetFixture();
        var type = fixture.Base("string", fixture.Facet("string", FacetKind.Length, "2"));

        await Assert.That(fixture.Assessor.Assess(type, "ab", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "abc", out _).FailedFacet).IsEqualTo(FacetKind.Length);
    }

    [Test]
    public async Task Restriction_EvaluatesPatternAndEnumeration()
    {
        var fixture = new FacetFixture();
        var pattern = fixture.Base("string", Pattern("[a-c]+"));
        var enumeration = fixture.Base("string", new LexicalEnumerationFacet(["red", "green"]));

        await Assert.That(fixture.Assessor.Assess(pattern, "abc", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(pattern, "abd", out _).FailedFacet).IsEqualTo(FacetKind.Pattern);
        await Assert.That(fixture.Assessor.Assess(enumeration, "red", out _).IsValid).IsTrue();
        await Assert
            .That(fixture.Assessor.Assess(enumeration, "blue", out _).FailedFacet)
            .IsEqualTo(FacetKind.Enumeration);
    }

    [Test]
    [Arguments(FacetKind.MaxInclusive, "10", "11", true)]
    [Arguments(FacetKind.MaxInclusive, "10", "10", false)]
    [Arguments(FacetKind.MaxExclusive, "10", "10", true)]
    [Arguments(FacetKind.MinInclusive, "10", "9", true)]
    [Arguments(FacetKind.MinExclusive, "10", "10", true)]
    public async Task Restriction_EvaluatesBounds(FacetKind kind, string bound, string lexical, bool fails)
    {
        var fixture = new FacetFixture();
        var type = fixture.Base("integer", fixture.Facet("integer", kind, bound));

        var result = fixture.Assessor.Assess(type, lexical, out _);

        await Assert.That(result.IsValid).IsEqualTo(!fails);
        await Assert.That(result.FailedFacet).IsEqualTo(fails ? kind : FacetKind.None);
    }

    [Test]
    public async Task Restriction_EvaluatesDigitFacets()
    {
        var fixture = new FacetFixture();
        var type = fixture.Base(
            "decimal",
            fixture.Facet("decimal", FacetKind.TotalDigits, "4"),
            fixture.Facet("decimal", FacetKind.FractionDigits, "1")
        );

        await Assert.That(fixture.Assessor.Assess(type, "123.4", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "12345", out _).FailedFacet).IsEqualTo(FacetKind.TotalDigits);
        await Assert.That(fixture.Assessor.Assess(type, "1.25", out _).FailedFacet).IsEqualTo(FacetKind.FractionDigits);
    }

    [Test]
    public async Task RestrictionOfARestriction_EvaluatesTheFacetsOfEveryStep()
    {
        var fixture = new FacetFixture();
        var type = fixture.Derive(
            fixture.Base("integer", fixture.Facet("integer", FacetKind.MaxInclusive, "100")),
            fixture.Facet("integer", FacetKind.MinInclusive, "10")
        );

        await Assert.That(fixture.Assessor.Assess(type, "50", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "5", out _).FailedFacet).IsEqualTo(FacetKind.MinInclusive);
        await Assert.That(fixture.Assessor.Assess(type, "500", out _).FailedFacet).IsEqualTo(FacetKind.MaxInclusive);
        await Assert.That(fixture.Assessor.Assess(type, "x", out _).FailedFacet).IsEqualTo(FacetKind.None);
    }

    [Test]
    public async Task Restriction_AppliesTheOwnWhiteSpaceMode()
    {
        var fixture = new FacetFixture();
        var type = fixture.Base(
            "string",
            fixture.Facet("string", FacetKind.WhiteSpace, "collapse"),
            fixture.Facet("string", FacetKind.MaxLength, "3")
        );

        await Assert.That(fixture.Assessor.Assess(type, "  a \t b ", out _).FailedFacet).IsEqualTo(FacetKind.None);
        await Assert.That(fixture.Assessor.Assess(type, "ab c d", out _).FailedFacet).IsEqualTo(FacetKind.MaxLength);
    }

    [Test]
    public async Task List_EvaluatesItsOwnFacetsOverTheItems()
    {
        var fixture = new FacetFixture();
        var type = fixture.Base("list:integer", fixture.Facet("list:integer", FacetKind.MaxLength, "2"));
        var pattern = fixture.Derive(fixture.Base("list:integer"), Pattern("[0-9 ]+"));

        await Assert.That(fixture.Assessor.Assess(type, " 1  2 ", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "1 2 3", out _).FailedFacet).IsEqualTo(FacetKind.MaxLength);
        await Assert.That(fixture.Assessor.Assess(type, "1 x", out _).Error).IsEqualTo(DatatypeError.InvalidListItem);
        await Assert.That(fixture.Assessor.Assess(pattern, "\t1\n2", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(pattern, "1 -2", out _).FailedFacet).IsEqualTo(FacetKind.Pattern);
    }

    [Test]
    public async Task Union_EvaluatesItsOwnFacets()
    {
        var fixture = new FacetFixture();
        var type = fixture.Derive(fixture.Base("union:integer+boolean"), Pattern("[0-9]+|true"));

        var valid = fixture.Assessor.Assess(type, "12", out var member);
        var failed = fixture.Assessor.Assess(type, "false", out _);

        await Assert.That(valid.IsValid).IsTrue();
        await Assert.That(member).IsEqualTo(0);
        await Assert.That(failed.FailedFacet).IsEqualTo(FacetKind.Pattern);
    }

    [Test]
    public async Task Restriction_EvaluatesAnEnumerationInTheValueSpace()
    {
        var fixture = new FacetFixture();
        var facets = FacetFactory.Create(
            fixture.Wrapped("integer"),
            [FacetFixture.Decl(FacetKind.Enumeration, "1"), FacetFixture.Decl(FacetKind.Enumeration, "2")],
            XsdVersion.V11
        );
        var type = fixture.Derive(fixture.Wrapped("integer"), [.. facets]);

        await Assert.That(fixture.Assessor.Assess(type, "+01", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "3", out _).FailedFacet).IsEqualTo(FacetKind.Enumeration);
    }

    [Test]
    [Arguments("integer", "1 2", "+01 02", true)]
    [Arguments("integer", "1 2", "2 1", false)]
    [Arguments("integer", "1 2", "1", false)]
    [Arguments("integer", "1 2", "1 2 3", false)]
    [Arguments("integer", "1 2", "1  2", true)]
    [Arguments("string", "a b", "a b", true)]
    [Arguments("string", "a b", "b a", false)]
    [Arguments("anyURI", "urn:a urn:b", "urn:a urn:b", true)]
    [Arguments("anyURI", "urn:a urn:b", "urn:a urn:c", false)]
    [Arguments("boolean", "true false", "1 0", true)]
    public async Task List_EvaluatesAnEnumerationItemByItemInTheValueSpace(
        string item,
        string listed,
        string lexical,
        bool valid
    )
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("list:" + item);
        var facets = FacetFactory.Create(
            baseType,
            [FacetFixture.Decl(FacetKind.Enumeration, listed), FacetFixture.Decl(FacetKind.Enumeration, "9 9")],
            XsdVersion.V11
        );
        var type = fixture.Derive(baseType, [.. facets]);

        var result = fixture.Assessor.Assess(type, lexical, out _);

        await Assert.That(result.IsValid).IsEqualTo(valid);
        if (!valid)
        {
            await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.Enumeration);
        }
    }

    [Test]
    [Arguments("NMTOKENS", false)]
    [Arguments("IDREFS", false)]
    [Arguments("ENTITIES", false)]
    [Arguments("NMTOKENS", true)]
    public async Task List_OfNames_EvaluatesAnEnumerationInTheTextValueSpace(string name, bool wrapped)
    {
        var fixture = new FacetFixture();
        var baseType = wrapped ? fixture.Wrapped(name) : fixture.BuiltIn(name);
        var facets = FacetFactory.Create(baseType, [FacetFixture.Decl(FacetKind.Enumeration, "a b")], XsdVersion.V11);
        var type = fixture.Derive(fixture.Wrapped(name), [.. facets]);

        await Assert.That(facets.Any(f => f.Kind == FacetKind.Enumeration)).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "a  b", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "a", out _).FailedFacet).IsEqualTo(FacetKind.Enumeration);
        await Assert.That(fixture.Assessor.Assess(type, "a b c", out _).FailedFacet).IsEqualTo(FacetKind.Enumeration);
    }

    [Test]
    public async Task List_OfIntegers_LeavesOutAListedValueWithAnItemThatDoesNotParse()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("list:integer");

        var none = FacetFactory.Create(baseType, [FacetFixture.Decl(FacetKind.Enumeration, "1 x")], XsdVersion.V11);
        var some = FacetFactory.Create(
            baseType,
            [FacetFixture.Decl(FacetKind.Enumeration, "1 x"), FacetFixture.Decl(FacetKind.Enumeration, "2")],
            XsdVersion.V11
        );
        var type = fixture.Derive(baseType, [.. some]);

        await Assert.That(none).IsEmpty();
        await Assert.That(fixture.Assessor.Assess(type, "2", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(type, "1", out _).FailedFacet).IsEqualTo(FacetKind.Enumeration);
    }

    [Test]
    public async Task List_OfAnUnsupportedItemType_CarriesNoEnumeration()
    {
        var fixture = new FacetFixture();

        var facets = FacetFactory.Create(
            fixture.Base("list:QName"),
            [FacetFixture.Decl(FacetKind.Enumeration, "a b")],
            XsdVersion.V11
        );

        await Assert.That(facets).IsEmpty();
    }

    [Test]
    public async Task Restriction_OfAnyUri_EvaluatesLexicalFacets()
    {
        var fixture = new FacetFixture();
        var length = fixture.Base("anyURI", fixture.Facet("anyURI", FacetKind.MaxLength, "5"));
        var facets = FacetFactory.Create(
            fixture.Wrapped("anyURI"),
            [FacetFixture.Decl(FacetKind.Enumeration, "urn:a"), FacetFixture.Decl(FacetKind.Enumeration, "urn:b")],
            XsdVersion.V11
        );
        var enumeration = fixture.Derive(fixture.Wrapped("anyURI"), [.. facets]);

        await Assert.That(fixture.Assessor.Assess(length, " urn:a ", out _).IsValid).IsTrue();
        await Assert.That(fixture.Assessor.Assess(length, "urn:abc", out _).FailedFacet).IsEqualTo(FacetKind.MaxLength);
        await Assert.That(fixture.Assessor.Assess(enumeration, "urn:b", out _).IsValid).IsTrue();
        await Assert
            .That(fixture.Assessor.Assess(enumeration, "urn:c", out _).FailedFacet)
            .IsEqualTo(FacetKind.Enumeration);
    }

    [Test]
    [Arguments("string", "a")]
    [Arguments("decimal", "1")]
    [Arguments("integer", "1")]
    [Arguments("dateTime", "2020-01-01T00:00:00")]
    [Arguments("duration", "P1D")]
    [Arguments("hexBinary", "0A")]
    [Arguments("double", "1")]
    [Arguments("float", "1")]
    [Arguments("boolean", "true")]
    [Arguments("QName", "a")]
    public async Task Restriction_OfEveryFamily_EvaluatesAPattern(string type, string lexical)
    {
        var fixture = new FacetFixture();
        var restricted = fixture.Base(type, Pattern("ZZZ"));

        var result = fixture.Assessor.Assess(restricted, lexical, out _);

        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.Pattern);
    }

    [Test]
    public async Task List_CollapsesALongLexicalFormBeforeItsPattern()
    {
        var fixture = new FacetFixture();
        var type = fixture.Derive(fixture.Base("list:integer"), Pattern("[0-9]+( [0-9]+)*"));
        var longForm = string.Join("  ", Enumerable.Repeat("12", 200));

        await Assert.That(fixture.Assessor.Assess(type, longForm, out _).IsValid).IsTrue();
    }

    [Test]
    public async Task Restriction_OfQName_IgnoresTheLengthFacets()
    {
        var fixture = new FacetFixture();
        var type = fixture.Base("QName", LengthFacet.MaxLength(1, LengthUnit.Characters));

        await Assert.That(fixture.Assessor.Assess(type, "xs:string", out _).IsValid).IsTrue();
    }

    [Test]
    public async Task Restriction_OfAnUnknownDatatype_IsStillAccepted()
    {
        var fixture = new FacetFixture();
        var wrapped = SimpleTypeDefinition.FromDatatype(
            new XPathQName(FacetFixture.XsNamespace, "custom"),
            FacetFixture.Where,
            new FakeDatatype("custom", DatatypeVariety.Atomic)
        );
        var type = SimpleTypeDefinition.Restrict(
            default,
            FacetFixture.Where,
            XsdDerivationMethod.None,
            wrapped,
            [LengthFacet.MaxLength(1, LengthUnit.Characters)],
            fixture.Reporter
        )!;

        await Assert.That(fixture.Assessor.Assess(type, "anything", out _).IsValid).IsTrue();
    }

    [Test]
    public async Task ValidatingAValue_DoesNotAllocate()
    {
        var fixture = new FacetFixture();
        var type = fixture.Base(
            "integer",
            fixture.Facet("integer", FacetKind.MaxInclusive, "100"),
            fixture.Facet("integer", FacetKind.MinInclusive, "10")
        );
        var text = "42".AsSpan();
        _ = fixture.Assessor.Assess(type, text, out _);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            _ = fixture.Assessor.Assess(type, text, out _);
        }

        await Assert.That(GC.GetAllocatedBytesForCurrentThread() - before).IsEqualTo(0);
    }
}
