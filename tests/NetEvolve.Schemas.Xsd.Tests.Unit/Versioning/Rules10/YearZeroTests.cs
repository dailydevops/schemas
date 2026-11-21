namespace NetEvolve.Schemas.Xsd.Tests.Unit.Versioning.Rules10;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

/// <summary>
/// Year zero (Datatypes 1.0, section 3.2.7): the year 0000 of dateTime, date, gYear, and gYearMonth is invalid in strict 1.0 mode and
/// valid in 1.1 mode. The version reaches the datatype of the Xml package through the registry of the built-in types; the Xsd
/// package parses no date itself.
/// </summary>
public class YearZeroTests
{
    private static readonly DocumentLocation Where = new("file:///f.xsd", 6, 4);

    [Test]
    [Arguments("dateTime", "0000-01-01T00:00:00")]
    [Arguments("dateTime", "0000-12-31T23:59:59Z")]
    [Arguments("date", "0000-01-01")]
    [Arguments("date", "0000-06-15+01:00")]
    [Arguments("gYear", "0000")]
    [Arguments("gYear", "0000Z")]
    [Arguments("gYearMonth", "0000-01")]
    [Arguments("gYearMonth", "0000-12-05:00")]
    public async Task YearZero_IsInvalidInStrict10ModeAndValidInVersion11Mode(string type, string value)
    {
        var strict = new FacetFixture(XsdVersion.V10);
        var relaxed = new FacetFixture(XsdVersion.V11);

        await Assert.That(strict.Assessor.Assess(strict.BuiltIn(type), value, out _).IsValid).IsFalse();
        await Assert.That(relaxed.Assessor.Assess(relaxed.BuiltIn(type), value, out _).IsValid).IsTrue();
    }

    [Test]
    [Arguments("dateTime", "-0001-12-31T23:59:59")]
    [Arguments("dateTime", "-0001-12-31T24:00:00")]
    [Arguments("dateTime", "0001-01-01T00:00:00")]
    [Arguments("dateTime", "-10000-01-01T00:00:00")]
    [Arguments("date", "-0001-12-31")]
    [Arguments("date", "0001-01-01")]
    [Arguments("gYear", "-0001")]
    [Arguments("gYear", "0001")]
    [Arguments("gYearMonth", "-0001-12")]
    [Arguments("gYearMonth", "0001-01")]
    public async Task NegativeYearsAndYearOne_AreValidInBothModes(string type, string value)
    {
        foreach (var version in new[] { XsdVersion.V10, XsdVersion.V11 })
        {
            var fixture = new FacetFixture(version);

            await Assert.That(fixture.Assessor.Assess(fixture.BuiltIn(type), value, out _).IsValid).IsTrue();
        }
    }

    [Test]
    [Arguments("0000-01-01 2000-01-01")]
    [Arguments("2000-01-01 0000-01-01")]
    public async Task YearZeroAsAListItem_IsInvalidInStrict10ModeAndValidInVersion11Mode(string value)
    {
        var strict = new FacetFixture(XsdVersion.V10);
        var relaxed = new FacetFixture(XsdVersion.V11);

        await Assert.That(strict.Assessor.Assess(strict.Base("list:date"), value, out _).IsValid).IsFalse();
        await Assert.That(relaxed.Assessor.Assess(relaxed.Base("list:date"), value, out _).IsValid).IsTrue();
    }

    [Test]
    public async Task YearZeroInAUnionWithoutAnotherAcceptingMember_IsInvalidInStrict10ModeOnly()
    {
        var strict = new FacetFixture(XsdVersion.V10);
        var relaxed = new FacetFixture(XsdVersion.V11);

        await Assert.That(strict.Assessor.Assess(strict.Base("union:gYear+date"), "0000", out _).IsValid).IsFalse();
        await Assert
            .That(relaxed.Assessor.Assess(relaxed.Base("union:gYear+date"), "0000", out var member).IsValid)
            .IsTrue();
        await Assert.That(member).IsEqualTo(0);
    }

    [Test]
    [Arguments("dateTime", "0000-01-01T00:00:00")]
    [Arguments("date", "0000-01-01")]
    [Arguments("gYear", "0000")]
    [Arguments("gYearMonth", "0000-01")]
    public async Task YearZeroAsADefaultOrFixedValue_IsALocatedSchemaErrorInStrict10ModeOnly(string type, string value)
    {
        var strict = new FacetFixture(XsdVersion.V10);
        var relaxed = new FacetFixture(XsdVersion.V11);

        foreach (var kind in new[] { AttributeValueConstraintKind.Default, AttributeValueConstraintKind.Fixed })
        {
            await Assert.That(Check(strict, type, kind, value)).IsFalse();
            await Assert.That(Check(relaxed, type, kind, value)).IsTrue();
        }

        await Assert.That(strict.Issues.Count).IsEqualTo(2);
        await Assert.That(strict.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("a-props-correct"));
        await Assert.That(strict.Issues[0].Location.Line).IsEqualTo(6);
        await Assert.That(relaxed.Issues.Count).IsEqualTo(0);
    }

    private static bool Check(FacetFixture fixture, string type, AttributeValueConstraintKind kind, string value)
    {
        var attribute = AttributeDeclaration.Global(
            "a",
            "urn:test",
            Where,
            fixture.Wrapped(type),
            new AttributeValueConstraint(kind, value),
            fixture.Reporter
        )!;
        return ValueConstraintCheck.Check(attribute, fixture.Assessor, fixture.Version, fixture.Reporter);
    }
}
