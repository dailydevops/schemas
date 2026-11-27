namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.V11;

using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The <c>xs:explicitTimezone</c> facet is compiled by the default builder and enforced by the assembled validator (XSD-78, #1430).</summary>
public class ExplicitTimezoneWiringTests
{
    private static string Schema(string type, string mode) =>
        AssemblyFixture.WrapSchema(
            $"<xs:element name=\"e\"><xs:simpleType><xs:restriction base=\"xs:{type}\"><xs:explicitTimezone value=\"{mode}\"/></xs:restriction></xs:simpleType></xs:element>"
        );

    private static bool IsValid(string type, string mode, string lexical) =>
        AssemblyFixture.Run(AssemblyFixture.Build(Schema(type, mode)), $"<e xmlns=\"urn:t\">{lexical}</e>").IsValid;

    [Test]
    [Arguments("dateTime", "2020-01-01T10:00:00Z", "2020-01-01T10:00:00")]
    [Arguments("date", "2020-01-01+01:00", "2020-01-01")]
    [Arguments("time", "10:00:00Z", "10:00:00")]
    [Arguments("gYear", "2020Z", "2020")]
    [Arguments("gYearMonth", "2020-01Z", "2020-01")]
    [Arguments("gMonth", "--01Z", "--01")]
    [Arguments("gMonthDay", "--01-01Z", "--01-01")]
    [Arguments("gDay", "---01Z", "---01")]
    public async Task Required_AcceptsATimezoneAndRejectsItsAbsence(string type, string withZone, string withoutZone)
    {
        await Assert.That(IsValid(type, "required", withZone)).IsTrue();
        await Assert.That(IsValid(type, "required", withoutZone)).IsFalse();
    }

    [Test]
    [Arguments("dateTime", "2020-01-01T10:00:00Z", "2020-01-01T10:00:00")]
    [Arguments("date", "2020-01-01+01:00", "2020-01-01")]
    [Arguments("time", "10:00:00Z", "10:00:00")]
    [Arguments("gYear", "2020Z", "2020")]
    [Arguments("gYearMonth", "2020-01Z", "2020-01")]
    [Arguments("gMonth", "--01Z", "--01")]
    [Arguments("gMonthDay", "--01-01Z", "--01-01")]
    [Arguments("gDay", "---01Z", "---01")]
    public async Task Prohibited_AcceptsNoTimezoneAndRejectsOne(string type, string withZone, string withoutZone)
    {
        await Assert.That(IsValid(type, "prohibited", withoutZone)).IsTrue();
        await Assert.That(IsValid(type, "prohibited", withZone)).IsFalse();
    }

    [Test]
    [Arguments("dateTime", "2020-01-01T10:00:00Z", "2020-01-01T10:00:00")]
    [Arguments("date", "2020-01-01+01:00", "2020-01-01")]
    [Arguments("time", "10:00:00Z", "10:00:00")]
    [Arguments("gYear", "2020Z", "2020")]
    public async Task Optional_AcceptsBoth(string type, string withZone, string withoutZone)
    {
        await Assert.That(IsValid(type, "optional", withZone)).IsTrue();
        await Assert.That(IsValid(type, "optional", withoutZone)).IsTrue();
    }

    [Test]
    public async Task AViolatedFacet_ReportsCvcExplicitTimezoneValid()
    {
        var result = AssemblyFixture.Run(
            AssemblyFixture.Build(Schema("dateTime", "required")),
            "<e xmlns=\"urn:t\">2020-01-01T10:00:00</e>"
        );

        await Assert.That(result.Issues.Any(i => i.Code == XsdRuleCodes.CvcExplicitTimezoneValid)).IsTrue();
    }

    [Test]
    public async Task ADerivedType_CannotLoosenTheFacetOfItsBase()
    {
        var schema = AssemblyFixture.WrapSchema(
            """
            <xs:simpleType name="b"><xs:restriction base="xs:dateTime"><xs:explicitTimezone value="required"/></xs:restriction></xs:simpleType>
            <xs:simpleType name="d"><xs:restriction base="t:b"><xs:explicitTimezone value="optional"/></xs:restriction></xs:simpleType>
            """
        );

        var failure = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        await Assert.That(failure.Issues).IsNotEmpty();
    }

    [Test]
    public async Task ADerivedType_MayTightenAnOptionalBase()
    {
        var schema = AssemblyFixture.WrapSchema(
            """
            <xs:simpleType name="b"><xs:restriction base="xs:dateTime"><xs:explicitTimezone value="optional"/></xs:restriction></xs:simpleType>
            <xs:simpleType name="d"><xs:restriction base="t:b"><xs:explicitTimezone value="prohibited"/></xs:restriction></xs:simpleType>
            <xs:element name="e" type="t:d"/>
            """
        );
        var validator = AssemblyFixture.Build(schema);

        await Assert
            .That(AssemblyFixture.Run(validator, "<e xmlns=\"urn:t\">2020-01-01T10:00:00</e>").IsValid)
            .IsTrue();
        await Assert
            .That(AssemblyFixture.Run(validator, "<e xmlns=\"urn:t\">2020-01-01T10:00:00Z</e>").IsValid)
            .IsFalse();
    }

    [Test]
    public async Task TheFacetOnANonDateType_IsALocatedSchemaError()
    {
        var failure = AssemblyFixture.Failure(() => AssemblyFixture.Build(Schema("string", "required")));

        await Assert.That(failure.Issues).IsNotEmpty();
        await Assert.That(failure.Issues[0].Code).IsNotEqualTo(XsdRuleCodes.NotYetSupportedExplicitTimezone);
    }

    [Test]
    public async Task UnderStrictVersion10_TheFacetIsStillRejectedWithTheStrictCode()
    {
        var failure = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(Schema("dateTime", "required"), XsdVersion.V10)
        );

        await Assert.That(failure.Issues[0].Code.ToString()).IsEqualTo("NES4033");
    }
}
