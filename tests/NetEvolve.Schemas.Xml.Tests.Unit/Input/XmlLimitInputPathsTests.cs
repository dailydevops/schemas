namespace NetEvolve.Schemas.Xml.Tests.Unit.Input;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;

/// <summary>
/// A resource limit error gives the same events, code, and position on every input path (#655): the span of characters, the span
/// of bytes (decoding stops past the document limit), the sequence of byte segments, and the stream at every buffer size.
/// </summary>
public class XmlLimitInputPathsTests
{
    private static readonly string[] Limits = ["document", "text", "value", "tag"];

    public static IEnumerable<Func<(string Xml, string Limit)>> LimitedDocuments()
    {
        foreach (
            var xml in new[]
            {
                "<a>ab<![CDATA[cd]]>ef&amp;gh</a>",
                "<?xml version=\"1.0\"?>\r\n<!--c--><r a=\"xy&amp;z\" b='w'>t\r\nu<e k=\"1\"/>x</r>\n",
                "<a x=\"12345&#65;&lt;678\" y='ab'>z<!--abcdef--></a>",
                "<a   b = \"1\"   c='2' >\U0001F600\U0001F600x</a >",
                "<a><!--abc",
            }
        )
        {
            foreach (var limit in Limits)
            {
                var text = xml;
                var name = limit;
                yield return () => (text, name);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(LimitedDocuments))]
    public async Task Read_LimitOnAnyInputPath_GivesTheResultOfTheSpanOfCharacters(
        string xml,
        string limit,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(limit);
        var bytes = Encoding.UTF8.GetBytes(xml);
        var divergent = new List<string>();
        for (var value = 1; value <= xml.Length + 1 && divergent.Count < 5; value++)
        {
            var options = XmlReaderOptions.Default.WithLimits(With(limit, value));
            var expected = Comparable(SequenceRuns.SpanChars(xml, options));

            Compare(divergent, limit, value, "bytes", expected, StreamRuns.Span(bytes, options));
            foreach (var size in new[] { 1, 3 })
            {
                Compare(
                    divergent,
                    limit,
                    value,
                    $"segments of {size}",
                    expected,
                    SequenceRuns.Sequence(SequenceBuilder.Chunks(bytes, size), options)
                );
            }

            foreach (var buffer in new[] { 1, 3, 4096 })
            {
                var run = await StreamRuns.StreamAsync(bytes, _ => 2, buffer, options, cancellationToken);
                Compare(divergent, limit, value, $"stream buffer {buffer}", expected, run);
            }
        }

        await Assert.That(divergent).IsEmpty();
    }

    private static void Compare(
        List<string> divergent,
        string limit,
        int value,
        string path,
        StreamRun expected,
        StreamRun actual
    )
    {
        var comparable = Comparable(actual);
        if (comparable != expected)
        {
            divergent.Add($"{limit} {value} {path}: expected {expected}, got {comparable}");
        }
    }

    // The position after an error follows the token boundaries and is not part of the contract; the position after a read is.
    private static StreamRun Comparable(StreamRun run) =>
        run.Status == XmlReadStatus.Error ? run with { Position = default } : run;

    private static XmlReaderLimits With(string limit, int value) =>
        limit switch
        {
            "document" => XmlReaderLimits.Default.WithMaxDocumentCharacters(value),
            "text" => XmlReaderLimits.Default.WithMaxTextNodeLength(value),
            "value" => XmlReaderLimits.Default.WithMaxAttributeValueLength(value),
            _ => XmlReaderLimits.Default.WithMaxStartTagCharacters(value),
        };
}
