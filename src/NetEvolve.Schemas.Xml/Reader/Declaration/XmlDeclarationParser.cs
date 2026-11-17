namespace NetEvolve.Schemas.Xml.Reader.Declaration;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Parses the XML declaration (XML 1.0 Fifth Edition productions 23 to 26 and 32) and the text declaration (production 77,
/// with the encoding declaration of productions 80 and 81).
/// </summary>
/// <remarks>
/// Limitation: a version <c>1.x</c> is accepted and reported but processed with XML 1.0 rules. Line and column of the
/// reported positions are unknown (zero); the reader owns line tracking.
/// </remarks>
internal static class XmlDeclarationParser
{
    private const string Target = "<?xml";

    /// <summary>Parses a declaration at the very start of <paramref name="input"/>.</summary>
    /// <param name="input">The decoded characters.</param>
    /// <param name="kind">The grammar to apply.</param>
    /// <returns>The result: absent when the input does not start with <c>&lt;?xml</c> as a target, otherwise valid or invalid.</returns>
    internal static XmlDeclarationParseResult Parse(ReadOnlySpan<char> input, XmlDeclarationKind kind)
    {
        var start = SkipSpace(input, 0);
        if (!IsDeclarationStart(input, start))
        {
            return XmlDeclarationParseResult.Absent;
        }

        if (start > 0)
        {
            return XmlDeclarationParseResult.Invalid(XmlErrorCodes.XML0007, start);
        }

        var scanner = new Scanner(input, kind);
        while (scanner.Step()) { }

        return scanner.Result;
    }

    // Walks the pseudo-attributes. Stage: 1 version, 2 encoding, 3 standalone; pseudo-attributes may only advance.
    private ref struct Scanner
    {
        private readonly ReadOnlySpan<char> _input;
        private readonly XmlDeclarationKind _kind;
        private int _pos;
        private int _stage;
        private ReadOnlySpan<char> _version;
        private ReadOnlySpan<char> _encoding;
        private bool? _standalone;
        private XmlPosition? _versionPosition;
        private XmlPosition? _encodingPosition;
        private XmlPosition? _standalonePosition;
        private XmlDeclarationParseResult _result;

        internal Scanner(ReadOnlySpan<char> input, XmlDeclarationKind kind)
        {
            _input = input;
            _kind = kind;
            _pos = Target.Length;
            _stage = 0;
            _version = default;
            _encoding = default;
            _standalone = null;
            _versionPosition = null;
            _encodingPosition = null;
            _standalonePosition = null;
            _result = default;
        }

        internal readonly XmlDeclarationParseResult Result => _result;

        // Consumes one pseudo-attribute or the end marker; returns false when the result is final.
        internal bool Step()
        {
            var afterSpace = SkipSpace(_input, _pos);
            var hadSpace = afterSpace > _pos;
            _pos = afterSpace;
            if (_pos >= _input.Length)
            {
                return Fail(XmlErrorCodes.XML0001, _pos);
            }

            if (_input[_pos] == '?')
            {
                return End();
            }

            var nameStart = _pos;
            while (_pos < _input.Length && IsAsciiLetter(_input[_pos]))
            {
                _pos++;
            }

            var next = IdentifyPseudoAttribute(_input.Slice(nameStart, _pos - nameStart), _kind, _stage);
            if (!hadSpace || next == 0)
            {
                return Fail(XmlErrorCodes.XML0003, nameStart);
            }

            _stage = next;
            return ReadValue(out var valueStart, out var valueLength) && Store(valueStart, valueLength);
        }

        private bool End()
        {
            if (_pos + 1 >= _input.Length)
            {
                return Fail(XmlErrorCodes.XML0001, _input.Length);
            }

            var complete = _input[_pos + 1] == '>' && (_kind == XmlDeclarationKind.Xml ? _stage >= 1 : _stage >= 2);
            if (!complete)
            {
                return Fail(XmlErrorCodes.XML0003, _pos);
            }

            _result = XmlDeclarationParseResult.Valid(
                _pos + 2,
                _version,
                _encoding,
                _standalone,
                _versionPosition,
                _encodingPosition,
                _standalonePosition
            );
            return false;
        }

        // Reads Eq and the quoted value; _pos ends after the closing quote.
        private bool ReadValue(out int valueStart, out int valueLength)
        {
            valueStart = 0;
            valueLength = 0;
            _pos = SkipSpace(_input, _pos);
            if (_pos >= _input.Length || _input[_pos] != '=')
            {
                return FailAtPosition();
            }

            _pos = SkipSpace(_input, _pos + 1);
            if (_pos >= _input.Length || _input[_pos] is not ('"' or '\''))
            {
                return FailAtPosition();
            }

            var length = _input.Slice(_pos + 1).IndexOf(_input[_pos]);
            if (length < 0)
            {
                return Fail(XmlErrorCodes.XML0001, _input.Length);
            }

            valueStart = _pos + 1;
            valueLength = length;
            _pos += length + 2;
            return true;
        }

        private bool Store(int valueStart, int valueLength)
        {
            var value = _input.Slice(valueStart, valueLength);
            var error = _stage switch
            {
                1 => IsVersionNum(value) ? null : XmlErrorCodes.XML0010,
                2 => IsEncName(value) ? null : XmlErrorCodes.XML0017,
                _ => value.SequenceEqual("yes".AsSpan()) || value.SequenceEqual("no".AsSpan())
                    ? null
                    : XmlErrorCodes.XML0017,
            };
            if (error is not null)
            {
                return Fail(error, valueStart);
            }

            var position = new XmlPosition(0, 0, valueStart);
            switch (_stage)
            {
                case 1:
                    _version = value;
                    _versionPosition = position;
                    break;
                case 2:
                    _encoding = value;
                    _encodingPosition = position;
                    break;
                default:
                    _standalone = value.SequenceEqual("yes".AsSpan());
                    _standalonePosition = position;
                    break;
            }

            return true;
        }

        private bool FailAtPosition() =>
            Fail(_pos >= _input.Length ? XmlErrorCodes.XML0001 : XmlErrorCodes.XML0003, _pos);

        private bool Fail(XmlErrorCode code, int offset)
        {
            _result = XmlDeclarationParseResult.Invalid(code, offset);
            return false;
        }
    }

    // The target must be followed by white space, '?', or the end; otherwise it is another target such as xml-stylesheet.
    private static bool IsDeclarationStart(ReadOnlySpan<char> input, int start)
    {
        if (!input.Slice(start).StartsWith(Target.AsSpan()))
        {
            return false;
        }

        var after = start + Target.Length;
        return after >= input.Length || IsSpace(input[after]) || input[after] == '?';
    }

    // Returns the stage of the pseudo-attribute when it may follow the current stage in this grammar, otherwise 0.
    private static int IdentifyPseudoAttribute(ReadOnlySpan<char> name, XmlDeclarationKind kind, int stage)
    {
        var candidate =
            name.SequenceEqual("version".AsSpan()) ? 1
            : name.SequenceEqual("encoding".AsSpan()) ? 2
            : name.SequenceEqual("standalone".AsSpan()) && kind == XmlDeclarationKind.Xml ? 3
            : 0;

        // XMLDecl requires the version first; TextDecl may start with the encoding.
        var mayStart = kind == XmlDeclarationKind.Text || candidate == 1 || stage >= 1;
        return candidate > stage && mayStart ? candidate : 0;
    }

    private static bool IsVersionNum(ReadOnlySpan<char> value)
    {
        if (value.Length < 3 || value[0] != '1' || value[1] != '.')
        {
            return false;
        }

        for (var i = 2; i < value.Length; i++)
        {
            if ((uint)(value[i] - '0') > 9)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsEncName(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty || !IsAsciiLetter(value[0]))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            if (!IsAsciiLetter(c) && (uint)(c - '0') > 9 && c is not ('.' or '_' or '-'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiLetter(char c) => (uint)((c | 0x20) - 'a') <= 'z' - 'a';

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\r' or '\n';

    private static int SkipSpace(ReadOnlySpan<char> input, int pos)
    {
        while (pos < input.Length && IsSpace(input[pos]))
        {
            pos++;
        }

        return pos;
    }
}
