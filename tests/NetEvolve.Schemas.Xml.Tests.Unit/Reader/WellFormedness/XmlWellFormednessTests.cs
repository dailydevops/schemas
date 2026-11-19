namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.WellFormedness;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// One test per well-formedness constraint of XML 1.0 (Fifth Edition) for documents without a document type declaration
/// (sections 2.1, 3.1, and 4.1): each asserts the error code and the position of the offending construct.
/// </summary>
public class XmlWellFormednessTests
{
    private static async Task AssertFatalAsync(string xml, XmlErrorCode code, int line, int column, long offset)
    {
        var run = ReaderHarness.Run(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsSameReferenceAs(code);
        await Assert.That(run.ErrorPosition).IsEqualTo(new XmlPosition(line, column, offset));
    }

    // Section 2.1: exactly one root element, nothing but Misc after it, no content before it.
    [Test]
    [Arguments("<a/><b/>", 1, 5, 4)]
    [Arguments("<a></a>\n<b/>", 2, 1, 8)]
    [Arguments("<a/><!--c--><?p?> <b/>", 1, 19, 18)]
    [Arguments("", 1, 1, 0)]
    [Arguments("  \n", 2, 1, 3)]
    [Arguments("<!--c--><?p?>", 1, 14, 13)]
    public async Task Document_WithoutExactlyOneRoot_IsFatalXml0012(string xml, int line, int column, long offset)
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0012, line, column, offset);
    }

    [Test]
    [Arguments("<a/>text", 1, 5, 4)]
    [Arguments("<a/>&amp;", 1, 5, 4)]
    [Arguments("<a/><![CDATA[x]]>", 1, 5, 4)]
    [Arguments("text<a/>", 1, 1, 0)]
    [Arguments("&#32;<a/>", 1, 1, 0)]
    [Arguments("<![CDATA[x]]><a/>", 1, 1, 0)]
    public async Task Document_WithContentOutsideTheRoot_IsFatalXml0018(string xml, int line, int column, long offset)
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0018, line, column, offset);
    }

    [Test]
    [Arguments("<a/>\n<!--c-->\n<?p x?>\n", "SD()|<a|>|/a|C'c'|PI(p,x)|ED")]
    [Arguments("\n<!--c--><?p?>\n<a>x</a>\t<?q?>", "SD()|C'c'|PI(p,)|<a|>|T'x'|/a|PI(q,)|ED")]
    public async Task Document_WithMiscAroundTheRoot_IsWellFormed(string xml, string expected)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    // Section 3.1, Element Type Match.
    [Test]
    [Arguments("<a>x</b>", 1, 5, 4)]
    [Arguments("<a><b></a>", 1, 7, 6)]
    [Arguments("<a>\n\n  <b>\n</c></a>", 4, 1, 11)]
    [Arguments("<a></A>", 1, 4, 3)]
    [Arguments("</a>", 1, 1, 0)]
    [Arguments("<a/></a>", 1, 5, 4)]
    public async Task ElementTypeMatch_EndTagWithADifferentName_IsFatalXml0005(
        string xml,
        int line,
        int column,
        long offset
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0005, line, column, offset);
    }

    // Section 3.1, Unique Att Spec.
    [Test]
    [Arguments("<a x='1' x='2'/>", 1, 10, 9)]
    [Arguments("<a x='1' y='2' x='3'></a>", 1, 16, 15)]
    [Arguments("<a\n  x='1'\n  y='2' x='3'/>", 3, 9, 19)]
    [Arguments("<a><b x='1' x='1'/></a>", 1, 13, 12)]
    public async Task UniqueAttSpec_RepeatedAttributeName_IsFatalXml0006(string xml, int line, int column, long offset)
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0006, line, column, offset);
    }

    // Section 3.1, No < in Attribute Values (a reference to &lt; is fine).
    [Test]
    [Arguments("<a x='<'/>", 1, 7, 6)]
    [Arguments("<a x='a<b'/>", 1, 8, 7)]
    [Arguments("<a x=\"<\"/>", 1, 7, 6)]
    [Arguments("<a x='1'\n y='&amp;<'/>", 2, 10, 18)]
    public async Task NoLessThanInAttributeValues_LiteralLessThan_IsFatalXml0013(
        string xml,
        int line,
        int column,
        long offset
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0013, line, column, offset);
    }

    [Test]
    public async Task NoLessThanInAttributeValues_ReferenceToLessThan_IsWellFormed()
    {
        var run = ReaderHarness.Run("<a x='&lt;&#60;&#x3C;'/>");

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo("SD()|<a|@x=<<<|>|/a|ED");
    }

    // Section 4.1 and production 2, Legal Character: a literal character outside Char, wherever it occurs.
    [Test]
    [Arguments("<a>\u0001</a>", 1, 4, 3)]
    [Arguments("<a x='\u0001'/>", 1, 7, 6)]
    [Arguments("<a/>\u0001", 1, 5, 4)]
    [Arguments("\u0001<a/>", 1, 1, 0)]
    [Arguments("<a><![CDATA[\u0001]]></a>", 1, 13, 12)]
    [Arguments("<a><!--\u0001--></a>", 1, 8, 7)]
    [Arguments("<a><?p \u0001?></a>", 1, 8, 7)]
    [Arguments("<a>\uFFFE</a>", 1, 4, 3)]
    [Arguments("<a>\uFFFF</a>", 1, 4, 3)]
    [Arguments("<a>\uD800</a>", 1, 4, 3)]
    [Arguments("<a>\uDC00x</a>", 1, 4, 3)]
    [Arguments("<a>\n\u0000</a>", 2, 1, 4)]
    public async Task LegalCharacter_LiteralCharacterOutsideChar_IsFatalXml0002(
        string xml,
        int line,
        int column,
        long offset
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0002, line, column, offset);
    }

    [Test]
    [Arguments("<a>&#1;</a>", 1, 4, 3)]
    [Arguments("<a x='&#0;'/>", 1, 7, 6)]
    [Arguments("<a>&#xD800;</a>", 1, 4, 3)]
    [Arguments("<a>&#xFFFE;</a>", 1, 4, 3)]
    [Arguments("<a>&#x110000;</a>", 1, 4, 3)]
    public async Task LegalCharacter_ReferenceToACharacterOutsideChar_IsFatalXml0009(
        string xml,
        int line,
        int column,
        long offset
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0009, line, column, offset);
    }

    [Test]
    public async Task LegalCharacter_CharactersOfTheCharProduction_AreWellFormed()
    {
        var run = ReaderHarness.Run("<a>\t\n\u0085퟿�😀</a>");

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
    }

    // Section 2.4: the sequence ]]> is not allowed in character data.
    [Test]
    [Arguments("<a>]]></a>", 1, 4, 3)]
    [Arguments("<a>x]]>y</a>", 1, 5, 4)]
    [Arguments("<a>\n]]></a>", 2, 1, 4)]
    public async Task CharacterData_CdataSectionEnd_IsFatalXml0004(string xml, int line, int column, long offset)
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0004, line, column, offset);
    }

    [Test]
    public async Task CharacterData_CdataSectionEndInAnAttributeValueOrBrokenUp_IsWellFormed()
    {
        var run = ReaderHarness.Run("<a x=']]>'>]]&gt;</a>");

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo("SD()|<a|@x=]]>|>|T']]>'|/a|ED");
    }

    // Section 2.5: the string -- is not allowed in a comment, and a comment cannot end in --->.
    [Test]
    [Arguments("<a><!-- a -- b --></a>", 1, 11, 10)]
    [Arguments("<a><!-- a ---></a>", 1, 11, 10)]
    [Arguments("<a><!--a--->", 1, 9, 8)]
    public async Task Comment_DoubleHyphenInside_IsFatalXml0014(string xml, int line, int column, long offset)
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0014, line, column, offset);
    }

    // Section 2.6: the target names xml in any case are reserved; the XML declaration is only valid at the very start.
    [Test]
    [Arguments("<?XML x?><a/>", 1, 1, 0)]
    [Arguments("<a><?xMl x?></a>", 1, 4, 3)]
    [Arguments("<a/><?Xml?>", 1, 5, 4)]
    public async Task ProcessingInstruction_ReservedTargetInAnyCase_IsFatalXml0015(
        string xml,
        int line,
        int column,
        long offset
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0015, line, column, offset);
    }

    [Test]
    [Arguments("<a><?xml x?></a>", 1, 4, 3)]
    [Arguments("<?xml version='1.0'?><?xml version='1.0'?><a/>", 1, 22, 21)]
    [Arguments("<a/><?xml x?>", 1, 5, 4)]
    [Arguments(" <?xml version='1.0'?><a/>", 1, 2, 1)]
    public async Task ProcessingInstruction_XmlDeclarationAfterTheStart_IsFatalXml0007(
        string xml,
        int line,
        int column,
        long offset
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        await AssertFatalAsync(xml, XmlErrorCodes.XML0007, line, column, offset);
    }

    [Test]
    public async Task ProcessingInstruction_TargetThatOnlyStartsWithXml_IsWellFormed()
    {
        var run = ReaderHarness.Run("<?xml-stylesheet href='s'?><a><?xmlfoo?></a>");

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
    }

    // Fatal error behavior (section 1.2): after the first error the parser delivers no further event.
    [Test]
    [Arguments("<a>x]]>y<b/></a>", 4)]
    [Arguments("<a><b></a><c/>", 5)]
    [Arguments("<a/><b/>", 4)]
    [Arguments("<a x='1' x='2'><b/></a>", 1)]
    public async Task FatalError_PumpAndFeedAfterwards_DeliverNoFurtherEvent(string xml, int eventsBeforeTheError)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var limits = XmlReaderLimits.Default;
        using var parser = new XmlParser(limits);
        var sink = new RecordingXmlSink(new SpanInterningNameTable(limits.MaxDistinctNames));

        parser.Feed(xml.AsMemory(), true);
        var first = parser.Pump(ref sink);
        var diagnostic = parser.Diagnostic;
        var delivered = sink.Events.Count;

        parser.Feed("<c>more</c>".AsMemory(), true);
        var second = parser.Pump(ref sink);
        var third = parser.Pump(ref sink);

        await Assert.That(first).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(delivered).IsEqualTo(eventsBeforeTheError);
        await Assert.That(second).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(third).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(sink.Events.Count).IsEqualTo(delivered);
        await Assert.That(parser.Diagnostic).IsEqualTo(diagnostic);
    }

    [Test]
    [Arguments("<a>x]]>y<b/></a>")]
    [Arguments("<a><b></a><c/>")]
    [Arguments("<a x='<'><b/></a>")]
    public async Task FatalError_InEveryChunking_StopsAtTheSamePlaceWithTheSameEvents(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var whole = ReaderHarness.Run(xml);

        for (var chunkSize = 1; chunkSize < xml.Length; chunkSize++)
        {
            var chunked = ReaderHarness.Run(xml, chunkSize: chunkSize);

            await Assert.That(chunked.Status).IsEqualTo(XmlReadStatus.Error);
            await Assert.That(chunked.Code).IsSameReferenceAs(whole.Code);
            await Assert.That(chunked.ErrorPosition).IsEqualTo(whole.ErrorPosition);
            await Assert.That(chunked.Events).IsEqualTo(whole.Events);
        }
    }
}
