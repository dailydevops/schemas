namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Decoding;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;

/// <summary>
/// A character that is not an XML <c>Char</c> (production 2) is the same well-formedness error for characters and for
/// bytes that are valid in their encoding (#712): the byte paths do not report a decoding error for it.
/// </summary>
public class NonXmlCharParityTests
{
    private static readonly XmlReaderOptions Options = XmlReaderOptions.Default.WithDtdProcessing(true);

    private const string Issue = "<a>abc\u0001def</a>";

    [Test]
    public async Task Characters_WithAControlCharacterInText_ReportXml0002AtTheCharacter()
    {
        var run = SequenceRuns.SpanChars(Issue);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML0002);
        await Assert.That(run.ErrorPosition!.Value.CharOffset).IsEqualTo(6L);
    }

    [Test]
    [Arguments(Issue)]
    [Arguments("<a x=\"b\u0001c\"/>")]
    [Arguments("<a><!--c\u0001--></a>")]
    [Arguments("<a><![CDATA[c\u0001]]></a>")]
    [Arguments("<a><?p c\u0001?></a>")]
    [Arguments("<a>\u0001</a>")]
    [Arguments("<a>x￾y</a>")]
    [Arguments("<a>x￿y</a>")]
    [Arguments("<a\u0001/>")]
    [Arguments("\u0001<a/>")]
    [Arguments("<?xml version=\"1.0\"?><a>\u0001</a>")]
    [Arguments("<a/>\u0001")]
    [Arguments("<a/><!--\u0001-->")]
    [Arguments("<?xml version=\"1.0\"\u0001?><a/>")]
    [Arguments("<!DOCTYPE a [\u0001]><a/>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e 'x\u0001'>]><a/>")]
    [Arguments("<!DOCTYPE a [<!-- \u0001 -->]><a/>")]
    public async Task Bytes_WithANonXmlCharacter_ReportTheCodeAndPositionOfTheCharacters(
        string xml,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expected = SequenceRuns.SpanChars(xml, Options);
        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Error);

        var mismatches = new List<string>();
        foreach (var (name, bytes) in Encoded(xml))
        {
            Compare(mismatches, name, expected, StreamRuns.Span(bytes, Options));

            foreach (var size in new[] { 1, 3 })
            {
                Compare(
                    mismatches,
                    $"{name} sequence {size}",
                    expected,
                    SequenceRuns.Sequence(SequenceBuilder.Chunks(bytes, size), Options)
                );
                Compare(
                    mismatches,
                    $"{name} stream {size}",
                    expected,
                    await StreamRuns.StreamAsync(bytes, _ => size, size, Options, cancellationToken)
                );
            }
        }

        await Assert.That(mismatches).IsEmpty();
    }

    private static (string Name, byte[] Bytes)[] Encoded(string xml) =>
        [
            ("utf-8", Encoding.UTF8.GetBytes(xml)),
            ("utf-16le", [0xFF, 0xFE, .. new UnicodeEncoding(false, false).GetBytes(xml)]),
            ("utf-16be", [0xFE, 0xFF, .. new UnicodeEncoding(true, false).GetBytes(xml)]),
        ];

    private static void Compare(List<string> mismatches, string name, StreamRun expected, StreamRun actual)
    {
        if (actual.Code != expected.Code)
        {
            mismatches.Add($"{name}: code {actual.Code?.Alias} instead of {expected.Code?.Alias}");
        }

        if (actual.ErrorPosition != expected.ErrorPosition)
        {
            mismatches.Add($"{name}: error at {actual.ErrorPosition} instead of {expected.ErrorPosition}");
        }

        if (!string.Equals(actual.Events, expected.Events, StringComparison.Ordinal))
        {
            mismatches.Add($"{name}: events {actual.Events} instead of {expected.Events}");
        }
    }
}
