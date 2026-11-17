namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Position;
using NetEvolve.Schemas.Xml.Reader.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Resuming at every character offset must give the events, positions, and errors of unsplit input.</summary>
public class XmlParserResumeTests
{
    // Every hazard of a chunk edge: BOM, declaration, CR and CRLF, surrogate pairs in text, a name, and an attribute,
    // references, "]" and "]]" near CDATA ends, "-" near a comment end, "?" near a PI end, and white space in values.
    private const string Hazards =
        "﻿<?xml version=\"1.0\" standalone='yes'?>\r\n<!-- a - b -->\r<?pi data?>\n"
        + "<r xmlns:p=\"urn:p\" a=\"x&amp;y\t\r\nz&#10;\" p:b='😀'>\r\n  text &lt; &#x1F600; 😀 ]] ] ]>x\r"
        + "<p:e/><x😀y/><![CDATA[a]b]]c]]]]><![CDATA[\r\nq]]>t<!-- - --><?pi2 ? ??>?>d?>  </r>\r\n<!--tail-->\n";

    public static IEnumerable<Func<string>> ValidDocuments()
    {
        yield return () => Hazards;
        yield return () => "<a x=\"1\" y='2'><b/>text<!--c--><?p d?></a>";
        yield return () => "<p:a xmlns:p=\"u\"><p:b p:c=\"v\"/></p:a>";
        yield return () => "<a>&#x41;&lt;&amp;</a>";
    }

    public static IEnumerable<Func<string>> InvalidDocuments()
    {
        yield return () => "<a>\r\n<b></c></a>";
        yield return () => "<a x=\"1\"\r\n x=\"2\"/>";
        yield return () => "<a>&foo<";
        yield return () => "<a><!-- -- --></a>";
        yield return () => "<a>]]></a>";
        yield return () => "<?xml version=\"1.1\"?><a/>";
        yield return () => "<a>\r\n  <![CDATA[x";
        yield return () => "<a>\uD800</a>";
        yield return () => "<a x=\"<\"/>";
        yield return () => "<a/>\r\n<!--c--><b/>";
        yield return () => "<p:a/>";
    }

    [Test]
    [MethodDataSource(nameof(ValidDocuments))]
    public async Task Pump_SplitAtEveryOffset_GivesTheEventsOfUnsplitInput(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var expected = ReaderHarness.Run(xml);

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Completed);
        for (var split = 0; split <= xml.Length; split++)
        {
            var run = ReaderHarness.Run(xml, split: split);

            await Assert.That(run).IsEqualTo(expected);
        }
    }

    [Test]
    [MethodDataSource(nameof(ValidDocuments))]
    public async Task Pump_EvenlyChunked_GivesTheEventsOfUnsplitInput(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var expected = ReaderHarness.Run(xml);

        foreach (var size in new[] { 1, 2, 3, 5, 7 })
        {
            var run = ReaderHarness.Run(xml, chunkSize: size);

            await Assert.That(run).IsEqualTo(expected);
        }
    }

    [Test]
    [MethodDataSource(nameof(InvalidDocuments))]
    public async Task Pump_InvalidInputSplitAtEveryOffset_GivesTheSameCodeAndPosition(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var expected = ReaderHarness.Run(xml);

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Error);
        for (var split = 0; split <= xml.Length; split++)
        {
            var run = ReaderHarness.Run(xml, split: split);

            await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
            await Assert.That(run.Code).IsSameReferenceAs(expected.Code);
            await Assert.That(run.ErrorPosition).IsEqualTo(expected.ErrorPosition);
        }
    }

    [Test]
    public async Task Pump_PositionAtTheEnd_EqualsTheLineTrackerPositionAtEverySplit()
    {
        var xml = Hazards.Substring(1);
        var tracker = default(XmlLineTracker);
        _ = tracker.Normalize(xml, new char[xml.Length]);
        var expected = tracker.Position;

        for (var split = 0; split <= xml.Length; split++)
        {
            var run = ReaderHarness.Run(xml, split: split);

            await Assert.That(run.Position).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task Pump_NonFinalInputInsideAConstruct_NeedsMoreDataUntilTheConstructIsWhole()
    {
        using var parser = new XmlParser(XmlReaderLimits.Default);
        var sink = new RecordingXmlSink(new SpanInterningNameTable(100));

        parser.Feed("<a>te".AsMemory(), false);
        var first = parser.Pump(ref sink);
        parser.Feed("xt</".AsMemory(), false);
        var second = parser.Pump(ref sink);
        parser.Feed("a>".AsMemory(), true);
        var third = parser.Pump(ref sink);
        var again = parser.Pump(ref sink);

        await Assert.That(first).IsEqualTo(XmlReadStatus.NeedMoreData);
        await Assert.That(second).IsEqualTo(XmlReadStatus.NeedMoreData);
        await Assert.That(third).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(again).IsEqualTo(XmlReadStatus.Completed);
    }

    [Test]
    public async Task Pump_AfterAnError_StaysInError()
    {
        using var parser = new XmlParser(XmlReaderLimits.Default);
        var sink = new RecordingXmlSink(new SpanInterningNameTable(100));

        parser.Feed("<a></b>".AsMemory(), true);
        var first = parser.Pump(ref sink);
        var again = parser.Pump(ref sink);

        await Assert.That(first).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(again).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(parser.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML0005);
    }

    [Test]
    public async Task Pump_StructSink_KeepsItsStateThroughTheReference()
    {
        using var parser = new XmlParser(XmlReaderLimits.Default);
        var sink = new CountingStructSink(new SpanInterningNameTable(100));

        parser.Feed("<a x=\"1\">t</a>".AsMemory(), true);
        var status = parser.Pump(ref sink);

        // StartDocument, StartElement, Attribute, EndAttributes, Text, EndElement, EndDocument.
        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(sink.Count).IsEqualTo(7);
    }
}
