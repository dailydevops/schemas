namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>The parser consumes the reader contract only: a scripted reader, no document model type.</summary>
public class SchemaReaderContractTests
{
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    /// <summary>Plays a fixed script of events to the sink in one pump.</summary>
    private sealed class FakeReader : IXmlReader
    {
        private readonly XmlReadStatus _status;

        public FakeReader(XmlReadStatus status = XmlReadStatus.Completed) => _status = status;

        public int Pumps { get; private set; }

        public XmlPosition Position => default;

        public XmlDiagnostic? Diagnostic => null;

        public XmlReadStatus Pump<TSink>(ref TSink sink)
            where TSink : IXmlSink
        {
            Pumps++;
            var names = sink.Names;
            var schema = names.Intern("xs", "schema", Xs);
            var element = names.Intern("xs", "element", Xs);
            var attribute = names.Intern(string.Empty, "name", string.Empty);

            sink.StartDocument(default);
            sink.StartElement(schema, new XmlPosition(1, 1, 0));
            sink.NamespaceDeclaration(names.Intern("xs"), names.Intern(Xs));
            sink.EndAttributes();
            sink.Text("\n  ".AsSpan(), XmlTextKind.Text);
            sink.StartElement(element, new XmlPosition(2, 3, 4));
            sink.Attribute(attribute, "e".AsSpan());
            sink.EndAttributes();
            sink.EndElement(element);
            sink.Comment("c".AsSpan());
            sink.EndElement(schema);
            sink.EndDocument();
            return _status;
        }
    }

    [Test]
    public async Task Parse_ScriptedReader_BuildsTheLocatedModelInOnePump()
    {
        var reader = new FakeReader();
        var reporter = NewReporter(out var collector);

        var document = SchemaDocumentParser.Parse(reader, "urn:fake", reporter);

        await Assert.That(reader.Pumps).IsEqualTo(1);
        await Assert.That(collector.Count).IsEqualTo(0);
        await Assert.That(document.BaseUri).IsEqualTo("urn:fake");
        await Assert.That(document.IsComplete).IsTrue();
        await Assert.That(Dump(document.Root!)).IsEqualTo("schema@urn:fake:1:1[]{element@urn:fake:2:3[|name=e@2:3]{}}");
    }

    [Test]
    public async Task Parse_ReaderThatNeedsMoreData_IsIncompleteWithoutRoot()
    {
        var reader = new FakeReader(XmlReadStatus.NeedMoreData);
        var reporter = NewReporter(out _);

        var document = SchemaDocumentParser.Parse(reader, null, reporter);

        await Assert.That(document.IsComplete).IsFalse();
        await Assert.That(document.Root).IsNull();
        await Assert.That(document.ReadError).IsNull();
    }

    [Test]
    public async Task Parse_ReaderError_IsIncompleteWithoutRoot()
    {
        var reader = new FakeReader(XmlReadStatus.Error);
        var reporter = NewReporter(out _);

        var document = SchemaDocumentParser.Parse(reader, null, reporter);

        await Assert.That(document.IsComplete).IsFalse();
        await Assert.That(document.Root).IsNull();
    }
}
