namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Sequence;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>ReadOnlySequence input (XML-56): segment-by-segment feeding of the resumable core, equal to the span path.</summary>
public class XmlSequenceReaderTests
{
    private static readonly int[] SeededRuns = [17, 2026, 31337];

    public static IEnumerable<Func<SequenceSample>> Samples() => SequenceRuns.AllSamples();

    public static IEnumerable<Func<Boundary>> Boundaries()
    {
        var utf8 = Encoding.UTF8;
        var utf16 = new UnicodeEncoding(false, true);
        yield return () => new Boundary("inside a name", "<root><abcdefgh x='1'/></root>", "abcdefgh", utf8);
        yield return () => new Boundary("inside an end tag name", "<abcdefgh>x</abcdefgh>", "</abcdefgh>", utf8);
        yield return () => new Boundary("inside a named reference", "<a>x&amp;y</a>", "&amp;", utf8);
        yield return () => new Boundary("inside a character reference", "<a>x&#x1F600;y</a>", "&#x1F600;", utf8);
        yield return () => new Boundary("inside a reference in an attribute", "<a b=\"x&lt;y\"/>", "&lt;", utf8);
        yield return () => new Boundary("inside the cdata start", "<a><![CDATA[x]]>y</a>", "<![CDATA[", utf8);
        yield return () => new Boundary("inside the cdata end", "<a><![CDATA[x]]>y</a>", "]]>", utf8);
        yield return () => new Boundary("inside the comment end", "<a><!--x-->y</a>", "-->", utf8);
        yield return () => new Boundary("inside the processing instruction end", "<a><?t d?>y</a>", "?>", utf8);
        yield return () => new Boundary("inside a line break", "<a>x\r\ny</a>", "\r\n", utf8);
        yield return () => new Boundary("inside a two-byte utf-8 sequence", "<a>xéy</a>", "é", utf8);
        yield return () => new Boundary("inside a three-byte utf-8 sequence", "<a>x€y</a>", "€", utf8);
        yield return () => new Boundary("inside a four-byte utf-8 sequence", "<a>x\U0001F600y</a>", "\U0001F600", utf8);
        yield return () =>
            new Boundary("inside a utf-8 sequence after a bom", "<a>x\U0001D11Ey</a>", "\U0001D11E", utf8, true);
        yield return () => new Boundary("inside a utf-16 unit", "<a>x€y</a>", "€", utf16, true);
        yield return () =>
            new Boundary("inside a utf-16 surrogate pair", "<a>x\U0001F600y</a>", "\U0001F600", utf16, true);
    }

    public static IEnumerable<Func<Boundary>> CharBoundaries()
    {
        foreach (var factory in Boundaries())
        {
            var boundary = factory();
            if (boundary.Marker.Length > 1)
            {
                yield return () => boundary;
            }
        }
    }

    public static IEnumerable<Func<string>> CharFailures()
    {
        yield return () => "<a></b>";
        yield return () => "<a><b>";
        yield return () => "<a>x]]>y</a>";
        yield return () => "<a b=\"1\" b=\"2\"/>";
        yield return () => " <?xml version=\"1.0\"?><a/>";
        yield return () => "<?xml version=\"x\"?><a/>";
        yield return () => "<a>ab\u0001cd</a>";
        yield return () => "<a>x\uD800y</a>";
        yield return () => "<a>\uDC00</a>";
        yield return () => "<a/>&amp;";
        yield return () => "<!DOCTYPE a><a/>";
        yield return () => "<a><!-- -- --></a>";
        yield return () => string.Empty;
    }

    private static byte[] Utf8(string xml) => Encoding.UTF8.GetBytes(xml);

    private static async Task AssertNoMismatch(List<string> mismatches) => await Assert.That(mismatches).IsEmpty();

    // ---- parity with the span path ----

    [Test]
    [MethodDataSource(nameof(Samples))]
    public async Task Read_BytesInTwoSegmentsAtEveryOffset_GiveTheEventsAndPositionsOfTheSpanPath(
        SequenceSample sample,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(sample);

        var bytes = sample.Bytes;
        var expected = SequenceRuns.Span(sample);
        var mismatches = new List<string>();

        for (var offset = 0; offset <= bytes.Length; offset++)
        {
            var actual = SequenceRuns.Sequence(SequenceBuilder.Split(bytes, offset), sample.Options);
            if (actual != expected)
            {
                mismatches.Add($"offset {offset}: {actual}");
            }
        }

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Completed);
        await AssertNoMismatch(mismatches);
    }

    [Test]
    [MethodDataSource(nameof(Samples))]
    public async Task Read_CharsInTwoSegmentsAtEveryOffset_GiveTheEventsAndPositionsOfTheSpanPath(
        SequenceSample sample,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(sample);

        var chars = sample.Xml.ToCharArray();
        var expected = SequenceRuns.SpanChars(sample.Xml, sample.Options);
        var mismatches = new List<string>();

        for (var offset = 0; offset <= chars.Length; offset++)
        {
            var actual = SequenceRuns.Sequence(SequenceBuilder.Split(chars, offset), sample.Options);
            if (actual != expected)
            {
                mismatches.Add($"offset {offset}: {actual}");
            }
        }

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Completed);
        await AssertNoMismatch(mismatches);
    }

    [Test]
    [MethodDataSource(nameof(Samples))]
    public async Task Read_BytesInOneElementSegmentsAndWithEmptySegments_GiveTheEventsOfTheSpanPath(
        SequenceSample sample,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(sample);

        var bytes = sample.Bytes;
        var expected = SequenceRuns.Span(sample);
        var mismatches = new List<string>();

        void Check(string name, ReadOnlySequence<byte> input)
        {
            var actual = SequenceRuns.Sequence(input, sample.Options);
            if (actual != expected)
            {
                mismatches.Add($"{name}: {actual}");
            }
        }

        Check("one element", SequenceBuilder.Chunks(bytes, 1));
        Check("one element with empty segments", SequenceBuilder.Chunks(bytes, 1, withEmptySegments: true));
        Check("three elements with empty segments", SequenceBuilder.Chunks(bytes, 3, withEmptySegments: true));
        Check("empty segments only around", SequenceBuilder.Split(bytes, 0, 0, bytes.Length, bytes.Length));
        foreach (var seed in SeededRuns)
        {
            Check($"seed {seed}", SequenceBuilder.Random(bytes, seed, 9));
        }

        await AssertNoMismatch(mismatches);
    }

    [Test]
    [MethodDataSource(nameof(Samples))]
    public async Task Read_CharsInOneElementSegmentsAndWithEmptySegments_GiveTheEventsOfTheSpanPath(
        SequenceSample sample,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(sample);

        var chars = sample.Xml.ToCharArray();
        var expected = SequenceRuns.SpanChars(sample.Xml, sample.Options);
        var mismatches = new List<string>();

        void Check(string name, ReadOnlySequence<char> input)
        {
            var actual = SequenceRuns.Sequence(input, sample.Options);
            if (actual != expected)
            {
                mismatches.Add($"{name}: {actual}");
            }
        }

        Check("one element", SequenceBuilder.Chunks(chars, 1));
        Check("one element with empty segments", SequenceBuilder.Chunks(chars, 1, withEmptySegments: true));
        Check("three elements with empty segments", SequenceBuilder.Chunks(chars, 3, withEmptySegments: true));
        Check("empty segments only around", SequenceBuilder.Split(chars, 0, 0, chars.Length, chars.Length));
        foreach (var seed in SeededRuns)
        {
            Check($"seed {seed}", SequenceBuilder.Random(chars, seed, 9));
        }

        await AssertNoMismatch(mismatches);
    }

    [Test]
    public async Task Read_LargeDocumentInSeededRandomSegments_GivesTheEventsOfTheSpanPath(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sample = new SequenceSample("large", SequenceRuns.Medium(600), Encoding.UTF8);
        var bytes = sample.Bytes;
        var chars = sample.Xml.ToCharArray();
        var expectedBytes = SequenceRuns.Span(sample);
        var expectedChars = SequenceRuns.SpanChars(sample.Xml);

        await Assert.That(expectedBytes.Status).IsEqualTo(XmlReadStatus.Completed);
        foreach (var seed in SeededRuns)
        {
            foreach (var max in new[] { 7, 300, 20_000 })
            {
                await Assert
                    .That(SequenceRuns.Sequence(SequenceBuilder.Random(bytes, seed, max)))
                    .IsEqualTo(expectedBytes);
                await Assert
                    .That(SequenceRuns.Sequence(SequenceBuilder.Random(chars, seed, max)))
                    .IsEqualTo(expectedChars);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Boundaries))]
    public async Task Read_BytesSplitInsideAConstruct_GiveTheEventsOfUnsplitInput(
        Boundary boundary,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(boundary);

        var sample = new SequenceSample(boundary.Name, boundary.Xml, boundary.Encoding, boundary.Bom);
        var bytes = sample.Bytes;
        var expected = SequenceRuns.Span(sample);
        var start =
            (boundary.Bom ? boundary.Encoding.GetPreamble().Length : 0)
            + boundary.Encoding.GetByteCount(
                boundary.Xml[..boundary.Xml.IndexOf(boundary.Marker, StringComparison.Ordinal)]
            );
        var length = boundary.Encoding.GetByteCount(boundary.Marker);
        var mismatches = new List<string>();

        for (var inner = 1; inner < length; inner++)
        {
            var actual = SequenceRuns.Sequence(SequenceBuilder.Split(bytes, start + inner));
            if (actual != expected)
            {
                mismatches.Add($"inner offset {inner}: {actual}");
            }
        }

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(length).IsGreaterThan(1);
        await AssertNoMismatch(mismatches);
    }

    [Test]
    [MethodDataSource(nameof(CharBoundaries))]
    public async Task Read_CharsSplitInsideAConstruct_GiveTheEventsOfUnsplitInput(
        Boundary boundary,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(boundary);

        var chars = boundary.Xml.ToCharArray();
        var expected = SequenceRuns.SpanChars(boundary.Xml);
        var start = boundary.Xml.IndexOf(boundary.Marker, StringComparison.Ordinal);
        var mismatches = new List<string>();

        for (var inner = 1; inner < boundary.Marker.Length; inner++)
        {
            var actual = SequenceRuns.Sequence(SequenceBuilder.Split(chars, start + inner));
            if (actual != expected)
            {
                mismatches.Add($"inner offset {inner}: {actual}");
            }
        }

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Completed);
        await AssertNoMismatch(mismatches);
    }

    [Test]
    public async Task Read_SplitInsideASurrogatePairOfText_KeepsThePairInOneTextRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The dump merges text events, so the assertion is on the merged text: the pair is not torn by the cut.
        var xml = "<a>x\U0001F600y</a>";
        var chars = xml.ToCharArray();
        var pair = xml.IndexOf('\uD83D', StringComparison.Ordinal);

        var run = SequenceRuns.Sequence(SequenceBuilder.Split(chars, pair + 1));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).Contains("Text'x\U0001F600y'");
    }

    // ---- malformed input ----

    [Test]
    [MethodDataSource(typeof(XmlStreamReaderTests), nameof(XmlStreamReaderTests.Failures))]
    public async Task Read_MalformedBytes_ReportTheErrorCodeAndPositionOfTheSpanPath(
        string _,
        byte[] bytes,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(bytes);

        var expected = SequenceRuns.WithoutPosition(StreamRuns.Span(bytes));
        var mismatches = new List<string>();

        void Check(string name, ReadOnlySequence<byte> input)
        {
            var actual = SequenceRuns.WithoutPosition(SequenceRuns.Sequence(input));
            if (actual != expected)
            {
                mismatches.Add($"{name}: {actual}");
            }
        }

        for (var offset = 0; offset <= bytes.Length; offset++)
        {
            Check($"offset {offset}", SequenceBuilder.Split(bytes, offset));
        }

        Check("one element", SequenceBuilder.Chunks(bytes, 1));
        Check("one element with empty segments", SequenceBuilder.Chunks(bytes, 1, withEmptySegments: true));
        Check("whole", SequenceBuilder.Whole(bytes));

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Error);
        await AssertNoMismatch(mismatches);
    }

    [Test]
    [MethodDataSource(nameof(CharFailures))]
    public async Task Read_MalformedChars_ReportTheErrorCodeAndPositionOfTheSpanPath(
        string xml,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(xml);

        var chars = xml.ToCharArray();
        var expected = SequenceRuns.WithoutPosition(SequenceRuns.SpanChars(xml));
        var mismatches = new List<string>();

        void Check(string name, ReadOnlySequence<char> input)
        {
            var actual = SequenceRuns.WithoutPosition(SequenceRuns.Sequence(input));
            if (actual != expected)
            {
                mismatches.Add($"{name}: {actual}");
            }
        }

        for (var offset = 0; offset <= chars.Length; offset++)
        {
            Check($"offset {offset}", SequenceBuilder.Split(chars, offset));
        }

        Check("one element", SequenceBuilder.Chunks(chars, 1));
        Check("one element with empty segments", SequenceBuilder.Chunks(chars, 1, withEmptySegments: true));
        Check("whole", SequenceBuilder.Whole(chars));

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Error);
        await AssertNoMismatch(mismatches);
    }

    [Test]
    public async Task Read_InvalidUtf8InALaterSegment_ReportsXml4003AfterTheEventsBeforeIt(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] bytes = [.. Utf8("<a>ok"), 0xFF, .. Utf8("</a>")];

        var run = SequenceRuns.Sequence(SequenceBuilder.Chunks(bytes, 2));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML4003);
        await Assert.That(run.Events).Contains("Text'ok'");
    }

    [Test]
    public async Task Read_DeclarationInsideTheHead_ConfirmsTheDeclaredEncodingLikeTheSpanPath(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The head is 4 x MaxXmlDeclarationLength + 4 bytes; a declaration inside it is read by the same rule as the span path.
        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxXmlDeclarationLength(16));
        var bytes = Utf8("<?xml version=\"1.0\" encoding=\"UTF-16\"?><a/>");

        var span = StreamRuns.Span(bytes, options);
        var sequence = SequenceRuns.Sequence(SequenceBuilder.Chunks(bytes, 3), options);

        await Assert.That(span.Code).IsSameReferenceAs(XmlErrorCodes.XML4002);
        await Assert.That(SequenceRuns.WithoutPosition(sequence)).IsEqualTo(SequenceRuns.WithoutPosition(span));
    }

    [Test]
    public async Task Read_DeclarationBeyondTheHead_ReportsTheLimitInsteadOfTheEncodingConflict(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Limitation (decision "encoding head"): a declaration whose end lies beyond 4 x MaxXmlDeclarationLength + 4
        // bytes is not confirmed against the detected encoding; it exceeds the limit anyway.
        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxXmlDeclarationLength(8));
        var bytes = Utf8("<?xml version=\"1.0\"" + new string(' ', 100) + "encoding=\"UTF-16\"?><a/>");

        var span = StreamRuns.Span(bytes, options);
        var sequence = SequenceRuns.Sequence(SequenceBuilder.Chunks(bytes, 7), options);

        await Assert.That(span.Code).IsSameReferenceAs(XmlErrorCodes.XML4002);
        await Assert.That(sequence.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(sequence.Code).IsSameReferenceAs(XmlErrorCodes.XML1114);
    }

    // ---- limits, large tokens, sinks ----

    [Test]
    public async Task Read_TokenLimitInOptions_IsEnforcedThroughTheSequence(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxTokenLength(1_000));
        var xml = "<a><!--" + new string('x', 5_000) + "--></a>";

        var bytes = SequenceRuns.Sequence(SequenceBuilder.Chunks(Utf8(xml), 13), options);
        var chars = SequenceRuns.Sequence(SequenceBuilder.Chunks(xml.ToCharArray(), 13), options);

        await Assert.That(bytes.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(bytes.Code).IsSameReferenceAs(SequenceRuns.SpanChars(xml, options).Code);
        await Assert.That(chars.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(chars.Code).IsSameReferenceAs(bytes.Code);
    }

    [Test]
    public async Task Read_DocumentCharacterLimit_IsEnforcedThroughTheSequence(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxDocumentCharacters(8));
        var xml = "<a>" + new string('x', 100_000) + "</a>";

        var bytes = SequenceRuns.Sequence(SequenceBuilder.Chunks(Utf8(xml), 4_000), options);
        var chars = SequenceRuns.Sequence(SequenceBuilder.Chunks(xml.ToCharArray(), 4_000), options);

        await Assert.That(bytes.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(bytes.Code).IsSameReferenceAs(XmlErrorCodes.XML1101);
        await Assert.That(chars.Code).IsSameReferenceAs(XmlErrorCodes.XML1101);
    }

    [Test]
    [Timeout(60_000)]
    public async Task Read_OneElementSegmentsWithLargeTokens_EqualTheSpanPath(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // One-element segments with a comment of 300 000 characters would rescan it per segment (about 4.5e10 character
        // scans) if the feeder did not follow the rule of #108 AP2; the test then runs into its timeout.
        var xml =
            "<a><!--"
            + new string('x', 300_000)
            + "--><b>"
            + new string('y', 300_000)
            + "</b><?p "
            + new string('z', 300_000)
            + "?></a>";
        var sample = new SequenceSample("tokens", xml, Encoding.UTF8);

        var bytes = SequenceRuns.Sequence(SequenceBuilder.Chunks(sample.Bytes, 1));
        var chars = SequenceRuns.Sequence(SequenceBuilder.Chunks(xml.ToCharArray(), 1));

        await Assert.That(bytes.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(bytes).IsEqualTo(SequenceRuns.Span(sample));
        await Assert.That(chars).IsEqualTo(SequenceRuns.SpanChars(xml));
    }

    [Test]
    public async Task Pump_StructSink_KeepsItsStateAcrossSegments(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var names = StreamRuns.NewSink().Names;
        var sink = new CountingStructSink(names);
        using var reader = XmlSequenceReader.Create(SequenceBuilder.Chunks(Utf8(SequenceRuns.Medium(300)), 37));

        var status = reader.Pump(ref sink);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(sink.Count).IsGreaterThan(1_000);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Pump_AfterTheEnd_ReturnsTheSameStatusWithoutNewEvents(bool bytes)
    {
        var sink = StreamRuns.NewSink();
        var xml = "<a><b/></a>";
        using var reader = bytes
            ? XmlSequenceReader.Create(SequenceBuilder.Chunks(Utf8(xml), 2))
            : XmlSequenceReader.Create(SequenceBuilder.Chunks(xml.ToCharArray(), 2));
        var first = reader.Pump(ref sink);
        var events = sink.Events.Count;

        var second = reader.Pump(ref sink);

        await Assert.That(first).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(second).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(sink.Events.Count).IsEqualTo(events);
    }

    [Test]
    public async Task Pump_DtdProcessingInOptions_ExpandsEntitiesLikeTheSpanPath(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sample = new SequenceSample("entities", SequenceRuns.Doctype, Encoding.UTF8, Dtd: true);

        var run = SequenceRuns.Sequence(SequenceBuilder.Chunks(sample.Bytes, 5), sample.Options);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run).IsEqualTo(SequenceRuns.Span(sample));
        await Assert.That(run.Events).Contains("x&");
    }

    // ---- the single-segment fast path ----

    [Test]
    [MethodDataSource(nameof(Samples))]
    public async Task Create_SingleSegment_GivesTheEventsOfTheSpanPath(
        SequenceSample sample,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(sample);

        var bytes = SequenceRuns.Sequence(SequenceBuilder.Whole(sample.Bytes), sample.Options);
        var chars = SequenceRuns.Sequence(SequenceBuilder.Whole(sample.Xml.ToCharArray()), sample.Options);

        await Assert.That(bytes).IsEqualTo(SequenceRuns.Span(sample));
        await Assert.That(chars).IsEqualTo(SequenceRuns.SpanChars(sample.Xml, sample.Options));
    }

    [Test]
    public async Task Create_EmptySequence_ReportsTheErrorOfTheEmptySpan()
    {
        var bytes = SequenceRuns.Sequence(ReadOnlySequence<byte>.Empty);
        var chars = SequenceRuns.Sequence(ReadOnlySequence<char>.Empty);

        await Assert.That(bytes.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(bytes).IsEqualTo(StreamRuns.Span([]));
        await Assert.That(chars).IsEqualTo(SequenceRuns.SpanChars(string.Empty));
    }

    [Test]
    public async Task Create_SingleSegmentOfBytes_AllocatesNoMoreThanTheSpanPath()
    {
        var bytes = Utf8(SequenceRuns.Medium(40));
        var sequence = SequenceBuilder.Whole(bytes);

        var span = AllocationAssert.Measure(() => CountSpan(bytes), warmups: 30, repeats: 10);
        var viaSequence = AllocationAssert.Measure(() => CountSequence(sequence), warmups: 30, repeats: 10);

        await Assert.That(CountSequence(sequence)).IsEqualTo(CountSpan(bytes));
        await Assert.That(viaSequence).IsLessThanOrEqualTo(span);
    }

    [Test]
    public async Task Create_SingleSegmentOfChars_AllocatesNoMoreThanTheSpanPath()
    {
        var chars = SequenceRuns.Medium(40).ToCharArray();
        var sequence = SequenceBuilder.Whole(chars);

        var span = AllocationAssert.Measure(() => CountSpan(chars), warmups: 30, repeats: 10);
        var viaSequence = AllocationAssert.Measure(() => CountSequence(sequence), warmups: 30, repeats: 10);

        await Assert.That(CountSequence(sequence)).IsEqualTo(CountSpan(chars));
        await Assert.That(viaSequence).IsLessThanOrEqualTo(span);
    }

    private static int CountSpan(byte[] bytes)
    {
        using var reader = XmlPipelineReader.Create(bytes.AsSpan());
        var sink = new CountingStructSink(new SpanInterningNameTable(1_000));
        return reader.Pump(ref sink) == XmlReadStatus.Completed ? sink.Count : -1;
    }

    private static int CountSpan(char[] chars)
    {
        using var reader = XmlPipelineReader.Create(chars.AsSpan());
        var sink = new CountingStructSink(new SpanInterningNameTable(1_000));
        return reader.Pump(ref sink) == XmlReadStatus.Completed ? sink.Count : -1;
    }

    private static int CountSequence(ReadOnlySequence<byte> sequence)
    {
        using var reader = XmlSequenceReader.Create(sequence);
        var sink = new CountingStructSink(new SpanInterningNameTable(1_000));
        return reader.Pump(ref sink) == XmlReadStatus.Completed ? sink.Count : -1;
    }

    private static int CountSequence(ReadOnlySequence<char> sequence)
    {
        using var reader = XmlSequenceReader.Create(sequence);
        var sink = new CountingStructSink(new SpanInterningNameTable(1_000));
        return reader.Pump(ref sink) == XmlReadStatus.Completed ? sink.Count : -1;
    }

    // ---- buffers, guards ----

    [Test]
    [Arguments("<a><b/></a>")]
    [Arguments("<a></b>")]
    public async Task Pump_CompletionAndParseError_ReturnEveryBufferOnDispose(string xml)
    {
        var bytePool = new TrackingPool<byte>();
        var charPool = new TrackingPool<char>();
        var reader = XmlSequenceReader.Create(SequenceBuilder.Chunks(Utf8(xml), 2), null, bytePool, charPool);
        var sink = StreamRuns.NewSink();
        _ = reader.Pump(ref sink);

        reader.Dispose();
        reader.Dispose();

        await Assert.That(bytePool.AllReturned).IsTrue();
        await Assert.That(charPool.AllReturned).IsTrue();
    }

    [Test]
    public async Task Pump_CharSequence_ReturnsEveryBufferOnDispose()
    {
        var charPool = new TrackingPool<char>();
        var reader = XmlSequenceReader.Create(SequenceBuilder.Chunks("<a>x</a>".ToCharArray(), 2), null, charPool);
        var sink = StreamRuns.NewSink();
        _ = reader.Pump(ref sink);

        reader.Dispose();

        await Assert.That(charPool.AllReturned).IsTrue();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Pump_NullSink_Throws(bool singleSegment)
    {
        var chars = "<a/>".ToCharArray();
        using var reader = XmlSequenceReader.Create(
            singleSegment ? SequenceBuilder.Whole(chars) : SequenceBuilder.Chunks(chars, 1)
        );

        await Assert.That(() => PumpNull(reader)).Throws<ArgumentNullException>();
    }

    private static XmlReadStatus PumpNull(XmlSequenceReader reader)
    {
        RecordingXmlSink sink = null!;
        return reader.Pump(ref sink);
    }

    [Test]
    public async Task Pump_DefaultReader_ThrowsInvalidOperation()
    {
        var reader = default(XmlSequenceReader);

        await Assert.That(() => PumpDefault(reader)).Throws<InvalidOperationException>();
        await Assert.That(() => reader.Position).Throws<InvalidOperationException>();
        await Assert.That(() => reader.Diagnostic).Throws<InvalidOperationException>();
        reader.Dispose();
    }

    private static XmlReadStatus PumpDefault(XmlSequenceReader reader)
    {
        var sink = StreamRuns.NewSink();
        return reader.Pump(ref sink);
    }

    [Test]
    public async Task Equals_CopiesAndDefaults_CompareByTheStateTheyShare()
    {
        var single = XmlSequenceReader.Create(SequenceBuilder.Whole("<a/>".ToCharArray()));
        var multi = XmlSequenceReader.Create(SequenceBuilder.Chunks("<a/>".ToCharArray(), 1));
        var copy = multi;

        await Assert.That(copy == multi).IsTrue();
        await Assert.That(copy.Equals((object)multi)).IsTrue();
        await Assert.That(copy.GetHashCode()).IsEqualTo(multi.GetHashCode());
        await Assert.That(single != multi).IsTrue();
        await Assert.That(single.Equals(null)).IsFalse();
        await Assert.That(default(XmlSequenceReader) == default).IsTrue();
        await Assert.That(default(XmlSequenceReader).GetHashCode()).IsEqualTo(0);
        single.Dispose();
        multi.Dispose();
    }
}
