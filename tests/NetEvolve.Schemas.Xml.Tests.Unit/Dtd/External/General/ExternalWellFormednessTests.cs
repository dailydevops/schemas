namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;

using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General.GeneralHarness;

/// <summary>The external entity must be a well-formed parsed entity (XML 1.0 section 4.3.2): balanced content and a valid text declaration.</summary>
public class ExternalWellFormednessTests
{
    [Test]
    [Arguments("<b>unclosed", "XML0001")]
    [Arguments("text</doc>", "XML0005")]
    [Arguments("</doc><doc>", "XML0005")]
    [Arguments("<b></c>", "XML0005")]
    [Arguments("<b", "XML0001")]
    [Arguments("<b a='1'", "XML0001")]
    [Arguments("<!-- never closed", "XML0001")]
    [Arguments("<![CDATA[ never closed", "XML0001")]
    [Arguments("<?pi never closed", "XML0001")]
    public async Task Entity_WithUnbalancedOrIncompleteMarkup_IsRejected(
        string entity,
        string alias,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;"), Serving(entity));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo(alias);
    }

    [Test]
    [Arguments("a&")]
    [Arguments("a & b")]
    [Arguments("a &undeclared; b")]
    public async Task Entity_WithAnInvalidReference_IsRejected(string entity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;"), Serving(entity));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsNotEqualTo("XML2002");
    }

    [Test]
    public async Task Entity_ThatClosesAnElementOpenedOutsideOfIt_IsRejected(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "<a>&e;</a>"), Serving("</a>"));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0005");
    }

    [Test]
    public async Task Entity_ThatOpensAnElementItDoesNotClose_IsRejected_EvenWhenTheDocumentClosesIt(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;</a></doc>"), Serving("<a>"));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0001");
    }

    [Test]
    public async Task Entity_Error_IsReportedAtTheOuterReference_NotInsideTheEntity(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "A&e;B"), Serving("line one\nline two <b>"));

        // The reference stands on line 2 of the document, after "<doc>A".
        await Assert.That(run.Code).IsEqualTo("XML0001");
        await Assert.That(run.ErrorPosition!.Value.Line).IsEqualTo(2);
        await Assert.That(run.ErrorPosition!.Value.Column).IsEqualTo(7);
    }

    [Test]
    [Arguments("<?xml version=\"1.0\"?>x", "XML0003")]
    [Arguments("<?xml encoding=\"UTF-8\" standalone=\"yes\"?>x", "XML0003")]
    [Arguments("<?xml encoding=\"UTF-8\"?><?xml encoding=\"UTF-8\"?>x", "XML0007")]
    [Arguments("<?xml encoding=\"KLINGON-1\"?>x", "XML4001")]
    [Arguments("<?xml version=\"1.1\" encoding=\"UTF-8\"?><b/>", "XML0010")]
    public async Task Entity_WithAnInvalidTextDeclaration_IsRejected(
        string entity,
        string alias,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;"), Serving(entity));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo(alias);
    }

    [Test]
    public async Task Entity_WithABytesThatAreNotInItsEncoding_IsXml4003_AfterTheCharactersBeforeThem(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;"), Serving(new byte[] { 0x61, 0x62, 0xFF, 0x63 }));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML4003");
        await Assert.That(run.Events).IsEqualTo("<doc|T'ab'");
    }

    [Test]
    public async Task Entity_WithACharacterThatIsNotAnXmlCharacter_IsXml0002(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;"), Serving("ab\u0001c"));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0002");
    }

    [Test]
    public async Task Entity_WithADocumentTypeDeclaration_IsRejected(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;"), Serving("<!DOCTYPE x>"));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsNotEqualTo("XML2002");
    }

    [Test]
    public async Task Entity_ThatIncludesItself_RaisesXml0020_AfterOneResolverCall(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("&e;");

        var run = Read(Doc(ExternalE, "&e;"), resolver);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0020");
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Entity_ThatIncludesItselfThroughAnInternalEntity_RaisesXml0020_AfterOneResolverCall(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("&i;");

        var run = Read(Doc("<!ENTITY i \"&e;\">" + ExternalE, "&i;"), resolver);

        await Assert.That(run.Code).IsEqualTo("XML0020");
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Entity_ThatIncludesAnotherExternalEntityThatIncludesIt_RaisesXml0020(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver()
            .Add(EntityUri, LoaderHarness.Utf8("&f;"))
            .Add("http://example.org/doc/f.ent", LoaderHarness.Utf8("&e;"));
        var declarations = ExternalE + "<!ENTITY f SYSTEM \"f.ent\">";

        var run = Read(Doc(declarations, "&e;"), resolver);

        await Assert.That(run.Code).IsEqualTo("XML0020");
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }
}
