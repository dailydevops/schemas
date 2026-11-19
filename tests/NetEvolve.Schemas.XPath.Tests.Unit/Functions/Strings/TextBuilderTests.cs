namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Limits;

/// <summary><see cref="TextBuilder"/> and <see cref="CodePoints"/>, the helpers that every string function builds on.</summary>
public class TextBuilderTests
{
    [Test]
    public async Task Append_WithinTheInitialBuffer_KeepsTheText()
    {
        var text = Build(new char[16], static (ref TextBuilder builder) =>
        {
            builder.Append('a');
            builder.Append("bc".AsSpan());
            builder.AppendCodePoint('d');
        });

        await Assert.That(text).IsEqualTo("abcd");
    }

    [Test]
    public async Task Append_BeyondTheInitialBuffer_GrowsAndKeepsTheText()
    {
        var text = Build(new char[4], static (ref TextBuilder builder) =>
        {
            for (var i = 0; i < 1000; i++)
            {
                builder.Append((char)('a' + (i % 26)));
            }

            builder.Append(new string('z', 5000).AsSpan());
        });

        await Assert.That(text.Length).IsEqualTo(6000);
        await Assert.That(text.Substring(0, 30)).IsEqualTo("abcdefghijklmnopqrstuvwxyzabcd");
        await Assert.That(text.Substring(1000)).IsEqualTo(new string('z', 5000));
    }

    [Test]
    public async Task AppendCodePoint_OutsideTheBmp_AppendsASurrogatePair()
    {
        var text = Build(new char[16], static (ref TextBuilder builder) =>
        {
            builder.AppendCodePoint(0x1F600);
            builder.AppendCodePoint(0x10FFFF);
            builder.AppendCodePoint(0xFFFF);
        });

        await Assert.That(text).IsEqualTo("\U0001F600\U0010FFFF\uFFFF");
    }

    [Test]
    public async Task AppendCodePoint_ALoneSurrogateValue_IsAppendedAsTheCodeUnit()
    {
        var text = Build(new char[16], static (ref TextBuilder builder) => builder.AppendCodePoint(0xD800));

        await Assert.That(text).IsEqualTo("\uD800");
    }

    [Test]
    [Arguments(-1)]
    [Arguments(0x110000)]
    public async Task AppendCodePoint_OutsideTheUnicodeRange_IsRejected(int codePoint)
    {
        var raised = false;
        try
        {
            _ = Build(new char[16], (ref TextBuilder builder) => builder.AppendCodePoint(codePoint));
        }
        catch (ArgumentOutOfRangeException)
        {
            raised = true;
        }

        await Assert.That(raised).IsTrue();
    }

    [Test]
    public async Task Append_BeyondTheStringLengthLimit_RaisesTheStringLengthError()
    {
        var code = string.Empty;
        try
        {
            _ = Build(
                new char[16],
                static (ref TextBuilder builder) =>
                {
                    builder.Append("abc".AsSpan());
                    builder.Append("def".AsSpan());
                },
                XPathLimits.Default.WithMaxStringLength(5)
            );
        }
        catch (XPathException failure)
        {
            code = failure.Diagnostic.Code;
        }

        await Assert.That(code).IsEqualTo(XPathLimitErrors.StringLengthExceeded.ToString());
    }

    [Test]
    public async Task Append_ExactlyTheStringLengthLimit_IsAccepted()
    {
        var text = Build(
            new char[16],
            static (ref TextBuilder builder) => builder.Append("abcde".AsSpan()),
            XPathLimits.Default.WithMaxStringLength(5)
        );

        await Assert.That(text).IsEqualTo("abcde");
    }

    [Test]
    public async Task Length_CountsCodeUnits()
    {
        var length = 0;
        _ = Build(new char[16], (ref TextBuilder builder) =>
        {
            builder.AppendCodePoint(0x1F600);
            builder.Append('x');
            length = builder.Length;
        });

        await Assert.That(length).IsEqualTo(3);
    }

    [Test]
    [Arguments("", 0)]
    [Arguments("abc", 3)]
    [Arguments("a\U0001F600b", 3)]
    [Arguments("\U0001F600\U0001F601", 2)]
    [Arguments("\uD800", 1)] // a lone high surrogate is one code point
    [Arguments("\uDC00\uD800", 2)] // a reversed pair is two
    [Arguments("a\uD800", 2)]
    [Arguments("\uD800a", 2)]
    public async Task Count_CountsOnePositionPerCodePointAndPerUnpairedSurrogate(string text, int expected) =>
        await Assert.That(CodePoints.Count(text.AsSpan())).IsEqualTo(expected);

    [Test]
    public async Task Next_ReadsTheCodePointAtTheIndexAndAdvances()
    {
        var text = "a\U0001F600\uD800b";
        var index = 0;
        var read = new int[4];
        for (var i = 0; i < read.Length; i++)
        {
            read[i] = CodePoints.Next(text.AsSpan(), ref index);
        }

        await Assert.That(read[0]).IsEqualTo('a');
        await Assert.That(read[1]).IsEqualTo(0x1F600);
        await Assert.That(read[2]).IsEqualTo(0xD800);
        await Assert.That(read[3]).IsEqualTo('b');
        await Assert.That(index).IsEqualTo(text.Length);
    }

    private delegate void Fill(ref TextBuilder builder);

    private static string Build(Span<char> initial, Fill fill, XPathLimits? limits = null)
    {
        var builder = new TextBuilder(initial, limits ?? XPathLimits.Default);
        try
        {
            fill(ref builder);
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }
}
