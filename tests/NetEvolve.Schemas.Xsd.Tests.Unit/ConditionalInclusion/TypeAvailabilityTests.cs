namespace NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion;

using static NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion.ConditionalInclusionFixture;

/// <summary>vc:typeAvailable and vc:typeUnavailable test the names against the built-in types (Structures 1.1, 4.2.2).</summary>
public class TypeAvailabilityTests
{
    [Test]
    [Arguments("typeAvailable", "xs:string", "a")]
    [Arguments("typeAvailable", "xs:anyType", "a")]
    [Arguments("typeAvailable", "xs:anySimpleType", "a")]
    [Arguments("typeAvailable", "xs:dateTime xs:integer", "a")]
    [Arguments("typeAvailable", "xs:foo", "")]
    [Arguments("typeAvailable", "xs:String", "")]
    [Arguments("typeAvailable", "xs:string xs:foo", "")]
    [Arguments("typeAvailable", "xs:foo xs:string", "")]
    [Arguments("typeAvailable", "", "a")]
    [Arguments("typeAvailable", "  ", "a")]
    [Arguments("typeUnavailable", "xs:string", "")]
    [Arguments("typeUnavailable", "xs:dateTime xs:integer", "")]
    [Arguments("typeUnavailable", "xs:foo", "a")]
    [Arguments("typeUnavailable", "xs:string xs:foo", "a")]
    [Arguments("typeUnavailable", "xs:foo xs:bar", "a")]
    [Arguments("typeUnavailable", "", "")]
    public async Task TypeAttribute_KeepsOrPrunesTheElement(string attribute, string value, string expected)
    {
        var (root, issues) = Run(Element("vc:" + attribute + "=\"" + value + "\""));

        await Assert.That(Children(root!)).IsEqualTo(expected);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ATypeOfAnotherNamespace_IsUnknown()
    {
        var text =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\""
            + Vc
            + "\" xmlns:tns=\"http://example.org/extension_types\" targetNamespace=\"http://example.org/extension_types\">"
            + "<xs:element vc:typeAvailable=\"tns:xpath_expression\" name=\"e\" type=\"tns:xpath_expression\"/>"
            + "<xs:element vc:typeUnavailable=\"tns:xpath_expression\" name=\"e\" type=\"xs:string\"/>"
            + "</xs:schema>";

        var (root, issues) = Run(text);

        await Assert.That(root!.ChildCount).IsEqualTo(1);
        await Assert.That(root.GetChildNode(0).FindAttribute("type")).IsEqualTo("xs:string");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ThePrefixIsResolvedThroughTheNamespaceDeclarationsInScope()
    {
        var text =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\""
            + Vc
            + "\" xmlns:t=\"http://www.w3.org/2001/XMLSchema\">"
            + "<xs:element name=\"onRoot\" vc:typeAvailable=\"t:string\"/>"
            + "<xs:element name=\"onElement\" xmlns:u=\"http://www.w3.org/2001/XMLSchema\" vc:typeAvailable=\"u:string\"/>"
            + "<xs:element name=\"shadowed\" xmlns:t=\"urn:other\" vc:typeAvailable=\"t:string\"/>"
            + "</xs:schema>";

        var (root, issues) = Run(text);

        await Assert.That(Children(root!)).IsEqualTo("onRoot,onElement");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnUnprefixedNameUsesTheDefaultNamespace()
    {
        var text =
            "<schema xmlns=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\""
            + Vc
            + "\"><element name=\"known\" vc:typeAvailable=\"string\"/>"
            + "<element name=\"unknown\" xmlns=\"urn:other\" vc:typeAvailable=\"string\"/></schema>";

        var (root, issues) = Run(text);

        await Assert.That(Children(root!)).IsEqualTo("known");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnUnprefixedNameWithoutADefaultNamespaceIsUnknown()
    {
        var (root, issues) = Run(Element("vc:typeAvailable=\"string\""));

        await Assert.That(root!.ChildCount).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(0);
    }
}
