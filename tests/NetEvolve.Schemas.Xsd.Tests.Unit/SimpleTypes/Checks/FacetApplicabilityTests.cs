namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A facet that does not apply to the type family of the base type is a schema error (<c>cos-applicable-facets</c>).</summary>
public class FacetApplicabilityTests
{
    [Test]
    [Arguments("boolean", FacetKind.Length, "1")]
    [Arguments("boolean", FacetKind.MaxLength, "1")]
    [Arguments("boolean", FacetKind.MinInclusive, "true")]
    [Arguments("boolean", FacetKind.TotalDigits, "1")]
    [Arguments("string", FacetKind.MinInclusive, "a")]
    [Arguments("string", FacetKind.MaxExclusive, "a")]
    [Arguments("string", FacetKind.TotalDigits, "3")]
    [Arguments("string", FacetKind.FractionDigits, "1")]
    [Arguments("decimal", FacetKind.Length, "1")]
    [Arguments("decimal", FacetKind.MinLength, "1")]
    [Arguments("int", FacetKind.MaxLength, "1")]
    [Arguments("float", FacetKind.TotalDigits, "3")]
    [Arguments("double", FacetKind.FractionDigits, "1")]
    [Arguments("double", FacetKind.Length, "1")]
    [Arguments("duration", FacetKind.FractionDigits, "1")]
    [Arguments("duration", FacetKind.MaxLength, "1")]
    [Arguments("dateTime", FacetKind.Length, "1")]
    [Arguments("dateTime", FacetKind.TotalDigits, "1")]
    [Arguments("hexBinary", FacetKind.TotalDigits, "1")]
    [Arguments("hexBinary", FacetKind.MaxInclusive, "00")]
    [Arguments("anyURI", FacetKind.MinExclusive, "a")]
    [Arguments("QName", FacetKind.MaxInclusive, "a")]
    [Arguments("list:int", FacetKind.MaxInclusive, "5")]
    [Arguments("list:int", FacetKind.MinExclusive, "5")]
    [Arguments("list:int", FacetKind.TotalDigits, "5")]
    [Arguments("list:int", FacetKind.FractionDigits, "1")]
    [Arguments("union:int+string", FacetKind.Length, "1")]
    [Arguments("union:int+string", FacetKind.MaxLength, "1")]
    [Arguments("union:int+string", FacetKind.WhiteSpace, "collapse")]
    [Arguments("union:int+string", FacetKind.MinInclusive, "1")]
    [Arguments("union:int+string", FacetKind.TotalDigits, "1")]
    public async Task AFacetOutsideTheTypeFamily_IsALocatedSchemaError(string type, FacetKind kind, string value)
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base(type), FacetFixture.Decl(kind, value, line: 31));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cos-applicable-facets"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(31);
        await Assert.That(fixture.Issues[0].Location.Column).IsEqualTo(9);
    }

    [Test]
    [Arguments("string", FacetKind.Length, "1")]
    [Arguments("string", FacetKind.WhiteSpace, "collapse")]
    [Arguments("boolean", FacetKind.WhiteSpace, "collapse")]
    [Arguments("boolean", FacetKind.Pattern, "true|false")]
    [Arguments("boolean", FacetKind.Enumeration, "true")]
    [Arguments("decimal", FacetKind.TotalDigits, "3")]
    [Arguments("decimal", FacetKind.FractionDigits, "1")]
    [Arguments("decimal", FacetKind.MaxInclusive, "1")]
    [Arguments("int", FacetKind.TotalDigits, "3")]
    [Arguments("float", FacetKind.MaxInclusive, "1")]
    [Arguments("duration", FacetKind.MinInclusive, "P1D")]
    [Arguments("date", FacetKind.MinInclusive, "2020-01-01Z")]
    [Arguments("hexBinary", FacetKind.Length, "2")]
    [Arguments("anyURI", FacetKind.MaxLength, "2")]
    [Arguments("QName", FacetKind.Length, "2")]
    [Arguments("list:int", FacetKind.Length, "2")]
    [Arguments("list:int", FacetKind.WhiteSpace, "collapse")]
    [Arguments("list:int", FacetKind.Pattern, "[0-9 ]+")]
    [Arguments("list:int", FacetKind.Enumeration, "1 2")]
    [Arguments("union:int+string", FacetKind.Pattern, "[a-z0-9]+")]
    [Arguments("union:int+string", FacetKind.Enumeration, "1")]
    public async Task AFacetInsideTheTypeFamily_IsApplicable(string type, FacetKind kind, string value)
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base(type), FacetFixture.Decl(kind, value));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EveryFacetOutsideTheFamily_IsReportedOnItsOwn()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("boolean"),
            FacetFixture.Decl(FacetKind.Length, "1", line: 41),
            FacetFixture.Decl(FacetKind.MinInclusive, "true", line: 42),
            FacetFixture.Decl(FacetKind.Pattern, "true", line: 43)
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(41);
        await Assert.That(fixture.Issues[1].Location.Line).IsEqualTo(42);
    }

    [Test]
    public async Task AFacetThatIsNotApplicable_DoesNotCauseASecondIssueFromTheValueChecks()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base("boolean"), FacetFixture.Decl(FacetKind.MaxLength, "not-a-number"));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cos-applicable-facets"));
    }

    [Test]
    public async Task ExplicitTimezoneAndAssertions_AreNotCheckedByThisPackage()
    {
        var fixture = new FacetFixture();

        var onDecimal = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.ExplicitTimezone, "required")
        );
        var assertion = fixture.Check(fixture.Base("string"), FacetFixture.Decl(FacetKind.Assertions, "$value"));

        await Assert.That(onDecimal.IsValid).IsTrue();
        await Assert.That(assertion.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }
}
