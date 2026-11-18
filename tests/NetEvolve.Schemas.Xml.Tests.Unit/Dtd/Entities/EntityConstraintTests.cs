namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entities;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>XML 1.0 sections 4.1 and 4.4: the well-formedness constraints of entity references, one error code each.</summary>
public class EntityConstraintTests
{
    [Test]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"x\">]><a>&f;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"x\">]><a x=\"&f;\"/>")]
    [Arguments("<a>&f;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&f;\">]><a>&e;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&f;\">]><a x=\"&e;\"/>")]
    [Arguments("<?xml version=\"1.0\" standalone=\"yes\"?><!DOCTYPE a SYSTEM \"a.dtd\"><a>&f;</a>")]
    [Arguments("<?xml version=\"1.0\" standalone=\"yes\"?><!DOCTYPE a [<!ENTITY % p \"\">%p;]><a>&f;</a>")]
    public async Task EntityDeclared_UndeclaredReference_RaisesXml0008(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0008");
    }

    [Test]
    [Arguments("<!DOCTYPE a SYSTEM \"a.dtd\"><a>[&f;]</a>", "<a|'[]'|/a")]
    [Arguments("<!DOCTYPE a SYSTEM \"a.dtd\"><a x=\"[&f;]\"/>", "<a|@x=[]|/a")]
    [Arguments("<!DOCTYPE a [<!ENTITY % p \"\">%p;]><a>[&f;]</a>", "<a|'[]'|/a")]
    [Arguments("<?xml version=\"1.0\" standalone=\"no\"?><!DOCTYPE a SYSTEM \"a.dtd\"><a>&f;</a>", "<a|/a")]
    public async Task EntityDeclared_UndeclaredReference_WhenTheDeclarationMayBeExternal_IsSkipped(
        string xml,
        string expected
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&e;\">]><a>&e;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"x&e;\">]><a>&e;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&f;\"><!ENTITY f \"&e;\">]><a>&e;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&f;\"><!ENTITY f \"&g;\"><!ENTITY g \"&e;\">]><a>&f;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&e;\">]><a x=\"&e;\"/>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&f;\"><!ENTITY f \"&e;\">]><a x=\"&e;\"/>")]
    public async Task NoRecursion_ReferenceToAnEntityBeingExpanded_RaisesXml0020(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0020");
    }

    [Test]
    public async Task NoRecursion_ReferencingTheSameEntityTwice_IsNotRecursion()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"x\"><!ENTITY f \"&e;&e;\">]><a>&f;&f;</a>");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|'xxxx'|/a");
    }

    [Test]
    public async Task NoRecursion_DeclaredButUnreferencedRecursiveEntity_IsIgnored()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"&e;\">]><a/>");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
    }

    [Test]
    [Arguments("<!DOCTYPE a [<!ENTITY u SYSTEM \"u.gif\" NDATA gif>]><a>&u;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY u PUBLIC \"-//U//U\" \"u.gif\" NDATA gif>]><a x=\"&u;\"/>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&u;\"><!ENTITY u SYSTEM \"u.gif\" NDATA gif>]><a>&e;</a>")]
    public async Task ParsedEntity_ReferenceToAnUnparsedEntity_RaisesXml0021(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0021");
    }

    [Test]
    [Arguments("<!DOCTYPE a [<!ENTITY x SYSTEM \"x.xml\">]><a x=\"&x;\"/>")]
    [Arguments("<!DOCTYPE a [<!ENTITY x PUBLIC \"-//X//X\" \"x.xml\">]><a x=\"[&x;]\"/>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&x;\"><!ENTITY x SYSTEM \"x.xml\">]><a x=\"&e;\"/>")]
    public async Task NoExternalEntityReferences_ReferenceInAnAttributeValue_RaisesXml0022(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0022");
    }

    [Test]
    public async Task ExternalParsedEntity_ReferenceInContent_IsExternalAccessAndRaisesXml2002()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY x SYSTEM \"x.xml\">]><a>&x;</a>");

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML2002");
    }

    [Test]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"x\">]>&e;<a/>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"x\">]><a/>&e;")]
    public async Task Reference_OutsideTheRootElement_RaisesXml0018(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0018");
    }

    [Test]
    public async Task ReplacementText_OpeningAnElementWithoutClosingIt_IsRejected()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"<b>\">]><a>&e;</b></a>");

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0001");
    }

    [Test]
    public async Task ReplacementText_ClosingAnElementOpenedOutsideTheEntity_IsRejected()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"</b><b>\">]><a><b>&e;</b></a>");

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0005");
    }

    [Test]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"<b\">]><a>&e;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"<b x='1'\">]><a>&e;/></a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"<!-- c\">]><a>&e;--></a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&#60;![CDATA[\">]><a>&e;]]></a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&#38;#9\">]><a>&e;7;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&#38;\">]><a>&e;</a>")]
    public async Task ReplacementText_ThatEndsInsideAConstruct_RaisesXml0001(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0001");
    }

    [Test]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"<b a='&#60;'/>\">]><a>&e;</a>", "XML0013")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"<b a='&#38;'/>\">]><a>&e;</a>", "XML0016")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"a&#93;&#93;&#62;\">]><a>&e;</a>", "XML0004")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&#38;\">]><a>&&e;97;</a>", "XML0016")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"<?xml version='1.0'?>\">]><a>&e;</a>", "XML0007")]
    public async Task ReplacementText_ThatIsNotContent_IsRejectedWithTheTokenizerCode(string xml, string code)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo(code);
    }

    [Test]
    public async Task WithoutDtdProcessing_AnyOtherEntityReference_StillRaisesXml0008()
    {
        using var reader = XmlPipelineReader.Create("<a>&e;</a>".AsSpan());
        var sink = new RecordingXmlSink(new SpanInterningNameTable(1024));

        var status = reader.Pump(ref sink);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(reader.Diagnostic?.Code.Alias).IsEqualTo("XML0008");
    }
}
