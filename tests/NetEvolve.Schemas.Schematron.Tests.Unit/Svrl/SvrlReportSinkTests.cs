namespace NetEvolve.Schemas.Schematron.Tests.Unit.Svrl;

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Xml.Writer;

public class SvrlReportSinkTests
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";

    private const string Snapshot = """
        <?xml version="1.0" encoding="UTF-8"?><svrl:schematron-output xmlns:svrl="http://purl.oclc.org/dsdl/svrl" xmlns:inv="urn:inv" title="Order checks" phase="main" schemaVersion="1.0" schematronEdition="2025">
          <svrl:ns-prefix-in-attribute-values prefix="inv" uri="urn:inv"/>
          <svrl:active-pattern id="p1" name="Order"/>
          <svrl:fired-rule id="r1" context="inv:order"/>
          <svrl:failed-assert id="a1" location="/inv:order[1]" test="@id" role="error" flag="f1 f2" severity="fatal">
            <svrl:diagnostic-reference diagnostic="d1">
              <svrl:text>Diag text</svrl:text>
            </svrl:diagnostic-reference>
            <svrl:property-reference property="pr1">
              <svrl:text>Prop &amp; text</svrl:text>
            </svrl:property-reference>
            <svrl:text>Order needs an id</svrl:text>
          </svrl:failed-assert>
          <svrl:successful-report location="/inv:order[1]" test="count(item) &gt; 1">
            <svrl:text>Many &lt;items&gt;</svrl:text>
          </svrl:successful-report>
          <svrl:active-group id="g1">
            <svrl:error document="file:///doc.xml" location="/" code="NES3301">bad</svrl:error>
          </svrl:active-group>
          <svrl:fired-rule context="inv:item" role="warn" flag="x"/>
        </svrl:schematron-output>
        """;

    private static readonly string[] TopLevelOrder = ["error", "ns-prefix-in-attribute-values", "active-pattern"];

    private static readonly string[] DeclaredNamespaces =
    [
        "svrl=http://purl.oclc.org/dsdl/svrl",
        "dup=urn:1",
        "ok=urn:ok",
    ];

    private static readonly string[] ReportedPrefixes = ["svrl", "xml", "xmlns", "dup", "dup", "ok"];

    private static readonly SvrlWriterOptions Indented = new(indent: true);

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Write(Action<ISchematronReportSink> run, SvrlWriterOptions? options = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var sink = new SvrlReportSink(buffer, options);
        run(sink);
        return SvrlRuns.Text(buffer.WrittenSpan.ToArray());
    }

    private static void Discard(SvrlReportSink sink) => sink.Dispose();

    private static XElement Root(string text) => XDocument.Parse(text).Root!;

    [Test]
    public async Task Svrl_FixedEventList_MatchesStoredSnapshot()
    {
        var text = Write(SvrlRuns.Fixed, Indented);

        await Assert.That(text).IsEqualTo(Normalize(Snapshot));
    }

    [Test]
    [Arguments("tr-TR")]
    [Arguments("ar-SA")]
    public async Task Svrl_FixedEventList_MatchesStoredSnapshot_UnderCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            var text = Write(SvrlRuns.Fixed, Indented);

            await Assert.That(text).IsEqualTo(Normalize(Snapshot));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public async Task Svrl_NotIndented_HasSameContentWithoutLayoutWhitespace()
    {
        var plain = Write(SvrlRuns.Fixed);
        var indented = Write(SvrlRuns.Fixed, Indented);

        await Assert.That(plain.Contains('\n', StringComparison.Ordinal)).IsFalse();
        await Assert
            .That(
                plain.StartsWith(
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><svrl:schematron-output",
                    StringComparison.Ordinal
                )
            )
            .IsTrue();
        await Assert.That(Root(plain).Descendants().Count()).IsEqualTo(Root(indented).Descendants().Count());
        await Assert.That(plain.Length).IsLessThan(indented.Length);
    }

    [Test]
    public async Task Svrl_StreamAndBufferWriter_AreByteIdentical(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var buffer = new ArrayBufferWriter<byte>();
        using (var sink = new SvrlReportSink(buffer))
        {
            SvrlRuns.Fixed(sink);
        }

        using var stream = new MemoryStream();
        await using (var sink = new SvrlReportSink(stream))
        {
            SvrlRuns.Fixed(sink);
            await sink.CompleteAsync(cancellationToken);
        }

        await Assert.That(stream.ToArray()).IsEquivalentTo(buffer.WrittenSpan.ToArray());
        await Assert.That(stream.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Svrl_StreamPath_MakesNoSynchronousWrite(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new AsyncOnlyStream();

        await using (var sink = new SvrlReportSink(stream))
        {
            SvrlRuns.Fixed(sink);
            await Assert.That(stream.Length).IsEqualTo(0);
            await sink.CompleteAsync(cancellationToken);
            await Assert.That(sink.IsCompleted).IsTrue();
        }

        await Assert.That(stream.AsyncWrites).IsGreaterThan(0);
        await Assert.That(stream.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Svrl_BufferWriterPath_IsCompletedAtFinish()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var sink = new SvrlReportSink(buffer);

        SvrlRuns.Begin(sink, "#ALL", SchematronEdition.Iso2016);
        var before = sink.IsCompleted;
        SvrlRuns.Pattern(sink, "p");
        SvrlRuns.Finish(sink, false);

        await Assert.That(before).IsFalse();
        await Assert.That(sink.IsCompleted).IsTrue();
        await Assert.That(buffer.WrittenCount).IsGreaterThan(0);
    }

    [Test]
    public async Task Svrl_StreamPath_NotCompleted_NothingWritten_IsDetectable()
    {
        using var stream = new MemoryStream();
        var sink = new SvrlReportSink(stream);
        SvrlRuns.Fixed(sink);

        Discard(sink);

        await Assert.That(sink.IsCompleted).IsFalse();
        await Assert.That(stream.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Svrl_DisposeAsync_AfterFinish_CompletesTheStreamPath()
    {
        using var stream = new MemoryStream();
        var sink = new SvrlReportSink(stream);
        SvrlRuns.Fixed(sink);

        await sink.DisposeAsync();

        await Assert.That(sink.IsCompleted).IsTrue();
        await Assert.That(stream.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Svrl_Dispose_AfterCancelledRun_NeverThrowsAndWritesNothing()
    {
        using var stream = new MemoryStream();
        var sink = new SvrlReportSink(stream);
        SvrlRuns.Begin(sink, "#ALL", SchematronEdition.Iso2016);
        SvrlRuns.Pattern(sink, "p");

        await sink.DisposeAsync();
        Discard(sink);

        await Assert.That(sink.IsCompleted).IsFalse();
        await Assert.That(stream.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Svrl_CompleteAsync_BeforeFinish_Throws()
    {
        using var stream = new MemoryStream();
        using var sink = new SvrlReportSink(stream);
        SvrlRuns.Begin(sink, "#ALL", SchematronEdition.Iso2016);

        await Assert.That(async () => await sink.CompleteAsync()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Svrl_CompleteAsync_Cancelled_Throws_AndStaysIncomplete()
    {
        using var stream = new MemoryStream();
        using var sink = new SvrlReportSink(stream);
        SvrlRuns.Fixed(sink);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.That(async () => await sink.CompleteAsync(cts.Token)).Throws<OperationCanceledException>();
        await Assert.That(sink.IsCompleted).IsFalse();
    }

    [Test]
    public async Task Svrl_UseAfterComplete_Throws()
    {
        using var sink = new SvrlReportSink(new ArrayBufferWriter<byte>());
        SvrlRuns.Fixed(sink);

        await Assert.That(() => SvrlRuns.Pattern(sink, "late")).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Svrl_UseAfterDispose_Throws()
    {
        var sink = new SvrlReportSink(new ArrayBufferWriter<byte>());
        Discard(sink);

        await Assert
            .That(() => SvrlRuns.Begin(sink, "#ALL", SchematronEdition.Iso2016))
            .Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task Svrl_CallbackBeforeBegin_Throws()
    {
        using var sink = new SvrlReportSink(new ArrayBufferWriter<byte>());

        await Assert.That(() => SvrlRuns.Pattern(sink, "p")).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Svrl_AllPhase_OmitsPhaseAttribute()
    {
        var text = Write(s =>
        {
            SvrlRuns.Begin(s, "#ALL", SchematronEdition.Iso2016);
            SvrlRuns.Pattern(s, "p");
            SvrlRuns.Finish(s, false);
        });

        await Assert.That(Root(text).Attribute("phase")).IsNull();
    }

    [Test]
    public async Task Svrl_Edition2016_OmitsEditionAndEdition2025WritesIt()
    {
        var old = Write(s =>
        {
            SvrlRuns.Begin(s, "main", SchematronEdition.Iso2016);
            SvrlRuns.Pattern(s, "p");
            SvrlRuns.Finish(s, false);
        });

        await Assert.That(Root(old).Attribute("schematronEdition")).IsNull();
        await Assert.That(Root(old).Attribute("phase")!.Value).IsEqualTo("main");
        await Assert.That(Root(Write(SvrlRuns.Fixed)).Attribute("schematronEdition")!.Value).IsEqualTo("2025");
    }

    [Test]
    [Arguments("a1#i2")]
    [Arguments("#p1.r1.a1")]
    [Arguments("r1#a2")]
    [Arguments("")]
    public async Task Svrl_GeneratedOrExpandedId_IsNotWrittenToIdAttribute(string id)
    {
        var text = Write(s =>
        {
            SvrlRuns.Begin(s, "#ALL", SchematronEdition.Iso2016);
            SvrlRuns.Pattern(s, "p");
            SvrlRuns.FiredRule(s, "r", "a");
            SvrlRuns.Assert(s, id, "m");
            SvrlRuns.Finish(s, false);
        });

        var assert = Root(text).Element(Svrl + "failed-assert")!;

        await Assert.That(assert.Attribute("id")).IsNull();
        await Assert.That(assert.Attribute("location")!.Value).IsEqualTo("/");
        await Assert.That(assert.Attribute("test")!.Value).IsEqualTo("true()");
    }

    [Test]
    public async Task Svrl_AuthorId_IsWrittenToIdAttribute()
    {
        var text = Write(s =>
        {
            SvrlRuns.Begin(s, "#ALL", SchematronEdition.Iso2016);
            SvrlRuns.Pattern(s, "p");
            SvrlRuns.FiredRule(s, "r", "a");
            SvrlRuns.Assert(s, "a1", "m");
            SvrlRuns.Finish(s, false);
        });

        await Assert.That(Root(text).Element(Svrl + "failed-assert")!.Attribute("id")!.Value).IsEqualTo("a1");
    }

    [Test]
    [Arguments("NES1005")]
    [Arguments("NES3501")]
    public async Task Svrl_NoEvaluation_WritesTopLevelError(string code)
    {
        var text = Write(s => SvrlRuns.TopLevelError(s, code));
        var root = Root(text);
        var error = root.Elements().Single();

        await Assert.That(error.Name).IsEqualTo(Svrl + "error");
        await Assert.That(error.Attribute("document")!.Value).IsEqualTo(SvrlRuns.Document);
        await Assert.That(error.Attribute("location")!.Value).IsEqualTo("/");
        await Assert.That(error.Attribute("code")!.Value).IsEqualTo(code);
        await Assert.That(error.Value).IsEqualTo("no evaluation");
    }

    [Test]
    public async Task Svrl_ErrorBeforeFirstPattern_PrecedesNamespaceReports()
    {
        var text = Write(s =>
        {
            SvrlRuns.Begin(s, "#ALL", SchematronEdition.Iso2016);
            SvrlRuns.Error(s, "NES3301", "early");
            SvrlRuns.Pattern(s, "p");
            SvrlRuns.Finish(s, false);
        });

        var names = Root(text).Elements().Select(e => e.Name.LocalName).ToArray();

        await Assert.That(names).IsEquivalentTo(TopLevelOrder);
    }

    [Test]
    public async Task Svrl_Truncated_AppendsComment_AtTheEvaluationStopPoint()
    {
        var text = Write(s =>
        {
            SvrlRuns.Begin(s, "#ALL", SchematronEdition.Iso2016);
            SvrlRuns.Pattern(s, "p");
            SvrlRuns.FiredRule(s, "r", "a");
            SvrlRuns.Assert(s, "a1", "m");
            SvrlRuns.Finish(s, true);
        });

        var document = XDocument.Parse(text);
        var comment = document.Nodes().OfType<XComment>().Single();

        await Assert.That(comment.Value.Contains("NES3502", StringComparison.Ordinal)).IsTrue();
        await Assert.That(document.Nodes().Last()).IsSameReferenceAs(comment);
        await Assert.That(document.Root!.Elements(Svrl + "failed-assert").Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Svrl_NotTruncated_HasNoComment()
    {
        var document = XDocument.Parse(Write(SvrlRuns.Fixed));

        await Assert.That(document.DescendantNodes().OfType<XComment>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Svrl_ManyPassingFiredRules_BufferIsBoundedOrStreams()
    {
        var options = new SvrlWriterOptions(maxBufferedBytes: 512);

        var text = Write(s => SvrlRuns.ManyFiredRules(s, 1000), options);
        var root = Root(text);
        var big = root.Elements(Svrl + "active-pattern").First();
        var error = big.Element(Svrl + "error")!;

        await Assert.That(root.Elements(Svrl + "fired-rule").Count()).IsEqualTo(1);
        await Assert.That(error.Attribute("code")!.Value).IsEqualTo("NES3500");
        await Assert.That(error.Attribute("location")!.Value).IsEqualTo("/");
        await Assert.That(root.Elements(Svrl + "failed-assert").Single().Attribute("id")!.Value).IsEqualTo("a9");
        await Assert.That(text.Length).IsLessThan(4000);
    }

    [Test]
    public async Task Svrl_PatternWithinTheBuffer_IsWrittenInFull()
    {
        var text = Write(s => SvrlRuns.ManyFiredRules(s, 20));
        var root = Root(text);

        await Assert.That(root.Elements(Svrl + "fired-rule").Count()).IsEqualTo(21);
        await Assert.That(root.Descendants(Svrl + "error").Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Svrl_NamespaceDeclarationsOption_ControlsRootDeclarationsOnly()
    {
        var with = Root(Write(SvrlRuns.Fixed));
        var without = Root(Write(SvrlRuns.Fixed, new SvrlWriterOptions(emitNamespaceDeclarations: false)));

        await Assert.That(with.Attribute(XNamespace.Xmlns + "inv")!.Value).IsEqualTo("urn:inv");
        await Assert.That(without.Attribute(XNamespace.Xmlns + "inv")).IsNull();
        await Assert.That(without.Element(Svrl + "ns-prefix-in-attribute-values")).IsNotNull();
    }

    [Test]
    public async Task Svrl_Utf16_WritesByteOrderMark()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var sink = new SvrlReportSink(buffer, new SvrlWriterOptions(XmlWriterEncoding.Utf16)))
        {
            SvrlRuns.Fixed(sink);
        }

        var bytes = buffer.WrittenSpan.ToArray();

        await Assert.That(bytes[0]).IsEqualTo((byte)0xFF);
        await Assert.That(bytes[1]).IsEqualTo((byte)0xFE);
    }

    [Test]
    public async Task Svrl_BeginTwice_Throws()
    {
        using var sink = new SvrlReportSink(new ArrayBufferWriter<byte>());
        SvrlRuns.Begin(sink, "#ALL", SchematronEdition.Iso2016);

        await Assert
            .That(() => SvrlRuns.Begin(sink, "#ALL", SchematronEdition.Iso2016))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Svrl_RuleOrFindingWithoutActivePattern_Throws()
    {
        using var sink = new SvrlReportSink(new ArrayBufferWriter<byte>());
        SvrlRuns.Begin(sink, "#ALL", SchematronEdition.Iso2016);

        await Assert.That(() => SvrlRuns.FiredRule(sink, "r", "a")).Throws<InvalidOperationException>();
        await Assert.That(() => SvrlRuns.Assert(sink, "a1", "m")).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Svrl_AfterOverflow_LaterFindingsAndErrorsOfThePatternAreDropped()
    {
        var text = Write(
            s =>
            {
                SvrlRuns.Begin(s, "#ALL", SchematronEdition.Iso2016);
                SvrlRuns.Pattern(s, "big");
                for (var i = 0; i < 100; i++)
                {
                    SvrlRuns.FiredRule(s, "r", "context-" + i);
                }

                SvrlRuns.Assert(s, "late", "dropped");
                SvrlRuns.Error(s, "NES3301", "dropped too");
                SvrlRuns.Finish(s, false);
            },
            new SvrlWriterOptions(maxBufferedBytes: 256)
        );

        var root = Root(text);
        var errors = root.Descendants(Svrl + "error").ToArray();

        await Assert.That(errors.Length).IsEqualTo(1);
        await Assert.That(errors[0].Attribute("code")!.Value).IsEqualTo("NES3500");
        await Assert.That(root.Descendants(Svrl + "failed-assert").Count()).IsEqualTo(0);
        await Assert.That(root.Descendants(Svrl + "fired-rule").Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Svrl_SchemaNamespaces_AreFilteredForDeclarationsButAllReported()
    {
        var text = Write(s =>
        {
            SvrlRuns.Begin(
                s,
                "#ALL",
                SchematronEdition.Iso2016,
                [
                    SvrlRuns.Namespace("", "urn:empty-prefix"),
                    SvrlRuns.Namespace("empty-uri", ""),
                    SvrlRuns.Namespace("svrl", "urn:other"),
                    SvrlRuns.Namespace("xml", "urn:other"),
                    SvrlRuns.Namespace("xmlns", "urn:other"),
                    SvrlRuns.Namespace("dup", "urn:1"),
                    SvrlRuns.Namespace("dup", "urn:2"),
                    SvrlRuns.Namespace("ok", "urn:ok"),
                ]
            );
            SvrlRuns.Pattern(s, "p");
            SvrlRuns.Finish(s, false);
        });

        var root = Root(text);
        var declared = root.Attributes()
            .Where(a => a.IsNamespaceDeclaration)
            .Select(a => a.Name.LocalName + "=" + a.Value)
            .ToArray();
        var reported = root.Elements(Svrl + "ns-prefix-in-attribute-values").Select(e => e.Attribute("prefix")!.Value);

        await Assert.That(declared).IsEquivalentTo(DeclaredNamespaces);
        await Assert.That(reported).IsEquivalentTo(ReportedPrefixes);
    }

    [Test]
    public async Task Svrl_CompleteAsync_OnTheBufferWriterPath_IsImmediate_AndThrowsAfterDispose(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sink = new SvrlReportSink(new ArrayBufferWriter<byte>());
        SvrlRuns.Fixed(sink);

        await sink.CompleteAsync(cancellationToken);
        await Assert.That(sink.IsCompleted).IsTrue();

        await sink.DisposeAsync();
        await Assert.That(async () => await sink.CompleteAsync(cancellationToken)).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task Svrl_ErrorWithoutCode_OmitsTheCodeAttribute()
    {
        var text = Write(s =>
        {
            SvrlRuns.Begin(s, "#ALL", SchematronEdition.Iso2016);
            SvrlRuns.ErrorWithoutCode(s);
            SvrlRuns.Finish(s, false);
        });

        var error = Root(text).Element(Svrl + "error")!;

        await Assert.That(error.Attribute("code")).IsNull();
        await Assert.That(error.Value).IsEqualTo("no code");
    }

    [Test]
    public async Task Svrl_PatternAttributes_AreWritten()
    {
        var text = Write(s =>
        {
            SvrlRuns.Begin(s, "#ALL", SchematronEdition.Iso2016);
            SvrlRuns.PatternWithAttributes(s);
            SvrlRuns.Finish(s, false);
        });

        var pattern = Root(text).Element(Svrl + "active-pattern")!;

        await Assert.That(pattern.Attribute("id")!.Value).IsEqualTo("p");
        await Assert.That(pattern.Attribute("documents")!.Value).IsEqualTo("doc.xml");
        await Assert.That(pattern.Attribute("name")!.Value).IsEqualTo("Named");
        await Assert.That(pattern.Attribute("role")!.Value).IsEqualTo("audit");
    }

    private sealed class AsyncOnlyStream : Stream
    {
        private readonly MemoryStream _inner = new();

        internal int AsyncWrites { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("synchronous write");

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncWrites++;
            _inner.Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncWrites++;
            _inner.Write(buffer.Span);
            return default;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
