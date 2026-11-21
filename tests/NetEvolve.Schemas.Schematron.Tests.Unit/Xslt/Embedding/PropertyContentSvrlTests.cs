namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using System;
using System.Buffers;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Svrl;
using NetEvolve.Schemas.Schematron.Xslt.Embedding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The SVRL writer puts the content of a referenced property into <c>svrl:text</c>: the attributes that <c>xsl:copy-of</c> copied
/// onto the element, and the copied nodes and text as its content. The case <c>svrl-property-copy-of</c> of the Schematron conformance
/// suite asserts <c>//svrl:property-reference/svrl:text/@attribute = 'value'</c> for <c>&lt;xsl:copy-of select="@attribute"/&gt;</c>; it
/// is mirrored here as far as this package reaches it, because the end-to-end run needs the XPath evaluator and the compile pipeline
/// (#297), so the conformance baseline is not changed.
/// </summary>
public class PropertyContentSvrlTests
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";

    private static XdmItem Node(string key) => XdmItem.FromNode(AssertionTree.Node(key));

    private static IFindingContent Content(Action<IFindingContentBuilder> push)
    {
        using var builder = new EmbeddedContent().BeginFindingContent();
        push(builder);
        return builder.TryComplete(out var content, out _)
            ? content
            : throw new InvalidOperationException("The content must complete.");
    }

    private static void Finding(ISchematronReportSink sink, string[] ids, string[] texts, IFindingContent?[] contents)
    {
        var finding = new SchematronFinding(
            false,
            "a1",
            "",
            "a1",
            "false()",
            "/",
            "",
            "",
            "",
            "message",
            default,
            new SchematronReferenceList(ids, texts, contents),
            null
        );
        sink.Finding(in finding);
    }

    private static XElement Write(Action<ISchematronReportSink> pattern)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var sink = new SvrlReportSink(buffer))
        {
            SvrlRuns.Begin(sink, "#ALL", SchematronEdition.Iso2016);
            SvrlRuns.Pattern(sink, "p1");
            SvrlRuns.FiredRule(sink, "r1", "*");
            pattern(sink);
            SvrlRuns.Finish(sink, false);
        }

        return XDocument.Parse(SvrlRuns.Text(buffer.WrittenSpan.ToArray())).Root!;
    }

    private static XElement[] References(XElement root) => [.. root.Descendants(Svrl + "property-reference")];

    [Test]
    public async Task CopyOfAnAttribute_IsAnAttributeOfSvrlText()
    {
        var content = Content(builder => builder.Item(Node("id")));

        var root = Write(sink => Finding(sink, ["copy-of"], ["7"], [content]));

        var text = References(root).Single().Element(Svrl + "text")!;
        await Assert.That((string?)text.Attribute("id")).IsEqualTo("7");
        await Assert.That(text.Nodes().Count()).IsEqualTo(0);
        await Assert.That((string?)References(root).Single().Attribute("property")).IsEqualTo("copy-of");
        var manager = new XmlNamespaceManager(new NameTable());
        manager.AddNamespace("svrl", Svrl.NamespaceName);
        var holds = (bool)root.XPathEvaluate("//svrl:property-reference/svrl:text/@id = '7'", manager);
        await Assert.That(holds).IsTrue();
    }

    [Test]
    public async Task CopyOfAnElementWithANamespace_WritesTheElementWithItsNamespace()
    {
        var content = Content(builder => builder.Item(Node("px")));

        var root = Write(sink => Finding(sink, ["p"], ["x"], [content]));

        var copy = References(root).Single().Element(Svrl + "text")!.Elements().Single();
        await Assert.That(copy.Name).IsEqualTo((XName)"{urn:p}x");
        await Assert.That(copy.GetNamespaceOfPrefix("p")?.NamespaceName).IsEqualTo("urn:p");
    }

    [Test]
    public async Task CopyOfAnElementWithContent_WritesItsAttributesAndChildren()
    {
        var content = Content(builder => builder.Item(Node("order1")));

        var root = Write(sink => Finding(sink, ["p"], ["txt12"], [content]));

        var order = References(root).Single().Element(Svrl + "text")!.Element("order")!;
        await Assert.That((string?)order.Attribute("id")).IsEqualTo("7");
        await Assert.That(order.Elements("item").Select(item => item.Value)).IsEquivalentTo(["1", "2"]);
        await Assert.That(order.Value).IsEqualTo("txt12");
    }

    [Test]
    public async Task CopyOfAnAtomicValue_WritesItsStringValue()
    {
        var content = Content(builder =>
        {
            var value = XdmItem.FromString("seven");
            builder.Item(in value);
        });

        var root = Write(sink => Finding(sink, ["p"], ["seven"], [content]));

        await Assert.That(References(root).Single().Element(Svrl + "text")!.Value).IsEqualTo("seven");
    }

    [Test]
    public async Task TextAndNodes_KeepTheirOrder()
    {
        var content = Content(builder =>
        {
            builder.Text("before ".AsSpan());
            builder.Item(Node("item1"));
            builder.Text(" after".AsSpan());
        });

        var root = Write(sink => Finding(sink, ["p"], ["before 1 after"], [content]));

        var text = References(root).Single().Element(Svrl + "text")!;
        await Assert.That(text.Nodes().Count()).IsEqualTo(3);
        await Assert.That(text.Value).IsEqualTo("before 1 after");
        await Assert.That(text.Elements().Single().Name.LocalName).IsEqualTo("item");
    }

    [Test]
    public async Task PropertiesWithAndWithoutContent_AreWrittenEachInItsOwnForm()
    {
        var content = Content(builder => builder.Item(Node("id")));

        var root = Write(sink => Finding(sink, ["plain", "rich", "plain2"], ["a", "7", "c"], [null, content, null]));

        var references = References(root);
        await Assert.That(references.Length).IsEqualTo(3);
        await Assert.That(references[0].Element(Svrl + "text")!.Value).IsEqualTo("a");
        await Assert.That((string?)references[1].Element(Svrl + "text")!.Attribute("id")).IsEqualTo("7");
        await Assert.That(references[2].Element(Svrl + "text")!.Value).IsEqualTo("c");
    }

    [Test]
    public async Task ContentSurvivesUntilThePatternEnds_AndIsNotKeptForTheNextOne()
    {
        var first = Content(builder => builder.Item(Node("id")));
        var second = Content(builder => builder.Item(Node("item1")));
        var buffer = new ArrayBufferWriter<byte>();
        using (var sink = new SvrlReportSink(buffer))
        {
            SvrlRuns.Begin(sink, "#ALL", SchematronEdition.Iso2016);
            SvrlRuns.Pattern(sink, "p1");
            Finding(sink, ["a"], ["7"], [first]);
            Finding(sink, ["b"], ["1"], [second]);
            SvrlRuns.Pattern(sink, "p2");
            Finding(sink, ["c"], ["plain"], [null]);
            SvrlRuns.Finish(sink, false);
        }

        var root = XDocument.Parse(SvrlRuns.Text(buffer.WrittenSpan.ToArray())).Root!;

        var references = References(root);
        await Assert.That(references.Length).IsEqualTo(3);
        await Assert.That((string?)references[0].Element(Svrl + "text")!.Attribute("id")).IsEqualTo("7");
        await Assert.That(references[1].Element(Svrl + "text")!.Element("item")!.Value).IsEqualTo("1");
        await Assert.That(references[2].Element(Svrl + "text")!.Value).IsEqualTo("plain");
    }

    [Test]
    public async Task ReferenceListWithoutContents_HasNoContent()
    {
        string[] ids = ["a"];
        string[] texts = ["t"];

        var list = new SchematronReferenceList(ids, texts);
        var first = list.ContentAt(0);
        var outside = list.ContentAt(5);

        await Assert.That(first).IsNull();
        await Assert.That(outside).IsNull();
    }
}
