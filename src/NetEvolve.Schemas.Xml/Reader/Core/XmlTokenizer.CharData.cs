namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.References;
using NetEvolve.Schemas.Xml.Text.CharClasses;

internal partial struct XmlTokenizer
{
    private static readonly char[] TextStops = ['<', '&', '\r', '\n', ']'];
    private static readonly char[] CDataStops = ['\r', '\n', ']'];
    private static readonly char[] DoubleQuoteStops = ['"', '&', '<', '\t', '\r', '\n'];
    private static readonly char[] SingleQuoteStops = ['\'', '&', '<', '\t', '\r', '\n'];

    // A line break (CRLF, CR, or LF) is one token; a CR at the end of non-final input is held back until the next character is known.
    private static Scan ScanLineBreak(
        ReadOnlySpan<char> input,
        ref int pos,
        bool isFinal,
        XmlTokenKind kind,
        char replacement,
        out XmlToken token
    )
    {
        token = default;
        if (input[pos] == '\r')
        {
            if (pos + 1 >= input.Length && !isFinal)
            {
                return Scan.NeedMore;
            }

            pos += pos + 1 < input.Length && input[pos + 1] == '\n' ? 2 : 1;
        }
        else
        {
            pos++;
        }

        token = XmlToken.Inline(kind, replacement);
        return Scan.Token;
    }

    // Productions 14 and 18 to 21: a literal run of character data or CDATA content.
    private Scan ScanCharData(ReadOnlySpan<char> input, ref int pos, bool isFinal, bool cdata, out XmlToken token)
    {
        token = default;
        var end = FindCharDataEnd(input, pos, isFinal, cdata ? CDataStops : TextStops, out var terminator);
        if (end == input.Length && !isFinal && end > pos && char.IsHighSurrogate(input[end - 1]))
        {
            end--;
        }

        if (end == pos)
        {
            if (terminator != pos)
            {
                return Scan.NeedMore;
            }

            if (!cdata)
            {
                return Fail(input, pos, XmlErrorCodes.XML0004);
            }

            pos += 3;
            _mode = Mode.Content;
            return Scan.Continue;
        }

        // The characters before an invalid one are a token of their own, so the events before the error do not depend on the chunk edges.
        var bad = XmlCharClass.IndexOfNonChar(input.Slice(pos, end - pos));
        if (bad == 0)
        {
            return Fail(input, pos, XmlErrorCodes.XML0002);
        }

        if (bad > 0)
        {
            end = pos + bad;
        }

        token = new XmlToken(cdata ? XmlTokenKind.CData : XmlTokenKind.Text, pos, end - pos);
        pos = end;
        return Scan.Token;
    }

    // Finds where the literal run ends. A trailing "]" or "]]" of non-final input is held back, so "]]>" is never split;
    // `terminator` is the offset of a complete "]]>" at the end of the run, otherwise -1.
    private static int FindCharDataEnd(
        ReadOnlySpan<char> input,
        int pos,
        bool isFinal,
        char[] stops,
        out int terminator
    )
    {
        terminator = -1;
        var i = pos;
        while (true)
        {
            var offset = input.Slice(i).IndexOfAny(stops);
            if (offset < 0)
            {
                return input.Length;
            }

            i += offset;
            if (input[i] != ']')
            {
                return i;
            }

            if (i + 2 < input.Length)
            {
                if (input[i + 1] == ']' && input[i + 2] == '>')
                {
                    terminator = i;
                    return i;
                }

                i++;
            }
            else if (isFinal)
            {
                i++;
            }
            else
            {
                return i;
            }
        }
    }

    // Production 66 and 68 with the five predefined entities. The whole name run is read first, so the code never depends on
    // a chunk edge; the hint is the examined offset from the ampersand.
    private Scan ScanReference(ReadOnlySpan<char> input, ref int pos, bool isFinal, int hint, out XmlToken token)
    {
        token = default;
        var runStart = pos + 1;
        var i = Math.Max(runStart, pos + hint);
        if (i == runStart && i < input.Length && input[i] == '#')
        {
            i++;
        }

        i = ScanNameChars(input, i, isFinal, out var incomplete);
        if (i - runStart > _limits.MaxNameLength)
        {
            return Fail(input, runStart, XmlErrorCodes.XML1105);
        }

        if (incomplete || i >= input.Length)
        {
            _hint = i - pos;
            return Pending(input, isFinal);
        }

        if (input[i] != ';')
        {
            return Fail(input, i, XmlErrorCodes.XML0016);
        }

        var error = XmlReferenceResolver.TryParseReference(input.Slice(pos, i + 1 - pos), out _, out var codePoint);
        if (error is null)
        {
            token = XmlToken.InlineCodePoint(XmlTokenKind.Reference, codePoint);
        }
        else if (ReferenceEquals(error, XmlErrorCodes.XML0008) && _allowEntityReferences)
        {
            token = new XmlToken(XmlTokenKind.EntityReference, runStart, i - runStart);
        }
        else
        {
            return Fail(input, pos, error);
        }

        pos = i + 1;
        return Scan.Token;
    }

    // Reads name characters (not checking the start character) from `from`; `incomplete` as for ScanName.
    private static int ScanNameChars(ReadOnlySpan<char> input, int from, bool isFinal, out bool incomplete)
    {
        incomplete = false;
        var i = from;
        while (i < input.Length)
        {
            var c = input[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= input.Length)
                {
                    incomplete = !isFinal;
                    break;
                }

                if (!char.IsLowSurrogate(input[i + 1]) || c > 0xDB7F)
                {
                    break;
                }

                i += 2;
            }
            else if (XmlCharClass.IsNameChar(c))
            {
                i++;
            }
            else
            {
                break;
            }
        }

        return i;
    }

    // Production 10 and section 3.3.3 for CDATA attributes: TAB, LF, CR, and CRLF become one space each.
    private Scan ScanAttributeValue(ReadOnlySpan<char> input, ref int pos, bool isFinal, int hint, out XmlToken token)
    {
        token = default;
        if (pos >= input.Length)
        {
            return Pending(input, isFinal);
        }

        var c = input[pos];
        if (c == _quote)
        {
            pos++;
            _mode = Mode.TagBody;
            _sawSpace = false;
            token = new XmlToken(XmlTokenKind.AttributeValueEnd, 0, 0);
            return Scan.Token;
        }

        switch (c)
        {
            case '&':
                return ScanReference(input, ref pos, isFinal, hint, out token);
            case '<':
                return Fail(input, pos, XmlErrorCodes.XML0013);
            case '\r':
                return ScanLineBreak(input, ref pos, isFinal, XmlTokenKind.Reference, ' ', out token);
            case '\n':
            case '\t':
                pos++;
                token = XmlToken.Inline(XmlTokenKind.Reference, ' ');
                return Scan.Token;
            default:
                return ScanAttributeText(input, ref pos, isFinal, out token);
        }
    }

    private Scan ScanAttributeText(ReadOnlySpan<char> input, ref int pos, bool isFinal, out XmlToken token)
    {
        token = default;
        var offset = input.Slice(pos).IndexOfAny(_quote == '"' ? DoubleQuoteStops : SingleQuoteStops);
        var end = offset < 0 ? input.Length : pos + offset;
        if (offset < 0 && !isFinal && char.IsHighSurrogate(input[end - 1]))
        {
            end--;
        }

        if (end == pos)
        {
            return Scan.NeedMore;
        }

        var bad = XmlCharClass.IndexOfNonChar(input.Slice(pos, end - pos));
        if (bad == 0)
        {
            return Fail(input, pos, XmlErrorCodes.XML0002);
        }

        if (bad > 0)
        {
            end = pos + bad;
        }

        token = new XmlToken(XmlTokenKind.Text, pos, end - pos);
        pos = end;
        return Scan.Token;
    }
}
