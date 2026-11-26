namespace NetEvolve.Schemas.Xml.Writer;

using System;
using System.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.CharClasses;

public sealed partial class XmlWriter
{
    private const int ChunkHint = 512;
    private const int MaxCachedSymbols = 4096;
    private const byte NCNameFlag = 1;
    private const byte CharsFlag = 2;

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

    // Validation results are kept per symbol: a symbol always resolves to the same text, and documents repeat few names.
    private bool IsValidNCName(XmlSymbol symbol) => IsValid(symbol, NCNameFlag, static text => IsNCName(text));

    private bool HasOnlyChars(XmlSymbol symbol) =>
        IsValid(symbol, CharsFlag, static text => XmlCharClass.IndexOfNonChar(text.AsSpan()) < 0);

    private bool IsValid(XmlSymbol symbol, byte flag, Func<string, bool> check)
    {
        var index = symbol.Value;
        if ((uint)index < (uint)_symbolFlags.Length && (_symbolFlags[index] & flag) != 0)
        {
            return true;
        }

        if (!check(Str(symbol)))
        {
            return false;
        }

        if (index < MaxCachedSymbols)
        {
            if (index >= _symbolFlags.Length)
            {
                GrowFlags(index + 1);
            }

            _symbolFlags[index] |= flag;
        }

        return true;
    }

    private void GrowFlags(int required)
    {
        var old = _symbolFlags;
        var grown = ArrayPool<byte>.Shared.Rent(Math.Max(old.Length * 2, required));
        Array.Clear(grown, 0, grown.Length);
        old.AsSpan().CopyTo(grown);
        _symbolFlags = grown;
        if (old.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(old);
        }
    }

    private void ValidateName(XmlName name)
    {
        if (!IsValidNCName(name.Local) || (!name.Prefix.IsEmpty && !IsValidNCName(name.Prefix)))
        {
            throw Error(XmlErrorCodes.XML3004);
        }

        if (!HasOnlyChars(name.NamespaceUri))
        {
            throw Error(XmlErrorCodes.XML0002);
        }

        if (!name.Prefix.IsEmpty && name.NamespaceUri.IsEmpty)
        {
            throw Error(XmlErrorCodes.XML3001);
        }
    }

    private void Put(ReadOnlySpan<char> text)
    {
        if (_adapter is not null)
        {
            _adapter.Write(text);
            return;
        }

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
        if (_adapter is not null)
        {
            _adapter.Write(c);
            return;
        }

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

    // Bit c is set for each character below U+0040 that needs a reference: & < > and carriage return in text, plus quote, tab and line feed in attribute values.
    private const ulong TextMask = (1UL << 0x0D) | (1UL << '&') | (1UL << '<') | (1UL << '>');
    private const ulong AttributeMask = TextMask | (1UL << '"') | (1UL << 0x09) | (1UL << 0x0A);

    private void PutEscaped(ReadOnlySpan<char> text, bool attribute)
    {
        var mask = attribute ? AttributeMask : TextMask;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c >= 0x40 || ((mask >> c) & 1) == 0)
            {
                continue;
            }

            Put(text.Slice(start, i - start));
            Put(Replacement(c).AsSpan());
            start = i + 1;
        }

        Put(text.Slice(start));
    }

    private static string Replacement(char c) =>
        c switch
        {
            '&' => "&amp;",
            '<' => "&lt;",
            '>' => "&gt;",
            '\r' => "&#13;",
            '"' => "&quot;",
            '\t' => "&#9;",
            _ => "&#10;",
        };
}
