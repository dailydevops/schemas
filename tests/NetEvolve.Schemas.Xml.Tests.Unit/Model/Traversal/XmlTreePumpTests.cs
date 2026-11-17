namespace NetEvolve.Schemas.Xml.Tests.Unit.Model.Traversal;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Traversal;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Text.NameTable;

public class XmlTreePumpTests
{
    private static XmlEventRecord Ev(XmlEventKind kind, XmlName name = default, XmlPosition position = default) =>
        new(kind, name, position);

    private static XmlEventRecord Ns(XmlSymbol prefix, XmlSymbol uri) =>
        new(XmlEventKind.NamespaceDeclaration, Prefix: prefix, Uri: uri);

    private static XmlEventRecord Attr(XmlName name, string value) => new(XmlEventKind.Attribute, name, Text: value);

    private static XmlEventRecord Txt(string text) => new(XmlEventKind.Text, Text: text);

    private static void Replay(IEnumerable<XmlEventRecord> events, XmlTreeBuilder b)
    {
        foreach (var e in events)
        {
            switch (e.Kind)
            {
                case XmlEventKind.StartDocument:
                    b.StartDocument(default);
                    break;
                case XmlEventKind.EndDocument:
                    b.EndDocument();
                    break;
                case XmlEventKind.StartElement:
                    b.StartElement(e.Name, e.Position);
                    break;
                case XmlEventKind.NamespaceDeclaration:
                    b.NamespaceDeclaration(e.Prefix, e.Uri);
                    break;
                case XmlEventKind.Attribute:
                    b.Attribute(e.Name, e.Text);
                    break;
                case XmlEventKind.EndAttributes:
                    b.EndAttributes();
                    break;
                case XmlEventKind.EndElement:
                    b.EndElement(e.Name);
                    break;
                case XmlEventKind.Text:
                    b.Text(e.Text, XmlTextKind.Text);
                    break;
                case XmlEventKind.Comment:
                    b.Comment(e.Text);
                    break;
                case XmlEventKind.ProcessingInstruction:
                    b.ProcessingInstruction(e.Prefix, e.Text);
                    break;
                default:
                    throw new InvalidOperationException(e.Kind.ToString());
            }
        }
    }

    // <a xmlns="urn:d" xmlns:p="urn:p" id="1"><b xmlns="" xmlns:p="urn:q">t</b><!--c--><?pi d?><p:c/></a>
    private static (XmlNameTable Names, List<XmlEventRecord> Script) Sample()
    {
        var n = new SpanInterningNameTable(1000);
        var p = n.Intern("p");
        var d = n.Intern("urn:d");
        var up = n.Intern("urn:p");
        var uq = n.Intern("urn:q");
        var a = n.Intern("", "a", "urn:d");
        var b = n.Intern("", "b", "");
        var c = n.Intern("p", "c", "urn:p");
        var script = new List<XmlEventRecord>
        {
            Ev(XmlEventKind.StartDocument),
            Ev(XmlEventKind.StartElement, a, new XmlPosition(1, 1, 0)),
            Ns(default, d),
            Ns(p, up),
            Attr(n.Intern("", "id", ""), "1"),
            Ev(XmlEventKind.EndAttributes),
            Ev(XmlEventKind.StartElement, b, new XmlPosition(2, 3, 20)),
            Ns(default, default),
            Ns(p, uq),
            Ev(XmlEventKind.EndAttributes),
            Txt("t"),
            Ev(XmlEventKind.EndElement, b),
            new(XmlEventKind.Comment, Text: "c"),
            new(XmlEventKind.ProcessingInstruction, Prefix: n.Intern("pi"), Text: "d"),
            Ev(XmlEventKind.StartElement, c, new XmlPosition(3, 5, 40)),
            Ev(XmlEventKind.EndAttributes),
            Ev(XmlEventKind.EndElement, c),
            Ev(XmlEventKind.EndElement, a),
            Ev(XmlEventKind.EndDocument),
        };
        return (n, script);
    }

    private static XmlDocumentTree Build(XmlNameTable names, IEnumerable<XmlEventRecord> script)
    {
        using var builder = new XmlTreeBuilder(names);
        Replay(script, builder);
        return builder.Complete();
    }

    [Test]
    public async Task Pump_Document_YieldsRecordedEventStream()
    {
        var (names, script) = Sample();
        var tree = Build(names, script);
        var sink = new RecordingXmlSink(names);

        XmlTreePump.Pump(tree.Document, sink);

        await Assert.That(sink.Events.SequenceEqual(script)).IsTrue();
    }

    [Test]
    public async Task Pump_Subtree_StartsWithItsRootAndContainsOnlyDescendants()
    {
        var (names, script) = Sample();
        var tree = Build(names, script);
        var b = tree.Document.FirstChild.FirstChild;
        var sink = new RecordingXmlSink(names);

        XmlTreePump.Pump(b, sink);

        await Assert.That(sink.Events[0].Kind).IsEqualTo(XmlEventKind.StartElement);
        await Assert.That(sink.Events[^1].Kind).IsEqualTo(XmlEventKind.EndElement);
        await Assert.That(sink.Events.Count(e => e.Kind is XmlEventKind.StartElement)).IsEqualTo(1);
        await Assert.That(sink.Events.Any(e => e.Kind is XmlEventKind.StartDocument or XmlEventKind.Comment)).IsFalse();
    }

    [Test]
    public async Task Pump_SubtreeUsingAncestorPrefix_DeclaresItOnItsRoot()
    {
        var (names, script) = Sample();
        var tree = Build(names, script);
        var c = tree.Document.FirstChild.FirstChild.NextSibling.NextSibling.NextSibling;
        var sink = new RecordingXmlSink(names);

        XmlTreePump.Pump(c, sink);

        var declarations = sink.Events.Where(e => e.Kind == XmlEventKind.NamespaceDeclaration).ToArray();
        await Assert.That(declarations.Length).IsEqualTo(2);
        await Assert
            .That(declarations.Any(e => e.Prefix == names.Intern("p") && e.Uri == names.Intern("urn:p")))
            .IsTrue();
    }

    [Test]
    public async Task Pump_IntoSinkWithOtherNameTable_TranslatesSymbols()
    {
        var (names, script) = Sample();
        var tree = Build(names, script);
        var sink = new RecordingXmlSink(new SpanInterningNameTable(1000));

        XmlTreePump.Pump(tree.Document, sink);

        var start = sink.Events.First(e => e.Kind == XmlEventKind.StartElement);
        await Assert.That(sink.Names.GetString(start.Name.Local)).IsEqualTo("a");
        await Assert.That(sink.Names.GetString(start.Name.NamespaceUri)).IsEqualTo("urn:d");
        var pi = sink.Events.First(e => e.Kind == XmlEventKind.ProcessingInstruction);
        await Assert.That(sink.Names.GetString(pi.Prefix)).IsEqualTo("pi");
    }

    [Test]
    public async Task Pump_LeafNodes_EmitTheirSingleEvent()
    {
        var (names, script) = Sample();
        var tree = Build(names, script);
        var a = tree.Document.FirstChild;
        var text = a.FirstChild.FirstChild;
        var comment = a.FirstChild.NextSibling;
        var pi = comment.NextSibling;
        var sink = new RecordingXmlSink(names);

        XmlTreePump.Pump(text, sink);
        XmlTreePump.Pump(comment, sink);
        XmlTreePump.Pump(pi, sink);

        XmlEventKind[] expected = [XmlEventKind.Text, XmlEventKind.Comment, XmlEventKind.ProcessingInstruction];
        await Assert.That(sink.Events.Select(e => e.Kind).SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Pump_IntoStructSink_AllocatesNothingPerNode()
    {
        var (names, script) = Sample();
        var tree = Build(names, script);
        var sink = new CountingStructSink(names);
        var root = tree.Document;

        AllocationAssert.NoAllocation(() => XmlTreePump.Pump(root, ref sink));

        await Assert.That(sink.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Pump_TreeOfDepth100000_DoesNotOverflowTheStack()
    {
        const int Depth = 100_000;
        var names = new SpanInterningNameTable(1000);
        var name = names.Intern("", "e", "");
        using var builder = new XmlTreeBuilder(names);
        for (var i = 0; i < Depth; i++)
        {
            builder.StartElement(name, default);
        }

        for (var i = 0; i < Depth; i++)
        {
            builder.EndElement(name);
        }

        var tree = builder.Complete();
        var sink = new CountingStructSink(names);

        XmlTreePump.Pump(tree.Document, ref sink);

        // StartElement, EndAttributes and EndElement per element.
        await Assert.That(sink.Count).IsEqualTo(Depth * 3);
    }
}
