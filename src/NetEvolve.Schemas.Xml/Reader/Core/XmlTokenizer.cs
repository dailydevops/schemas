namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Declaration;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// The lexical, resumable tokenizer over character spans (design #108, AP1 to AP4): XML 1.0 (Fifth Edition) productions
/// 14 to 21 and 39 to 44 and the XML declaration. It owns no buffer, no element stack, and no names.
/// </summary>
/// <remarks>
/// An incomplete atomic token consumes nothing and returns <see cref="XmlTokenStatus.NeedMoreData"/> with <c>consumed</c>
/// at its start; the caller re-presents that tail followed by more data. A pending token carries a scan-resume hint, so
/// names, references, comments, and processing instructions are scanned in linear total work however the input is chunked.
/// The tokens before a malformed construct, the first error code, and the position of that error do not depend on the chunking:
/// a run of characters before an invalid character is a token of its own, and white space at the start is held until it is known
/// whether an XML declaration follows (XML0007), up to <see cref="XmlReaderLimits.MaxXmlDeclarationLength"/> characters.
/// Limitations: the document type declaration (only with a handler) is rescanned only after the pending tail has doubled
/// (feeder rule of AP2 without a hint, because the subset scanner is XML-14); XML 1.1 line ends are not recognized.
/// The tokenizer trusts the caller to re-present the same tail, so it stays internal. Not thread-safe.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal partial struct XmlTokenizer
{
    private enum Mode : byte
    {
        Content,
        TagBody,
        AfterAttributeName,
        BeforeAttributeValue,
        AttributeValue,
        EndTagClose,
        CData,
    }

    private enum Scan : byte
    {
        Token,
        Continue,
        NeedMore,
        End,
        Error,
    }

    private readonly XmlReaderLimits _limits;
    private readonly bool _allowDoctype;
    private readonly bool _allowEntityReferences;
    private Mode _mode;
    private bool _sawSpace;
    private bool _bomPending;
    private bool _atStart;
    private char _quote;
    private int _hint;
    private int _hintData;
    private int _doctypeRetry;
    private int _failOffset;
    private XmlErrorCode? _failCode;
    private int _origin;
    private int _constructStart;
    private int _line;
    private int _column;
    private long _offset;
    private bool _afterCr;
    private long _scanned;
    private XmlPosition _tokenPosition;
    private XmlDiagnostic? _diagnostic;
    private XmlDoctypeParts _doctype;

    /// <summary>Initializes a new instance of the <see cref="XmlTokenizer"/> struct.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="allowDoctype">Whether a document type declaration is scanned (a handler is installed) or rejected.</param>
    /// <param name="allowEntityReferences">Whether references to other entities become tokens (an entity input stack is installed) or are rejected.</param>
    internal XmlTokenizer(XmlReaderLimits limits, bool allowDoctype, bool allowEntityReferences)
    {
        _limits = limits;
        _allowDoctype = allowDoctype;
        _allowEntityReferences = allowEntityReferences;
        _mode = Mode.Content;
        _sawSpace = false;
        _bomPending = true;
        _atStart = true;
        _quote = '\0';
        _hint = 0;
        _hintData = 0;
        _doctypeRetry = 0;
        _failOffset = 0;
        _failCode = null;
        _origin = 0;
        _constructStart = 0;
        _line = 0;
        _column = 0;
        _offset = 0;
        _afterCr = false;
        _scanned = 0;
        _tokenPosition = default;
        _diagnostic = null;
        _doctype = default;
    }

    /// <summary>Creates a tokenizer for the replacement text of an entity (production 43): content from its first character.</summary>
    /// <param name="limits">The limits.</param>
    /// <returns>A tokenizer that expects neither a byte order mark nor an XML declaration, rejects a document type declaration, and turns references to other entities into tokens.</returns>
    internal static XmlTokenizer ForReplacementText(XmlReaderLimits limits)
    {
        Argument.ThrowIfNull(limits);

        return new XmlTokenizer(limits, false, true) { _bomPending = false, _atStart = false };
    }

    /// <summary>Gets the position of the next unconsumed character; a leading byte order mark is not counted.</summary>
    internal readonly XmlPosition Position => new(_line + 1, _column + 1, _offset);

    /// <summary>Gets the position where the construct of the last token began.</summary>
    internal readonly XmlPosition TokenPosition => _tokenPosition;

    /// <summary>Gets the error, valid when the status was <see cref="XmlTokenStatus.Error"/>.</summary>
    internal readonly XmlDiagnostic? Diagnostic => _diagnostic;

    /// <summary>Gets the parts of the last <see cref="XmlTokenKind.Doctype"/> token, relative to the span of that call.</summary>
    internal readonly XmlDoctypeParts LastDoctype => _doctype;

    /// <summary>Gets the number of characters examined by comment and processing instruction scans, the test oracle for linear rescan cost.</summary>
    internal readonly long ScannedCharacters => _scanned;

    /// <summary>Produces the next token.</summary>
    /// <param name="input">The unconsumed tail followed by new data; <paramref name="consumed"/> is relative to it.</param>
    /// <param name="isFinal">Whether no more data follows.</param>
    /// <param name="token">The token, valid when the status is <see cref="XmlTokenStatus.Token"/>; its slices are relative to <paramref name="input"/>.</param>
    /// <param name="consumed">The number of characters to drop before the next call.</param>
    /// <returns>The status.</returns>
    internal XmlTokenStatus Next(ReadOnlySpan<char> input, bool isFinal, out XmlToken token, out int consumed)
    {
        token = default;
        consumed = 0;
        if (_diagnostic is not null)
        {
            return XmlTokenStatus.Error;
        }

        _origin = 0;
        if (_bomPending && !input.IsEmpty)
        {
            _bomPending = false;
            if (input[0] == '﻿')
            {
                _origin = 1;
            }
        }

        var pos = _origin;
        var hint = _hint;
        var hintData = _hintData;
        _hint = 0;
        _hintData = 0;
        var result = Scan.Continue;
        while (result == Scan.Continue)
        {
            var first = pos == _origin;
            result = Step(input, ref pos, isFinal, first ? hint : 0, first ? hintData : 0, out token);
        }

        if (result == Scan.Error)
        {
            return XmlTokenStatus.Error;
        }

        if (result == Scan.Token)
        {
            _tokenPosition = PositionAt(input, _constructStart);
            _atStart = false;
        }
        else if (pos > _origin)
        {
            _atStart = false;
        }

        Advance(input.Slice(_origin, pos - _origin));
        consumed = pos;
        return result switch
        {
            Scan.Token => XmlTokenStatus.Token,
            Scan.End => XmlTokenStatus.End,
            _ => XmlTokenStatus.NeedMoreData,
        };
    }

    private Scan Step(ReadOnlySpan<char> input, ref int pos, bool isFinal, int hint, int hintData, out XmlToken token)
    {
        token = default;
        _constructStart = pos;
        if (_atStart)
        {
            var declaration = ScanDeclaration(input, ref pos, isFinal, out token);
            if (declaration is not null)
            {
                return declaration.Value;
            }
        }

        switch (_mode)
        {
            case Mode.Content:
                return ScanContent(input, ref pos, isFinal, hint, hintData, out token);
            case Mode.TagBody:
                return ScanTagBody(input, ref pos, isFinal, hint, out token);
            case Mode.AfterAttributeName:
                return ScanAfterAttributeName(input, ref pos, isFinal);
            case Mode.BeforeAttributeValue:
                return ScanBeforeAttributeValue(input, ref pos, isFinal);
            case Mode.AttributeValue:
                return ScanAttributeValue(input, ref pos, isFinal, hint, out token);
            case Mode.EndTagClose:
                return ScanEndTagClose(input, ref pos, isFinal, out token);
            default:
                return ScanCData(input, ref pos, isFinal, out token);
        }
    }

    private Scan ScanContent(
        ReadOnlySpan<char> input,
        ref int pos,
        bool isFinal,
        int hint,
        int hintData,
        out XmlToken token
    )
    {
        token = default;
        if (pos >= input.Length)
        {
            return isFinal ? Scan.End : Scan.NeedMore;
        }

        switch (input[pos])
        {
            case '<':
                return ScanMarkup(input, ref pos, isFinal, hint, hintData, out token);
            case '&':
                return ScanReference(input, ref pos, isFinal, hint, out token);
            case '\r':
            case '\n':
                return ScanLineBreak(input, ref pos, isFinal, XmlTokenKind.LineBreak, '\n', out token);
            default:
                return ScanCharData(input, ref pos, isFinal, false, out token);
        }
    }

    private Scan ScanTagBody(ReadOnlySpan<char> input, ref int pos, bool isFinal, int hint, out XmlToken token)
    {
        token = default;
        pos = SkipSpace(input, pos, ref _sawSpace);
        _constructStart = pos;
        if (pos >= input.Length)
        {
            return Pending(input, isFinal);
        }

        var c = input[pos];
        if (c is '>' or '/')
        {
            return ScanTagEnd(input, ref pos, isFinal, out token);
        }

        if (!_sawSpace)
        {
            return Fail(input, pos, XmlErrorCodes.XML0011);
        }

        var end = ScanName(input, pos, pos + hint, isFinal, out var incomplete);
        if (end - pos > _limits.MaxNameLength)
        {
            return Fail(input, pos, XmlErrorCodes.XML1105);
        }

        if (incomplete || end >= input.Length)
        {
            _hint = end - pos;
            return Pending(input, isFinal);
        }

        if (end == pos)
        {
            return Fail(input, pos, XmlErrorCodes.XML0011);
        }

        token = new XmlToken(XmlTokenKind.AttributeName, pos, end - pos);
        pos = end;
        _mode = Mode.AfterAttributeName;
        return Scan.Token;
    }

    // The end of a start tag: '>' or '/>' (production 40 and 44).
    private Scan ScanTagEnd(ReadOnlySpan<char> input, ref int pos, bool isFinal, out XmlToken token)
    {
        token = default;
        var isEmpty = input[pos] == '/';
        if (isEmpty)
        {
            if (pos + 1 >= input.Length)
            {
                return Pending(input, isFinal);
            }

            if (input[pos + 1] != '>')
            {
                return Fail(input, pos, XmlErrorCodes.XML0011);
            }

            pos++;
        }

        pos++;
        _mode = Mode.Content;
        token = new XmlToken(isEmpty ? XmlTokenKind.EmptyTagEnd : XmlTokenKind.StartTagEnd, 0, 0);
        return Scan.Token;
    }

    private Scan ScanAfterAttributeName(ReadOnlySpan<char> input, ref int pos, bool isFinal)
    {
        var ignored = false;
        pos = SkipSpace(input, pos, ref ignored);
        if (pos >= input.Length)
        {
            return Pending(input, isFinal);
        }

        if (input[pos] != '=')
        {
            return Fail(input, pos, XmlErrorCodes.XML0011);
        }

        pos++;
        _mode = Mode.BeforeAttributeValue;
        return Scan.Continue;
    }

    private Scan ScanBeforeAttributeValue(ReadOnlySpan<char> input, ref int pos, bool isFinal)
    {
        var ignored = false;
        pos = SkipSpace(input, pos, ref ignored);
        if (pos >= input.Length)
        {
            return Pending(input, isFinal);
        }

        var c = input[pos];
        if (c is not ('"' or '\''))
        {
            return Fail(input, pos, XmlErrorCodes.XML0011);
        }

        _quote = c;
        pos++;
        _mode = Mode.AttributeValue;
        return Scan.Continue;
    }

    private Scan ScanEndTagClose(ReadOnlySpan<char> input, ref int pos, bool isFinal, out XmlToken token)
    {
        token = default;
        var ignored = false;
        pos = SkipSpace(input, pos, ref ignored);
        _constructStart = pos;
        if (pos >= input.Length)
        {
            return Pending(input, isFinal);
        }

        if (input[pos] != '>')
        {
            return Fail(input, pos, XmlErrorCodes.XML0011);
        }

        pos++;
        _mode = Mode.Content;
        token = new XmlToken(XmlTokenKind.EndTagEnd, 0, 0);
        return Scan.Token;
    }

    private Scan ScanCData(ReadOnlySpan<char> input, ref int pos, bool isFinal, out XmlToken token)
    {
        token = default;
        if (pos >= input.Length)
        {
            return Pending(input, isFinal);
        }

        return input[pos] is '\r' or '\n'
            ? ScanLineBreak(input, ref pos, isFinal, XmlTokenKind.CData, '\n', out token)
            : ScanCharData(input, ref pos, isFinal, true, out token);
    }

    // The end of the input inside a construct: more data, or the fatal XML0001 for final input.
    private Scan Pending(ReadOnlySpan<char> input, bool isFinal) =>
        isFinal ? Fail(input, input.Length, XmlErrorCodes.XML0001) : Scan.NeedMore;

    private Scan Fail(ReadOnlySpan<char> input, int offset, XmlErrorCode code)
    {
        _diagnostic = new XmlDiagnostic(code, PositionAt(input, offset));
        return Scan.Error;
    }

    private static int SkipSpace(ReadOnlySpan<char> input, int pos, ref bool sawSpace)
    {
        while (pos < input.Length && input[pos] is ' ' or '\t' or '\r' or '\n')
        {
            pos++;
            sawSpace = true;
        }

        return pos;
    }

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\r' or '\n';

    // The position of input[offset], counted from the start of this call (a leading byte order mark is skipped).
    private readonly XmlPosition PositionAt(ReadOnlySpan<char> input, int offset)
    {
        var line = _line;
        var column = _column;
        var charOffset = _offset;
        var afterCr = _afterCr;
        Advance(
            input.Slice(_origin, Math.Max(offset, _origin) - _origin),
            ref line,
            ref column,
            ref charOffset,
            ref afterCr
        );
        return new XmlPosition(line + 1, column + 1, charOffset);
    }

    private void Advance(ReadOnlySpan<char> consumed) =>
        Advance(consumed, ref _line, ref _column, ref _offset, ref _afterCr);

    // CRLF and a lone CR are one line break, even when a consumed span ends between the CR and the LF; the offset still
    // counts every character.
    private static void Advance(
        ReadOnlySpan<char> text,
        ref int line,
        ref int column,
        ref long offset,
        ref bool afterCr
    )
    {
        foreach (var c in text)
        {
            offset++;
            var skipLineFeed = afterCr && c == '\n';
            afterCr = c == '\r';
            if (skipLineFeed)
            {
                continue;
            }

            if (c is '\r' or '\n')
            {
                line++;
                column = 0;
            }
            else
            {
                column++;
            }
        }
    }
}
