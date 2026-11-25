namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.CharClasses;

internal partial struct XmlTokenizer
{
    private enum DoctypeResult : byte
    {
        Complete,
        Incomplete,
        Error,
    }

    // Production 28 at lexical level (design #108, AP12). Without a handler the declaration is rejected at the opener and
    // nothing of it is scanned.
    private Scan ScanDoctype(ReadOnlySpan<char> input, ref int pos, bool isFinal, out XmlToken token)
    {
        token = default;
        if (!_allowDoctype)
        {
            return Fail(input, pos, XmlErrorCodes.XML2001);
        }

        var length = input.Length - pos;
        if (!isFinal && length < _doctypeRetry)
        {
            return Scan.NeedMore;
        }

        var result = ScanDoctypeCore(input, pos, isFinal, out var parts, out var end);
        if (result == DoctypeResult.Error)
        {
            return Fail(input, _failOffset, _failCode!);
        }

        if (result == DoctypeResult.Incomplete)
        {
            if (length > _limits.MaxDoctypeLength)
            {
                return Fail(input, pos, XmlErrorCodes.XML1115);
            }

            _doctypeRetry = length * 2;
            return Pending(input, isFinal);
        }

        if (end - pos > _limits.MaxDoctypeLength)
        {
            return Fail(input, pos, XmlErrorCodes.XML1115);
        }

        var bad = XmlCharClass.IndexOfNonChar(input.Slice(pos, end - pos));
        if (bad >= 0)
        {
            return Fail(input, pos + bad, XmlErrorCodes.XML0002);
        }

        _doctypeRetry = 0;
        _doctype = parts;
        token = new XmlToken(XmlTokenKind.Doctype, pos, end - pos);
        pos = end;
        return Scan.Token;
    }

    private DoctypeResult ScanDoctypeCore(
        ReadOnlySpan<char> input,
        int pos,
        bool isFinal,
        out XmlDoctypeParts parts,
        out int end
    )
    {
        parts = default;
        end = 0;
        var result = ScanDoctypeName(input, pos + DoctypeOpener.Length, isFinal, out var name, out var i);
        if (result != DoctypeResult.Complete)
        {
            return result;
        }

        result = ScanExternalId(input, ref i, out var publicId, out var systemId);
        if (result != DoctypeResult.Complete)
        {
            return result;
        }

        var subset = (0, 0);
        if (input[i] == '[')
        {
            var subsetEnd = ScanSubset(input, i + 1, isFinal);
            if (subsetEnd < 0)
            {
                return DoctypeResult.Incomplete;
            }

            subset = (i + 1, subsetEnd - i - 1);
            i = SkipSpaces(input, subsetEnd + 1);
            if (i >= input.Length)
            {
                return DoctypeResult.Incomplete;
            }
        }

        if (input[i] != '>')
        {
            return Failed(i, XmlErrorCodes.XML0011);
        }

        end = i + 1;
        parts = new XmlDoctypeParts(name, publicId, systemId, subset);
        return DoctypeResult.Complete;
    }

    // S Name after the opener; `next` is the offset of the first character after the name and the following white space.
    private DoctypeResult ScanDoctypeName(
        ReadOnlySpan<char> input,
        int start,
        bool isFinal,
        out (int Start, int Length) name,
        out int next
    )
    {
        name = default;
        next = start;
        if (start >= input.Length)
        {
            return DoctypeResult.Incomplete;
        }

        if (!IsSpace(input[start]))
        {
            return Failed(start, XmlErrorCodes.XML0011);
        }

        var nameStart = SkipSpaces(input, start);
        var nameEnd = ScanName(input, nameStart, nameStart, isFinal, out var incomplete);
        if (nameEnd - nameStart > _limits.MaxNameLength)
        {
            return Failed(nameStart, XmlErrorCodes.XML1105);
        }

        if (incomplete || nameEnd >= input.Length)
        {
            return DoctypeResult.Incomplete;
        }

        if (nameEnd == nameStart)
        {
            return Failed(nameEnd, XmlErrorCodes.XML0011);
        }

        name = (nameStart, nameEnd - nameStart);
        next = SkipSpaces(input, nameEnd);
        return next >= input.Length ? DoctypeResult.Incomplete : DoctypeResult.Complete;
    }

    // Optional production 75: SYSTEM S literal, or PUBLIC S literal S literal.
    private DoctypeResult ScanExternalId(
        ReadOnlySpan<char> input,
        ref int i,
        out (int Start, int Length) publicId,
        out (int Start, int Length) systemId
    )
    {
        publicId = default;
        systemId = default;
        if (input[i] is not ('S' or 'P'))
        {
            return DoctypeResult.Complete;
        }

        var isPublic = input[i] == 'P';
        var keyword = isPublic ? "PUBLIC" : "SYSTEM";
        var match = Match(input.Slice(i), keyword);
        if (match == 0)
        {
            return DoctypeResult.Incomplete;
        }

        if (match < 0)
        {
            return Failed(i, XmlErrorCodes.XML0011);
        }

        i += keyword.Length;
        for (var literal = 0; literal < (isPublic ? 2 : 1); literal++)
        {
            var result = ScanLiteral(input, ref i, out var value);
            if (result != DoctypeResult.Complete)
            {
                return result;
            }

            if (isPublic && literal == 0)
            {
                // Production 12: only PubidChar is allowed.
                for (var k = 0; k < value.Length; k++)
                {
                    if (!XmlCharClass.IsPubidChar(input[value.Start + k]))
                    {
                        return Failed(value.Start + k, XmlErrorCodes.XML0011);
                    }
                }

                publicId = value;
            }
            else
            {
                systemId = value;
            }
        }

        i = SkipSpaces(input, i);
        return i >= input.Length ? DoctypeResult.Incomplete : DoctypeResult.Complete;
    }

    // S followed by a quoted literal; `i` ends after the closing quote.
    private DoctypeResult ScanLiteral(ReadOnlySpan<char> input, ref int i, out (int Start, int Length) value)
    {
        value = default;
        if (i >= input.Length)
        {
            return DoctypeResult.Incomplete;
        }

        if (!IsSpace(input[i]))
        {
            return Failed(i, XmlErrorCodes.XML0011);
        }

        i = SkipSpaces(input, i);
        if (i >= input.Length)
        {
            return DoctypeResult.Incomplete;
        }

        var quote = input[i];
        if (quote is not ('"' or '\''))
        {
            return Failed(i, XmlErrorCodes.XML0011);
        }

        var close = input.Slice(i + 1).IndexOf(quote);
        if (close < 0)
        {
            return DoctypeResult.Incomplete;
        }

        value = (i + 1, close);
        i += close + 2;
        return DoctypeResult.Complete;
    }

    // Finds the closing bracket of the internal subset, skipping quoted literals, comments, and processing instructions
    // so that a bracket inside them does not end it; -1 when the input ends first.
    private static int ScanSubset(ReadOnlySpan<char> input, int start, bool isFinal)
    {
        var i = start;
        while (i < input.Length)
        {
            var c = input[i];
            if (c is '"' or '\'')
            {
                var close = input.Slice(i + 1).IndexOf(c);
                i = close < 0 ? input.Length : i + close + 2;
            }
            else if (c == '<')
            {
                i = SkipMarkup(input, i, isFinal);
                if (i < 0)
                {
                    return -1;
                }
            }
            else if (c == ']')
            {
                return i;
            }
            else
            {
                i++;
            }
        }

        return -1;
    }

    // Skips a comment or processing instruction that starts at `i`; any other '<' is one character. -1 when incomplete.
    private static int SkipMarkup(ReadOnlySpan<char> input, int i, bool isFinal)
    {
        var rest = input.Slice(i);
        var comment = Match(rest, CommentOpener);
        if (!isFinal && comment == 0)
        {
            return -1;
        }

        var isInstruction = Match(rest, "<?") == 1;
        if (comment != 1 && !isInstruction)
        {
            return i + 1;
        }

        var open = comment == 1 ? CommentOpener.Length : 2;
        var close = rest.Slice(open).IndexOf((comment == 1 ? "-->" : "?>").AsSpan());
        return close < 0 ? -1 : i + open + close + (comment == 1 ? 3 : 2);
    }

    private static int SkipSpaces(ReadOnlySpan<char> input, int pos)
    {
        while (pos < input.Length && IsSpace(input[pos]))
        {
            pos++;
        }

        return pos;
    }

    private DoctypeResult Failed(int offset, XmlErrorCode code)
    {
        _failOffset = offset;
        _failCode = code;
        return DoctypeResult.Error;
    }
}
