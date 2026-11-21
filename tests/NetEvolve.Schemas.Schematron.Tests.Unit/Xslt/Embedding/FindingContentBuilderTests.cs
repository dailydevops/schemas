namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using System;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Embedding;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The content of a referenced property: the items and the text that <c>xsl:copy-of</c> and the text of the property push, completed
/// into an owned snapshot whose attributes belong to the element that holds it and whose children follow.
/// </summary>
public class FindingContentBuilderTests
{
    private static XdmItem Node(string key) => XdmItem.FromNode(AssertionTree.Node(key));

    private static string Dump(IFindingContent content) => Describe(content, holder =>
        {
            var text = new StringBuilder();
            for (var child = holder.FirstChild; !child.IsEmpty; child = child.NextSibling)
            {
                _ = text.Append(NodeDump.Of(child));
            }

            return text.ToString();
        });

    private static string Namespaces(IFindingContent content) => Describe(content, holder =>
        {
            var names = holder.Tree!.Names;
            var found = new StringBuilder();
            for (var ns = holder.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
            {
                _ = found.Append(names.GetString(ns.Name.Local)).Append('=').Append(ns.StringValue).Append(';');
            }

            return found.ToString();
        });

    private static string Path(IFindingContent content, string path) => Describe(content, holder =>
        {
            var item = XdmItem.FromNode(new XPathNode(holder));
            var nodes = PathQuery.Select(in item, path);
            var items = new string[nodes.Count];
            for (var i = 0; i < items.Length; i++)
            {
                var node = nodes[i];
                items[i] = PathQuery.Describe(in node);
            }

            return string.Join("|", items);
        });

    // The holder of the content, as the report writer provides it: an element whose start tag takes the attributes.
    private static string Describe(IFindingContent content, Func<XmlNode, string> read)
    {
        using var tree = new XmlTreeBuilder();
        tree.StartDocument(default);
        var name = tree.Names.Intern(string.Empty, "holder", string.Empty);
        tree.StartElement(name, default);
        content.WriteAttributes(tree);
        tree.EndAttributes();
        content.WriteChildren(tree);
        tree.EndElement(name);
        tree.EndDocument();
        return read(tree.Complete().Document.FirstChild);
    }

    private static (bool Ok, IFindingContent Content, SchematronProblem Problem) Complete(
        IFindingContentBuilder builder
    )
    {
        var ok = builder.TryComplete(out var content, out var problem);
        return (ok, content, problem);
    }

    private static IFindingContentBuilder Begin() => new EmbeddedContent().BeginFindingContent();

    [Test]
    public async Task Empty_IsAnEmptyHolder()
    {
        using var builder = Begin();

        var (ok, content, _) = Complete(builder);

        await Assert.That(ok).IsTrue();
        await Assert.That(Dump(content)).IsEqualTo(string.Empty);
        await Assert.That(Path(content, "@*")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Text_Coalesces()
    {
        using var builder = Begin();
        builder.Text("a".AsSpan());
        builder.Text("b".AsSpan());

        var (_, content, _) = Complete(builder);

        await Assert.That(Path(content, "node()")).IsEqualTo("Text:=ab");
    }

    [Test]
    public async Task AdjacentAtomicValues_AreSeparatedByOneSpace_AndTextResetsTheAdjacency()
    {
        using var builder = Begin();
        var x = XdmItem.FromString("x");
        var y = XdmItem.FromString("y");
        builder.Item(in x);
        builder.Item(in y);
        builder.Text("-".AsSpan());
        builder.Item(in x);
        builder.Item(in y);

        var (_, content, _) = Complete(builder);

        await Assert.That(Path(content, "node()")).IsEqualTo("Text:=x y-x y");
    }

    [Test]
    public async Task Element_IsCopiedWithItsAttributesChildrenAndANewIdentity()
    {
        using var builder = Begin();
        builder.Item(Node("order1"));

        var (_, content, _) = Complete(builder);

        await Assert.That(Path(content, "order")).IsEqualTo("Element:order=txt12");
        await Assert.That(Path(content, "order/@id")).IsEqualTo("Attribute:id=7");
        await Assert.That(Path(content, "order/item")).IsEqualTo("Element:item=1|Element:item=2");
        await Assert.That(Path(content, "@*")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Attribute_IsCopiedOntoTheHolder()
    {
        using var builder = Begin();
        builder.Item(Node("id"));

        var (ok, content, _) = Complete(builder);

        await Assert.That(ok).IsTrue();
        await Assert.That(Path(content, "@*")).IsEqualTo("Attribute:id=7");
        await Assert.That(Path(content, "node()")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Attribute_BeforeTheChildren_IsAcceptedAfterText_IsXtde0410()
    {
        using var builder = Begin();
        builder.Text("text".AsSpan());
        builder.Item(Node("id"));

        var (ok, _, problem) = Complete(builder);

        await Assert.That(ok).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problem.Constraint).IsEqualTo("XTDE0410");
    }

    [Test]
    public async Task Attribute_AfterAnElement_IsXtde0410()
    {
        using var builder = Begin();
        builder.Item(Node("item1"));
        builder.Item(Node("id"));

        var (ok, _, problem) = Complete(builder);

        await Assert.That(ok).IsFalse();
        await Assert.That(problem.Constraint).IsEqualTo("XTDE0410");
    }

    [Test]
    public async Task Element_WithANamespace_KeepsItsNamespaceNodes()
    {
        using var builder = Begin();
        builder.Item(Node("px"));

        var (_, content, _) = Complete(builder);

        await Assert.That(Dump(content)).IsEqualTo("<p:x xmlns:p='urn:p'/>");
    }

    [Test]
    public async Task NamespaceNode_IsDeclaredOnTheHolder()
    {
        using var builder = Begin();
        builder.Item(Node("ns"));

        var (ok, content, _) = Complete(builder);

        await Assert.That(ok).IsTrue();
        await Assert.That(Namespaces(content)).Contains("p=urn:p;");
    }

    [Test]
    public async Task Document_ContributesItsChildren()
    {
        using var builder = Begin();
        builder.Item(Node("doc"));

        var (_, content, _) = Complete(builder);

        await Assert.That(Path(content, "root/order")).IsEqualTo("Element:order=txt12|Element:order=");
    }

    [Test]
    public async Task CommentAndProcessingInstruction_AreCopied()
    {
        using var builder = Begin();
        builder.Item(Node("c"));
        builder.Item(Node("pi"));

        var (_, content, _) = Complete(builder);

        await Assert.That(Path(content, "node()")).IsEqualTo("Comment:=c|ProcessingInstruction:pi=d");
    }

    [Test]
    public async Task Boolean_IsItsLexicalForm()
    {
        using var builder = Begin();
        var flag = XdmItem.FromBoolean(true);
        builder.Item(in flag);

        var (_, content, _) = Complete(builder);

        await Assert.That(Path(content, "node()")).IsEqualTo("Text:=true");
    }

    [Test]
    public async Task Content_WritesThroughTheEventsOfTheTree()
    {
        using var builder = Begin();
        builder.Content.Text("direct".AsSpan(), XmlTextKind.Text);

        var (_, content, _) = Complete(builder);

        await Assert.That(builder.Kind).IsEqualTo(SequenceSinkKind.Tree);
        await Assert.That(Path(content, "node()")).IsEqualTo("Text:=direct");
    }

    [Test]
    public async Task NestedTree_IsAnIndependentDocument()
    {
        using var builder = Begin();

        var tree = builder.BeginTree();
        var document = builder.EndTree(tree);

        _ = document.TryGetNode(out var node);
        await Assert.That(node.Kind).IsEqualTo(XdmNodeKind.Document);
    }

    [Test]
    public async Task Content_WithAFullNameTable_IsTheLimitStop()
    {
        using var builder = new EmbeddedContent(new SpanInterningNameTable(1)).BeginFindingContent();
        builder.Item(Node("order1"));

        var (ok, _, problem) = Complete(builder);

        await Assert.That(ok).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationLimitStop);
    }

    [Test]
    public async Task Dispose_WithoutCompletion_IsAllowedAndRepeatable()
    {
        var builder = Begin();
        builder.Text("never completed".AsSpan());

        builder.Dispose();
        builder.Dispose();

        await Assert.That(() => builder.Text("late".AsSpan())).Throws<InvalidOperationException>();
    }
}
