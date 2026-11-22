namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

/// <summary>The declared type, the specified flag, and the normalization flag of an attribute as the reader reports them to a sink.</summary>
public class AttributeTypeTests
{
    private static string Doc(string declarations, string element) => $"<!DOCTYPE a [{declarations}]>{element}";

    [Test]
    [Arguments("CDATA", "Cdata")]
    [Arguments("ID", "Id")]
    [Arguments("IDREF", "IdRef")]
    [Arguments("IDREFS", "IdRefs")]
    [Arguments("ENTITY", "Entity")]
    [Arguments("ENTITIES", "Entities")]
    [Arguments("NMTOKEN", "NmToken")]
    [Arguments("NMTOKENS", "NmTokens")]
    [Arguments("NOTATION (n)", "Notation")]
    [Arguments("(x|y)", "Enumeration")]
    public async Task Reader_ReportsTheDeclaredTypeOfEveryKind(string type, string expected)
    {
        var run = AttributeHarness.Read(Doc($"<!ATTLIST a v {type} #IMPLIED>", "<a v=\"x\"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo($"<a|@v=x{{{expected}}}|/a");
        await Assert.That(run.Sink.Infos[0].DeclaredType).IsEqualTo(Enum.Parse<DtdAttributeType>(expected));
    }

    [Test]
    public async Task Reader_ReportsAnAttributeDeclaredAsIdWithTheIdType()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a i ID #REQUIRED>", "<a i=\"n1\"><a i=\"n2\"/></a>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@i=n1{Id}|<a|@i=n2{Id}|/a|/a");
        await Assert.That(run.Sink.Infos.Count).IsEqualTo(2);
        await Assert.That(run.Sink.Infos[0].DeclaredType).IsEqualTo(DtdAttributeType.Id);
    }

    [Test]
    public async Task Info_CarriesTheNameAsWrittenTheNormalizedValueAndThePosition()
    {
        const string xml = "<!DOCTYPE a [<!ATTLIST a x NMTOKENS #IMPLIED d CDATA \"v\">]>\n<a  x=\" 1  2 \" u=\"3\"/>";
        var element = xml.IndexOf("<a ", StringComparison.Ordinal);

        var run = AttributeHarness.Read(xml);

        var x = run.Sink.Infos[0];
        var u = run.Sink.Infos[1];
        var d = run.Sink.Infos[2];
        await Assert.That(string.Join(",", run.Sink.InfoNames)).IsEqualTo("x,u,d");
        await Assert.That(string.Join(",", run.Sink.InfoValues)).IsEqualTo("1 2,3,v");
        await Assert.That(x.DeclaredType).IsEqualTo(DtdAttributeType.NmTokens);
        await Assert.That(x.IsSpecified).IsTrue();
        await Assert.That(x.IsChangedByNormalization).IsTrue();
        await Assert.That(x.Position).IsEqualTo(new XmlPosition(2, 5, element + 4));
        await Assert.That(u.DeclaredType).IsNull();
        await Assert.That(u.IsSpecified).IsTrue();
        await Assert.That(u.IsChangedByNormalization).IsFalse();
        await Assert.That(u.Position).IsEqualTo(new XmlPosition(2, 16, element + 15));
        await Assert.That(d.DeclaredType).IsEqualTo(DtdAttributeType.Cdata);
        await Assert.That(d.IsSpecified).IsFalse();
        await Assert.That(d.IsChangedByNormalization).IsFalse();
        await Assert.That(d.Position).IsEqualTo(new XmlPosition(2, 1, element));
    }

    [Test]
    public async Task Info_IsReportedForANamespaceDeclarationBeforeTheAttributes()
    {
        var run = AttributeHarness.Read(
            Doc("<!ATTLIST a xmlns:p CDATA #IMPLIED>", "<a xmlns:p=\"u\" xmlns:q=\"v\" p:x=\"1\"/>")
        );

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|#p=u{Cdata}|#q=v{-}|@{u}p:x=1{-}|/a");
        await Assert.That(string.Join(",", run.Sink.InfoNames)).IsEqualTo("xmlns:p,xmlns:q,p:x");
    }

    [Test]
    [Arguments("CDATA", " a  b ", false)]
    [Arguments("NMTOKENS", " a  b ", true)]
    [Arguments("NMTOKENS", "a b", false)]
    [Arguments("ID", "a ", true)]
    public async Task Info_IsChangedByNormalization_OnlyWhenTheDeclaredTypeChangedTheValue(
        string type,
        string value,
        bool expected
    )
    {
        var run = AttributeHarness.Read(Doc($"<!ATTLIST a x {type} #IMPLIED>", $"<a x=\"{value}\"/>"));

        await Assert.That(run.Sink.Infos[0].IsChangedByNormalization).IsEqualTo(expected);
    }

    [Test]
    public async Task Info_OfADocumentWithoutADeclarationOfTheElement_HasNoDeclaredType()
    {
        var run = AttributeHarness.Read(Doc("", "<a x=\"1\" y=\"2\"/>"));

        await Assert.That(run.Events).IsEqualTo("<a|@x=1{-}|@y=2{-}|/a");
        await Assert.That(run.Sink.Infos[0].DeclaredType).IsNull();
        await Assert.That(run.Sink.Infos[0].IsSpecified).IsTrue();
    }

    [Test]
    public async Task StructSink_IsNotOfferedTheInfoEvent()
    {
        using var reader = XmlPipelineReader.Create(
            Doc("<!ATTLIST a d CDATA \"v\">", "<a x=\"1\"/>").AsSpan(),
            AttributeHarness.DtdOn
        );
        var sink = new CountingInfoStructSink();

        var status = reader.Pump(ref sink);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(sink.Attributes).IsEqualTo(2);
        await Assert.That(sink.Infos).IsEqualTo(0);
    }

    [Test]
    public async Task SinkWithoutTheInfoInterface_ReceivesTheDefaultedAttributeAsAnOrdinaryEvent()
    {
        using var reader = XmlPipelineReader.Create(
            Doc("<!ATTLIST a d CDATA \"v\">", "<a x=\"1\"/>").AsSpan(),
            AttributeHarness.DtdOn
        );
        var sink = new RecordingXmlSink();

        var status = reader.Pump(ref sink);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        var values = sink.Events.FindAll(e => e.Kind == XmlEventKind.Attribute).ConvertAll(e => e.Text);
        await Assert.That(string.Join(",", values)).IsEqualTo("1,v");
    }

    [Test]
    public async Task AllInputKinds_GiveTheSameEventsAndInfo(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string xml =
            "<!DOCTYPE a [<!ENTITY e \" p  q \"><!ATTLIST a i ID #IMPLIED t NMTOKENS \" d1 &e;\" xmlns:p CDATA \"urn:p\" c CDATA \"x&e;y\">]>"
            + "<a i=' n1 ' p:x='1'><a i='n2' t='  z '/></a>";
        const string expected =
            "<a|#p=urn:p{Cdata!}|@i=n1{Id~}|@{urn:p}p:x=1{-}|@t=d1 p q{NmTokens!~}|@c=x p  q y{Cdata!}"
            + "|<a|#p=urn:p{Cdata!}|@i=n2{Id}|@t=z{NmTokens~}|@c=x p  q y{Cdata!}|/a|/a";

        var characters = AttributeHarness.Read(xml);
        using var bytes = XmlPipelineReader.Create(Encoding.UTF8.GetBytes(xml).AsSpan(), AttributeHarness.DtdOn);
        var byteSink = new InfoRecordingSink();
        _ = bytes.Pump(ref byteSink);
        var sequence = AttributeHarness.ReadSequence(xml);
        var stream = await AttributeHarness.ReadStreamAsync(xml, cancellationToken);

        await Assert.That(characters.Code).IsNull();
        await Assert.That(characters.Events).IsEqualTo(expected);
        await Assert.That(byteSink.Dump).IsEqualTo(expected);
        await Assert.That(sequence).IsEqualTo(expected);
        await Assert.That(stream).IsEqualTo(expected);
    }

    [StructLayout(LayoutKind.Auto)]
    private struct CountingInfoStructSink : IXmlAttributeInfoSink
    {
        public int Attributes;
        public int Infos;

        public readonly XmlNameTable Names => s_names;

        private static readonly XmlNameTable s_names = new FakeNameTable();

        public readonly void StartDocument(in XmlDeclaration declaration) { }

        public readonly void EndDocument() { }

        public readonly void StartElement(XmlName name, XmlPosition position) { }

        public readonly void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

        public void Attribute(XmlName name, ReadOnlySpan<char> value) => Attributes++;

        public void AttributeInfo(in ValidationAttributeInfo attribute) => Infos++;

        public readonly void EndAttributes() { }

        public readonly void EndElement(XmlName name) { }

        public readonly void Text(ReadOnlySpan<char> text, XmlTextKind kind) { }

        public readonly void Comment(ReadOnlySpan<char> text) { }

        public readonly void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

        public readonly void Doctype(in XmlDoctypeInfo doctype) { }
    }
}
