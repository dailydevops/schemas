namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// One row per facet and type family: the base type carries the facet (or the built-in type implies it), a narrowing value passes,
/// and a widening value is a schema error that carries the rule of the facet and the location of the facet element.
/// </summary>
public class FacetRestrictionMatrixTests
{
    // Length facets of the string-like families: characters, binary octets, anyURI, QName, and list items.
    [Test]
    [Arguments("string", FacetKind.MaxLength, "10", "5", "20", "maxLength-valid-restriction")]
    [Arguments("string", FacetKind.MinLength, "2", "5", "1", "minLength-valid-restriction")]
    [Arguments("string", FacetKind.Length, "5", "5", "6", "length-valid-restriction")]
    [Arguments("anyURI", FacetKind.MaxLength, "10", "5", "20", "maxLength-valid-restriction")]
    [Arguments("anyURI", FacetKind.MinLength, "2", "5", "1", "minLength-valid-restriction")]
    [Arguments("QName", FacetKind.MaxLength, "10", "5", "20", "maxLength-valid-restriction")]
    [Arguments("QName", FacetKind.Length, "5", "5", "6", "length-valid-restriction")]
    [Arguments("hexBinary", FacetKind.MaxLength, "10", "5", "20", "maxLength-valid-restriction")]
    [Arguments("hexBinary", FacetKind.MinLength, "2", "5", "1", "minLength-valid-restriction")]
    [Arguments("hexBinary", FacetKind.Length, "4", "4", "6", "length-valid-restriction")]
    [Arguments("base64Binary", FacetKind.MaxLength, "10", "5", "20", "maxLength-valid-restriction")]
    [Arguments("base64Binary", FacetKind.MinLength, "2", "5", "1", "minLength-valid-restriction")]
    [Arguments("list:int", FacetKind.MaxLength, "10", "5", "20", "maxLength-valid-restriction")]
    [Arguments("list:int", FacetKind.MinLength, "2", "5", "1", "minLength-valid-restriction")]
    [Arguments("list:int", FacetKind.Length, "3", "3", "4", "length-valid-restriction")]
    // whiteSpace: implied by the built-in type, so the base carries no facet.
    [Arguments("normalizedString", FacetKind.WhiteSpace, "", "collapse", "preserve", "whiteSpace-valid-restriction")]
    [Arguments("token", FacetKind.WhiteSpace, "", "collapse", "replace", "whiteSpace-valid-restriction")]
    [Arguments("list:int", FacetKind.WhiteSpace, "", "collapse", "preserve", "whiteSpace-valid-restriction")]
    // decimal: the four bounds and the two digit facets.
    [Arguments("decimal", FacetKind.MinInclusive, "0", "5", "-5", "minInclusive-valid-restriction")]
    [Arguments("decimal", FacetKind.MaxInclusive, "100", "50", "150", "maxInclusive-valid-restriction")]
    [Arguments("decimal", FacetKind.MinExclusive, "0", "1", "-1", "minExclusive-valid-restriction")]
    [Arguments("decimal", FacetKind.MaxExclusive, "100", "99", "101", "maxExclusive-valid-restriction")]
    [Arguments("decimal", FacetKind.TotalDigits, "5", "3", "6", "totalDigits-valid-restriction")]
    [Arguments("decimal", FacetKind.FractionDigits, "3", "2", "4", "fractionDigits-valid-restriction")]
    // The integer family: the bounds of the built-in types are part of the datatype, so the base carries no facet.
    [Arguments("integer", FacetKind.TotalDigits, "5", "3", "6", "totalDigits-valid-restriction")]
    [Arguments("integer", FacetKind.MaxInclusive, "100", "50", "150", "maxInclusive-valid-restriction")]
    [Arguments("int", FacetKind.MaxInclusive, "", "100", "2147483648", "maxInclusive-valid-restriction")]
    [Arguments("int", FacetKind.MinInclusive, "", "-100", "-2147483649", "minInclusive-valid-restriction")]
    [Arguments("int", FacetKind.MaxExclusive, "", "2147483647", "2147483648", "maxExclusive-valid-restriction")]
    [Arguments("int", FacetKind.MinExclusive, "", "-2147483648", "-2147483649", "minExclusive-valid-restriction")]
    // An exclusive bound on the opposite inclusive bound of the base leaves an empty value space (Datatypes 4.3.8.4 and 4.3.9.4).
    [Arguments("byte", FacetKind.MaxExclusive, "", "100", "-128", "maxExclusive-valid-restriction")]
    [Arguments("byte", FacetKind.MinExclusive, "", "-100", "127", "minExclusive-valid-restriction")]
    [Arguments("unsignedByte", FacetKind.MaxExclusive, "", "255", "0", "maxExclusive-valid-restriction")]
    [Arguments("positiveInteger", FacetKind.MaxExclusive, "", "100", "1", "maxExclusive-valid-restriction")]
    [Arguments("nonPositiveInteger", FacetKind.MinExclusive, "", "-5", "0", "minExclusive-valid-restriction")]
    [Arguments("byte", FacetKind.MaxInclusive, "", "100", "128", "maxInclusive-valid-restriction")]
    [Arguments("unsignedByte", FacetKind.MaxInclusive, "", "255", "256", "maxInclusive-valid-restriction")]
    [Arguments("positiveInteger", FacetKind.MinInclusive, "", "5", "0", "minInclusive-valid-restriction")]
    [Arguments("nonPositiveInteger", FacetKind.MaxInclusive, "", "-1", "1", "maxInclusive-valid-restriction")]
    // float, double, and duration.
    [Arguments("float", FacetKind.MaxInclusive, "10", "5", "20", "maxInclusive-valid-restriction")]
    [Arguments("float", FacetKind.MinInclusive, "0", "5", "-5", "minInclusive-valid-restriction")]
    [Arguments("float", FacetKind.MaxExclusive, "10", "9", "11", "maxExclusive-valid-restriction")]
    [Arguments("double", FacetKind.MaxInclusive, "10", "5", "20", "maxInclusive-valid-restriction")]
    [Arguments("double", FacetKind.MinExclusive, "0", "5", "-5", "minExclusive-valid-restriction")]
    [Arguments("duration", FacetKind.MaxInclusive, "P10D", "P5D", "P20D", "maxInclusive-valid-restriction")]
    [Arguments("duration", FacetKind.MinInclusive, "P1D", "P5D", "PT1H", "minInclusive-valid-restriction")]
    // The date and time family.
    [Arguments(
        "dateTime",
        FacetKind.MaxInclusive,
        "2020-01-01T00:00:00Z",
        "2019-06-01T00:00:00Z",
        "2021-06-01T00:00:00Z",
        "maxInclusive-valid-restriction"
    )]
    [Arguments(
        "dateTime",
        FacetKind.MinExclusive,
        "2020-01-01T00:00:00Z",
        "2020-06-01T00:00:00Z",
        "2019-06-01T00:00:00Z",
        "minExclusive-valid-restriction"
    )]
    [Arguments(
        "date",
        FacetKind.MaxInclusive,
        "2020-01-01Z",
        "2019-01-01Z",
        "2021-01-01Z",
        "maxInclusive-valid-restriction"
    )]
    [Arguments(
        "date",
        FacetKind.MinInclusive,
        "2020-01-01Z",
        "2021-01-01Z",
        "2019-01-01Z",
        "minInclusive-valid-restriction"
    )]
    [Arguments("time", FacetKind.MaxInclusive, "10:00:00Z", "09:00:00Z", "11:00:00Z", "maxInclusive-valid-restriction")]
    [Arguments("gYear", FacetKind.MaxInclusive, "2020Z", "2019Z", "2021Z", "maxInclusive-valid-restriction")]
    [Arguments(
        "gYearMonth",
        FacetKind.MaxExclusive,
        "2020-05Z",
        "2020-04Z",
        "2020-06Z",
        "maxExclusive-valid-restriction"
    )]
    public async Task NarrowingPasses_WideningIsALocatedSchemaError(
        string type,
        FacetKind kind,
        string baseValue,
        string narrowing,
        string widening,
        string rule
    )
    {
        var fixture = new FacetFixture();
        var baseType = string.IsNullOrEmpty(baseValue)
            ? fixture.Base(type)
            : fixture.Base(type, fixture.Facet(type, kind, baseValue));

        var narrowed = fixture.Check(baseType, FacetFixture.Decl(kind, narrowing));
        await Assert.That(narrowed.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);

        var widened = fixture.Check(baseType, FacetFixture.Decl(kind, widening, line: 11));
        await Assert.That(widened.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf(rule));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(11);
        await Assert.That(fixture.Issues[0].Location.Column).IsEqualTo(9);
    }

    [Test]
    [Arguments("string")]
    [Arguments("hexBinary")]
    [Arguments("list:int")]
    public async Task EqualLengthValues_AreValidRestrictions(string type)
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base(
            type,
            fixture.Facet(type, FacetKind.MinLength, "2"),
            fixture.Facet(type, FacetKind.MaxLength, "10")
        );

        var result = fixture.Check(
            baseType,
            FacetFixture.Decl(FacetKind.MinLength, "2"),
            FacetFixture.Decl(FacetKind.MaxLength, "10")
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExclusiveBoundEqualToTheInclusiveBoundOfTheBase_IsAValidRestriction()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("decimal", fixture.Facet("decimal", FacetKind.MaxInclusive, "100"));

        var result = fixture.Check(baseType, FacetFixture.Decl(FacetKind.MaxExclusive, "100"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task InclusiveBoundEqualToTheExclusiveBoundOfTheBase_IsAnError()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("decimal", fixture.Facet("decimal", FacetKind.MaxExclusive, "100"));

        var result = fixture.Check(baseType, FacetFixture.Decl(FacetKind.MaxInclusive, "100", line: 12));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("maxInclusive-valid-restriction"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(12);
    }

    [Test]
    public async Task RestrictingTheBuiltInTypeComponentDirectly_ChecksLikeTheWrappedType()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.BuiltIn("byte");

        var narrowed = fixture.Check(baseType, FacetFixture.Decl(FacetKind.MaxInclusive, "100"));
        var widened = fixture.Check(baseType, FacetFixture.Decl(FacetKind.MaxInclusive, "128", line: 13));

        await Assert.That(narrowed.IsValid).IsTrue();
        await Assert.That(widened.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("maxInclusive-valid-restriction"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(13);
    }

    [Test]
    public async Task ARestrictionOfARestriction_SeesTheFacetsOfEveryAncestor()
    {
        var fixture = new FacetFixture();
        var first = fixture.Base("decimal", fixture.Facet("decimal", FacetKind.MaxInclusive, "100"));
        var second = fixture.Derive(first, fixture.Facet("decimal", FacetKind.MinInclusive, "0"));

        var inRange = fixture.Check(second, FacetFixture.Decl(FacetKind.MaxInclusive, "50"));
        var aboveFirst = fixture.Check(second, FacetFixture.Decl(FacetKind.MaxInclusive, "150", line: 14));
        var belowSecond = fixture.Check(second, FacetFixture.Decl(FacetKind.MinInclusive, "-1", line: 15));

        await Assert.That(inRange.IsValid).IsTrue();
        await Assert.That(aboveFirst.IsValid).IsFalse();
        await Assert.That(belowSecond.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("maxInclusive-valid-restriction"));
        await Assert.That(fixture.Issues[1].Code).IsEqualTo(FacetFixture.CodeOf("minInclusive-valid-restriction"));
    }
}
