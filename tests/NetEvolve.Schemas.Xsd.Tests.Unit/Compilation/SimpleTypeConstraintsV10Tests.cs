namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>Simple type constraints that Structures 1.0 states and Structures 1.1 relaxes.</summary>
public class SimpleTypeConstraintsV10Tests
{
    [Test]
    [Arguments(XsdVersion.V10, 1)]
    [Arguments(XsdVersion.V11, 0)]
    public async Task Restriction_OfAnySimpleType_IsRejectedOnlyInXsd10(XsdVersion version, int expected)
    {
        var compilation = Compile(
            Schema("<xs:simpleType name=\"s\"><xs:restriction base=\"xs:anySimpleType\"/></xs:simpleType>"),
            version
        );

        await Assert.That(Codes(compilation).FindAll(c => c == XsdRuleCodes.CosStRestricts).Count).IsEqualTo(expected);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Restriction_OfAnAtomicType_IsValid(XsdVersion version)
    {
        var compilation = Compile(
            Schema("<xs:simpleType name=\"s\"><xs:restriction base=\"xs:string\"/></xs:simpleType>"),
            version
        );

        await Assert.That(Describe(compilation)).IsEmpty();
    }

    [Test]
    [Arguments(XsdVersion.V10, 1)]
    [Arguments(XsdVersion.V11, 0)]
    public async Task Union_WithAnEmptyMemberTypesList_IsRejectedOnlyInXsd10(XsdVersion version, int expected)
    {
        var compilation = Compile(
            Schema("<xs:simpleType name=\"u\"><xs:union memberTypes=\"\"/></xs:simpleType>"),
            version
        );

        await Assert
            .That(Codes(compilation).FindAll(c => c == XsdRuleCodes.SrcUnionMemberTypesOrSimpleTypes).Count)
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Union_WithMemberTypes_IsValid(XsdVersion version)
    {
        var compilation = Compile(
            Schema("<xs:simpleType name=\"u\"><xs:union memberTypes=\"xs:int xs:string\"/></xs:simpleType>"),
            version
        );

        await Assert.That(Describe(compilation)).IsEmpty();
    }

    [Test]
    [Arguments(XsdVersion.V10, 1)]
    [Arguments(XsdVersion.V11, 0)]
    public async Task SimpleType_FinalExtension_IsRejectedOnlyInXsd10(XsdVersion version, int expected)
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"s\" final=\"extension\"><xs:restriction base=\"xs:string\"/></xs:simpleType>"
            ),
            version
        );

        await Assert.That(Codes(compilation).FindAll(c => c == XsdRuleCodes.SrcSimpleType).Count).IsEqualTo(expected);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task SimpleType_FinalRestriction_IsValid(XsdVersion version)
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"s\" final=\"restriction\"><xs:restriction base=\"xs:string\"/></xs:simpleType>"
            ),
            version
        );

        await Assert.That(Describe(compilation)).IsEmpty();
    }
}
