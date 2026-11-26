namespace NetEvolve.Schemas.Xml.Tests.Unit.Model;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;

public class XmlTreeBuilderSinkTests
{
    [Test]
    public async Task Events_BuildTheSameTreeAsTheBuilderItself(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var viaBuilder = new XmlTreeBuilder();
        using var viaSink = new XmlTreeBuilder();
        var direct = Drive(viaBuilder, viaBuilder);
        var adapted = Drive(new XmlTreeBuilderSink(viaSink), viaSink);

        await Assert.That(adapted.Count).IsEqualTo(direct.Count);
        await Assert.That(adapted.Document.StringValue).IsEqualTo(direct.Document.StringValue);
        await Assert
            .That(adapted.Document.FirstChild.Name.Local.Value)
            .IsEqualTo(direct.Document.FirstChild.Name.Local.Value);
        await Assert.That(adapted.GetPosition(adapted.Document.FirstChild.Index)).IsEqualTo(new XmlPosition(1, 2, 1));
    }

    [Test]
    public async Task Names_ReturnsTheNameTableOfTheBuilder(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var builder = new XmlTreeBuilder();

        await Assert.That(new XmlTreeBuilderSink(builder).Names).IsSameReferenceAs(builder.Names);
    }

    // <r a="1">t<!--c--><?p d?></r> with a document type event in front.
    private static XmlDocumentTree Drive<TSink>(TSink sink, XmlTreeBuilder builder)
        where TSink : IXmlSink
    {
        var names = sink.Names;
        var r = names.Intern(string.Empty, "r", string.Empty);
        sink.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        sink.Doctype(default);
        sink.StartElement(r, new XmlPosition(1, 2, 1));
        sink.NamespaceDeclaration(names.Intern("p"), names.Intern("urn:p"));
        sink.Attribute(names.Intern(string.Empty, "a", string.Empty), "1");
        sink.EndAttributes();
        sink.Text("t", XmlTextKind.Text);
        sink.Comment("c");
        sink.ProcessingInstruction(names.Intern("p"), "d");
        sink.EndElement(r);
        sink.EndDocument();
        return builder.Complete();
    }
}
