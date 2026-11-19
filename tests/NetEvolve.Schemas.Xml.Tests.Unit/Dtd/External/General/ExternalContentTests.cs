namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;

using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General.GeneralHarness;

/// <summary>An external general parsed entity in content (XML-62): its events arrive in place, as if the text stood there.</summary>
public class ExternalContentTests
{
    private const string Markup = "x<b a='1'>y</b>z";
    private const string MarkupEvents = "<doc|T'Ax'|<b|@a=1|T'y'|/b|T'zB'|/doc";

    [Test]
    public async Task Reference_InContent_DeliversTheEventsOfTheEntityInPlace(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "A&e;B"), Serving(Markup));
        var inline = Read(Doc(string.Empty, "A" + Markup + "B"), Serving(string.Empty));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(MarkupEvents);
        await Assert.That(run.Events).IsEqualTo(inline.Events);
    }

    [Test]
    public async Task Reference_InContent_EventsCarryTheBaseUriOfTheEntity(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "<a/>&e;<c/>"), Serving("<b>y</b>"));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert
            .That(run.Bases)
            .IsEqualTo(
                $"<doc@{DocumentUri}|<a@{DocumentUri}|/a@{DocumentUri}|<b@{EntityUri}|T@{EntityUri}|/b@{EntityUri}|<c@{DocumentUri}|/c@{DocumentUri}|/doc@{DocumentUri}"
            );
        await Assert.That(run.BaseAfterwards).IsEqualTo(DocumentUri);
    }

    [Test]
    public async Task Reference_WhenTheResolverReportsAFinalUri_ThatUriIsTheBaseUriOfTheEntity(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string final = "http://cdn.example.org/moved/e.ent";
        var run = Read(Doc(ExternalE, "&e;"), Serving("<b/>", finalUri: final));

        await Assert.That(run.Bases).IsEqualTo($"<doc@{DocumentUri}|<b@{final}|/b@{final}|/doc@{DocumentUri}");
    }

    [Test]
    public async Task Reference_RelativeSystemIdInsideAnEntity_ResolvesAgainstTheEntityThatDeclaresIt(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Both entities are declared in the document, so both identifiers resolve against the document, although the
        // reference to f stands inside the text of e (XML 1.0 section 4.2.2).
        var resolver = new RecordingResolver()
            .Add("http://example.org/doc/sub/a.ent", LoaderHarness.Utf8("<b>&f;</b>"))
            .Add("http://example.org/doc/f.ent", LoaderHarness.Utf8("inner"));
        var declarations = "<!ENTITY e SYSTEM \"sub/a.ent\"><!ENTITY f SYSTEM \"f.ent\">";

        var run = Read(Doc(declarations, "&e;"), resolver);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo("<doc|<b|T'inner'|/b|/doc");
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
        await Assert.That(resolver.Requests[0].Kind).IsEqualTo(ResourceKind.ExternalEntity);
        await Assert.That(resolver.Requests[0].ResolvedUri).IsEqualTo("http://example.org/doc/sub/a.ent");
        await Assert.That(resolver.Requests[1].ResolvedUri).IsEqualTo("http://example.org/doc/f.ent");
        await Assert.That(resolver.Requests[1].BaseUri).IsEqualTo(DocumentUri);
    }

    [Test]
    public async Task Reference_PublicAndSystemIdentifier_ReachTheResolver(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("data");
        var declarations = "<!ENTITY e PUBLIC \"-//Example//ENTITY e//EN\" \"e.ent\">";

        var run = Read(Doc(declarations, "&e;"), resolver);

        await Assert.That(run.Events).IsEqualTo("<doc|T'data'|/doc");
        await Assert.That(resolver.Requests[0].PublicId).IsEqualTo("-//Example//ENTITY e//EN");
        await Assert.That(resolver.Requests[0].Reference).IsEqualTo("e.ent");
    }

    [Test]
    public async Task Reference_InsideTheReplacementTextOfAnInternalEntity_IsReadInPlace(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var declarations = "<!ENTITY i \"[&e;]\">" + ExternalE;

        var run = Read(Doc(declarations, "&i;"), Serving("data"));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo("<doc|T'[data]'|/doc");
    }

    [Test]
    public async Task Reference_ToAnEntityThatReferencesAnInternalEntity_ExpandsIt(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var declarations = "<!ENTITY i \"in\">" + ExternalE;

        var run = Read(Doc(declarations, "&e;"), Serving("(&i;)"));

        await Assert.That(run.Events).IsEqualTo("<doc|T'(in)'|/doc");
    }

    [Test]
    public async Task Reference_TheSameEntityTwice_IsRequestedAndReadTwice(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("<b/>");

        var run = Read(Doc(ExternalE, "&e;&e;"), resolver);

        await Assert.That(run.Events).IsEqualTo("<doc|<b|/b|<b|/b|/doc");
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Entity_WithATextDeclaration_HasItConsumedAndItsLineEndsNormalized(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;"), Serving("<?xml version=\"1.0\" encoding=\"UTF-8\"?>a\r\nb\rc"));

        var inline = Read(Doc(string.Empty, "a\nb\nc"), Serving(string.Empty));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(inline.Events);
        await Assert.That(run.Events).DoesNotContain("\r");
    }

    [Test]
    public async Task Entity_InUtf16WithAByteOrderMark_LoadsIntoACharacterDocument(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "X&e;Z"), Serving(LoaderHarness.Utf16Le("Y")));

        await Assert.That(run.Events).IsEqualTo("<doc|T'XYZ'|/doc");
    }

    [Test]
    public async Task Entity_InUtf16WithATextDeclaration_LoadsIntoACharacterDocument(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "X&e;Z"), Serving(LoaderHarness.Utf16Le("<?xml encoding=\"UTF-16\"?>Y")));

        await Assert.That(run.Events).IsEqualTo("<doc|T'XYZ'|/doc");
    }

    [Test]
    public async Task Entity_StartingWithAByteOrderMarkAndAByteOrderMarkCharacter_KeepsExactlyOneAsData(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Case 014 of the suite: the byte order mark is consumed by the decoder, the second one is a character of the text.
        var bytes = new byte[] { 0xFF, 0xFE, 0xFF, 0xFE, 0x64, 0x00, 0x61, 0x00, 0x74, 0x00, 0x61, 0x00 };

        var run = Read(Doc(ExternalE, "&e;"), Serving(bytes));

        await Assert.That(run.Events).IsEqualTo("<doc|T'﻿data'|/doc");
    }

    [Test]
    public async Task Entity_Empty_AddsNoEvents(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "a&e;b"), Serving(string.Empty));

        await Assert.That(run.Events).IsEqualTo("<doc|T'ab'|/doc");
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(7)]
    [Arguments(int.MaxValue)]
    public async Task Entity_Events_AreIdenticalForEveryReadSizeOfTheResolverStream(
        int maxRead,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // A supplementary character, a CR LF, a comment, a processing instruction, a CDATA section, and references.
        const string content =
            "a\r\nb \U0001D11E <x a='1&amp;2'>t</x><!-- c --><?p d?><![CDATA[<>]]>&amp;&#65;z\r\n<y/>";
        var inline = Read(Doc(string.Empty, content), Serving(string.Empty));

        var run = Read(Doc(ExternalE, "&e;"), Serving(content, maxRead));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(inline.Events);
    }

    [Test]
    [Arguments(4096)]
    [Arguments(4095)]
    [Arguments(8191)]
    public async Task Entity_LargerThanOneChunk_GivesTheEventsOfTheInlineText(
        int unit,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Tokens of every shape straddle the chunk boundaries of the entity; the filler moves the boundary.
        var content = new StringBuilder();
        for (var i = 0; content.Length < 3 * unit; i++)
        {
            _ = content.Append("<item id='k").Append(i).Append("'>text &amp; more</item><!--c-->\n");
        }

        var text = content.ToString();
        var inline = Read(Doc(string.Empty, text), Serving(string.Empty));

        var run = Read(Doc(ExternalE, "&e;"), Serving(text));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(inline.Events);
    }

    [Test]
    public async Task Entity_CommentAndProcessingInstruction_AreDeliveredInPlace(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;"), Serving("<!--c--><?pi data?>"));

        await Assert.That(run.Events).IsEqualTo("<doc|!c|?pi data|/doc");
    }
}
