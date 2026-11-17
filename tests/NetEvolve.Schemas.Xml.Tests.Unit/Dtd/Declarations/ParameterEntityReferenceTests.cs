namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Section 2.8 and 4.1: parameter-entity references between declarations; WFC PEs in Internal Subset.</summary>
public class ParameterEntityReferenceTests
{
    [Test]
    public async Task ReferenceBetweenDeclarations_IsAcceptedAndIncludesTheReplacementText()
    {
        var (model, error) = DtdHarness.Parse("<!ENTITY % d \"<!ELEMENT a EMPTY>\"> %d; <!ELEMENT b ANY>");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|E:b:ANY|PE:d=\"<!ELEMENT a EMPTY>\"");
    }

    [Test]
    public async Task ReferenceInReplacementText_ThroughCharacterReference_IsExpandedToo()
    {
        var (model, error) = DtdHarness.Parse(
            "<!ENTITY % inner \"<!ENTITY g 'v'>\"><!ENTITY % outer \"&#37;inner;\">%outer;"
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).Contains("GE:g=\"v\"");
    }

    [Test]
    [Arguments("<!ENTITY g \"a%p;b\">")]
    [Arguments("<!ENTITY % q \"a%p;b\">")]
    [Arguments("<!ELEMENT a %c;>")]
    [Arguments("<!ELEMENT a (b|%c;)>")]
    [Arguments("<!ATTLIST e %x; CDATA #IMPLIED>")]
    [Arguments("<!ATTLIST e a CDATA #IMPLIED %x;>")]
    [Arguments("<!NOTATION %n; SYSTEM \"x\">")]
    [Arguments("<!ENTITY g SYSTEM \"s\" %n;>")]
    public async Task ReferenceInsideADeclaration_RaisesXml0019(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0019);
    }

    [Test]
    public async Task ReferenceInsideADeclaration_IsAnErrorEvenWhenTheEntityIsDeclared()
    {
        var (_, error) = DtdHarness.Parse("<!ENTITY % c \"EMPTY\"><!ELEMENT a %c;>");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0019);
    }

    [Test]
    public async Task UndeclaredReference_WithoutExternalSubset_RaisesXml0008()
    {
        var (_, error) = DtdHarness.Parse("%missing;");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0008);
    }

    [Test]
    public async Task UndeclaredReference_WithExternalSubset_IsSkipped()
    {
        var (model, error) = DtdHarness.Parse("%missing;<!ELEMENT a EMPTY>", systemId: "x.dtd");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY");
    }

    [Test]
    public async Task ReferenceToAnExternalEntity_IsSkipped()
    {
        var (model, error) = DtdHarness.Parse("<!ENTITY % e SYSTEM \"x.ent\">%e;");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("PE:e - x.ent");
    }

    [Test]
    [Arguments("<!ENTITY % p \"x\">%p")]
    [Arguments("<!ENTITY % p \"x\">% p;")]
    public async Task MalformedReference_RaisesXml0011(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task ReplacementTextWithAPartialDeclaration_RaisesXml0011()
    {
        var (_, error) = DtdHarness.Parse("<!ENTITY % d \"<!ELEMENT a\">%d;");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task SelfReferencingEntity_RaisesTheEntityDepthLimit()
    {
        var (_, error) = DtdHarness.Parse("<!ENTITY % a \"&#37;a;\">%a;");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1110);
    }

    [Test]
    public async Task ExpansionBeyondTheCharacterBudget_RaisesXml1109()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(20);
        var subset = "<!ENTITY % d \"<!ELEMENT a EMPTY><!ELEMENT b EMPTY>\">%d;%d;";

        var (_, error) = DtdHarness.Parse(subset, limits: limits);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1109);
    }
}
