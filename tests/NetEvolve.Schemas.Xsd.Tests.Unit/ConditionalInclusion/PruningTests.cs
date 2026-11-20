namespace NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion.ConditionalInclusionFixture;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>An excluded element is removed with its attributes and descendants before any syntax check (Structures 1.1, 4.2.2).</summary>
public class PruningTests
{
    [Test]
    public async Task AnExcludedSubtreeWithInvalidSchemaContent_CausesNoError()
    {
        var (root, issues) = Run(
            Schema(
                "<xs:element name=\"a\" vc:minVersion=\"3.2\" bogus=\"x\">"
                    + "<xs:bogus/><xs:complexType name=\"\" nonsense=\"1\"><xs:sequence><xs:element/></xs:sequence></xs:complexType>"
                    + "<xs:unknownFutureElement vc:minVersion=\"3.2\"/>"
                    + "</xs:element>"
                    + "<xs:element name=\"b\" type=\"xs:string\"/>"
            )
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Children(root!)).IsEqualTo("b");
    }

    [Test]
    public async Task TheSameContentWithoutTheVersionAttribute_IsReportedAsSchemaErrors()
    {
        var (_, issues) = Run(
            Schema("<xs:element name=\"a\" bogus=\"x\"><xs:bogus/><xs:complexType nonsense=\"1\"/></xs:element>")
        );

        await Assert.That(issues.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ADescendantOfAnExcludedElement_IsNotEvaluated()
    {
        var (root, issues) = Run(
            Schema(
                "<xs:complexType name=\"t\" vc:minVersion=\"3.2\"><xs:sequence>"
                    + "<xs:element name=\"x\" vc:minVersion=\"not-a-number\"/>"
                    + "</xs:sequence></xs:complexType>"
            )
        );

        await Assert.That(root!.ChildCount).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnExcludedDescendantOfAKeptElement_IsRemovedAndItsSiblingsStay()
    {
        var (root, issues) = Run(
            Schema(
                "<xs:complexType name=\"t\"><xs:sequence>"
                    + "<xs:element name=\"x\"/>"
                    + "<xs:element name=\"y\" vc:maxVersion=\"1.1\"><xs:bogus/></xs:element>"
                    + "<xs:element name=\"z\" vc:minVersion=\"1.1\"/>"
                    + "</xs:sequence></xs:complexType>"
            )
        );

        var sequence = root!.GetChildNode(0).GetChildNode(0);
        await Assert.That(Children(sequence)).IsEqualTo("x,z");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TheLocationOfTheFollowingElements_IsKeptAsInTheSource()
    {
        var (root, _) = Run(
            Schema(
                "<xs:element name=\"a\" vc:minVersion=\"3.2\"/>\n<xs:element name=\"b\"/>\n<xs:element name=\"c\" vc:minVersion=\"3.2\"/>\n<xs:element name=\"d\"/>"
            )
        );

        await Assert.That(Children(root!)).IsEqualTo("b,d");
        await Assert.That(root!.GetChildNode(0).Location.Line).IsEqualTo(3);
        await Assert.That(root.GetChildNode(0).Location.Column).IsEqualTo(1);
        await Assert.That(root.GetChildNode(1).Location.Line).IsEqualTo(5);
    }

    [Test]
    public async Task SeveralVersionControlAttributes_MustAllHold()
    {
        string[] holding =
        [
            "vc:minVersion=\"1.0\"",
            "vc:maxVersion=\"2.0\"",
            "vc:typeAvailable=\"xs:string\"",
            "vc:typeUnavailable=\"xs:foo\"",
            "vc:facetAvailable=\"xs:pattern\"",
            "vc:facetUnavailable=\"xs:foo\"",
        ];
        string[] failing =
        [
            "vc:minVersion=\"1.2\"",
            "vc:maxVersion=\"1.1\"",
            "vc:typeAvailable=\"xs:foo\"",
            "vc:typeUnavailable=\"xs:string\"",
            "vc:facetAvailable=\"xs:foo\"",
            "vc:facetUnavailable=\"xs:pattern\"",
        ];

        var (kept, keptIssues) = Run(Element(string.Join(" ", holding)));
        await Assert.That(Children(kept!)).IsEqualTo("a");
        await Assert.That(keptIssues.Count).IsEqualTo(0);

        for (var i = 0; i < holding.Length; i++)
        {
            var attributes = (string[])holding.Clone();
            attributes[i] = failing[i];

            var (root, issues) = Run(Element(string.Join(" ", attributes)));

            await Assert.That(root!.ChildCount).IsEqualTo(0);
            await Assert.That(issues.Count).IsEqualTo(0);
        }
    }

    [Test]
    [Arguments("vc:minVersion=\"1.0\" vc:typeAvailable=\"xs:foo\"")]
    [Arguments("vc:typeAvailable=\"xs:string\" vc:maxVersion=\"1.1\"")]
    [Arguments("vc:facetAvailable=\"xs:pattern\" vc:typeUnavailable=\"xs:string\"")]
    [Arguments("vc:minVersion=\"2.0\" vc:maxVersion=\"3.0\"")]
    public async Task TwoVersionControlAttributesWhereOneFails_PruneTheElement(string attributes)
    {
        var (root, issues) = Run(Element(attributes));

        await Assert.That(root!.ChildCount).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TheVersionControlAttributesOfAKeptElement_StayInTheModel()
    {
        var (root, _) = Run(Element("vc:minVersion=\"1.0\" vc:typeAvailable=\"xs:string\""));

        var element = root!.GetChildNode(0);
        await Assert.That(element.AttributeCount).IsEqualTo(4);
        await Assert.That(element.GetAttribute(2).Name.NamespaceName).IsEqualTo(Vc);
        await Assert.That(element.GetAttribute(2).Name.LocalName).IsEqualTo("minVersion");
        await Assert.That(element.GetAttribute(2).Value).IsEqualTo("1.0");
        await Assert.That(element.GetAttribute(3).Value).IsEqualTo("xs:string");
    }

    [Test]
    public async Task AnExcludedRoot_KeepsOnlyTargetNamespaceAndTheVersionAttributesAndHasNoChildren()
    {
        var (root, issues) = Run(
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\""
                + Vc
                + "\" targetNamespace=\"urn:t\" elementFormDefault=\"qualified\" vc:minVersion=\"3.2\" vc:maxVersion=\"9\" bogus=\"x\">"
                + "<xs:bogus/><xs:element name=\"a\"/></xs:schema>"
        );

        await Assert.That(root!.Name.LocalName).IsEqualTo("schema");
        await Assert.That(root.ChildCount).IsEqualTo(0);
        await Assert.That(root.AttributeCount).IsEqualTo(3);
        await Assert.That(root.FindAttribute("targetNamespace")).IsEqualTo("urn:t");
        await Assert.That(root.FindAttribute("elementFormDefault")).IsNull();
        await Assert.That(root.GetAttribute(1).Name.LocalName).IsEqualTo("minVersion");
        await Assert.That(root.GetAttribute(2).Name.LocalName).IsEqualTo("maxVersion");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AKeptRoot_KeepsItsChildren()
    {
        var (root, issues) = Run(
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\""
                + Vc
                + "\" vc:minVersion=\"1.1\"><xs:element name=\"a\"/></xs:schema>"
        );

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task InStrict10Mode_TheVersionControlAttributesAreIgnored()
    {
        var (root, issues) = Run(
            Element("vc:minVersion=\"3.2\" vc:typeAvailable=\"xs:foo\" vc:facetAvailable=\"xs:foo\""),
            XsdVersion.V10
        );

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(root!.GetChildNode(0).AttributeCount).IsEqualTo(5);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ADocumentWithoutVersionControlAttributes_IsPassedThroughUnchanged()
    {
        var text = Schema(
            "<xs:annotation><xs:documentation>text</xs:documentation></xs:annotation>\n<xs:element name=\"a\" type=\"xs:string\"/>\n<xs:complexType name=\"t\"><xs:sequence><xs:element name=\"x\"/></xs:sequence></xs:complexType>"
        );

        var (filtered, filteredIssues) = Run(text);
        var (direct, directIssues) = Run(text, XsdVersion.V10);

        await Assert.That(Dump(filtered!)).IsEqualTo(Dump(direct!));
        await Assert.That(filteredIssues.Count).IsEqualTo(directIssues.Count);
    }

    [Test]
    public async Task TheVersionControlAttributeNamespace_MustMatchExactly()
    {
        var text =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\"http://www.w3.org/2007/XMLSchema-versioning/other\">"
            + "<xs:element name=\"a\" vc:minVersion=\"3.2\"/><xs:element name=\"b\" minVersion=\"3.2\"/></xs:schema>";

        var (root, _) = Run(text);

        await Assert.That(Children(root!)).IsEqualTo("a,b");
    }
}
