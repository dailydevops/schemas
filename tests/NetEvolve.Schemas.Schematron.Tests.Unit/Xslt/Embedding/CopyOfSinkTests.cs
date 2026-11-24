namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Xslt.Embedding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The sink that the <see langword="select"/> of an <c>xsl:copy-of</c> in a property evaluates into.</summary>
public class CopyOfSinkTests
{
    [Test]
    public async Task Item_OfANode_AddsItsStringValueToTheTextAndTheNodeToTheContent()
    {
        using var text = new TextSink(100);
        using var content = new EmbeddedContent().BeginFindingContent();
        var sink = new CopyOfSink();
        sink.Begin(text, content);

        sink.Item(XdmItem.FromNode(AssertionTree.Node("order1")));

        await Assert.That(text.ToString()).IsEqualTo("txt12");
        await Assert.That(content.TryComplete(out _, out _)).IsTrue();
    }

    [Test]
    public async Task Item_OfAnAtomicValue_AddsItToBothAndSeparatesTheNextOneByASpace()
    {
        using var text = new TextSink(100);
        using var content = new EmbeddedContent().BeginFindingContent();
        var sink = new CopyOfSink();
        sink.Begin(text, content);

        sink.Item(XdmItem.FromString("a"));
        sink.Item(XdmItem.FromString("b"));

        await Assert.That(text.ToString()).IsEqualTo("a b");
    }

    [Test]
    public async Task Item_OfAValueWithoutATextForm_AddsNothingToTheContent()
    {
        using var text = new TextSink(100);
        using var content = new EmbeddedContent().BeginFindingContent();
        var sink = new CopyOfSink();
        sink.Begin(text, content);

        sink.Item(default(XdmItem));

        await Assert.That(text.ToString()).IsEqualTo(string.Empty);
        await Assert.That(text.Failure).IsNotNull();
    }

    [Test]
    public async Task Text_GoesToBoth()
    {
        using var text = new TextSink(100);
        using var content = new EmbeddedContent().BeginFindingContent();
        var sink = new CopyOfSink();
        sink.Begin(text, content);

        sink.Text("plain".AsSpan());

        await Assert.That(text.ToString()).IsEqualTo("plain");
        await Assert.That(sink.Kind).IsEqualTo(SequenceSinkKind.Items);
    }

    [Test]
    public async Task Tree_IsAProgrammingError()
    {
        var sink = new CopyOfSink();
        using var content = new EmbeddedContent().BeginFindingContent();

        await Assert.That(() => sink.Content).Throws<InvalidOperationException>();
        await Assert.That(() => sink.BeginTree()).Throws<InvalidOperationException>();
        await Assert.That(() => sink.EndTree(content.BeginTree())).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Begin_NullArguments_Throw()
    {
        var sink = new CopyOfSink();
        using var text = new TextSink(10);
        using var content = new EmbeddedContent().BeginFindingContent();

        await Assert.That(() => sink.Begin(null!, content)).Throws<ArgumentNullException>();
        await Assert.That(() => sink.Begin(text, null!)).Throws<ArgumentNullException>();
    }
}
