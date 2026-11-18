namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using System;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Text.CharClasses;

internal sealed partial class DtdSubsetParser
{
    private static readonly char[] ReferenceStarts = ['&', '%'];

    private static bool IsSpace(char c) => WhitespaceNormalizer.IsXmlSpace(c);

    private bool Fail(XmlErrorCode code)
    {
        _error ??= code;
        return false;
    }

    // A parameter-entity reference where a declaration part is expected violates WFC PEs in Internal Subset.
    private bool FailHere() =>
        Fail(_pos < _text.Length && _text[_pos] == '%' ? XmlErrorCodes.XML0019 : XmlErrorCodes.XML0011);

    private bool SkipSpaces()
    {
        var start = _pos;
        while (_pos < _text.Length && IsSpace(_text[_pos]))
        {
            _pos++;
        }

        return _pos > start;
    }

    private bool RequireSpace() => SkipSpaces() || FailHere();

    private bool Match(string keyword)
    {
        if (string.CompareOrdinal(_text, _pos, keyword, 0, keyword.Length) != 0)
        {
            return false;
        }

        _pos += keyword.Length;
        return true;
    }

    private bool Current(char c) => _pos < _text.Length && _text[_pos] == c;

    // Production 5.
    private bool ScanName(out string name)
    {
        name = string.Empty;
        var length = XmlCharClass.NameLength(_text.AsSpan(_pos));
        if (length == 0)
        {
            return FailHere();
        }

        if (length > _limits.MaxNameLength)
        {
            return Fail(XmlErrorCodes.XML1105);
        }

        name = _text.Substring(_pos, length);
        _pos += length;
        return true;
    }

    // Namespaces in XML 1.0: an entity name and a notation name are NCNames, so a colon is an error.
    private bool ScanNcName(out string name) =>
        ScanName(out name) && (name.AsSpan().IndexOf(':') < 0 || Fail(XmlErrorCodes.XML3004));

    // Production 7.
    private bool ScanNmtoken(out string token)
    {
        token = string.Empty;
        var start = _pos;
        while (_pos < _text.Length)
        {
            var c = _text[_pos];
            if (char.IsHighSurrogate(c) && _pos + 1 < _text.Length && char.IsLowSurrogate(_text[_pos + 1]))
            {
                if (!XmlCharClass.IsNameChar(char.ConvertToUtf32(c, _text[_pos + 1])))
                {
                    break;
                }

                _pos += 2;
            }
            else if (XmlCharClass.IsNameChar(c))
            {
                _pos++;
            }
            else
            {
                break;
            }
        }

        if (_pos == start)
        {
            return FailHere();
        }

        if (_pos - start > _limits.MaxNameLength)
        {
            return Fail(XmlErrorCodes.XML1105);
        }

        token = _text.Substring(start, _pos - start);
        return true;
    }

    // The raw text between matching quotes; `_pos` ends after the closing quote.
    private bool ScanQuoted(out string value)
    {
        value = string.Empty;
        if (_pos >= _text.Length || _text[_pos] is not ('"' or '\''))
        {
            return FailHere();
        }

        var close = _text.AsSpan(_pos + 1).IndexOf(_text[_pos]);
        if (close < 0)
        {
            return Fail(XmlErrorCodes.XML0011);
        }

        value = _text.Substring(_pos + 1, close);
        _pos += close + 2;
        return true;
    }

    // Production 12: only PubidChar is allowed.
    private bool ScanPublicLiteral(out string value)
    {
        if (!ScanQuoted(out value))
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!IsPubidChar(c))
            {
                return Fail(XmlErrorCodes.XML0011);
            }
        }

        return true;
    }

    private static bool IsPubidChar(char c) =>
        c
            is ' '
                or '\r'
                or '\n'
                or (>= 'a' and <= 'z')
                or (>= 'A' and <= 'Z')
                or (>= '0' and <= '9')
                or '-'
                or '\''
                or '('
                or ')'
                or '+'
                or ','
                or '.'
                or '/'
                or ':'
                or '='
                or '?'
                or ';'
                or '!'
                or '*'
                or '#'
                or '@'
                or '$'
                or '_'
                or '%';

    // Production 75 (and 83 when `publicOnly`): SYSTEM literal, or PUBLIC literal and an optional or required literal.
    private bool ScanExternalId(bool publicOnly, out string? publicId, out string? systemId)
    {
        publicId = null;
        systemId = null;
        if (Match("SYSTEM"))
        {
            if (!RequireSpace() || !ScanQuoted(out var system))
            {
                return false;
            }

            systemId = system;
            return true;
        }

        if (!Match("PUBLIC"))
        {
            return FailHere();
        }

        if (!RequireSpace() || !ScanPublicLiteral(out var pub))
        {
            return false;
        }

        publicId = pub;
        var afterPublic = _pos;
        if (!SkipSpaces() || _pos >= _text.Length || _text[_pos] is not ('"' or '\''))
        {
            // A notation may carry only the public identifier (production 83).
            if (publicOnly)
            {
                _pos = afterPublic;
                return true;
            }

            return FailHere();
        }

        if (!ScanQuoted(out var sys))
        {
            return false;
        }

        systemId = sys;
        return true;
    }

    // Reads the reference at `amp` of `source` (productions 66 and 68): `expansion` is the character of a character
    // reference, null for an entity reference, which stays as written.
    private bool ReadReference(string source, int amp, out int next, out string? expansion)
    {
        expansion = null;
        next = amp + 1;
        if (next < source.Length && source[next] == '#')
        {
            next++;
            var hex = next < source.Length && source[next] == 'x';
            if (hex)
            {
                next++;
            }

            var start = next;
            long value = 0;
            while (next < source.Length && DigitValue(source[next], hex) is var digit and >= 0)
            {
                value = Math.Min((value * (hex ? 16 : 10)) + digit, 0x110000);
                next++;
            }

            if (next == start || next >= source.Length || source[next] != ';')
            {
                return Fail(XmlErrorCodes.XML0011);
            }

            next++;
            if (!XmlCharClass.IsChar((int)value))
            {
                return Fail(XmlErrorCodes.XML0009);
            }

            expansion = char.ConvertFromUtf32((int)value);
            return true;
        }

        var length = XmlCharClass.NameLength(source.AsSpan(next));
        if (length == 0 || next + length >= source.Length || source[next + length] != ';')
        {
            return Fail(XmlErrorCodes.XML0011);
        }

        next += length + 1;
        return true;
    }

    private static int DigitValue(char c, bool hex) =>
        c is >= '0' and <= '9' ? c - '0'
        : hex && c is >= 'a' and <= 'f' ? c - 'a' + 10
        : hex && c is >= 'A' and <= 'F' ? c - 'A' + 10
        : -1;

    // Production 9: the replacement text has character references expanded; a parameter-entity reference is not allowed in the
    // internal subset and general-entity references are bypassed.
    private bool ScanEntityValue(out string value)
    {
        if (!ScanQuoted(out var raw))
        {
            value = string.Empty;
            return false;
        }

        value = raw;
        var special = raw.IndexOfAny(ReferenceStarts);
        if (special < 0)
        {
            return true;
        }

        var builder = new StringBuilder(raw.Length);
        var i = 0;
        while (i < raw.Length)
        {
            if (raw[i] == '%')
            {
                return Fail(XmlErrorCodes.XML0019);
            }

            if (raw[i] != '&')
            {
                builder.Append(raw[i++]);
                continue;
            }

            if (!ReadReference(raw, i, out var next, out var expansion))
            {
                return false;
            }

            builder.Append(expansion ?? raw.Substring(i, next - i));
            i = next;
        }

        value = builder.ToString();
        return true;
    }

    // Production 10: the literal stays as written (XML-16 expands and normalizes it); `<` is not allowed.
    private bool ScanAttributeValue(out string value)
    {
        if (!ScanQuoted(out value))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '<')
            {
                return Fail(XmlErrorCodes.XML0013);
            }

            if (value[i] == '&')
            {
                if (!ReadReference(value, i, out var next, out _))
                {
                    return false;
                }

                i = next - 1;
            }
        }

        return true;
    }
}
