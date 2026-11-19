namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A facet that the base type fixes may not change; the type system fixes some facets of the built-in types itself.</summary>
public class FixedFacetTests
{
    [Test]
    [Arguments("string", FacetKind.Length, "5", "5", "3", "length-valid-restriction")]
    [Arguments("string", FacetKind.MinLength, "2", "2", "5", "minLength-valid-restriction")]
    [Arguments("string", FacetKind.MaxLength, "10", "10", "5", "maxLength-valid-restriction")]
    [Arguments("hexBinary", FacetKind.MaxLength, "10", "10", "5", "maxLength-valid-restriction")]
    [Arguments("list:int", FacetKind.MinLength, "2", "2", "5", "minLength-valid-restriction")]
    [Arguments("decimal", FacetKind.MinInclusive, "0", "0", "5", "minInclusive-valid-restriction")]
    [Arguments("decimal", FacetKind.MaxInclusive, "100", "100", "50", "maxInclusive-valid-restriction")]
    [Arguments("decimal", FacetKind.MinExclusive, "0", "0", "1", "minExclusive-valid-restriction")]
    [Arguments("decimal", FacetKind.MaxExclusive, "100", "100", "99", "maxExclusive-valid-restriction")]
    [Arguments("decimal", FacetKind.TotalDigits, "5", "5", "3", "totalDigits-valid-restriction")]
    [Arguments("decimal", FacetKind.FractionDigits, "3", "3", "2", "fractionDigits-valid-restriction")]
    [Arguments("float", FacetKind.MaxInclusive, "10", "10", "5", "maxInclusive-valid-restriction")]
    [Arguments(
        "dateTime",
        FacetKind.MaxInclusive,
        "2020-01-01T00:00:00Z",
        "2020-01-01T00:00:00Z",
        "2019-01-01T00:00:00Z",
        "maxInclusive-valid-restriction"
    )]
    public async Task ChangingAFixedFacet_IsALocatedSchemaError_RepeatingItIsNot(
        string type,
        FacetKind kind,
        string baseValue,
        string same,
        string other,
        string rule
    )
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base(type, fixture.Facet(type, kind, baseValue));
        var fixedFacets = FacetFixture.Fixed(kind);

        var repeated = fixture.Check(baseType, fixedFacets, FacetFixture.Decl(kind, same));
        await Assert.That(repeated.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);

        var changed = fixture.Check(baseType, fixedFacets, FacetFixture.Decl(kind, other, line: 21));
        await Assert.That(changed.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf(rule));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(21);
    }

    [Test]
    public async Task TheSameChangeWithoutTheFixedFlag_IsAValidNarrowing()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("string", fixture.Facet("string", FacetKind.MaxLength, "10"));

        var result = fixture.Check(baseType, FacetFixture.Decl(FacetKind.MaxLength, "5"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("boolean")]
    [Arguments("decimal")]
    [Arguments("int")]
    [Arguments("dateTime")]
    [Arguments("anyURI")]
    [Arguments("hexBinary")]
    [Arguments("list:int")]
    public async Task WhiteSpaceOfEveryTypeButTheStringFamily_IsFixedAtCollapse(string type)
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base(type);

        var repeated = fixture.Check(baseType, FacetFixture.Decl(FacetKind.WhiteSpace, "collapse"));
        var changed = fixture.Check(baseType, FacetFixture.Decl(FacetKind.WhiteSpace, "preserve", line: 22));

        await Assert.That(repeated.IsValid).IsTrue();
        await Assert.That(changed.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("whiteSpace-valid-restriction"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(22);
    }

    [Test]
    [Arguments("string")]
    [Arguments("normalizedString")]
    [Arguments("token")]
    public async Task WhiteSpaceOfTheStringFamily_IsNotFixed(string type)
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base(type), FacetFixture.Decl(FacetKind.WhiteSpace, "collapse"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.WhiteSpace)).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("integer")]
    [Arguments("int")]
    [Arguments("unsignedByte")]
    [Arguments("positiveInteger")]
    public async Task FractionDigitsOfTheIntegerFamily_IsFixedAtZero(string type)
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base(type);

        var zero = fixture.Check(baseType, FacetFixture.Decl(FacetKind.FractionDigits, "0"));
        var one = fixture.Check(baseType, FacetFixture.Decl(FacetKind.FractionDigits, "1", line: 23));

        await Assert.That(zero.IsValid).IsTrue();
        await Assert.That(zero.Fixed.Contains(FacetKind.FractionDigits)).IsTrue();
        await Assert.That(one.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("fractionDigits-valid-restriction"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(23);
    }

    [Test]
    public async Task FractionDigitsOfDecimal_IsNotFixed()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base("decimal"), FacetFixture.Decl(FacetKind.FractionDigits, "2"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.FractionDigits)).IsFalse();
    }

    [Test]
    public async Task TheResult_UnitesTheFixedFacetsOfTheBaseTheTypeSystemAndTheStep()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("integer");

        var result = fixture.Check(
            baseType,
            FacetFixture.Fixed(FacetKind.MinInclusive),
            FacetFixture.Decl(FacetKind.TotalDigits, "5", isFixed: true),
            FacetFixture.Decl(FacetKind.MaxInclusive, "100")
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.MinInclusive)).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.WhiteSpace)).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.FractionDigits)).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.TotalDigits)).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.MaxInclusive)).IsFalse();
    }

    [Test]
    public async Task TheFixedFacetsOfAResult_CarryIntoTheNextRestrictionStep()
    {
        var fixture = new FacetFixture();
        var first = fixture.Check(fixture.Base("string"), FacetFixture.Decl(FacetKind.MaxLength, "10", isFixed: true));
        var second = fixture.Base("string", fixture.Facet("string", FacetKind.MaxLength, "10"));

        var changed = fixture.Check(second, first.Fixed, FacetFixture.Decl(FacetKind.MaxLength, "5", line: 24));

        await Assert.That(first.IsValid).IsTrue();
        await Assert.That(changed.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("maxLength-valid-restriction"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(24);
    }

    [Test]
    public async Task AFixedFlagOnAFacetThatIsNotApplicable_IsNotRecorded()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base("boolean"), FacetFixture.Decl(FacetKind.MaxLength, "5", isFixed: true));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Fixed.Contains(FacetKind.MaxLength)).IsFalse();
    }
}
