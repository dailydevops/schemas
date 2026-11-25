namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System.Linq;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>
/// Structures 1.0 drops the prohibited uses of an attribute group and keeps a prohibited use with a value constraint that removes nothing.
/// </summary>
public class ProhibitedAttributeUseV10Tests
{
    private const string GroupRestriction =
        "<xs:complexType name=\"base\"><xs:attribute name=\"a\"/></xs:complexType>"
        + "<xs:attributeGroup name=\"g\"><xs:attribute name=\"a\" use=\"prohibited\"/></xs:attributeGroup>"
        + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:base\"><xs:attributeGroup ref=\"t:g\"/></xs:restriction></xs:complexContent></xs:complexType>";

    private const string DirectRestriction =
        "<xs:complexType name=\"base\"><xs:attribute name=\"a\"/></xs:complexType>"
        + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:base\"><xs:attribute name=\"a\" use=\"prohibited\"/></xs:restriction></xs:complexContent></xs:complexType>";

    private const string FixedOverBaseUse =
        "<xs:complexType name=\"base\"><xs:attribute name=\"a\"/></xs:complexType>"
        + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:base\"><xs:attribute name=\"a\" use=\"prohibited\" fixed=\"37\"/></xs:restriction></xs:complexContent></xs:complexType>";

    private const string FixedOverOtherBaseUse =
        "<xs:complexType name=\"base\"><xs:attribute name=\"b\"/></xs:complexType>"
        + "<xs:complexType name=\"d\"><xs:complexContent><xs:extension base=\"t:base\"><xs:attribute name=\"a\" use=\"prohibited\" fixed=\"37\"/></xs:extension></xs:complexContent></xs:complexType>";

    private const string FixedOverSimpleBase =
        "<xs:complexType name=\"d\"><xs:simpleContent><xs:extension base=\"xs:string\"><xs:attribute name=\"a\" use=\"prohibited\" fixed=\"37\"/></xs:extension></xs:simpleContent></xs:complexType>";

    private const string FixedWithoutBase =
        "<xs:complexType name=\"d\"><xs:attribute name=\"a\" use=\"prohibited\" fixed=\"37\"/></xs:complexType>";

    [Test]
    [Arguments(XsdVersion.V10, 1)]
    [Arguments(XsdVersion.V11, 0)]
    public async Task GroupProhibition_IsDroppedOnlyInXsd10(XsdVersion version, int expected)
    {
        var compilation = Compile(Schema(GroupRestriction), version);

        var d = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "d");

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(d.AttributeUses.Count).IsEqualTo(expected);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task DirectProhibition_RemovesTheBaseUse(XsdVersion version)
    {
        var compilation = Compile(Schema(DirectRestriction), version);

        var d = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "d");

        await Assert.That(d.AttributeUses).IsEmpty();
    }

    [Test]
    [Arguments(XsdVersion.V10, 1)]
    [Arguments(XsdVersion.V11, 0)]
    public async Task ProhibitedFixedUseWithoutBaseUse_IsAnOptionalUseOnlyInXsd10(XsdVersion version, int expected)
    {
        var compilation = Compile(Schema(FixedWithoutBase), version);

        var d = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "d");

        await Assert.That(d.AttributeUses.Count(u => !u.Prohibited && !u.Required)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(FixedOverBaseUse, 0)]
    [Arguments(FixedOverOtherBaseUse, 1)]
    [Arguments(FixedOverSimpleBase, 1)]
    public async Task ProhibitedFixedUse_IsKeptOnlyWhenItRemovesNothingOfTheBase(string types, int expected)
    {
        var compilation = Compile(Schema(types), XsdVersion.V10);

        var d = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "d");

        await Assert
            .That(d.AttributeUses.Count(u => u.Name.LocalName == "a" && !u.Prohibited && !u.Required))
            .IsEqualTo(expected);
    }
}
