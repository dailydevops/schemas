namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The doctype handler and entity input stack seams, exercised with fakes.</summary>
public class XmlParserSeamTests
{
    private const string FullDoctype =
        "<!DOCTYPE note PUBLIC \"-//X//Y\" 'http://x/y.dtd' [<!ENTITY a \"]\"><!-- ] --><?p ] ?>]>\r\n<note/>";

    [Test]
    public async Task Doctype_WithHandler_CallsTheSeamWithNameIdentifiersAndSubset()
    {
        var handler = new FakeDoctypeHandler();
        var run = ReaderHarness.Run(FullDoctype, doctype: handler);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo("SD()|DT(note)|<note|>|/note|ED");
        await Assert.That(handler.Calls.Count).IsEqualTo(1);
        await Assert.That(handler.Calls[0].Name).IsEqualTo("note");
        await Assert.That(handler.Calls[0].PublicId).IsEqualTo("-//X//Y");
        await Assert.That(handler.Calls[0].SystemId).IsEqualTo("http://x/y.dtd");
        await Assert.That(handler.Calls[0].Subset).IsEqualTo("<!ENTITY a \"]\"><!-- ] --><?p ] ?>");
    }

    [Test]
    [Arguments("<!DOCTYPE a><a/>", "a", "", "", "")]
    [Arguments("<!DOCTYPE a SYSTEM \"s.dtd\"><a/>", "a", "", "s.dtd", "")]
    [Arguments("<!DOCTYPE a [<!ELEMENT a EMPTY>]><a/>", "a", "", "", "<!ELEMENT a EMPTY>")]
    [Arguments("<!DOCTYPE  a \r\n [ ] ><a/>", "a", "", "", " ")]
    public async Task Doctype_WithHandler_PassesEachPart(
        string xml,
        string name,
        string publicId,
        string systemId,
        string subset
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        var handler = new FakeDoctypeHandler();
        var run = ReaderHarness.Run(xml, doctype: handler);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(handler.Calls[0]).IsEqualTo((name, publicId, systemId, subset));
    }

    [Test]
    public async Task Doctype_WithHandler_SplitAtEveryOffset_GivesTheSameEventsAndArguments()
    {
        var expectedHandler = new FakeDoctypeHandler();
        var expected = ReaderHarness.Run(FullDoctype, doctype: expectedHandler);

        for (var split = 0; split <= FullDoctype.Length; split++)
        {
            var handler = new FakeDoctypeHandler();
            var run = ReaderHarness.Run(FullDoctype, split: split, doctype: handler);

            await Assert.That(run).IsEqualTo(expected);
            await Assert.That(handler.Calls[0]).IsEqualTo(expectedHandler.Calls[0]);
        }
    }

    [Test]
    public async Task Doctype_HandlerError_IsReportedAtTheDeclaration()
    {
        var handler = new FakeDoctypeHandler(XmlErrorCodes.XML2002);
        var run = ReaderHarness.Run("<!--c-->\n<!DOCTYPE a SYSTEM \"x\"><a/>", doctype: handler);

        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML2002);
        await Assert.That(run.ErrorPosition).IsEqualTo(new XmlPosition(2, 1, 9));
    }

    [Test]
    public async Task Doctype_WithoutHandler_ReportsXml2001AtTheOpenerWithoutScanning()
    {
        // Nothing after the opener is scanned, so the unterminated declaration is still XML2001.
        var run = ReaderHarness.Run("\n<!DOCTYPE a [ \"unterminated");

        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML2001);
        await Assert.That(run.ErrorPosition).IsEqualTo(new XmlPosition(2, 1, 1));
    }

    [Test]
    [Arguments("<a/><!DOCTYPE a><b/>")]
    [Arguments("<a><!DOCTYPE a></a>")]
    [Arguments("<!DOCTYPE a><!DOCTYPE a><a/>")]
    public async Task Doctype_OutsideTheProlog_OrTwice_ReportsXml0011(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml, doctype: new FakeDoctypeHandler());

        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    [Arguments("<!DOCTYPE>")]
    [Arguments("<!DOCTYPE a SYSTEM>")]
    [Arguments("<!DOCTYPE a b>")]
    [Arguments("<!DOCTYPE a [>")]
    public async Task Doctype_WithHandler_MalformedDeclaration_ReportsAnError(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml, doctype: new FakeDoctypeHandler());

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
    }

    [Test]
    public async Task EntityReference_WithStack_CallsTheSeamWithNameAndOuterPosition()
    {
        var stack = new FakeEntityInputStack();
        var run = ReaderHarness.Run("<a>\n x&foo;<b y=\"&bar;\"/></a>", stack: stack);

        // The stack accepts the first reference but no frame can be entered yet, so the reader reports XML0008.
        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML0008);
        await Assert.That(stack.Calls.Count).IsEqualTo(1);
        await Assert.That(stack.Calls[0]).IsEqualTo(("foo", new XmlPosition(2, 3, 6)));
    }

    [Test]
    public async Task EntityReference_InAnAttributeValue_CallsTheSeam()
    {
        var stack = new FakeEntityInputStack();
        var run = ReaderHarness.Run("<a y=\"&bar;\"/>", stack: stack);

        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML0008);
        await Assert.That(stack.Calls[0]).IsEqualTo(("bar", new XmlPosition(1, 7, 6)));
    }

    [Test]
    public async Task EntityReference_StackError_IsReportedWithItsCode()
    {
        var stack = new FakeEntityInputStack(XmlErrorCodes.XML1110);
        var run = ReaderHarness.Run("<a>&foo;</a>", stack: stack);

        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML1110);
    }

    [Test]
    public async Task EntityReference_PredefinedAndCharacterReferences_DoNotCallTheSeam()
    {
        var stack = new FakeEntityInputStack();
        var run = ReaderHarness.Run("<a>&lt;&amp;&#65;</a>", stack: stack);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(stack.Calls.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EntityReference_WithoutStack_ReportsXml0008()
    {
        var run = ReaderHarness.Run("<a>&foo;</a>");

        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML0008);
    }
}
