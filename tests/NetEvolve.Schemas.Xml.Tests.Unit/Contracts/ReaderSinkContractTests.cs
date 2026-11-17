namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

public class ReaderSinkContractTests
{
    private static readonly XmlPosition ElementPosition = new(1, 1, 0);

    private static (ScriptedXmlReader Reader, RecordingXmlSink Sink, XmlEventRecord[] Script) Create(
        bool splitAfterAttribute
    )
    {
        var sink = new RecordingXmlSink();
        var names = sink.Names;
        var a = names.Intern("p", "a", "urn:p");
        var id = names.Intern("", "id", "");
        var prefix = names.Intern("p");
        var uri = names.Intern("urn:p");
        var target = names.Intern("pi");

        XmlEventRecord[] script =
        [
            new(XmlEventKind.StartDocument, Text: "1.0"),
            new(XmlEventKind.Doctype, Text: "p:a"),
            new(XmlEventKind.StartElement, a, ElementPosition),
            new(XmlEventKind.NamespaceDeclaration, Prefix: prefix, Uri: uri),
            new(XmlEventKind.Attribute, id, Text: "1"),
            new(XmlEventKind.EndAttributes),
            new(XmlEventKind.Text, Text: "x", TextKind: XmlTextKind.Text),
            new(XmlEventKind.Text, Text: "y", TextKind: XmlTextKind.CData),
            new(XmlEventKind.Text, Text: "&", TextKind: XmlTextKind.Reference),
            new(XmlEventKind.Comment, Text: " c "),
            new(XmlEventKind.ProcessingInstruction, Prefix: target, Text: "data"),
            new(XmlEventKind.EndElement, a),
            new(XmlEventKind.EndDocument),
        ];

        var reader = new ScriptedXmlReader();
        for (var i = 0; i < script.Length; i++)
        {
            reader.Event(script[i]);
            if (splitAfterAttribute && script[i].Kind == XmlEventKind.Attribute)
            {
                reader.NeedMoreData();
            }
        }

        return (reader, sink, script);
    }

    [Test]
    public async Task Pump_ScriptedEvents_RecordingSinkReceivesSameSequence()
    {
        var (reader, sink, script) = Create(splitAfterAttribute: false);
        var target = sink;

        var status = reader.Pump(ref target);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(sink.Events).IsEquivalentTo(script);
    }

    [Test]
    public async Task Pump_Script_CoversEveryEventKind()
    {
        var (_, _, script) = Create(splitAfterAttribute: false);
        var kinds = new HashSet<XmlEventKind>();
        foreach (var record in script)
        {
            kinds.Add(record.Kind);
        }

        await Assert.That(kinds).Count().IsEqualTo(Enum.GetValues<XmlEventKind>().Length);
    }

    [Test]
    public async Task Pump_NeedMoreData_StopsThenResumesWithoutLosingOrRepeatingEvents()
    {
        var (reader, sink, script) = Create(splitAfterAttribute: true);
        var target = sink;

        var first = reader.Pump(ref target);
        var deliveredBeforeResume = sink.Events.Count;
        var second = reader.Pump(ref target);

        await Assert.That(first).IsEqualTo(XmlReadStatus.NeedMoreData);
        await Assert.That(deliveredBeforeResume).IsEqualTo(5);
        await Assert.That(second).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(sink.Events).IsEquivalentTo(script);
    }

    [Test]
    public async Task Pump_AfterCompleted_StaysCompletedAndDeliversNothing()
    {
        var (reader, sink, script) = Create(splitAfterAttribute: false);
        var target = sink;
        _ = reader.Pump(ref target);

        var again = reader.Pump(ref target);

        await Assert.That(again).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(sink.Events.Count).IsEqualTo(script.Length);
    }

    [Test]
    public async Task Pump_ErrorStep_ReturnsErrorWithDiagnosticAndPosition()
    {
        var sink = new RecordingXmlSink();
        var position = new XmlPosition(3, 7, 42);
        var reader = new ScriptedXmlReader()
            .Event(new XmlEventRecord(XmlEventKind.StartDocument, Text: "1.0"))
            .Fail(XmlErrorCodes.XML0005, position)
            .Event(new XmlEventRecord(XmlEventKind.EndDocument));
        var target = sink;

        var status = reader.Pump(ref target);
        var again = reader.Pump(ref target);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(again).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(reader.Diagnostic).IsNotNull();
        await Assert.That(reader.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML0005);
        await Assert.That(reader.Diagnostic.Position).IsEqualTo(position);
        await Assert.That(reader.Position).IsEqualTo(position);
        await Assert.That(sink.Events.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Position_AfterStartElement_IsThePositionOfTheStartTag()
    {
        var (reader, sink, _) = Create(splitAfterAttribute: false);
        var target = sink;
        var before = reader.Position;

        _ = reader.Pump(ref target);

        await Assert.That(before).IsEqualTo(default(XmlPosition));
        await Assert.That(reader.Position).IsEqualTo(ElementPosition);
        await Assert.That(reader.Diagnostic).IsNull();
    }

    [Test]
    public async Task Pump_StructSink_KeepsItsStateBecauseItIsPassedByReference()
    {
        var (reader, _, script) = Create(splitAfterAttribute: true);
        var sink = new CountingStructSink(new FakeNameTable());

        _ = reader.Pump(ref sink);
        var afterFirst = sink.Count;
        _ = reader.Pump(ref sink);

        await Assert.That(afterFirst).IsEqualTo(5);
        await Assert.That(sink.Count).IsEqualTo(script.Length);
    }

    [Test]
    public async Task Sink_NamesTable_ResolvesTheHandlesOfItsEvents()
    {
        var (reader, sink, _) = Create(splitAfterAttribute: false);
        var target = sink;
        _ = reader.Pump(ref target);

        var start = sink.Events[2];

        await Assert.That(sink.Names.GetString(start.Name.Prefix)).IsEqualTo("p");
        await Assert.That(sink.Names.GetString(start.Name.Local)).IsEqualTo("a");
        await Assert.That(sink.Names.GetString(start.Name.NamespaceUri)).IsEqualTo("urn:p");
    }
}
