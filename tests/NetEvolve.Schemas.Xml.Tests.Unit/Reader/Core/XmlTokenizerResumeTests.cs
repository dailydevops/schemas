namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>The resume contract of the tokenizer: consumed is relative, and the scan-resume hint keeps rescans linear.</summary>
public class XmlTokenizerResumeTests
{
    private const int Size = 100_000;

    // Feeds one character per call and keeps the unconsumed tail, as a feeder does.
    private static (XmlTokenKind Kind, long Scanned) FeedOneCharPerCall(string text)
    {
        var tokenizer = new XmlTokenizer(XmlReaderLimits.Default, false, false);
        var buffer = new char[text.Length];
        var start = 0;
        var length = 0;
        for (var i = 0; i < text.Length; i++)
        {
            buffer[length++] = text[i];
            var status = tokenizer.Next(buffer.AsSpan(start, length - start), false, out var token, out var consumed);
            start += consumed;
            if (status == XmlTokenStatus.Token)
            {
                return (token.Kind, tokenizer.ScannedCharacters);
            }

            if (status == XmlTokenStatus.Error)
            {
                throw new InvalidOperationException(tokenizer.Diagnostic!.Code.Alias);
            }
        }

        throw new InvalidOperationException("No token.");
    }

    [Test]
    public async Task Next_CommentOneCharPerCall_ScanWorkIsLinear()
    {
        var text = "<!--" + new string('x', Size) + "-->";
        var (kind, scanned) = FeedOneCharPerCall(text);

        await Assert.That(kind).IsEqualTo(XmlTokenKind.Comment);
        await Assert.That(scanned).IsLessThanOrEqualTo(3L * text.Length);
    }

    [Test]
    public async Task Next_ProcessingInstructionOneCharPerCall_ScanWorkIsLinear()
    {
        var text = "<?t " + new string('x', Size) + "?>";
        var (kind, scanned) = FeedOneCharPerCall(text);

        await Assert.That(kind).IsEqualTo(XmlTokenKind.ProcessingInstruction);
        await Assert.That(scanned).IsLessThanOrEqualTo(3L * text.Length);
    }

    [Test]
    public async Task Next_ProcessingInstructionWithLongWhiteSpace_ScanWorkIsLinear()
    {
        var text = "<?t" + new string(' ', Size) + "d?>";
        var (kind, scanned) = FeedOneCharPerCall(text);

        await Assert.That(kind).IsEqualTo(XmlTokenKind.ProcessingInstruction);
        await Assert.That(scanned).IsLessThanOrEqualTo(3L * text.Length);
    }

    [Test]
    public async Task Next_IncompleteName_ConsumesNothingAndResumesAtTheTokenStart()
    {
        var tokenizer = new XmlTokenizer(XmlReaderLimits.Default, false, false);

        var first = tokenizer.Next("<ab".AsSpan(), false, out _, out var consumedFirst);
        var second = tokenizer.Next("<abc>".AsSpan(), false, out var token, out var consumedSecond);

        await Assert.That(first).IsEqualTo(XmlTokenStatus.NeedMoreData);
        await Assert.That(consumedFirst).IsEqualTo(0);
        await Assert.That(second).IsEqualTo(XmlTokenStatus.Token);
        await Assert.That(token.Kind).IsEqualTo(XmlTokenKind.StartTagName);
        await Assert.That(consumedSecond).IsEqualTo(4);
    }

    [Test]
    public async Task Next_EndTag_IsLexicalOnly()
    {
        var tokenizer = new XmlTokenizer(XmlReaderLimits.Default, false, false);

        var status = tokenizer.Next("</a>".AsSpan(), true, out var token, out var consumed);

        await Assert.That(status).IsEqualTo(XmlTokenStatus.Token);
        await Assert.That(token.Kind).IsEqualTo(XmlTokenKind.EndTagName);
        await Assert.That(consumed).IsEqualTo(3);
    }
}
