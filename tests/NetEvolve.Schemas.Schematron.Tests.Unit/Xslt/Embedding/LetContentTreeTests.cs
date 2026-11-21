namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Embedding;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A <c>let</c> with element content is a temporary tree: a document node whose children are copies of the content, kept as written
/// and never run as XSLT instructions, bound to the slot of the variable.
/// </summary>
public class LetContentTreeTests
{
    private static readonly DocumentLocation Where = new("let.sch", 2, 3);

    private static SchematronForeignElement Element(
        string uri,
        string local,
        KeyValuePair<string, string>[]? attributes = null,
        params SchematronNode[] children
    ) => new(Where, uri, local, attributes ?? [], children);

    private static SchematronText Text(string value) => new(Where, value);

    private static KeyValuePair<string, string> Attr(string key, string value) => new(key, value);

    private static (QueryStatus Status, XdmItem Document, SchematronProblem Problem, MiniRun Run) Bind(
        params SchematronNode[] content
    )
    {
        var run = new MiniRun();
        var status = new EmbeddedContent().TryBindLetContent(run, 4, content, out var problem);
        XdmItem document = default;
        if (run.TryGetValue(4, out var value) && value.Count == 1)
        {
            document = value[0];
        }

        return (status, document, problem, run);
    }

    private static string Path(in XdmItem context, string path)
    {
        var nodes = PathQuery.Select(in context, path);
        var items = new string[nodes.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var node = nodes[i];
            items[i] = PathQuery.Describe(in node);
        }

        return string.Join("|", items);
    }

    [Test]
    public async Task Content_IsADocumentNode_WhoseChildrenAreAddressableByPath()
    {
        var (status, document, _, _) = Bind(
            Element(
                string.Empty,
                "root",
                null,
                Element(string.Empty, "item", [Attr("n", "1")], Text("one")),
                Element(string.Empty, "item", [Attr("n", "2")], Text("two"))
            )
        );

        _ = document.TryGetNode(out var node);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(node.Kind).IsEqualTo(XdmNodeKind.Document);
        await Assert.That(Path(in document, "/root/item")).IsEqualTo("Element:item=one|Element:item=two");
        await Assert.That(Path(in document, "/root/item/@n")).IsEqualTo("Attribute:n=1|Attribute:n=2");
        await Assert.That(Path(in document, "//item")).IsEqualTo("Element:item=one|Element:item=two");
    }

    [Test]
    public async Task Content_BindsExactlyOneValueToTheSlot()
    {
        var (_, _, _, run) = Bind(Element(string.Empty, "root"));

        _ = run.TryGetValue(4, out var value);

        await Assert.That(value.Count).IsEqualTo(1);
        await Assert.That(run.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Content_OfTwoElementsAndText_HasThreeChildrenInOrder()
    {
        var (_, document, _, _) = Bind(Element(string.Empty, "a"), Text("between"), Element(string.Empty, "b"));

        await Assert.That(Path(in document, "/*")).IsEqualTo("Element:a=|Element:b=");
        await Assert.That(Path(in document, "/node()")).IsEqualTo("Element:a=|Text:=between|Element:b=");
    }

    [Test]
    public async Task Content_OfText_IsADocumentWithOneTextNode()
    {
        var (status, document, _, _) = Bind(Text("hello"));

        _ = document.TryGetNode(out var node);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(node.StringValue()).IsEqualTo("hello");
        await Assert.That(Path(in document, "/node()")).IsEqualTo("Text:=hello");
    }

    [Test]
    public async Task Content_WhiteSpaceOnlyText_IsStripped()
    {
        var (_, document, _, _) = Bind(
            Text("\n  "),
            Element(string.Empty, "a", null, Text("\n   "), Element(string.Empty, "b"), Text(" x ")),
            Text("\n")
        );

        await Assert.That(Path(in document, "/node()")).IsEqualTo("Element:a= x ");
        await Assert.That(Path(in document, "/a/node()")).IsEqualTo("Element:b=|Text:= x ");
    }

    [Test]
    public async Task Content_ElementInTheXsltNamespace_IsCopiedAsAnElementAndNeverRun()
    {
        var (status, document, problem, _) = Bind(
            Element(
                EmbeddingNodes.XsltNamespace,
                "message",
                [Attr("terminate", "yes")],
                Text("This should NOT terminate the program")
            ),
            Text("ß\n")
        );

        _ = document.TryGetNode(out var node);
        var children = PathQuery.Select(in document, "/*");
        _ = children[0].TryGetNode(out var message);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(problem.Code).IsEqualTo(default(DiagnosticCode));
        await Assert.That(children.Count).IsEqualTo(1);
        await Assert
            .That(message.Tree!.Names.GetString(message.Node.Name.NamespaceUri))
            .IsEqualTo(EmbeddingNodes.XsltNamespace);
        await Assert.That(message.Tree!.Names.GetString(message.Node.Name.Local)).IsEqualTo("message");
        await Assert.That(Path(in document, "/*/@terminate")).IsEqualTo("Attribute:terminate=yes");
        await Assert.That(node.StringValue()).Contains("NOT terminate");
    }

    [Test]
    public async Task Content_ElementInANamespace_DeclaresTheNamespaceOnce()
    {
        var (_, document, _, _) = Bind(
            Element("urn:a", "outer", null, Element("urn:a", "inner"), Element("urn:b", "other"))
        );

        _ = PathQuery.Select(in document, "/*")[0].TryGetNode(out var outer);
        var declarations = new List<string>();
        for (var ns = outer.Node.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
        {
            declarations.Add(outer.Tree!.Names.GetString(ns.Name.Local) + "=" + ns.StringValue);
        }

        await Assert.That(declarations).IsEquivalentTo(["=urn:a"]);
    }

    [Test]
    public async Task Content_AttributeInANamespace_GetsAGeneratedPrefix()
    {
        var (status, document, _, _) = Bind(Element("urn:a", "root", [Attr("{urn:b}id", "7")]));

        _ = PathQuery.Select(in document, "/*/@*")[0].TryGetNode(out var attribute);
        var names = attribute.Tree!.Names;

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(names.GetString(attribute.Node.Name.NamespaceUri)).IsEqualTo("urn:b");
        await Assert.That(names.GetString(attribute.Node.Name.Local)).IsEqualTo("id");
        await Assert.That(names.GetString(attribute.Node.Name.Prefix)).IsNotEmpty();
        await Assert.That(attribute.Node.StringValue).IsEqualTo("7");
    }

    [Test]
    public async Task Content_BoundTwice_IsTwoIndependentTrees()
    {
        var embedded = new EmbeddedContent();
        var run = new MiniRun();
        SchematronNode[] content = [Element(string.Empty, "root")];

        _ = embedded.TryBindLetContent(run, 1, content, out _);
        _ = embedded.TryBindLetContent(run, 2, content, out _);

        _ = run.TryGetValue(1, out var first);
        _ = run.TryGetValue(2, out var second);
        _ = first[0].TryGetNode(out var a);
        _ = second[0].TryGetNode(out var b);

        await Assert.That(a.Equals(b)).IsFalse();
        await Assert.That(ReferenceEquals(a.Tree, b.Tree)).IsFalse();
    }

    [Test]
    public async Task Content_WithARunThatCannotHoldAValue_IsTheUnsupportedConstructError()
    {
        var run = new PlainRun();

        var status = new EmbeddedContent().TryBindLetContent(run, 0, [Text("x")], out var problem);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
    }

    [Test]
    public async Task Content_WithAFullNameTable_IsTheLimitStop()
    {
        var run = new MiniRun();
        var embedded = new EmbeddedContent(new SpanInterningNameTable(1));

        var status = embedded.TryBindLetContent(
            run,
            0,
            [Element("urn:a", "one", null, Element("urn:b", "two", null, Element("urn:c", "three")))],
            out var problem
        );

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationLimitStop);
    }

    [Test]
    public async Task TryBindLetContent_NullArguments_Throw()
    {
        var embedded = new EmbeddedContent();

        await Assert.That(() => embedded.TryBindLetContent(null!, 0, [], out _)).Throws<ArgumentNullException>();
        await Assert
            .That(() => embedded.TryBindLetContent(new MiniRun(), 0, null!, out _))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new EmbeddedContent(null!)).Throws<ArgumentNullException>();
    }

    // A run that does not implement the value hooks of the function run.
    private sealed class PlainRun : IQueryRun
    {
        public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
        {
            value = false;
            return QueryStatus.Ok;
        }

        public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink) =>
            QueryStatus.Ok;

        public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus) => QueryStatus.Ok;

        public void BindVariable(int slot, string value) { }

        public SchematronProblem GetProblem() => default;

        public void Dispose() { }
    }
}
