namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Validation.Assembly;
using NetEvolve.Schemas.Xsd.Versioning.Constructs;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>
/// The final check of the XSD 1.1 wiring (XSD-78, #360, #1432): the default builder runs <c>assert</c>, <c>alternative</c>, and
/// <c>openContent</c> without a not-yet-supported error, the construct table holds only the permanent limitations (#1431), and strict
/// 1.0 rejects the same constructs with its own codes.
/// </summary>
public class DefaultBuilderXsd11Tests
{
    private static readonly string Schema = AssemblyFixture.WrapSchema(
        """
        <xs:element name="root">
          <xs:complexType>
            <xs:openContent mode="interleave"><xs:any namespace="##other" processContents="lax"/></xs:openContent>
            <xs:sequence>
              <xs:element name="a" type="xs:int"/>
              <xs:element name="b" type="xs:int"/>
            </xs:sequence>
            <xs:assert test="a lt b" xpathDefaultNamespace="##targetNamespace"/>
          </xs:complexType>
        </xs:element>
        <xs:element name="poly">
          <xs:alternative test="@kind = 'c'">
            <xs:complexType><xs:sequence><xs:element name="x" type="xs:int"/></xs:sequence><xs:attribute name="kind"/></xs:complexType>
          </xs:alternative>
        </xs:element>
        """
    );

    [Test]
    [Arguments("<root xmlns=\"urn:t\" xmlns:o=\"urn:o\"><a>1</a><o:extra/><b>2</b></root>", true)]
    [Arguments("<root xmlns=\"urn:t\"><a>3</a><b>2</b></root>", false)]
    [Arguments("<root xmlns=\"urn:t\"><a>1</a><u:extra xmlns:u=\"urn:t\"/><b>2</b></root>", false)]
    [Arguments("<poly xmlns=\"urn:t\" kind=\"c\"><x>1</x></poly>", true)]
    [Arguments("<poly xmlns=\"urn:t\" kind=\"c\"><y/></poly>", false)]
    [Arguments("<poly xmlns=\"urn:t\" kind=\"d\"><y/></poly>", true)]
    public async Task Validate_DefaultBuilder_AppliesAssertAlternativeAndOpenContent(string instance, bool expected)
    {
        var validator = AssemblyFixture.Build(Schema);

        await Assert.That(validator.Version).IsEqualTo(XsdVersion.V11);
        await Assert.That(AssemblyFixture.Run(validator, instance).IsValid).IsEqualTo(expected);
    }

    [Test]
    public async Task Table_HoldsOnlyThePermanentLimitationsOfXsd11()
    {
        // precisionDecimal (NES4076) and xs:error (NES4077) wait for the decision of #1431; every other row is wired.
        var codes = Xsd11ConstructTable.Entries.Select(e => e.NotYetSupported.Code.Number).OrderBy(c => c);

        await Assert.That(codes).IsEquivalentTo([4076, 4077]);
    }

    [Test]
    public async Task Build_StrictVersion10_RejectsTheSameConstructsWithItsOwnCodes()
    {
        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(Schema, XsdVersion.V10));

        var codes = exception.Issues.Select(i => i.Code).ToArray();
        await Assert.That(codes).Contains(XsdRuleCodes.Strict10Assert);
        await Assert.That(codes).Contains(XsdRuleCodes.Strict10OpenContent);
        await Assert.That(codes).Contains(XsdRuleCodes.Strict10Alternative);
        await Assert.That(codes.Any(c => c.Number is >= 4060 and <= 4077)).IsFalse();
    }
}
