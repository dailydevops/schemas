namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using System.Linq;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Wildcards.Core;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>Components are built from syntax in dependency order, so a definition may use one that the document declares later.</summary>
public class ConstructionTests
{
    [Test]
    public async Task SimpleType_RestrictingATypeDeclaredLater_HasThatTypeAsBase()
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"b\"><xs:restriction base=\"t:a\"><xs:maxLength value=\"2\"/></xs:restriction></xs:simpleType>"
                    + "<xs:simpleType name=\"a\"><xs:restriction base=\"xs:string\"><xs:maxLength value=\"5\"/></xs:restriction></xs:simpleType>"
            )
        );

        var b = Get<SimpleTypeDefinition>(compilation, XsdComponentKind.SimpleType, "b");
        var a = Get<SimpleTypeDefinition>(compilation, XsdComponentKind.SimpleType, "a");

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(b.BaseType).IsSameReferenceAs(a);
        await Assert.That(a.BaseType!.Name).IsEqualTo(new XPathQName(Xs, "string"));
        await Assert.That(b.Facets.Count).IsEqualTo(2);
    }

    [Test]
    public async Task SimpleType_ListAndUnion_ReferToTheirItemAndMemberTypes()
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"l\"><xs:list itemType=\"t:u\"/></xs:simpleType>"
                    + "<xs:simpleType name=\"u\"><xs:union memberTypes=\"xs:int t:code\"><xs:simpleType><xs:restriction base=\"xs:boolean\"/></xs:simpleType></xs:union></xs:simpleType>"
                    + "<xs:simpleType name=\"code\"><xs:restriction base=\"xs:string\"/></xs:simpleType>"
            )
        );

        var list = Get<SimpleTypeDefinition>(compilation, XsdComponentKind.SimpleType, "l");
        var union = Get<SimpleTypeDefinition>(compilation, XsdComponentKind.SimpleType, "u");

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(list.Variety).IsEqualTo(DatatypeVariety.List);
        await Assert.That(list.ItemType).IsSameReferenceAs(union);
        await Assert.That(union.MemberTypes.Count).IsEqualTo(3);
        await Assert.That(union.MemberTypes[1].Name).IsEqualTo(new XPathQName(Tns, "code"));
    }

    [Test]
    public async Task SimpleType_ThatIsItsOwnBase_IsOneCircularDefinitionAndTerminates()
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"a\"><xs:restriction base=\"t:b\"/></xs:simpleType>"
                    + "<xs:simpleType name=\"b\"><xs:restriction base=\"t:a\"/></xs:simpleType>"
            )
        );

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.StPropsCorrect);
    }

    [Test]
    public async Task ComplexType_DerivedFromATypeDeclaredLater_HasThatTypeAsBase()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:complexContent><xs:extension base=\"t:b\"><xs:sequence><xs:element name=\"z\"/></xs:sequence></xs:extension></xs:complexContent></xs:complexType>"
                    + "<xs:complexType name=\"b\"><xs:complexContent><xs:extension base=\"t:a\"><xs:attribute name=\"y\"/></xs:extension></xs:complexContent></xs:complexType>"
                    + "<xs:complexType name=\"a\"><xs:sequence><xs:element name=\"x\"/></xs:sequence><xs:attribute name=\"w\"/></xs:complexType>"
            )
        );

        var a = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "a");
        var b = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "b");
        var c = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "c");

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(c.BaseType).IsSameReferenceAs(b);
        await Assert.That(b.BaseType).IsSameReferenceAs(a);
        await Assert.That(c.AttributeUses.Select(u => u.Name.LocalName)).IsEquivalentTo(["w", "y"]);
        await Assert.That(c.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
    }

    [Test]
    public async Task ComplexType_ThatDerivesFromItself_IsOneCircularDefinitionAndTerminates()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"a\"><xs:complexContent><xs:extension base=\"t:b\"/></xs:complexContent></xs:complexType>"
                    + "<xs:complexType name=\"b\"><xs:complexContent><xs:extension base=\"t:a\"/></xs:complexContent></xs:complexType>"
            )
        );

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.CtPropsCorrect);
    }

    [Test]
    public async Task ComplexType_WithSimpleContent_TakesTheSimpleTypeOfItsBase()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"p\"><xs:simpleContent><xs:extension base=\"t:code\"><xs:attribute name=\"unit\" type=\"xs:string\"/></xs:extension></xs:simpleContent></xs:complexType>"
                    + "<xs:complexType name=\"q\"><xs:simpleContent><xs:restriction base=\"t:p\"><xs:maxLength value=\"2\"/></xs:restriction></xs:simpleContent></xs:complexType>"
                    + "<xs:simpleType name=\"code\"><xs:restriction base=\"xs:string\"/></xs:simpleType>"
            )
        );

        var p = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "p");
        var q = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "q");

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(p.ContentKind).IsEqualTo(ComplexContentKind.Simple);
        await Assert.That(p.SimpleContentType!.Name).IsEqualTo(new XPathQName(Tns, "code"));
        await Assert.That(q.SimpleContentType!.BaseType!.Name).IsEqualTo(new XPathQName(Tns, "code"));
        await Assert.That(q.AttributeUses.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Element_WithAnAnonymousType_GetsThatTypeAndItsNestedReferencesAreBound()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"e\"><xs:complexType><xs:sequence>"
                    + "<xs:element name=\"inner\" type=\"t:named\"/>"
                    + "<xs:element name=\"deep\"><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:element>"
                    + "</xs:sequence></xs:complexType></xs:element>"
                    + "<xs:simpleType name=\"named\"><xs:restriction base=\"xs:string\"/></xs:simpleType>"
            )
        );

        var e = Get<ElementDeclaration>(compilation, XsdComponentKind.ElementDeclaration, "e");
        var type = (ComplexTypeDefinition)e.TypeDefinition!;
        var group = (ModelGroup)type.ContentParticle!.Term!;
        var inner = (ElementDeclaration)group.Particles[0].Term!;
        var deep = (ElementDeclaration)group.Particles[1].Term!;

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(type.IsAnonymous).IsTrue();
        await Assert.That(inner.TypeDefinition!.Name).IsEqualTo(new XPathQName(Tns, "named"));
        await Assert.That(deep.TypeDefinition).IsTypeOf<SimpleTypeDefinition>();
    }

    [Test]
    public async Task Element_WithoutAType_IsTheUrType()
    {
        var compilation = Compile(Schema("<xs:element name=\"e\"/>"));

        var e = Get<ElementDeclaration>(compilation, XsdComponentKind.ElementDeclaration, "e");

        await Assert.That(e.TypeDefinition!.Name).IsEqualTo(new XPathQName(Xs, "anyType"));
    }

    [Test]
    public async Task Attribute_GroupsAndReferences_AreFlattenedIntoTheUsesOfTheType()
    {
        var compilation = Compile(
            Schema(
                "<xs:attribute name=\"g\" type=\"xs:int\"/>"
                    + "<xs:attributeGroup name=\"ag\"><xs:attribute name=\"x\" type=\"xs:string\"/><xs:attributeGroup ref=\"t:inner\"/></xs:attributeGroup>"
                    + "<xs:attributeGroup name=\"inner\"><xs:attribute ref=\"t:g\" use=\"required\"/><xs:anyAttribute namespace=\"##other\"/></xs:attributeGroup>"
                    + "<xs:complexType name=\"c\"><xs:attributeGroup ref=\"t:ag\"/><xs:attribute name=\"own\" default=\"d\"/></xs:complexType>"
            )
        );

        var c = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "c");

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(c.AttributeUses.Select(u => u.Name.LocalName)).IsEquivalentTo(["x", "g", "own"]);
        await Assert
            .That(c.AttributeUses.Single(u => string.Equals(u.Name.LocalName, "g", StringComparison.Ordinal)).Required)
            .IsTrue();
        await Assert.That(c.AttributeWildcard).IsTypeOf<Wildcard>();
    }

    [Test]
    public async Task Content_GroupReferenceDeclaredLater_IsResolvedToTheDefinition()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:group ref=\"t:g\"/></xs:complexType>"
                    + "<xs:group name=\"g\"><xs:choice><xs:element name=\"a\"/><xs:element name=\"b\"/></xs:choice></xs:group>"
            )
        );

        var c = Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "c");

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(c.ContentParticle!.Term).IsTypeOf<ModelGroupDefinition>();
        await Assert.That(c.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
    }

    [Test]
    [Arguments("<xs:complexType name=\"c\"><xs:group ref=\"t:nope\"/></xs:complexType>")]
    [Arguments("<xs:complexType name=\"c\"><xs:attributeGroup ref=\"t:nope\"/></xs:complexType>")]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:complexContent><xs:extension base=\"t:nope\"/></xs:complexContent></xs:complexType>"
    )]
    [Arguments("<xs:complexType name=\"c\"><xs:attribute ref=\"t:nope\"/></xs:complexType>")]
    [Arguments("<xs:attribute name=\"a\" type=\"t:nope\"/>")]
    [Arguments("<xs:simpleType name=\"s\"><xs:restriction base=\"t:nope\"/></xs:simpleType>")]
    [Arguments("<xs:simpleType name=\"s\"><xs:list itemType=\"t:nope\"/></xs:simpleType>")]
    [Arguments("<xs:simpleType name=\"s\"><xs:union memberTypes=\"t:nope\"/></xs:simpleType>")]
    [Arguments(
        "<xs:element name=\"e\"><xs:complexType><xs:sequence><xs:element ref=\"t:nope\"/></xs:sequence></xs:complexType></xs:element>"
    )]
    public async Task Reference_ToAMissingComponent_IsOneUnresolvedReferenceError(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcResolve]);
    }

    [Test]
    public async Task Union_WithNoResolvedMembersBecauseAMemberTypeFailed_DoesNotReportTheEmptyUnion()
    {
        var compilation = Compile(
            Schema("<xs:simpleType name=\"s\"><xs:union memberTypes=\"t:nope\"/></xs:simpleType>")
        );

        await Assert.That(Codes(compilation)).DoesNotContain(XsdRuleCodes.SrcUnionMemberTypesOrSimpleTypes);
    }

    [Test]
    [Arguments("<xs:simpleType name=\"s\"/>", "src-simple-type")]
    [Arguments(
        "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:restriction></xs:simpleType>",
        "src-restriction-base-or-simpleType"
    )]
    [Arguments("<xs:simpleType name=\"s\"><xs:restriction/></xs:simpleType>", "src-restriction-base-or-simpleType")]
    [Arguments("<xs:simpleType name=\"s\"><xs:list/></xs:simpleType>", "src-list-itemType-or-simpleType")]
    [Arguments("<xs:simpleType name=\"s\"><xs:union/></xs:simpleType>", "src-union-memberTypes-or-simpleTypes")]
    [Arguments(
        "<xs:attribute name=\"a\" type=\"xs:int\"><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:attribute>",
        "src-attribute.4"
    )]
    [Arguments("<xs:attribute name=\"a\" default=\"1\" fixed=\"1\"/>", "src-attribute.1")]
    [Arguments(
        "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"><xs:pattern value=\"1\"/><xs:pattern value=\"(\"/></xs:restriction></xs:simpleType>",
        "src-pattern-value"
    )]
    public async Task Representation_Violation_IsOneLocatedErrorOfItsConstraint(string body, string constraint)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(compilation.Result.Issues.Count).IsEqualTo(1);
        await Assert.That(compilation.Result.Issues[0].Constraint).StartsWith(constraint);
        await Assert.That(compilation.Result.Issues[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Compile_ASchemaThatUsesEveryConstructOfTheLanguage_HasNoIssue()
    {
        var compilation = Compile(
            Schema(
                "<xs:annotation><xs:documentation>All of it.</xs:documentation></xs:annotation>"
                    + "<xs:notation name=\"n\" public=\"p\"/>"
                    + "<xs:element name=\"doc\" type=\"t:docType\"><xs:key name=\"k\"><xs:selector xpath=\"t:item\"/><xs:field xpath=\"@id\"/></xs:key></xs:element>"
                    + "<xs:complexType name=\"docType\" mixed=\"true\"><xs:sequence>"
                    + "<xs:element name=\"item\" type=\"t:itemType\" minOccurs=\"0\" maxOccurs=\"unbounded\" nillable=\"true\"/>"
                    + "<xs:any namespace=\"##other\" processContents=\"lax\" minOccurs=\"0\"/>"
                    + "</xs:sequence><xs:anyAttribute processContents=\"skip\"/></xs:complexType>"
                    + "<xs:complexType name=\"itemType\"><xs:simpleContent><xs:extension base=\"t:small\"><xs:attribute name=\"id\" type=\"xs:ID\" use=\"required\"/></xs:extension></xs:simpleContent></xs:complexType>"
                    + "<xs:simpleType name=\"small\"><xs:restriction base=\"xs:integer\"><xs:minInclusive value=\"1\"/><xs:maxInclusive value=\"9\"/><xs:totalDigits value=\"1\"/></xs:restriction></xs:simpleType>"
                    + "<xs:simpleType name=\"color\" final=\"restriction\"><xs:restriction base=\"xs:string\"><xs:enumeration value=\"red\"/><xs:enumeration value=\"blue\"/><xs:pattern value=\"[a-z]+\"/></xs:restriction></xs:simpleType>"
                    + "<xs:simpleType name=\"colors\"><xs:list itemType=\"t:color\"/></xs:simpleType>"
                    + "<xs:simpleType name=\"colorsOrNumber\"><xs:union memberTypes=\"t:colors xs:int\"/></xs:simpleType>"
            )
        );

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(compilation.IsValid).IsTrue();
        await Assert.That(compilation.Model!.ProgramCount).IsGreaterThan(0);
    }
}
