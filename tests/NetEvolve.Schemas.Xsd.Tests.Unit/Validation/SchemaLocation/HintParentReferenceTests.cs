namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

public class HintParentReferenceTests
{
    private const string ConfiguredSchema =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\""
        + HintFixture.Base
        + "\" elementFormDefault=\"qualified\">"
        + "<xs:element name=\"r\"><xs:complexType><xs:sequence>"
        + "<xs:any namespace=\"##other\" processContents=\"lax\" minOccurs=\"0\" maxOccurs=\"unbounded\"/>"
        + "</xs:sequence></xs:complexType></xs:element>"
        + "<xs:element name=\"g\" type=\"xs:int\"/>"
        + "<xs:attribute name=\"at\" type=\"xs:int\"/>"
        + "<xs:attributeGroup name=\"ag\"><xs:attribute name=\"b\" type=\"xs:int\"/></xs:attributeGroup>"
        + "<xs:group name=\"gr\"><xs:sequence><xs:element name=\"q\" type=\"xs:int\"/></xs:sequence></xs:group>"
        + "<xs:complexType name=\"T\"><xs:sequence><xs:element name=\"a\" type=\"xs:int\"/></xs:sequence></xs:complexType>"
        + "<xs:simpleType name=\"S\"><xs:restriction base=\"xs:int\"><xs:maxInclusive value=\"9\"/></xs:restriction></xs:simpleType>"
        + "</xs:schema>";

    private static string Hinted(string body) =>
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" xmlns:t=\""
        + HintFixture.Base
        + "\" targetNamespace=\""
        + HintFixture.Hinted
        + "\" elementFormDefault=\"qualified\"><xs:import namespace=\""
        + HintFixture.Base
        + "\"/>"
        + body
        + "</xs:schema>";

    private static ValidationResult Run(string body, string content)
    {
        var resolver = new MapResolver().Add(HintFixture.HintedId, Hinted(body));
        var validator = HintFixture.Build(ConfiguredSchema, resolver);
        return HintFixture.Run(
            validator,
            HintFixture.BaseRoot("xsi:schemaLocation=\"" + HintFixture.Hinted + " h.xsd\"", content)
        );
    }

    [Test]
    public async Task Type_OfTheConfiguredSchema_IsUsed()
    {
        const string body = "<xs:element name=\"root\" type=\"t:T\"/>";
        var valid = Run(body, "<h:root><a xmlns=\"" + HintFixture.Base + "\">1</a></h:root>");
        var invalid = Run(body, "<h:root><a xmlns=\"" + HintFixture.Base + "\">x</a></h:root>");

        await Assert.That(HintFixture.Errors(valid)).IsEmpty();
        await Assert.That(HintFixture.Errors(invalid)).IsNotEmpty();
    }

    [Test]
    public async Task SimpleType_OfTheConfiguredSchema_IsBase()
    {
        const string body = "<xs:element name=\"root\" type=\"t:S\"/>";

        await Assert.That(HintFixture.Errors(Run(body, "<h:root>5</h:root>"))).IsEmpty();
        await Assert.That(HintFixture.Errors(Run(body, "<h:root>50</h:root>"))).IsNotEmpty();
    }

    [Test]
    public async Task ElementRef_ToTheConfiguredSchema_IsUsed()
    {
        const string body =
            "<xs:element name=\"root\"><xs:complexType><xs:sequence><xs:element ref=\"t:g\"/></xs:sequence></xs:complexType></xs:element>";

        await Assert
            .That(HintFixture.Errors(Run(body, "<h:root><g xmlns=\"" + HintFixture.Base + "\">1</g></h:root>")))
            .IsEmpty();
        await Assert
            .That(HintFixture.Errors(Run(body, "<h:root><g xmlns=\"" + HintFixture.Base + "\">x</g></h:root>")))
            .IsNotEmpty();
    }

    [Test]
    public async Task GroupAndAttributes_OfTheConfiguredSchema_AreUsed()
    {
        const string body =
            "<xs:element name=\"root\"><xs:complexType><xs:group ref=\"t:gr\"/><xs:attribute ref=\"t:at\"/>"
            + "<xs:attributeGroup ref=\"t:ag\"/></xs:complexType></xs:element>";
        var content =
            "<h:root xmlns:t=\""
            + HintFixture.Base
            + "\" t:at=\"1\" b=\"2\"><q xmlns=\""
            + HintFixture.Base
            + "\">3</q></h:root>";

        await Assert.That(HintFixture.Errors(Run(body, content))).IsEmpty();
    }

    [Test]
    public async Task ExtensionOfAConfiguredType_IsValid()
    {
        const string body =
            "<xs:element name=\"root\"><xs:complexType><xs:complexContent><xs:extension base=\"t:T\"><xs:sequence>"
            + "<xs:element name=\"z\" type=\"xs:int\"/></xs:sequence></xs:extension></xs:complexContent></xs:complexType></xs:element>";
        var content = "<h:root><a xmlns=\"" + HintFixture.Base + "\">1</a><h:z>2</h:z></h:root>";

        await Assert.That(HintFixture.Errors(Run(body, content))).IsEmpty();
    }

    [Test]
    public async Task UnknownConfiguredName_IsAnError()
    {
        const string body = "<xs:element name=\"root\" type=\"t:Missing\"/>";

        await Assert.That(HintFixture.Errors(Run(body, "<h:root>1</h:root>"))).IsNotEmpty();
    }
}
