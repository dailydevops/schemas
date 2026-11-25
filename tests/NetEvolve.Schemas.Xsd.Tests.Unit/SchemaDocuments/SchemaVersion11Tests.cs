namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using System;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>The XSD 1.1 elements and attributes are represented in the model; nothing is evaluated.</summary>
public class SchemaVersion11Tests
{
    private const string Xmlns = "xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"";

    [Test]
    [Arguments(
        "^<xs:schema " + Xmlns + " defaultAttributes=\"ag\" xpathDefaultNamespace=\"##targetNamespace\"/>",
        "",
        "defaultAttributes=ag;xpathDefaultNamespace=##targetNamespace"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:assert id=\"i\" test=\"a &lt; b\" xpathDefaultNamespace=\"##local\"/></xs:complexType>",
        "complexType/assert",
        "id=i;test=a < b;xpathDefaultNamespace=##local"
    )]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\">^<xs:assertion id=\"i\" test=\"$value gt 0\" xpathDefaultNamespace=\"##local\"/></xs:restriction></xs:simpleType>",
        "simpleType/restriction/assertion",
        "id=i;test=$value gt 0;xpathDefaultNamespace=##local"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:simpleContent><xs:restriction base=\"b\"><xs:assertion test=\"$value gt 0\"/>^<xs:assert test=\"1\"/></xs:restriction></xs:simpleContent></xs:complexType>",
        "complexType/simpleContent/restriction/assert",
        "test=1"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:simpleContent><xs:restriction base=\"b\">^<xs:assertion test=\"$value gt 0\"/></xs:restriction></xs:simpleContent></xs:complexType>",
        "complexType/simpleContent/restriction/assertion",
        "test=$value gt 0"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:simpleContent><xs:extension base=\"b\">^<xs:assert test=\"1\"/></xs:extension></xs:simpleContent></xs:complexType>",
        "complexType/simpleContent/extension/assert",
        "test=1"
    )]
    [Arguments(
        Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:dateTime\">^<xs:explicitTimezone id=\"i\" value=\"required\" fixed=\"true\"/></xs:restriction></xs:simpleType>",
        "simpleType/restriction/explicitTimezone",
        "id=i;value=required;fixed=true"
    )]
    [Arguments(
        Open
            + "  <xs:element name=\"e\">^<xs:alternative id=\"i\" test=\"@k = 1\" type=\"t\" xpathDefaultNamespace=\"##defaultNamespace\"/></xs:element>",
        "element/alternative",
        "id=i;test=@k = 1;type=t;xpathDefaultNamespace=##defaultNamespace"
    )]
    [Arguments(
        Open
            + "  <xs:element name=\"e\"><xs:alternative test=\"true()\">^<xs:complexType/></xs:alternative></xs:element>",
        "element/alternative/complexType",
        ""
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:openContent id=\"i\" mode=\"suffix\"><xs:any/></xs:openContent></xs:complexType>",
        "complexType/openContent",
        "id=i;mode=suffix"
    )]
    [Arguments(
        Open
            + "  ^<xs:defaultOpenContent id=\"i\" appliesToEmpty=\"true\" mode=\"interleave\"><xs:any namespace=\"##other\"/></xs:defaultOpenContent>",
        "defaultOpenContent",
        "id=i;appliesToEmpty=true;mode=interleave"
    )]
    [Arguments(
        Open
            + "  ^<xs:override id=\"i\" schemaLocation=\"o.xsd\"><xs:element name=\"e\"/><xs:attribute name=\"a\"/><xs:notation name=\"n\"/></xs:override>",
        "override",
        "id=i;schemaLocation=o.xsd"
    )]
    [Arguments(
        Open
            + "  <xs:override schemaLocation=\"o.xsd\">^<xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:override>",
        "override/simpleType",
        "name=t"
    )]
    [Arguments(
        Open + "  ^<xs:element name=\"e\" targetNamespace=\"urn:o\"/>",
        "element",
        "name=e;targetNamespace=urn:o"
    )]
    [Arguments(
        Open + "  ^<xs:attribute name=\"a\" targetNamespace=\"urn:o\" inheritable=\"true\"/>",
        "attribute",
        "name=a;targetNamespace=urn:o;inheritable=true"
    )]
    [Arguments(
        Open + "  ^<xs:complexType name=\"c\" defaultAttributesApply=\"false\"/>",
        "complexType",
        "name=c;defaultAttributesApply=false"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:sequence>^<xs:any notNamespace=\"##local urn:x\" notQName=\"##defined a\"/></xs:sequence></xs:complexType>",
        "complexType/sequence/any",
        "notNamespace=##local urn:x;notQName=##defined a"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:anyAttribute notNamespace=\"urn:x\" notQName=\"##defined\"/></xs:complexType>",
        "complexType/anyAttribute",
        "notNamespace=urn:x;notQName=##defined"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:complexContent><xs:extension base=\"b\">^<xs:assert test=\"1\"/></xs:extension></xs:complexContent></xs:complexType>",
        "complexType/complexContent/extension/assert",
        "test=1"
    )]
    [Arguments(Open + "  <xs:element name=\"e\">^<xs:key ref=\"k\"/></xs:element>", "element/key", "ref=k")]
    public async Task Syntax11_IsRepresentedInTheModel(string marked, string path, string attributes)
    {
        ArgumentNullException.ThrowIfNull(marked);
        ArgumentNullException.ThrowIfNull(path);

        var (text, line, column) = Unmark(marked);

        var (document, issues) = Parse(text);
        var node = Find(document, path);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Attributes(node)).IsEqualTo(attributes);
        await Assert.That(node.Location.Line).IsEqualTo(line);
        await Assert.That(node.Location.Column).IsEqualTo(column);
    }
}
