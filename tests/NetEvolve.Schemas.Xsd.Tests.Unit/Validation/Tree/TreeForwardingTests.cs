namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Tree;

using System;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Tree;

/// <summary>The forwarders of the tree sink for comments, processing instructions, and the document type, and the growth of its stack of open elements.</summary>
public class TreeForwardingTests
{
    private const string Nesting = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:element name="n">
            <xs:complexType>
              <xs:choice minOccurs="0">
                <xs:element ref="n"/>
                <xs:element name="leaf" type="xs:int"/>
              </xs:choice>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    // More than the 16 open elements that the stack of the sink starts with, so that it grows more than once.
    private const int Depth = 40;

    [Test]
    public async Task Validate_DeepNestingWithCommentsAndInstructions_GivesTheStreamingIssuesAndMapsTheLeaf(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var instance = Deep();
        var streamed = TreeFixture
            .Streaming(Nesting)
            .Validate(instance.AsSpan(), TreeFixture.InstanceId, cancellationToken);

        var result = TreeFixture.Validator(Nesting).Validate(TreeFixture.Load(instance).Document, cancellationToken);

        await Assert.That(result.Result.IsValid).IsFalse();
        await Assert.That(TreeFixture.Describe(result.Result)).IsEqualTo(TreeFixture.Describe(streamed));
        await Assert.That(result.TryGetIssueNode(result.Result.Issues[0], out var node)).IsTrue();
        var leaf = TreeFixture.Element(result.Document, "leaf");
        await Assert.That(node).IsEqualTo(leaf);
        var depth = 0;
        for (var parent = leaf.Parent; parent.Kind == XmlNodeKind.Element; parent = parent.Parent)
        {
            depth++;
        }

        await Assert.That(depth).IsEqualTo(Depth);
    }

    [Test]
    public async Task Validate_CommentAndInstructionBetweenElements_AreNodesOfTheCopy(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = TreeFixture.Validator(Nesting).Validate(TreeFixture.Load(Deep()).Document, cancellationToken);

        var comments = 0;
        var instructions = 0;
        for (var node = result.Document.Document.FirstChild; !node.IsEmpty; node = Next(node))
        {
            if (node.Kind == XmlNodeKind.Comment)
            {
                comments++;
                await Assert.That(node.StringValue).IsEqualTo(" c ");
            }
            else if (node.Kind == XmlNodeKind.ProcessingInstruction)
            {
                instructions++;
                await Assert.That(node.StringValue).IsEqualTo("data");
            }
        }

        await Assert.That(comments).IsEqualTo(Depth);
        await Assert.That(instructions).IsEqualTo(Depth);
    }

    [Test]
    public async Task Validate_SubtreeThatUsesAPrefixOfItsAncestor_ResolvesItThroughTheScopeOfTheAncestor(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = TreeFixture.Load(
            "<root xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:t=\"http://www.w3.org/2001/XMLSchema\">"
                + "<item xsi:type=\"t:int\">1</item></root>"
        );
        var item = TreeFixture.Element(input, "item");

        var result = TreeFixture.Validator().Validate(item, cancellationToken);

        await Assert.That(result.Result.IsValid).IsTrue();
        await Assert
            .That(result.TryGetAnnotation(TreeFixture.Element(result.Document, "item"), out var annotation))
            .IsTrue();
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("int");
    }

    [Test]
    public async Task Doctype_IsForwardedToTheSinkOfTheRunAndAddsNothingToTheCopy(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var copy = new XmlTreeBuilder();
        var inner = new RecordingSink(copy.Names);
        var sink = new TreeRunSink(
            inner,
            copy,
            new ValidationIssueCollector(ValidationLimits.Default),
            new TreeIssueLog(),
            new TreeDefaults(new AnnotationTable())
        );

        sink.StartDocument(default);
        sink.Doctype(default);
        sink.EndDocument();

        await Assert.That(inner.Doctypes).IsEqualTo(1);
        await Assert.That(copy.Complete().Document.FirstChild.IsEmpty).IsTrue();
    }

    // The next node in document order below the document node.
    private static XmlNode Next(XmlNode node)
    {
        var child = node.FirstChild;
        if (!child.IsEmpty)
        {
            return child;
        }

        while (node.NextSibling.IsEmpty)
        {
            node = node.Parent;
            if (node.IsEmpty || node.Index == 0)
            {
                return default;
            }
        }

        return node.NextSibling;
    }

    // Forty elements, each with a comment and an instruction, around a leaf whose value is no integer.
    private static string Deep()
    {
        var text = new StringBuilder();
        for (var i = 0; i < Depth; i++)
        {
            _ = text.Append("<n><!-- c --><?pi data?>");
        }

        _ = text.Append("<leaf>x</leaf>");
        for (var i = 0; i < Depth; i++)
        {
            _ = text.Append("</n>");
        }

        return text.ToString();
    }

    private sealed class RecordingSink(XmlNameTable names) : IXmlSink
    {
        public int Doctypes { get; private set; }

        public XmlNameTable Names { get; } = names;

        public void StartDocument(in XmlDeclaration declaration) { }

        public void EndDocument() { }

        public void StartElement(XmlName name, XmlPosition position) { }

        public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

        public void Attribute(XmlName name, ReadOnlySpan<char> value) { }

        public void EndAttributes() { }

        public void EndElement(XmlName name) { }

        public void Text(ReadOnlySpan<char> text, XmlTextKind kind) { }

        public void Comment(ReadOnlySpan<char> text) { }

        public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

        public void Doctype(in XmlDoctypeInfo doctype) => Doctypes++;
    }
}
