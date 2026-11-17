namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using System;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>One case per XSD 1.0 schema element: the produced node, its attributes, and its line and column.</summary>
public class SchemaElementTests
{
    private const string Xmlns = "xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"";

    [Test]
    [Arguments(
        "^<xs:schema "
            + Xmlns
            + " id=\"s\" targetNamespace=\"urn:t\" version=\"1\" attributeFormDefault=\"unqualified\" elementFormDefault=\"qualified\" blockDefault=\"#all\" finalDefault=\"extension list\" xml:lang=\"en\"/>",
        "",
        "id=s;targetNamespace=urn:t;version=1;attributeFormDefault=unqualified;elementFormDefault=qualified;blockDefault=#all;finalDefault=extension list;lang=en"
    )]
    [Arguments(Open + "  ^<xs:annotation id=\"a\"><xs:appinfo/></xs:annotation>", "annotation", "id=a")]
    [Arguments(
        Open
            + "  <xs:annotation>^<xs:appinfo source=\"urn:s\"><foreign><xs:element/></foreign></xs:appinfo></xs:annotation>",
        "annotation/appinfo",
        "source=urn:s"
    )]
    [Arguments(
        Open
            + "  <xs:annotation>^<xs:documentation source=\"urn:d\" xml:lang=\"en\">text<b/></xs:documentation></xs:annotation>",
        "annotation/documentation",
        "source=urn:d;lang=en"
    )]
    [Arguments(Open + "  ^<xs:include id=\"i\" schemaLocation=\"a.xsd\"/>", "include", "id=i;schemaLocation=a.xsd")]
    [Arguments(
        Open + "  ^<xs:import id=\"i\" namespace=\"urn:o\" schemaLocation=\"o.xsd\"/>",
        "import",
        "id=i;namespace=urn:o;schemaLocation=o.xsd"
    )]
    [Arguments(
        Open
            + "  ^<xs:redefine schemaLocation=\"b.xsd\"><xs:simpleType name=\"t\"><xs:restriction base=\"xs:string\"/></xs:simpleType></xs:redefine>",
        "redefine",
        "schemaLocation=b.xsd"
    )]
    [Arguments(
        Open
            + "  ^<xs:simpleType id=\"i\" name=\"t\" final=\"#all\"><xs:restriction base=\"xs:string\"/></xs:simpleType>",
        "simpleType",
        "id=i;name=t;final=#all"
    )]
    [Arguments(
        Open + "  <xs:simpleType name=\"t\">^<xs:restriction id=\"i\" base=\"xs:string\"/></xs:simpleType>",
        "simpleType/restriction",
        "id=i;base=xs:string"
    )]
    [Arguments(
        Open + "  <xs:simpleType name=\"t\">^<xs:list id=\"i\" itemType=\"xs:int\"/></xs:simpleType>",
        "simpleType/list",
        "id=i;itemType=xs:int"
    )]
    [Arguments(
        Open + "  <xs:simpleType name=\"t\">^<xs:union id=\"i\" memberTypes=\"xs:int xs:string\"/></xs:simpleType>",
        "simpleType/union",
        "id=i;memberTypes=xs:int xs:string"
    )]
    [Arguments(
        Open
            + "  ^<xs:complexType id=\"i\" name=\"c\" mixed=\"true\" abstract=\"false\" final=\"#all\" block=\"extension\"/>",
        "complexType",
        "id=i;name=c;mixed=true;abstract=false;final=#all;block=extension"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:simpleContent id=\"i\"><xs:extension base=\"xs:int\"/></xs:simpleContent></xs:complexType>",
        "complexType/simpleContent",
        "id=i"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:complexContent id=\"i\" mixed=\"false\"><xs:extension base=\"b\"/></xs:complexContent></xs:complexType>",
        "complexType/complexContent",
        "id=i;mixed=false"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:simpleContent>^<xs:restriction id=\"i\" base=\"b\"><xs:enumeration value=\"a\"/><xs:attribute name=\"x\"/></xs:restriction></xs:simpleContent></xs:complexType>",
        "complexType/simpleContent/restriction",
        "id=i;base=b"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:complexContent>^<xs:restriction base=\"b\"><xs:sequence/></xs:restriction></xs:complexContent></xs:complexType>",
        "complexType/complexContent/restriction",
        "base=b"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:simpleContent>^<xs:extension base=\"b\"><xs:anyAttribute/></xs:extension></xs:simpleContent></xs:complexType>",
        "complexType/simpleContent/extension",
        "base=b"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:complexContent>^<xs:extension id=\"i\" base=\"b\"><xs:choice/></xs:extension></xs:complexContent></xs:complexType>",
        "complexType/complexContent/extension",
        "id=i;base=b"
    )]
    [Arguments(Open + "  ^<xs:group id=\"i\" name=\"g\"><xs:sequence/></xs:group>", "group", "id=i;name=g")]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:sequence>^<xs:group id=\"i\" ref=\"g\" minOccurs=\"0\" maxOccurs=\"unbounded\"/></xs:sequence></xs:complexType>",
        "complexType/sequence/group",
        "id=i;ref=g;minOccurs=0;maxOccurs=unbounded"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:all id=\"i\" minOccurs=\"1\" maxOccurs=\"1\"><xs:element name=\"e\"/></xs:all></xs:complexType>",
        "complexType/all",
        "id=i;minOccurs=1;maxOccurs=1"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:choice id=\"i\" minOccurs=\"2\" maxOccurs=\"3\"><xs:any/></xs:choice></xs:complexType>",
        "complexType/choice",
        "id=i;minOccurs=2;maxOccurs=3"
    )]
    [Arguments(
        Open + "  <xs:complexType name=\"c\">^<xs:sequence id=\"i\"><xs:sequence/></xs:sequence></xs:complexType>",
        "complexType/sequence",
        "id=i"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\"><xs:sequence>^<xs:any id=\"i\" namespace=\"##other\" processContents=\"lax\" minOccurs=\"0\" maxOccurs=\"unbounded\"/></xs:sequence></xs:complexType>",
        "complexType/sequence/any",
        "id=i;namespace=##other;processContents=lax;minOccurs=0;maxOccurs=unbounded"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:anyAttribute id=\"i\" namespace=\"##any\" processContents=\"skip\"/></xs:complexType>",
        "complexType/anyAttribute",
        "id=i;namespace=##any;processContents=skip"
    )]
    [Arguments(
        Open
            + "  ^<xs:attribute id=\"i\" name=\"a\" type=\"xs:int\" use=\"required\" default=\"1\" form=\"qualified\"/>",
        "attribute",
        "id=i;name=a;type=xs:int;use=required;default=1;form=qualified"
    )]
    [Arguments(
        Open
            + "  <xs:complexType name=\"c\">^<xs:attribute ref=\"a\" use=\"prohibited\" fixed=\"2\"><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:attribute></xs:complexType>",
        "complexType/attribute",
        "ref=a;use=prohibited;fixed=2"
    )]
    [Arguments(
        Open + "  ^<xs:attributeGroup id=\"i\" name=\"ag\"><xs:attribute name=\"a\"/></xs:attributeGroup>",
        "attributeGroup",
        "id=i;name=ag"
    )]
    [Arguments(
        Open + "  <xs:complexType name=\"c\">^<xs:attributeGroup id=\"i\" ref=\"ag\"/></xs:complexType>",
        "complexType/attributeGroup",
        "id=i;ref=ag"
    )]
    [Arguments(
        Open
            + "  ^<xs:element id=\"i\" name=\"e\" type=\"t\" substitutionGroup=\"h\" default=\"d\" form=\"unqualified\" nillable=\"true\" abstract=\"true\" final=\"restriction\" block=\"#all\" minOccurs=\"0\" maxOccurs=\"2\"/>",
        "element",
        "id=i;name=e;type=t;substitutionGroup=h;default=d;form=unqualified;nillable=true;abstract=true;final=restriction;block=#all;minOccurs=0;maxOccurs=2"
    )]
    [Arguments(
        Open
            + "  <xs:element name=\"e\">^<xs:unique id=\"i\" name=\"u\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@b\"/></xs:unique></xs:element>",
        "element/unique",
        "id=i;name=u"
    )]
    [Arguments(
        Open
            + "  <xs:element name=\"e\">^<xs:key name=\"k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@b\"/></xs:key></xs:element>",
        "element/key",
        "name=k"
    )]
    [Arguments(
        Open
            + "  <xs:element name=\"e\">^<xs:keyref name=\"r\" refer=\"k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@b\"/></xs:keyref></xs:element>",
        "element/keyref",
        "name=r;refer=k"
    )]
    [Arguments(
        Open
            + "  <xs:element name=\"e\"><xs:key name=\"k\">^<xs:selector id=\"i\" xpath=\"a/b\"/><xs:field xpath=\"@b\"/></xs:key></xs:element>",
        "element/key/selector",
        "id=i;xpath=a/b"
    )]
    [Arguments(
        Open
            + "  <xs:element name=\"e\"><xs:key name=\"k\"><xs:selector xpath=\"a\"/>^<xs:field id=\"i\" xpath=\"@b\"/></xs:key></xs:element>",
        "element/key/field",
        "id=i;xpath=@b"
    )]
    [Arguments(
        Open + "  ^<xs:notation id=\"i\" name=\"n\" public=\"p\" system=\"s\"/>",
        "notation",
        "id=i;name=n;public=p;system=s"
    )]
    public async Task Element_IsProducedWithAttributesAndLocation(string marked, string path, string attributes)
    {
        ArgumentNullException.ThrowIfNull(marked);
        ArgumentNullException.ThrowIfNull(path);

        var (text, line, column) = Unmark(marked);

        var (document, issues) = Parse(text);
        var node = Find(document, path);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(node.Name.NamespaceName).IsEqualTo("http://www.w3.org/2001/XMLSchema");
        await Assert.That(node.Name.LocalName).IsEqualTo(path.Length == 0 ? "schema" : path.Split('/')[^1]);
        await Assert.That(Attributes(node)).IsEqualTo(attributes);
        await Assert.That(node.Location.Source).IsEqualTo(SystemId);
        await Assert.That(node.Location.Line).IsEqualTo(line);
        await Assert.That(node.Location.Column).IsEqualTo(column);
    }

    [Test]
    [Arguments("minExclusive", "1")]
    [Arguments("minInclusive", "1")]
    [Arguments("maxExclusive", "1")]
    [Arguments("maxInclusive", "1")]
    [Arguments("totalDigits", "1")]
    [Arguments("fractionDigits", "1")]
    [Arguments("length", "1")]
    [Arguments("minLength", "1")]
    [Arguments("maxLength", "1")]
    [Arguments("enumeration", "a")]
    [Arguments("whiteSpace", "collapse")]
    [Arguments("pattern", "[a-z]+")]
    public async Task Facet_IsProducedWithValueAndLocation(string facet, string value)
    {
        var marked =
            Open
            + "  <xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\">^<xs:"
            + facet
            + " id=\"i\" value=\""
            + value
            + "\"/></xs:restriction></xs:simpleType>";
        var (text, line, column) = Unmark(marked);

        var (document, issues) = Parse(text);
        var node = Find(document, "simpleType/restriction/" + facet);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(node.Name.LocalName).IsEqualTo(facet);
        await Assert.That(Attributes(node)).IsEqualTo("id=i;value=" + value);
        await Assert.That(node.Location.Line).IsEqualTo(line);
        await Assert.That(node.Location.Column).IsEqualTo(column);
    }

    [Test]
    public async Task Schema_FixedFacetAttribute_IsKept()
    {
        var (document, issues) = Parse(
            Open
                + "<xs:simpleType name=\"t\"><xs:restriction base=\"xs:int\"><xs:length value=\"1\" fixed=\"true\"/></xs:restriction></xs:simpleType>"
                + Close
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Attributes(Find(document, "simpleType/restriction/length"))).IsEqualTo("value=1;fixed=true");
    }

    [Test]
    public async Task Document_KeepsBaseUriAndChildrenInDocumentOrder()
    {
        var (document, _) = Parse(Open + "<xs:element name=\"a\"/>\n<xs:element name=\"b\"/>" + Close);

        await Assert.That(document.BaseUri).IsEqualTo(SystemId);
        await Assert.That(document.IsComplete).IsTrue();
        await Assert.That(document.Root!.ChildCount).IsEqualTo(2);
        await Assert.That(document.Root.GetChildNode(0).FindAttribute("name")).IsEqualTo("a");
        await Assert.That(document.Root.GetChildNode(1).FindAttribute("name")).IsEqualTo("b");
        await Assert.That(document.Root.GetChildNode(1).Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Node_ResolvesPrefixesThroughItsAncestors()
    {
        var (document, _) = Parse(
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:a=\"urn:a\" xmlns=\"urn:d\">\n<xs:element name=\"e\" xmlns:a=\"urn:b\"/></xs:schema>"
        );
        var element = Find(document, "element");

        await Assert.That(element.TryResolvePrefix("a", out var inner)).IsTrue();
        await Assert.That(inner).IsEqualTo("urn:b");
        await Assert.That(document.Root!.TryResolvePrefix("a", out var outer)).IsTrue();
        await Assert.That(outer).IsEqualTo("urn:a");
        await Assert.That(element.TryResolvePrefix(string.Empty, out var defaultNamespace)).IsTrue();
        await Assert.That(defaultNamespace).IsEqualTo("urn:d");
        await Assert.That(element.TryResolvePrefix("zz", out _)).IsFalse();
    }

    [Test]
    public async Task Attributes_OfOtherNamespaces_AreKeptWithoutIssue()
    {
        var (document, issues) = Parse(Open + "<xs:element name=\"e\" xmlns:f=\"urn:f\" f:note=\"x\"/>" + Close);

        await Assert.That(issues.Count).IsEqualTo(0);
        var element = Find(document, "element");
        await Assert.That(element.AttributeCount).IsEqualTo(2);
        await Assert.That(element.GetAttribute(1).Name.NamespaceName).IsEqualTo("urn:f");
    }
}
