namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Productions 70 to 76: EntityDecl, GEDecl, PEDecl, EntityValue, ExternalID, NDataDecl.</summary>
public class EntityDeclarationTests
{
    [Test]
    [Arguments("<!ENTITY g \"v\">", "GE:g=\"v\"")]
    [Arguments("<!ENTITY g 'v \"q\"'>", "GE:g=\"v \"q\"\"")]
    [Arguments("<!ENTITY g \"a&#65;b&amp;c\">", "GE:g=\"aAb&amp;c\"")]
    [Arguments("<!ENTITY g \"&#60;&#38;#60;\">", "GE:g=\"<&#60;\"")]
    [Arguments("<!ENTITY g \"\">", "GE:g=\"\"")]
    [Arguments("<!ENTITY g SYSTEM \"s.xml\">", "GE:g - s.xml")]
    [Arguments("<!ENTITY g PUBLIC \"-//X//Y\" 's.xml'>", "GE:g -//X//Y s.xml")]
    [Arguments("<!ENTITY g SYSTEM \"s\" NDATA n>", "GE:g - s NDATA n")]
    [Arguments("<!ENTITY  g  SYSTEM\r\n\"s\"\tNDATA  n >", "GE:g - s NDATA n")]
    [Arguments("<!ENTITY g \"a\r\nb\rc\nd\">", "GE:g=\"a\nb\nc\nd\"")]
    [Arguments("<!ENTITY g \"a&#13;&#10;b&#13;\">", "GE:g=\"a\r\nb\r\"")]
    [Arguments("<!ENTITY % p \"v\">", "PE:p=\"v\"")]
    [Arguments("<!ENTITY % p SYSTEM \"s.ent\">", "PE:p - s.ent")]
    [Arguments("<!ENTITY % p PUBLIC \"a b\" \"s\">", "PE:p a b s")]
    [Arguments("<!ENTITY g \"a\"><!ENTITY % g \"b\">", "GE:g=\"a\"|PE:g=\"b\"")]
    public async Task ValidDeclaration_ProducesTheModel(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<!ENTITY g>")]
    [Arguments("<!ENTITY>")]
    [Arguments("<!ENTITYg \"v\">")]
    [Arguments("<!ENTITY g SYSTEM>")]
    [Arguments("<!ENTITY g PUBLIC \"p\">")]
    [Arguments("<!ENTITY g PUBLIC \"p<q\" \"s\">")]
    [Arguments("<!ENTITY g SYSTEM \"s\" NDATA>")]
    [Arguments("<!ENTITY g \"v\" NDATA n>")]
    [Arguments("<!ENTITY % p SYSTEM \"s\" NDATA n>")]
    [Arguments("<!ENTITY g \"v\" x>")]
    [Arguments("<!ENTITY g \"a &b\">")]
    [Arguments("<!ENTITY g \"a &b c;\">")]
    [Arguments("<!ENTITY %p \"v\">")]
    [Arguments("<!ENTITY 1g \"v\">")]
    public async Task InvalidDeclaration_RaisesXml0011(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task IllegalCharacterReferenceInValue_RaisesXml0009()
    {
        var (_, error) = DtdHarness.Parse("<!ENTITY g \"&#xD800;\">");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0009);
    }

    [Test]
    public async Task SecondDeclarationOfTheSameEntity_IsIgnored()
    {
        var (model, error) = DtdHarness.Parse(
            "<!ENTITY g \"one\"><!ENTITY g \"two\"><!ENTITY % p \"one\"><!ENTITY % p SYSTEM \"two\">"
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("GE:g=\"one\"|PE:p=\"one\"");
    }
}
