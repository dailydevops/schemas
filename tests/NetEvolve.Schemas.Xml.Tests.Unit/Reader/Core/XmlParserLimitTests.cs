namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Core;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>One reader-level test per non-expansion limit: at the limit the document parses, one below it the limit code is reported.</summary>
public class XmlParserLimitTests
{
    private static async Task AssertLimit(
        string xml,
        Func<int, XmlReaderLimits> limits,
        int atLimit,
        XmlErrorCode code,
        IXmlDoctypeHandler? doctype = null
    )
    {
        var passes = ReaderHarness.Run(xml, limits(atLimit), doctype: doctype);
        var fails = ReaderHarness.Run(xml, limits(atLimit - 1), doctype: doctype);
        var failsSplit = ReaderHarness.Run(xml, limits(atLimit - 1), chunkSize: 1, doctype: doctype);

        await Assert.That(passes.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(fails.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(fails.Code).IsSameReferenceAs(code);
        await Assert.That(failsSplit.Code).IsSameReferenceAs(code);
    }

    private static XmlReaderLimits Base => XmlReaderLimits.Default;

    [Test]
    public async Task Limit_MaxDocumentCharacters_ReportsXml1101() =>
        await AssertLimit("<a/>", n => Base.WithMaxDocumentCharacters(n), 4, XmlErrorCodes.XML1101);

    [Test]
    public async Task Limit_MaxElementDepth_ReportsXml1102() =>
        await AssertLimit("<a><b/></a>", n => Base.WithMaxElementDepth(n), 2, XmlErrorCodes.XML1102);

    [Test]
    public async Task Limit_MaxAttributesPerElement_ReportsXml1103() =>
        await AssertLimit(
            "<a x=\"1\" y=\"2\" xmlns:p=\"u\"/>",
            n => Base.WithMaxAttributesPerElement(n),
            2,
            XmlErrorCodes.XML1103
        );

    [Test]
    public async Task Limit_MaxNamespaceDeclarationsPerElement_ReportsXml1104() =>
        await AssertLimit(
            "<a xmlns:p=\"u\" xmlns=\"v\" x=\"1\"/>",
            n => Base.WithMaxNamespaceDeclarationsPerElement(n),
            2,
            XmlErrorCodes.XML1104
        );

    [Test]
    [Arguments("<abcd/>")]
    [Arguments("<a abcd=\"1\"/>")]
    [Arguments("<a></abcd>")]
    [Arguments("<a>&abcd;</a>")]
    [Arguments("<a><?abcd?></a>")]
    public async Task Limit_MaxNameLength_ReportsXml1105(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var passes = ReaderHarness.Run(xml, Base.WithMaxNameLength(4));
        var fails = ReaderHarness.Run(xml, Base.WithMaxNameLength(3));
        var failsSplit = ReaderHarness.Run(xml, Base.WithMaxNameLength(3), chunkSize: 1);

        // The end tag and the reference of the samples are not valid documents, but they get past the length check.
        await Assert.That(passes.Code).IsNotSameReferenceAs(XmlErrorCodes.XML1105);
        await Assert.That(fails.Code).IsSameReferenceAs(XmlErrorCodes.XML1105);
        await Assert.That(failsSplit.Code).IsSameReferenceAs(XmlErrorCodes.XML1105);
    }

    [Test]
    public async Task Limit_MaxTokenLength_Comment_ReportsXml1106() =>
        await AssertLimit("<a><!--abcd--></a>", n => Base.WithMaxTokenLength(n), 4, XmlErrorCodes.XML1106);

    [Test]
    public async Task Limit_MaxTokenLength_ProcessingInstruction_ReportsXml1106() =>
        await AssertLimit("<a><?t abcd?></a>", n => Base.WithMaxTokenLength(n), 4, XmlErrorCodes.XML1106);

    [Test]
    public async Task Limit_MaxTextNodeLength_ReportsXml1107() =>
        await AssertLimit("<a>ab<![CDATA[cd]]></a>", n => Base.WithMaxTextNodeLength(n), 4, XmlErrorCodes.XML1107);

    [Test]
    public async Task Limit_MaxTextNodeLength_ResetsAtTheNextEvent()
    {
        var run = ReaderHarness.Run("<a>abcd<b/>abcd</a>", Base.WithMaxTextNodeLength(4));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
    }

    [Test]
    public async Task Limit_MaxNodeCount_ReportsXml1108() =>
        await AssertLimit("<a x=\"1\">t<!--c--></a>", n => Base.WithMaxNodeCount(n), 4, XmlErrorCodes.XML1108);

    [Test]
    public async Task Limit_MaxDistinctNames_ReportsXml1111()
    {
        // The table holds the empty string, the xml prefix and namespace, then a and b.
        var passes = ReaderHarness.Run("<a><b/></a>", names: new SpanInterningNameTable(5));
        var fails = ReaderHarness.Run("<a><b/></a>", names: new SpanInterningNameTable(4));
        var tooSmall = ReaderHarness.Run("<a/>", names: new SpanInterningNameTable(2));

        await Assert.That(passes.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(fails.Code).IsSameReferenceAs(XmlErrorCodes.XML1111);
        await Assert.That(tooSmall.Code).IsSameReferenceAs(XmlErrorCodes.XML1111);
    }

    [Test]
    public async Task Limit_FrozenNameTable_ReportsXml1116()
    {
        var table = new SpanInterningNameTable(100);
        _ = table.Intern("a".AsSpan());
        _ = table.Intern("xml".AsSpan());
        _ = table.Intern("http://www.w3.org/XML/1998/namespace".AsSpan());
        table.Freeze();

        var known = ReaderHarness.Run("<a/>", names: table);
        var unknown = ReaderHarness.Run("<b/>", names: table);

        await Assert.That(known.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(unknown.Code).IsSameReferenceAs(XmlErrorCodes.XML1116);
    }

    [Test]
    public async Task Limit_MaxAttributeValueLength_ReportsXml1112() =>
        await AssertLimit("<a x=\"ab&amp;c\"/>", n => Base.WithMaxAttributeValueLength(n), 4, XmlErrorCodes.XML1112);

    [Test]
    public async Task Limit_MaxStartTagCharacters_ReportsXml1113() =>
        await AssertLimit("<a x=\"1\"/>", n => Base.WithMaxStartTagCharacters(n), 10, XmlErrorCodes.XML1113);

    [Test]
    public async Task Limit_MaxXmlDeclarationLength_ReportsXml1114() =>
        await AssertLimit(
            "<?xml version=\"1.0\"?><a/>",
            n => Base.WithMaxXmlDeclarationLength(n),
            21,
            XmlErrorCodes.XML1114
        );

    [Test]
    public async Task Limit_MaxDoctypeLength_WithHandler_ReportsXml1115() =>
        await AssertLimit(
            "<!DOCTYPE a><a/>",
            n => Base.WithMaxDoctypeLength(n),
            12,
            XmlErrorCodes.XML1115,
            new FakeDoctypeHandler()
        );
}
