namespace NetEvolve.Schemas.Xml.Writer;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.CharClasses;

public sealed partial class XmlWriter
{
    private const int ChunkHint = 512;

    private string Str(XmlSymbol symbol) =>
        _names.TryGetString(symbol, out var text) && text is not null ? text : throw Error(XmlErrorCodes.XML3004);

    private static bool IsNCName(string name) =>
        name.Length > 0 && XmlCharClass.NameLength(name.AsSpan()) == name.Length && name.AsSpan().IndexOf(':') < 0;

    private static void CheckChars(ReadOnlySpan<char> text)
    {
        if (XmlCharClass.IndexOfNonChar(text) >= 0)
        {
            throw Error(XmlErrorCodes.XML0002);
        }
    }

    private void ValidateName(XmlName name)
    {
        var local = Str(name.Local);
        var prefix = Str(name.Prefix);
        if (!IsNCName(local) || (prefix.Length > 0 && !IsNCName(prefix)))
        {
            throw Error(XmlErrorCodes.XML3004);
        }

        CheckChars(Str(name.NamespaceUri).AsSpan());
        if (prefix.Length > 0 && name.NamespaceUri.IsEmpty)
        {
            throw Error(XmlErrorCodes.XML3001);
        }
    }

    private void Put(ReadOnlySpan<char> text)
    {
        while (!text.IsEmpty)
        {
            var span = _output.GetSpan(Math.Min(text.Length, ChunkHint));
            var count = Math.Min(span.Length, text.Length);
            if (count == 0)
            {
                throw new InvalidOperationException("The output returned an empty buffer.");
            }

            text.Slice(0, count).CopyTo(span);
            _output.Advance(count);
            text = text.Slice(count);
        }
    }

    private void Put(char c)
    {
        var span = _output.GetSpan(1);
        span[0] = c;
        _output.Advance(1);
    }

    private void PutName(XmlName name)
    {
        var prefix = Str(name.Prefix);
        if (prefix.Length > 0)
        {
            Put(prefix.AsSpan());
            Put(':');
        }

        Put(Str(name.Local).AsSpan());
    }

    private void PutQuoted(ReadOnlySpan<char> literal)
    {
        var quote = literal.IndexOf('"') >= 0 ? '\'' : '"';
        Put(quote);
        Put(literal);
        Put(quote);
    }

    private void PutCData(ReadOnlySpan<char> text)
    {
        Put("<![CDATA[");
        int index;
        while ((index = text.IndexOf("]]>".AsSpan())) >= 0)
        {
            Put(text.Slice(0, index + 2));
            Put("]]><![CDATA[");
            text = text.Slice(index + 2);
        }

        Put(text);
        Put("]]>");
    }

    private void PutEscaped(ReadOnlySpan<char> text, bool attribute)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var replacement = Replacement(text[i], attribute);
            if (replacement is null)
            {
                continue;
            }

            Put(text.Slice(start, i - start));
            Put(replacement.AsSpan());
            start = i + 1;
        }

        Put(text.Slice(start));
    }

    private static string? Replacement(char c, bool attribute) =>
        c switch
        {
            '&' => "&amp;",
            '<' => "&lt;",
            '>' => "&gt;",
            '\r' => "&#13;",
            '"' when attribute => "&quot;",
            '\t' when attribute => "&#9;",
            '\n' when attribute => "&#10;",
            _ => null,
        };
}
