namespace NetEvolve.Schemas.Xml.Tests.Unit.Model.Loading;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Document loading (XML-55): one entry per input kind, all giving the same tree, positions, and errors.</summary>
public class XmlDocumentLoaderTests
{
    // Several lines with CR and CRLF, a declaration, namespaces, references, CDATA, a comment, a processing instruction,
    // multi-byte and surrogate pair characters: every hazard of a segment or read edge.
    private const string Hazards =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<!-- cé --><?pi däta?>\n"
        + "<r xmlns=\"urn:d\" xmlns:p=\"urn:p\" a=\"x&amp;y\" p:b='\U0001F600'>\r\n  café \U0001D11E &lt; &#x1F600;\r"
        + "<p:e/><x\U0001F600y/><![CDATA[a]b]]c]]]]><![CDATA[\r\nq]]>t<!-- - --><?pi2 ? ??>?>d?>  </r>\r\n<!--tail-->\n";

    public enum InputKind
    {
        CharSpan,
        ByteSpan,
        CharMemory,
        ByteMemory,
        CharSequenceSingle,
        ByteSequenceSingle,
        CharSequenceChunks,
        ByteSequenceChunks,
        StreamWhole,
        StreamByteWise,
    }

    public static IEnumerable<Func<InputKind>> Kinds() =>
        [.. Enum.GetValues<InputKind>().Select(kind => (Func<InputKind>)(() => kind))];

    private static Task<XmlDocumentTree> LoadAsync(
        InputKind kind,
        string xml,
        XmlReaderOptions? options = null,
        Uri? baseUri = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = new UTF8Encoding(false).GetBytes(xml);
        var chars = xml.ToCharArray();
        switch (kind)
        {
            case InputKind.CharSpan:
                return Task.FromResult(XmlDocumentLoader.Load(xml.AsSpan(), options, baseUri));
            case InputKind.ByteSpan:
                return Task.FromResult(XmlDocumentLoader.Load(bytes.AsSpan(), options, baseUri));
            case InputKind.CharMemory:
                return Task.FromResult(XmlDocumentLoader.LoadFromMemory(chars.AsMemory(), options, baseUri));
            case InputKind.ByteMemory:
                return Task.FromResult(XmlDocumentLoader.LoadFromMemory(bytes.AsMemory(), options, baseUri));
            case InputKind.CharSequenceSingle:
                return Task.FromResult(XmlDocumentLoader.Load(SequenceBuilder.Whole(chars), options, baseUri));
            case InputKind.ByteSequenceSingle:
                return Task.FromResult(XmlDocumentLoader.Load(SequenceBuilder.Whole(bytes), options, baseUri));
            case InputKind.CharSequenceChunks:
                return Task.FromResult(XmlDocumentLoader.Load(SequenceBuilder.Chunks(chars, 3), options, baseUri));
            case InputKind.ByteSequenceChunks:
                return Task.FromResult(XmlDocumentLoader.Load(SequenceBuilder.Chunks(bytes, 3), options, baseUri));
            case InputKind.StreamWhole:
                return LoadStreamAsync(new ChunkedReadStream(bytes, _ => 4096), options, baseUri, cancellationToken);
            default:
                return LoadStreamAsync(new ChunkedReadStream(bytes, _ => 1), options, baseUri, cancellationToken);
        }
    }

    private static async Task<XmlDocumentTree> LoadStreamAsync(
        ChunkedReadStream stream,
        XmlReaderOptions? options,
        Uri? baseUri,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using (stream)
        {
            return await XmlDocumentLoader.LoadAsync(stream, options, baseUri, cancellationToken);
        }
    }

    private static async Task<XmlParseException> FailAsync(
        InputKind kind,
        string xml,
        XmlReaderOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _ = await LoadAsync(kind, xml, options, cancellationToken: cancellationToken);
        }
        catch (XmlParseException exception)
        {
            return exception;
        }

        throw new InvalidOperationException("The load did not fail.");
    }

    // Every node with its kind, name, value, and position, element positions by index: deep equality as one string.
    private static string Dump(XmlDocumentTree tree)
    {
        var builder = new StringBuilder();
        DumpNode(tree, tree.Document, 0, builder);
        return builder.ToString();
    }

    private static void DumpNode(XmlDocumentTree tree, XmlNode node, int depth, StringBuilder builder)
    {
        var name = node.Name;
        _ = builder
            .Append(' ', depth * 2)
            .Append(node.Kind)
            .Append(' ')
            .Append(tree.Names.GetString(name.Prefix))
            .Append('|')
            .Append(tree.Names.GetString(name.Local))
            .Append('|')
            .Append(tree.Names.GetString(name.NamespaceUri))
            .Append(" = ")
            .Append(
                node.StringValue.Replace("\n", "\\n", StringComparison.Ordinal)
                    .Replace("\r", "\\r", StringComparison.Ordinal)
            )
            .Append(" @")
            .Append(tree.GetPosition(node.Index))
            .Append(" base ")
            .Append(node.BaseUri)
            .AppendLine();
        for (var ns = node.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
        {
            DumpNode(tree, ns, depth + 1, builder);
        }

        for (var attribute = node.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            DumpNode(tree, attribute, depth + 1, builder);
        }

        for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            DumpNode(tree, child, depth + 1, builder);
        }
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_SameDocument_IsDeepEqualIncludingPositionsForEveryInputKind(
        InputKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expected = Dump(await LoadAsync(InputKind.CharSpan, Hazards, cancellationToken: cancellationToken));

        var actual = Dump(await LoadAsync(kind, Hazards, cancellationToken: cancellationToken));

        await Assert.That(actual).IsEqualTo(expected);
        await Assert.That(expected).Contains("|x\U0001F600y|");
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_ElementsCarryTheirStartPosition(InputKind kind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tree = await LoadAsync(kind, "<a>\n  <b/>\n</a>", cancellationToken: cancellationToken);

        var a = tree.Document.FirstChild;
        var b = a.FirstChild.NextSibling;

        await Assert.That(tree.GetPosition(a.Index)).IsEqualTo(new XmlPosition(1, 1, 0));
        await Assert.That(tree.GetPosition(b.Index)).IsEqualTo(new XmlPosition(2, 3, 6));
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_WellFormednessError_GivesSameCodeAndPositionForEveryInputKind(
        InputKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        string[] documents = ["<a></b>", "<a>\r\n<b>\n  </a>", "<a><b>", "", "<a x='1' x='2'/>", "<a>é\U0001D11E</b>"];
        foreach (var document in documents)
        {
            var expected = await FailAsync(InputKind.CharSpan, document, cancellationToken: cancellationToken);

            var actual = await FailAsync(kind, document, cancellationToken: cancellationToken);

            await Assert.That(actual.Diagnostic!.Code.Alias).IsEqualTo(expected.Diagnostic!.Code.Alias);
            await Assert.That(actual.Diagnostic.Position).IsEqualTo(expected.Diagnostic.Position);
        }
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_MismatchedEndTag_ThrowsXmlParseExceptionWithTheDiagnostic(
        InputKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exception = await FailAsync(kind, "<a>\n<b></a>", cancellationToken: cancellationToken);

        await Assert.That(exception.Diagnostic!.Code.Alias).IsEqualTo("XML0005");
        await Assert.That(exception.Diagnostic.Position.Line).IsEqualTo(2);
        await Assert.That(exception.Message).IsEqualTo(exception.Diagnostic.Message);
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_LimitFromOptions_GivesSameCodeAndPositionForEveryInputKind(
        InputKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxElementDepth(2));
        const string Deep = "<a><b>\n<c><d/></c></b></a>";
        var expected = await FailAsync(InputKind.CharSpan, Deep, options, cancellationToken: cancellationToken);

        var actual = await FailAsync(kind, Deep, options, cancellationToken: cancellationToken);

        await Assert.That(actual.Diagnostic!.Code.Alias).IsEqualTo(expected.Diagnostic!.Code.Alias);
        await Assert.That(actual.Diagnostic.Position).IsEqualTo(expected.Diagnostic.Position);
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_DtdProcessingFromOptions_ExpandsInternalEntitiesForEveryInputKind(
        InputKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string Xml = "<!DOCTYPE a [<!ENTITY e \"x&#233;y\">]><a>&e;</a>";

        var tree = await LoadAsync(
            kind,
            Xml,
            XmlReaderOptions.Default.WithDtdProcessing(true),
            cancellationToken: cancellationToken
        );
        var rejected = await FailAsync(kind, Xml, cancellationToken: cancellationToken);

        await Assert.That(tree.Document.FirstChild.StringValue).IsEqualTo("xéy");
        await Assert.That(rejected.Diagnostic!.Code.Alias).IsEqualTo("XML2001");
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_NameTableFromOptions_IsSharedAndNotFrozen(
        InputKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var table = new SpanInterningNameTable(1000);
        var options = XmlReaderOptions.Default.WithNameTable(table);

        var first = await LoadAsync(kind, "<a><b/></a>", options, cancellationToken: cancellationToken);
        var second = await LoadAsync(kind, "<b><a/></b>", options, cancellationToken: cancellationToken);

        await Assert.That(first.Names).IsSameReferenceAs(table);
        await Assert.That(second.Names).IsSameReferenceAs(table);
        await Assert.That(table.IsFrozen).IsFalse();
        await Assert
            .That(first.Document.FirstChild.Name.Local)
            .IsEqualTo(second.Document.FirstChild.FirstChild.Name.Local);
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_WithoutNameTable_GivesEachDocumentItsOwnFrozenTable(
        InputKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var first = await LoadAsync(kind, "<a/>", cancellationToken: cancellationToken);
        var second = await LoadAsync(kind, "<a/>", cancellationToken: cancellationToken);

        await Assert.That(first.Names).IsNotSameReferenceAs(second.Names);
        await Assert.That(first.Names.IsFrozen).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_BaseUri_IsReportedByTheDocumentNodeAndItsDescendants(
        InputKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var baseUri = new Uri("http://example.test/dir/doc.xml");

        var tree = await LoadAsync(
            kind,
            "<a><b xml:base='sub/'><c/></b></a>",
            baseUri: baseUri,
            cancellationToken: cancellationToken
        );

        var a = tree.Document.FirstChild;
        var b = a.FirstChild;
        await Assert.That(tree.Document.BaseUri).IsEqualTo(baseUri);
        await Assert.That(tree.Document.DocumentUri).IsEqualTo(baseUri);
        await Assert.That(a.BaseUri).IsEqualTo(baseUri);
        await Assert.That(b.FirstChild.BaseUri).IsEqualTo(new Uri("http://example.test/dir/sub/"));
    }

    [Test]
    [MethodDataSource(nameof(Kinds))]
    public async Task Load_WithoutBaseUri_ReportsNone(InputKind kind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tree = await LoadAsync(kind, "<a/>", cancellationToken: cancellationToken);

        await Assert.That(tree.Document.BaseUri).IsNull();
        await Assert.That(tree.Document.DocumentUri).IsNull();
    }

    [Test]
    public async Task Load_RelativeBaseUri_IsReportedAsGiven(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var relative = new Uri("dir/doc.xml", UriKind.Relative);

        var tree = XmlDocumentLoader.Load("<a/>".AsSpan(), null, relative);

        await Assert.That(tree.Document.BaseUri).IsEqualTo(relative);
    }

    [Test]
    public async Task WithBaseUri_AbsoluteUri_ReachesTheOptionsAsAbsoluteString(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = XmlDocumentLoader.WithBaseUri(null, new Uri("http://example.test/dir/doc.xml"));

        await Assert.That(options.BaseUri).IsEqualTo("http://example.test/dir/doc.xml");
    }

    [Test]
    public async Task WithBaseUri_NoOrRelativeUri_KeepsTheOptionsBaseUri(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = XmlReaderOptions.Default.WithBaseUri("http://example.test/kept.xml");

        var none = XmlDocumentLoader.WithBaseUri(options, null);
        var relative = XmlDocumentLoader.WithBaseUri(options, new Uri("rel.xml", UriKind.Relative));
        var untouched = XmlDocumentLoader.WithBaseUri(null, null);

        await Assert.That(none).IsSameReferenceAs(options);
        await Assert.That(relative).IsSameReferenceAs(options);
        await Assert.That(untouched).IsSameReferenceAs(XmlReaderOptions.Default);
    }

    [Test]
    public async Task LoadAsync_CanceledToken_ThrowsOperationCanceledException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await source.CancelAsync();
        using var stream = new ChunkedReadStream(Encoding.UTF8.GetBytes("<a/>"), _ => 4096);

        await Assert
            .That(async () => await XmlDocumentLoader.LoadAsync(stream, cancellationToken: source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task LoadAsync_CancelDuringARead_ThrowsOperationCanceledException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var stream = new ChunkedReadStream(Encoding.UTF8.GetBytes("<a>" + new string('x', 400) + "</a>"), _ => 20)
        {
            CancelOnRead = 3,
            Source = source,
        };

        await Assert
            .That(async () => await XmlDocumentLoader.LoadAsync(stream, cancellationToken: source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task LoadAsync_StreamFault_PropagatesTheStreamException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new ChunkedReadStream(Encoding.UTF8.GetBytes("<a>" + new string('x', 400) + "</a>"), _ => 20)
        {
            FaultOnRead = 2,
        };

        await Assert
            .That(async () => await XmlDocumentLoader.LoadAsync(stream, cancellationToken: cancellationToken))
            .Throws<IOException>();
    }

    [Test]
    public async Task LoadAsync_Stream_StaysOpenAndIsReadFromItsCurrentPosition(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("junk<a>t</a>"));
        _ = stream.Seek(4, SeekOrigin.Begin);

        var tree = await XmlDocumentLoader.LoadAsync(stream, cancellationToken: cancellationToken);

        await Assert.That(tree.Document.StringValue).IsEqualTo("t");
        await Assert.That(stream.CanRead).IsTrue();
    }

    [Test]
    public async Task LoadAsync_NullStream_ThrowsArgumentNullException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(async () => await XmlDocumentLoader.LoadAsync(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Load_ByteInputWithByteOrderMark_DecodesAndKeepsPositions(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] bytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("<a>\n<b/></a>")];

        var tree = XmlDocumentLoader.Load(bytes.AsSpan());

        await Assert.That(tree.GetPosition(tree.Document.FirstChild.Index)).IsEqualTo(new XmlPosition(1, 1, 0));
    }

    [Test]
    public async Task Load_EmptyMultiSegmentSequence_ThrowsXmlParseException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var empty = SequenceBuilder.Chunks(Array.Empty<byte>(), 3, withEmptySegments: true);

        await Assert.That(() => XmlDocumentLoader.Load(empty)).Throws<XmlParseException>();
    }
}
