namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Productions 52 to 60: AttlistDecl, AttDef, AttType, DefaultDecl.</summary>
public class AttlistDeclarationTests
{
    [Test]
    [Arguments("<!ATTLIST e a CDATA #IMPLIED>", "A:e/a:CDATA:#IMPLIED")]
    [Arguments("<!ATTLIST e a ID #REQUIRED>", "A:e/a:ID:#REQUIRED")]
    [Arguments("<!ATTLIST e a IDREF #IMPLIED>", "A:e/a:IDREF:#IMPLIED")]
    [Arguments("<!ATTLIST e a IDREFS #IMPLIED>", "A:e/a:IDREFS:#IMPLIED")]
    [Arguments("<!ATTLIST e a ENTITY #IMPLIED>", "A:e/a:ENTITY:#IMPLIED")]
    [Arguments("<!ATTLIST e a ENTITIES #IMPLIED>", "A:e/a:ENTITIES:#IMPLIED")]
    [Arguments("<!ATTLIST e a NMTOKEN #IMPLIED>", "A:e/a:NMTOKEN:#IMPLIED")]
    [Arguments("<!ATTLIST e a NMTOKENS #IMPLIED>", "A:e/a:NMTOKENS:#IMPLIED")]
    [Arguments("<!ATTLIST e a (x|y|1) 'x'>", "A:e/a:(x|y|1):\"x\"")]
    [Arguments("<!ATTLIST e a ( x | y ) #REQUIRED>", "A:e/a:(x|y):#REQUIRED")]
    [Arguments("<!ATTLIST e a NOTATION (n1|n2) #IMPLIED>", "A:e/a:NOTATION(n1|n2):#IMPLIED")]
    [Arguments("<!ATTLIST e a NOTATION (n1) #IMPLIED>", "A:e/a:NOTATION(n1):#IMPLIED")]
    [Arguments("<!ATTLIST e a CDATA #FIXED \"v\">", "A:e/a:CDATA:#FIXED \"v\"")]
    [Arguments("<!ATTLIST e a CDATA 'a&amp;&#65;b%c'>", "A:e/a:CDATA:\"a&amp;&#65;b%c\"")]
    [Arguments("<!ATTLIST e a CDATA #IMPLIED b ID #REQUIRED>", "A:e/a:CDATA:#IMPLIED|A:e/b:ID:#REQUIRED")]
    [Arguments("<!ATTLIST e>", "")]
    public async Task ValidDeclaration_ProducesTheModel(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<!ATTLIST e a>")]
    [Arguments("<!ATTLIST e a FOO #IMPLIED>")]
    [Arguments("<!ATTLIST e a cdata #IMPLIED>")]
    [Arguments("<!ATTLIST e a CDATA>")]
    [Arguments("<!ATTLIST e a CDATA #FIXED>")]
    [Arguments("<!ATTLIST e a CDATA x>")]
    [Arguments("<!ATTLIST e a (x|) #IMPLIED>")]
    [Arguments("<!ATTLIST e a () #IMPLIED>")]
    [Arguments("<!ATTLIST e a NOTATION #IMPLIED>")]
    [Arguments("<!ATTLIST e a NOTATION(n) #IMPLIED>")]
    [Arguments("<!ATTLIST e a NOTATION (1n) #IMPLIED>")]
    [Arguments("<!ATTLIST e a CDATA 'a & b'>")]
    [Arguments("<!ATTLIST e a CDATA 'a&b'>")]
    [Arguments("<!ATTLIST e aCDATA #IMPLIED>")]
    [Arguments("<!ATTLISTe a CDATA #IMPLIED>")]
    public async Task InvalidDeclaration_RaisesXml0011(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task LessThanInDefaultValue_RaisesXml0013()
    {
        var (_, error) = DtdHarness.Parse("<!ATTLIST e a CDATA 'a<b'>");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0013);
    }

    [Test]
    public async Task IllegalCharacterReferenceInDefaultValue_RaisesXml0009()
    {
        var (_, error) = DtdHarness.Parse("<!ATTLIST e a CDATA '&#0;'>");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0009);
    }

    [Test]
    public async Task SecondDefinitionOfTheSameAttribute_IsIgnored_AcrossDeclarations()
    {
        var (model, error) = DtdHarness.Parse(
            "<!ATTLIST e a CDATA 'one' a CDATA 'dup'><!ATTLIST e a CDATA 'two' b ID #IMPLIED>"
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("A:e/a:CDATA:\"one\"|A:e/b:ID:#IMPLIED");
    }
}
