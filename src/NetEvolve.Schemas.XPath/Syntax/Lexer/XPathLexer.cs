namespace NetEvolve.Schemas.XPath.Syntax.Lexer;

using System;
using NetEvolve.Schemas.Xml.Text.CharClasses;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Turns an XPath 2.0 expression into tokens without allocating; tokens are ranges into the input.
/// </summary>
/// <remarks>
/// <para>
/// Terminals follow XPath 2.0 (Second Edition) A.2.1 and the delimiting rules of A.2.2. Comments nest and count as
/// whitespace. A reserved word is an operator or keyword only where the grammar allows it: after an operand
/// (name, literal, <c>)</c>, <c>]</c>, <c>.</c>, <c>..</c>, wildcard) it is an operator, elsewhere a name. The words
/// <see langword="if"/>, <see langword="for"/>, <c>some</c>, and <c>every</c> are keywords only when <c>(</c>
/// respectively <c>$</c> follows, and <c>of</c> and <see langword="as"/> only after <c>instance</c>, <c>treat</c>,
/// <c>cast</c>, and <c>castable</c>. After a <c>/</c> the next token is a step start, so <c>/ * 5</c> reads as
/// <c>/*</c> followed by <c>5</c>, as the leading-lone-slash constraint requires.
/// </para>
/// <para>
/// Limitation: the lexer stops at the first error and reports the end of the input afterwards. After
/// <c>instance of</c>, <c>treat as</c>, <c>cast as</c>, and <c>castable as</c> the characters <c>*</c>, <c>+</c>, and
/// <c>?</c> are occurrence indicators until a keyword or a comma outside the type ends the sequence type, as the
/// occurrence-indicators constraint requires. The terminals of XPath 3.0 (<c>let</c>, <c>:=</c>, <c>!</c>, <c>||</c>,
/// <c>#</c>, braces, <c>function</c>, and <c>Q{uri}local</c>) are recognized only when the version is XPath 3.0 or
/// later, so XPath 2.0 input is tokenized as before; XPath 3.1 terminals are not recognized.
/// </para>
/// </remarks>
internal ref struct XPathLexer
{
    private readonly ReadOnlySpan<char> _text;
    private readonly bool _v30;
    private int _position;
    private int _depth;
    private int _typeDepth;
    private bool _afterOperand;
    private bool _expectOfOrAs;
    private bool _inType;
    private bool _failed;

    /// <summary>Initializes a new instance of the <see cref="XPathLexer"/> struct.</summary>
    /// <param name="text">The expression; it must outlive the lexer.</param>
    /// <param name="version">The language version; the terminals of XPath 3.0 are recognized from <see cref="XPathVersion.V30"/> on.</param>
    public XPathLexer(ReadOnlySpan<char> text, XPathVersion version = XPathVersion.V20)
    {
        _text = text;
        _v30 = version >= XPathVersion.V30;
        _position = 0;
        _depth = 0;
        _typeDepth = 0;
        _afterOperand = false;
        _expectOfOrAs = false;
        _inType = false;
        _failed = false;
    }

    /// <summary>Reads the next token; at the end of the input and after an error it returns <see cref="XPathTokenKind.EndOfInput"/>.</summary>
    /// <returns>The token.</returns>
    public XPathToken Next()
    {
        if (_failed)
        {
            return Token(XPathTokenKind.EndOfInput, _text.Length, 0);
        }

        var start = SkipTrivia(_position, out var unterminated);
        if (unterminated)
        {
            return Fail(XPathLexicalError.UnterminatedComment, start, _text.Length);
        }

        _position = start;
        if (start >= _text.Length)
        {
            return Token(XPathTokenKind.EndOfInput, start, 0);
        }

        var c = _text[start];
        if (IsDigit(c) || (c == '.' && IsDigit(Peek(start + 1))))
        {
            return Number(start);
        }

        if (c is '"' or '\'')
        {
            return StringLiteral(start, c);
        }

        var nameLength = NcNameLength(start);
        return nameLength > 0 ? Word(start, nameLength) : Symbol(start, c);
    }

    // Skips whitespace and nested comments. When a comment is not closed, returns the position of its outermost
    // opening delimiter and sets unterminated.
    private readonly int SkipTrivia(int position, out bool unterminated)
    {
        unterminated = false;
        while (position < _text.Length)
        {
            var c = _text[position];
            if (c is ' ' or '\t' or '\r' or '\n')
            {
                position++;
            }
            else if (c == '(' && Peek(position + 1) == ':')
            {
                var opening = position;
                var depth = 0;
                do
                {
                    if (position >= _text.Length)
                    {
                        unterminated = true;
                        return opening;
                    }

                    if (_text[position] == '(' && Peek(position + 1) == ':')
                    {
                        depth++;
                        position += 2;
                    }
                    else if (_text[position] == ':' && Peek(position + 1) == ')')
                    {
                        depth--;
                        position += 2;
                    }
                    else
                    {
                        position++;
                    }
                } while (depth > 0);
            }
            else
            {
                break;
            }
        }

        return position;
    }

    private XPathToken Number(int start)
    {
        var i = start;
        var kind = XPathTokenKind.IntegerLiteral;
        i = SkipDigits(i);

        if (Peek(i) == '.')
        {
            kind = XPathTokenKind.DecimalLiteral;
            i = SkipDigits(i + 1);
        }

        if (Peek(i) is 'e' or 'E')
        {
            var j = i + 1;
            if (Peek(j) is '+' or '-')
            {
                j++;
            }

            if (IsDigit(Peek(j)))
            {
                kind = XPathTokenKind.DoubleLiteral;
                i = SkipDigits(j);
            }
        }

        // A.2.2: a numeric literal must be delimited from a following name.
        return NcNameLength(i) > 0
            ? Fail(XPathLexicalError.InvalidNumber, start, i)
            : Emit(kind, start, i - start, true);
    }

    private XPathToken StringLiteral(int start, char quote)
    {
        var i = start + 1;
        while (i < _text.Length)
        {
            if (_text[i] == quote)
            {
                if (Peek(i + 1) != quote)
                {
                    return Emit(XPathTokenKind.StringLiteral, start, i + 1 - start, true);
                }

                i++;
            }

            i++;
        }

        return Fail(XPathLexicalError.UnterminatedString, start, _text.Length);
    }

    private XPathToken Word(int start, int length)
    {
        var end = start + length;
        if (Peek(end) == ':')
        {
            if (Peek(end + 1) == '*')
            {
                return Emit(XPathTokenKind.PrefixWildcard, start, length + 2, true);
            }

            var local = NcNameLength(end + 1);
            if (local > 0)
            {
                return Emit(XPathTokenKind.QName, start, length + 1 + local, true);
            }
        }

        if (_v30 && length == 1 && _text[start] == 'Q' && Peek(end) == '{')
        {
            return BracedName(start);
        }

        var word = _text.Slice(start, length);
        if (_expectOfOrAs)
        {
            _expectOfOrAs = false;
            var typeKeyword = word switch
            {
                "of" => XPathTokenKind.Of,
                "as" => XPathTokenKind.As,
                _ => XPathTokenKind.Name,
            };
            if (typeKeyword != XPathTokenKind.Name)
            {
                _inType = true;
                _typeDepth = _depth;
                return Emit(typeKeyword, start, length, false);
            }
        }

        var kind = _afterOperand ? OperatorKeyword(word) : OperandKeyword(word, end);
        if (kind == XPathTokenKind.Name)
        {
            return Emit(kind, start, length, true);
        }

        if (kind == XPathTokenKind.As)
        {
            // The "as" of a parameter or return type of an inline function: a sequence type follows.
            _inType = true;
            _typeDepth = _depth;
            return Emit(kind, start, length, false);
        }

        if (kind == XPathTokenKind.Function)
        {
            // Inside a sequence type, "function" starts a function test and keeps the occurrence handling.
            return Emit(kind, start, length, false);
        }

        _inType = false;
        _expectOfOrAs =
            kind is XPathTokenKind.Instance or XPathTokenKind.Treat or XPathTokenKind.Cast or XPathTokenKind.Castable;
        return Emit(kind, start, length, false);
    }

    // Q{uri}local: the braces hold no braces, and an NCName follows.
    private XPathToken BracedName(int start)
    {
        var close = _text.Slice(start + 2).IndexOfAny('{', '}');
        if (close < 0)
        {
            return Fail(XPathLexicalError.InvalidBracedName, start, _text.Length);
        }

        var closeAt = start + 2 + close;
        if (_text[closeAt] == '{')
        {
            return Fail(XPathLexicalError.InvalidBracedName, start, closeAt + 1);
        }

        var localStart = closeAt + 1;
        var local = NcNameLength(localStart);
        return local > 0
            ? Emit(XPathTokenKind.BracedName, start, localStart + local - start, true)
            : Fail(XPathLexicalError.InvalidBracedName, start, localStart);
    }

    private XPathToken Symbol(int start, char c)
    {
        switch (c)
        {
            case ',':
                if (_depth == _typeDepth)
                {
                    _inType = false;
                }

                return Emit(XPathTokenKind.Comma, start, 1, false);
            case '(':
                _depth++;
                return Emit(XPathTokenKind.LeftParen, start, 1, false);
            case ')':
                _depth--;
                if (_depth < _typeDepth)
                {
                    _inType = false;
                }

                return Emit(XPathTokenKind.RightParen, start, 1, true);
            case '[':
                return Emit(XPathTokenKind.LeftBracket, start, 1, false);
            case ']':
                // A sequence type never contains a bracket, so this closes the predicate that held the type.
                _inType = false;
                return Emit(XPathTokenKind.RightBracket, start, 1, true);
            case '.':
                return Peek(start + 1) == '.'
                    ? Emit(XPathTokenKind.DotDot, start, 2, true)
                    : Emit(XPathTokenKind.Dot, start, 1, true);
            case '/':
                return Peek(start + 1) == '/'
                    ? Emit(XPathTokenKind.SlashSlash, start, 2, false)
                    : Emit(XPathTokenKind.Slash, start, 1, false);
            case '@':
                return Emit(XPathTokenKind.At, start, 1, false);
            case '|':
                return _v30 && Peek(start + 1) == '|'
                    ? Emit(XPathTokenKind.Concat, start, 2, false)
                    : Emit(XPathTokenKind.Pipe, start, 1, false);
            case '-':
                return Emit(XPathTokenKind.Minus, start, 1, false);
            case '$':
                return Emit(XPathTokenKind.Dollar, start, 1, false);
            case '?':
                return Emit(XPathTokenKind.Question, start, 1, true);
            case '=':
                return Emit(XPathTokenKind.Equal, start, 1, false);
            case '+':
                return Emit(XPathTokenKind.Plus, start, 1, _inType && _afterOperand);
            case '*':
                return Star(start);
            default:
                return Compound(start, c);
        }
    }

    private XPathToken Compound(int start, char c)
    {
        switch (c)
        {
            case '!' when Peek(start + 1) == '=':
                return Emit(XPathTokenKind.NotEqual, start, 2, false);
            case '<':
                return Peek(start + 1) switch
                {
                    '=' => Emit(XPathTokenKind.LessOrEqual, start, 2, false),
                    '<' => Emit(XPathTokenKind.Precedes, start, 2, false),
                    _ => Emit(XPathTokenKind.Less, start, 1, false),
                };
            case '>':
                return Peek(start + 1) switch
                {
                    '=' => Emit(XPathTokenKind.GreaterOrEqual, start, 2, false),
                    '>' => Emit(XPathTokenKind.Follows, start, 2, false),
                    _ => Emit(XPathTokenKind.Greater, start, 1, false),
                };
            case ':' when Peek(start + 1) == ':':
                return Emit(XPathTokenKind.ColonColon, start, 2, false);
            case ':' when _v30 && Peek(start + 1) == '=':
                return Emit(XPathTokenKind.ColonEqual, start, 2, false);
            case '!' when _v30:
                return Emit(XPathTokenKind.Bang, start, 1, false);
            case '#' when _v30:
                return Emit(XPathTokenKind.Hash, start, 1, false);
            case '{' when _v30:
                // A sequence type never contains a brace, so this ends the return type of an inline function.
                _inType = false;
                return Emit(XPathTokenKind.LeftBrace, start, 1, false);
            case '}' when _v30:
                return Emit(XPathTokenKind.RightBrace, start, 1, true);
            default:
                var width = char.IsHighSurrogate(c) && char.IsLowSurrogate(Peek(start + 1)) ? 2 : 1;
                return Fail(XPathLexicalError.IllegalCharacter, start, start + width);
        }
    }

    private XPathToken Star(int start)
    {
        if (_afterOperand)
        {
            // A multiplication, or an occurrence indicator while a sequence type is being read.
            return Emit(XPathTokenKind.Star, start, 1, _inType);
        }

        if (Peek(start + 1) == ':')
        {
            var local = NcNameLength(start + 2);
            if (local > 0)
            {
                return Emit(XPathTokenKind.LocalWildcard, start, 2 + local, true);
            }
        }

        return Emit(XPathTokenKind.Wildcard, start, 1, true);
    }

    private readonly XPathTokenKind OperatorKeyword(ReadOnlySpan<char> word) =>
        word switch
        {
            "as" when _v30 => XPathTokenKind.As,
            "and" => XPathTokenKind.And,
            "or" => XPathTokenKind.Or,
            "div" => XPathTokenKind.Div,
            "idiv" => XPathTokenKind.IDiv,
            "mod" => XPathTokenKind.Mod,
            "union" => XPathTokenKind.Union,
            "intersect" => XPathTokenKind.Intersect,
            "except" => XPathTokenKind.Except,
            "to" => XPathTokenKind.To,
            "in" => XPathTokenKind.In,
            "return" => XPathTokenKind.Return,
            "satisfies" => XPathTokenKind.Satisfies,
            "then" => XPathTokenKind.Then,
            "else" => XPathTokenKind.Else,
            "instance" => XPathTokenKind.Instance,
            "treat" => XPathTokenKind.Treat,
            "castable" => XPathTokenKind.Castable,
            "cast" => XPathTokenKind.Cast,
            "eq" => XPathTokenKind.Eq,
            "ne" => XPathTokenKind.Ne,
            "lt" => XPathTokenKind.Lt,
            "le" => XPathTokenKind.Le,
            "gt" => XPathTokenKind.Gt,
            "ge" => XPathTokenKind.Ge,
            "is" => XPathTokenKind.Is,
            _ => XPathTokenKind.Name,
        };

    // Keywords that start an expression need the next significant character to be a keyword and not a name.
    private readonly XPathTokenKind OperandKeyword(ReadOnlySpan<char> word, int end)
    {
        var next = SkipTrivia(end, out var unterminated);
        var following = unterminated ? '\0' : Peek(next);
        return word switch
        {
            "if" when following == '(' => XPathTokenKind.If,
            "for" when following == '$' => XPathTokenKind.For,
            "some" when following == '$' => XPathTokenKind.Some,
            "every" when following == '$' => XPathTokenKind.Every,
            "let" when _v30 && following == '$' => XPathTokenKind.Let,
            "function" when _v30 && following == '(' => XPathTokenKind.Function,
            _ => XPathTokenKind.Name,
        };
    }

    private readonly char Peek(int index) => (uint)index < (uint)_text.Length ? _text[index] : '\0';

    private static bool IsDigit(char c) => (uint)(c - '0') <= 9;

    private readonly int SkipDigits(int index)
    {
        while (IsDigit(Peek(index)))
        {
            index++;
        }

        return index;
    }

    // NCName is a Name without colons: the first colon, itself a name character, ends it.
    private readonly int NcNameLength(int index)
    {
        if (index >= _text.Length)
        {
            return 0;
        }

        var rest = _text.Slice(index);
        var length = XmlCharClass.NameLength(rest);
        var colon = rest.Slice(0, length).IndexOf(':');
        return colon >= 0 ? colon : length;
    }

    private XPathToken Emit(XPathTokenKind kind, int start, int length, bool afterOperand)
    {
        _position = start + length;
        _afterOperand = afterOperand;
        return Token(kind, start, length);
    }

    private XPathToken Fail(XPathLexicalError error, int start, int end)
    {
        _failed = true;
        _position = end;
        return new XPathToken(XPathTokenKind.Error, new SourceSpan(start, end - start), error);
    }

    private static XPathToken Token(XPathTokenKind kind, int start, int length) =>
        new(kind, new SourceSpan(start, length));
}
