namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Declaration;
using NetEvolve.Schemas.Xml.Text.CharClasses;

internal partial struct XmlTokenizer
{
    private const string CommentOpener = "<!--";
    private const string CDataOpener = "<![CDATA[";
    private const string DoctypeOpener = "<!DOCTYPE";

    private Scan ScanMarkup(
        ReadOnlySpan<char> input,
        ref int pos,
        bool isFinal,
        int hint,
        int hintData,
        out XmlToken token
    )
    {
        token = default;
        if (pos + 1 >= input.Length)
        {
            return Pending(input, isFinal);
        }

        switch (input[pos + 1])
        {
            case '/':
                return ScanTagName(input, ref pos, isFinal, hint, 2, XmlTokenKind.EndTagName, out token);
            case '?':
                return ScanProcessingInstruction(input, ref pos, isFinal, hint, hintData, out token);
            case '!':
                return ScanBang(input, ref pos, isFinal, hint, out token);
            default:
                return ScanTagName(input, ref pos, isFinal, hint, 1, XmlTokenKind.StartTagName, out token);
        }
    }

    // A start tag name after '<' (prefix length 1) or an end tag name after '</' (prefix length 2).
    private Scan ScanTagName(
        ReadOnlySpan<char> input,
        ref int pos,
        bool isFinal,
        int hint,
        int prefixLength,
        XmlTokenKind kind,
        out XmlToken token
    )
    {
        token = default;
        var nameStart = pos + prefixLength;
        var end = ScanName(input, nameStart, Math.Max(nameStart, pos + hint), isFinal, out var incomplete);
        if (end - nameStart > _limits.MaxNameLength)
        {
            return Fail(input, nameStart, XmlErrorCodes.XML1105);
        }

        if (incomplete)
        {
            _hint = end - pos;
            return Scan.NeedMore;
        }

        if (end == nameStart)
        {
            return Fail(input, nameStart, XmlErrorCodes.XML0011);
        }

        token = new XmlToken(kind, nameStart, end - nameStart);
        pos = end;
        _sawSpace = false;
        _mode = kind == XmlTokenKind.StartTagName ? Mode.TagBody : Mode.EndTagClose;
        return Scan.Token;
    }

    // Scans a Name from `from` (the start offset, or the resume hint inside the name). `incomplete` is true when the input
    // ended, or a high surrogate is held back, before a delimiter, so the name may continue.
    private static int ScanName(ReadOnlySpan<char> input, int start, int from, bool isFinal, out bool incomplete)
    {
        var i = from;
        if (i == start)
        {
            if (i >= input.Length)
            {
                incomplete = !isFinal;
                return i;
            }

            // A supplementary start character is checked as part of the pair by ScanNameChars.
            if (!char.IsHighSurrogate(input[i]))
            {
                if (!XmlCharClass.IsNameStartChar(input[i]))
                {
                    incomplete = false;
                    return i;
                }

                i++;
            }
        }

        i = ScanNameChars(input, i, isFinal, out incomplete);
        incomplete |= i >= input.Length && !isFinal;
        return i;
    }

    // Returns 1 when `rest` starts with `literal`, 0 when it is a proper prefix of it, otherwise -1.
    private static int Match(ReadOnlySpan<char> rest, string literal)
    {
        var length = Math.Min(rest.Length, literal.Length);
        if (!rest.Slice(0, length).SequenceEqual(literal.AsSpan(0, length)))
        {
            return -1;
        }

        return rest.Length >= literal.Length ? 1 : 0;
    }

    private Scan ScanBang(ReadOnlySpan<char> input, ref int pos, bool isFinal, int hint, out XmlToken token)
    {
        token = default;
        var rest = input.Slice(pos);
        var comment = Match(rest, CommentOpener);
        if (comment == 1)
        {
            return ScanComment(input, ref pos, isFinal, hint, out token);
        }

        var cdata = Match(rest, CDataOpener);
        if (cdata == 1)
        {
            pos += CDataOpener.Length;
            _mode = Mode.CData;
            token = new XmlToken(XmlTokenKind.CDataStart, 0, 0);
            return Scan.Token;
        }

        var doctype = Match(rest, DoctypeOpener);
        if (doctype == 1)
        {
            return ScanDoctype(input, ref pos, isFinal, out token);
        }

        return comment == 0 || cdata == 0 || doctype == 0
            ? Pending(input, isFinal)
            : Fail(input, pos, XmlErrorCodes.XML0011);
    }

    // Production 15. The hint is the offset, from the comment start, of the first character not yet examined.
    private Scan ScanComment(ReadOnlySpan<char> input, ref int pos, bool isFinal, int hint, out XmlToken token)
    {
        token = default;
        var contentStart = pos + CommentOpener.Length;
        var i = hint > CommentOpener.Length ? pos + hint : contentStart;
        while (true)
        {
            var dash = input.Slice(i).IndexOf('-');
            var regionEnd = dash < 0 ? input.Length : i + dash;
            var checkEnd = regionEnd;
            if (dash < 0 && !isFinal && checkEnd > i && char.IsHighSurrogate(input[checkEnd - 1]))
            {
                checkEnd--;
            }

            var bad = XmlCharClass.IndexOfNonChar(input.Slice(i, checkEnd - i));
            _scanned += checkEnd - i;
            if (bad >= 0)
            {
                return Fail(input, i + bad, XmlErrorCodes.XML0002);
            }

            if (dash < 0)
            {
                // Two more characters may belong to the terminator.
                if (checkEnd - contentStart > _limits.MaxTokenLength + 2)
                {
                    return Fail(input, pos, XmlErrorCodes.XML1106);
                }

                _hint = checkEnd - pos;
                return Pending(input, isFinal);
            }

            if (regionEnd + 1 < input.Length && input[regionEnd + 1] != '-')
            {
                i = regionEnd + 1;
                continue;
            }

            if (regionEnd + 2 >= input.Length)
            {
                _hint = regionEnd - pos;
                return Pending(input, isFinal);
            }

            if (input[regionEnd + 2] != '>')
            {
                return Fail(input, regionEnd, XmlErrorCodes.XML0014);
            }

            if (regionEnd - contentStart > _limits.MaxTokenLength)
            {
                return Fail(input, pos, XmlErrorCodes.XML1106);
            }

            token = new XmlToken(XmlTokenKind.Comment, contentStart, regionEnd - contentStart);
            pos = regionEnd + 3;
            return Scan.Token;
        }
    }

    // Productions 16 and 17. The hint is the examined offset from the token start, the data hint the offset of the data start.
    private Scan ScanProcessingInstruction(
        ReadOnlySpan<char> input,
        ref int pos,
        bool isFinal,
        int hint,
        int hintData,
        out XmlToken token
    )
    {
        token = default;
        var nameStart = pos + 2;

        // The target is rescanned on every call; that is bounded by MaxNameLength.
        var nameEnd = ScanName(input, nameStart, nameStart, isFinal, out var incomplete);
        if (nameEnd - nameStart > _limits.MaxNameLength)
        {
            return Fail(input, nameStart, XmlErrorCodes.XML1105);
        }

        if (incomplete)
        {
            return Scan.NeedMore;
        }

        if (nameEnd == nameStart)
        {
            return Fail(input, nameStart, XmlErrorCodes.XML0011);
        }

        var target = input.Slice(nameStart, nameEnd - nameStart);
        if (target.Length == 3 && target.Equals("xml".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return Fail(
                input,
                pos,
                target.SequenceEqual("xml".AsSpan()) ? XmlErrorCodes.XML0007 : XmlErrorCodes.XML0015
            );
        }

        var dataStart = hintData > 0 ? pos + hintData : 0;
        if (hintData == 0)
        {
            var located = LocateInstructionData(input, pos, nameEnd, isFinal, hint, out dataStart);
            if (located is not null)
            {
                return located.Value;
            }
        }

        return ScanInstructionData(input, ref pos, isFinal, hint, dataStart, (nameStart, nameEnd), out token);
    }

    // Finds where the data of a processing instruction starts: after the white space that follows the target, or at the
    // terminator of an instruction without data. Returns null when found.
    private Scan? LocateInstructionData(
        ReadOnlySpan<char> input,
        int pos,
        int nameEnd,
        bool isFinal,
        int hint,
        out int dataStart
    )
    {
        dataStart = nameEnd;
        if (nameEnd >= input.Length)
        {
            return Pending(input, isFinal);
        }

        var c = input[nameEnd];
        if (c == '?')
        {
            if (nameEnd + 1 >= input.Length)
            {
                return Pending(input, isFinal);
            }

            return input[nameEnd + 1] == '>' ? null : Fail(input, nameEnd, XmlErrorCodes.XML0011);
        }

        if (!IsSpace(c))
        {
            return Fail(input, nameEnd, XmlErrorCodes.XML0011);
        }

        dataStart = SkipSpaces(input, hint > 0 ? Math.Max(nameEnd, pos + hint) : nameEnd);
        if (dataStart < input.Length)
        {
            return null;
        }

        _hint = input.Length - pos;
        return Pending(input, isFinal);
    }

    private Scan ScanInstructionData(
        ReadOnlySpan<char> input,
        ref int pos,
        bool isFinal,
        int hint,
        int dataStart,
        (int Start, int End) name,
        out XmlToken token
    )
    {
        token = default;
        var validFrom = hint > 0 ? Math.Max(dataStart, pos + hint) : dataStart;
        var searchFrom = Math.Max(dataStart, validFrom - 1);
        var terminator = input.Slice(searchFrom).IndexOf("?>".AsSpan());
        var end = terminator >= 0 ? searchFrom + terminator : input.Length;
        if (terminator < 0 && !isFinal && end > validFrom && char.IsHighSurrogate(input[end - 1]))
        {
            end--;
        }

        var checkLength = Math.Max(0, end - validFrom);
        var bad = XmlCharClass.IndexOfNonChar(input.Slice(validFrom, checkLength));
        _scanned += checkLength;
        if (bad >= 0)
        {
            return Fail(input, validFrom + bad, XmlErrorCodes.XML0002);
        }

        // Without a terminator a trailing question mark may still start it.
        var limit = _limits.MaxTokenLength + (terminator < 0 ? 1 : 0);
        if (end - dataStart > limit)
        {
            return Fail(input, pos, XmlErrorCodes.XML1106);
        }

        if (terminator < 0)
        {
            _hint = end - pos;
            _hintData = dataStart - pos;
            return Pending(input, isFinal);
        }

        token = new XmlToken(
            XmlTokenKind.ProcessingInstruction,
            name.Start,
            name.End - name.Start,
            dataStart,
            end - dataStart
        );
        pos = end + 2;
        return Scan.Token;
    }

    // Whether the text so far is, or may become, an XML declaration: "<?xml" followed by white space, "?", or nothing yet.
    private static bool MaybeDeclaration(ReadOnlySpan<char> rest)
    {
        var match = Match(rest, "<?xml");
        return match == 0 || (match == 1 && (rest.Length == 5 || IsSpace(rest[5]) || rest[5] == '?'));
    }

    // Whether the text may still turn out to start with "<?xml" followed by one more character: nothing yet, or up to the five characters of the target.
    private static bool StartsDeclarationLater(ReadOnlySpan<char> rest) =>
        rest.Length <= 5 && Match(rest, "<?xml") >= 0;

    // Productions 23 to 26 through the shared declaration scanner; only at the very start of the input. White space before
    // "<?xml" is XML0007 at the declaration, so the white space is held until the characters after it show whether they start a
    // declaration; a longer run than the declaration limit is content, the same for every chunking.
    private Scan? ScanDeclaration(ReadOnlySpan<char> input, ref int pos, bool isFinal, out XmlToken token)
    {
        token = default;
        var rest = input.Slice(pos);
        var spaces = SkipSpaces(rest, 0);
        if (spaces > _limits.MaxXmlDeclarationLength)
        {
            return null;
        }

        if (spaces > 0 && !isFinal && StartsDeclarationLater(rest.Slice(spaces)))
        {
            return Scan.NeedMore;
        }

        return ScanDeclarationText(input, ref pos, isFinal, out token);
    }

    private Scan? ScanDeclarationText(ReadOnlySpan<char> input, ref int pos, bool isFinal, out XmlToken token)
    {
        token = default;
        var rest = input.Slice(pos);

        // The scanner cannot tell a truncated pseudo-attribute from a bad one, so it only sees whole declarations.
        if (
            !isFinal
            && MaybeDeclaration(rest)
            && rest.Slice(0, Math.Min(rest.Length, _limits.MaxXmlDeclarationLength + 2)).IndexOf("?>".AsSpan()) < 0
        )
        {
            return rest.Length > _limits.MaxXmlDeclarationLength
                ? Fail(input, pos, XmlErrorCodes.XML1114)
                : Scan.NeedMore;
        }

        var result = XmlDeclarationParser.Parse(rest, XmlDeclarationKind.Xml);
        if (!result.IsPresent)
        {
            return null;
        }

        if (result.Error is { } error)
        {
            if (!ReferenceEquals(error, XmlErrorCodes.XML0001))
            {
                return Fail(input, pos + result.ErrorOffset, error);
            }

            return rest.Length > _limits.MaxXmlDeclarationLength
                ? Fail(input, pos, XmlErrorCodes.XML1114)
                : Pending(input, isFinal);
        }

        if (result.Length > _limits.MaxXmlDeclarationLength)
        {
            return Fail(input, pos, XmlErrorCodes.XML1114);
        }

        var versionStart = pos + (int)result.VersionPosition!.Value.CharOffset;
        if (
            !result.Version.SequenceEqual("1.0".AsSpan())
            && !(_allowXml11Declaration && result.Version.SequenceEqual("1.1".AsSpan()))
        )
        {
            return Fail(input, versionStart, XmlErrorCodes.XML0010);
        }

        var encodingStart = result.EncodingPosition is { } encoding ? pos + (int)encoding.CharOffset : 0;
        token = new XmlToken(
            XmlTokenKind.Declaration,
            versionStart,
            result.Version.Length,
            encodingStart,
            result.Encoding.Length,
            result.Standalone is { } standalone ? (standalone ? 'y' : 'n') : '\0'
        );
        pos += result.Length;
        return Scan.Token;
    }
}
