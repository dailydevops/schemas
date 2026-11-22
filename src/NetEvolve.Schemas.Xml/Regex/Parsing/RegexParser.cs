namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;
using NetEvolve.Schemas.Xml.Text.Unicode;

/// <summary>
/// Parses a pattern of the XSD or the XPath regular expression dialect into a <see cref="RegexTree"/>. The parser never throws
/// for a pattern: a defect is a <see cref="RegexError"/> with the position in the pattern.
/// </summary>
/// <remarks>
/// Limitations: in the XSD dialect the anchors <c>^</c> and <c>$</c> outside a character class, back references (<c>\1</c> to
/// <c>\9</c>), and reluctant quantifiers are rejected with <see cref="RegexError.ConstructNotAllowed"/> as the approved design
/// stipulates; XSD Part 2 itself would read a bare <c>^</c> or <c>$</c> as a literal character. Groups do not capture in the XSD
/// dialect (<see cref="RegexNode.NoGroup"/>). The XPath dialect adds the anchors, reluctant quantifiers, back references, the
/// escape <c>\$</c>, and non-capturing groups <c>(?:</c>; its capturing groups are numbered by their opening parenthesis, and
/// any other <c>(?</c> construct, such as a lookahead, is rejected as an invalid quantifier. The flag <c>x</c> removes the
/// whitespace of the pattern before it is read, the flag <c>i</c> turns each character and each character or range of a class
/// into the set of its case variants by the simple case folding of the embedded tables before a class is negated or has a
/// class subtracted, and category and block escapes stay unaffected; the flags <c>s</c> and <c>m</c> belong to the matcher.
/// The flag <c>q</c> is not supported (approved design AP-13 A). A hyphen in a character class is a literal only at its start or
/// end, and a counted repeat is limited to <see cref="int.MaxValue"/>.
/// </remarks>
public static class RegexParser
{
    /// <summary>Parses a pattern with the default limits.</summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="syntax">The dialect, the name edition, and the flags.</param>
    /// <returns>The tree or the error.</returns>
    public static RegexParseResult Parse(ReadOnlySpan<char> pattern, RegexSyntax syntax) =>
        Parse(pattern, syntax, RegexLimits.Default);

    /// <summary>Parses a pattern.</summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="syntax">The dialect, the name edition, and the flags.</param>
    /// <param name="limits">The parser limits; must not be <see langword="null"/>.</param>
    /// <returns>The tree or the error.</returns>
    public static RegexParseResult Parse(ReadOnlySpan<char> pattern, RegexSyntax syntax, RegexLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        if (syntax.Dialect != RegexDialect.XPath && syntax.Options != RegexOptions.None)
        {
            return new(RegexError.InvalidFlag, 0);
        }

        if (pattern.Length > limits.MaxPatternLength)
        {
            return new(RegexError.LimitExceeded, limits.MaxPatternLength);
        }

        if ((syntax.Options & RegexOptions.FreeSpacing) == RegexOptions.None)
        {
            return new Parser(pattern, syntax, limits.MaxNesting, null, pattern.Length, pattern.Length).Run();
        }

        var buffer = new char[pattern.Length];
        var map = new int[pattern.Length];
        var count = RemoveWhitespace(pattern, buffer, map);
        return new Parser(buffer.AsSpan(0, count), syntax, limits.MaxNesting, map, count, pattern.Length).Run();
    }

    // The flag x: whitespace outside character classes is removed before the pattern is read; the map keeps the index of each
    // remaining character in the original pattern for the error positions. A backslash does not protect a whitespace character
    // after it, but it keeps the character after it from opening or closing a class.
    private static int RemoveWhitespace(ReadOnlySpan<char> pattern, char[] buffer, int[] map)
    {
        var count = 0;
        var depth = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (depth == 0 && WhitespaceNormalizer.IsXmlSpace(c))
            {
                continue;
            }

            buffer[count] = c;
            map[count++] = i;
            if (c == '\\' && i + 1 < pattern.Length && (depth > 0 || !WhitespaceNormalizer.IsXmlSpace(pattern[i + 1])))
            {
                i++;
                buffer[count] = pattern[i];
                map[count++] = i;
            }
            else if (c == '[')
            {
                depth++;
            }
            else if (c == ']' && depth > 0)
            {
                depth--;
            }
        }

        return count;
    }

    private enum EscapeKind
    {
        Failed,
        Single,
        Set,
        BackReference,
    }

    private ref struct Parser
    {
        // Same order as UnicodeGeneralCategory.
        private static readonly string[] CategoryNames =
        [
            "Lu",
            "Ll",
            "Lt",
            "Lm",
            "Lo",
            "Mn",
            "Mc",
            "Me",
            "Nd",
            "Nl",
            "No",
            "Pc",
            "Pd",
            "Ps",
            "Pe",
            "Pi",
            "Pf",
            "Po",
            "Sm",
            "Sc",
            "Sk",
            "So",
            "Zs",
            "Zl",
            "Zp",
            "Cc",
            "Cf",
            "Cs",
            "Co",
            "Cn",
        ];

        private static readonly int[] SpaceRanges = [9, 10, 13, 13, 32, 32];
        private static readonly int[]?[] NameCache = new int[4][];

        private readonly ReadOnlySpan<char> _p;
        private readonly NameEdition _names;
        private readonly RegexDialect _dialect;
        private readonly RegexOptions _options;
        private readonly int[]? _map;
        private readonly int _mappedLength;
        private readonly int _originalLength;
        private readonly int _maxNesting;
        private RegexNode[] _nodes;
        private int _count;
        private int[] _stack;
        private int _sp;
        private readonly List<RegexCharacterSet> _sets;
        private bool[] _closed;
        private int _groupCount;
        private RegexSetMath.Builder? _literals;
        private int _pos;
        private RegexError _error;
        private int _errorPos;

        internal Parser(
            ReadOnlySpan<char> pattern,
            RegexSyntax syntax,
            int maxNesting,
            int[]? map,
            int mappedLength,
            int originalLength
        )
        {
            _p = pattern;
            _names = syntax.Names;
            _dialect = syntax.Dialect;
            _options = syntax.Options;
            _map = map;
            _mappedLength = mappedLength;
            _originalLength = originalLength;
            _maxNesting = maxNesting;
            _nodes = new RegexNode[Math.Max(8, pattern.Length + 2)];
            _count = 0;
            _stack = new int[8];
            _sp = 0;
            _sets = [];
            _closed = new bool[4];
            _groupCount = 0;
            _literals = null;
            _pos = 0;
            _error = RegexError.None;
            _errorPos = 0;
        }

        internal RegexParseResult Run()
        {
            var root = ParseRegExp(0);
            if (root == RegexNode.NoNode)
            {
                return new(_error, Original(_errorPos));
            }

            // Only an unmatched ')' stops the top level before the end.
            return _pos < _p.Length
                ? new(RegexError.Unbalanced, Original(_pos))
                : new(new RegexTree(_nodes, _count, root, _sets.ToArray(), _dialect, _options));
        }

        private readonly bool IsXPath => _dialect == RegexDialect.XPath;

        // The position in the pattern as the caller wrote it, before the whitespace of the flag x was removed.
        private readonly int Original(int position) =>
            _map is null ? position : (position < _mappedLength ? _map[position] : _originalLength);

        private void SetError(RegexError error, int position)
        {
            _error = error;
            _errorPos = position;
        }

        private void SetEnd() => SetError(RegexError.UnexpectedEnd, _p.Length);

        private int Fail(RegexError error, int position)
        {
            SetError(error, position);
            return RegexNode.NoNode;
        }

        private int FailEnd() => Fail(RegexError.UnexpectedEnd, _p.Length);

        private int Add(RegexNode node)
        {
            if (_count == _nodes.Length)
            {
                Array.Resize(ref _nodes, _count * 2);
            }

            _nodes[_count] = node;
            return _count++;
        }

        private void Push(int index)
        {
            if (_sp == _stack.Length)
            {
                Array.Resize(ref _stack, _sp * 2);
            }

            _stack[_sp++] = index;
        }

        // Chains the nodes pushed since start as siblings, pops them, and returns the first.
        private int LinkChildren(int start)
        {
            for (var i = start; i < _sp - 1; i++)
            {
                var index = _stack[i];
                _nodes[index] = _nodes[index].WithNextSibling(_stack[i + 1]);
            }

            var first = _stack[start];
            _sp = start;
            return first;
        }

        private int ParseRegExp(int level)
        {
            var start = _sp;
            while (true)
            {
                var branch = ParseBranch(level);
                if (branch == RegexNode.NoNode)
                {
                    return branch;
                }

                Push(branch);
                if (_pos < _p.Length && _p[_pos] == '|')
                {
                    _pos++;
                    continue;
                }

                break;
            }

            return _sp - start == 1 ? _stack[--_sp] : Add(RegexNode.Alternation(LinkChildren(start)));
        }

        private int ParseBranch(int level)
        {
            var start = _sp;
            while (_pos < _p.Length && _p[_pos] != '|' && _p[_pos] != ')')
            {
                var piece = ParsePiece(level);
                if (piece == RegexNode.NoNode)
                {
                    return piece;
                }

                Push(piece);
            }

            return (_sp - start) switch
            {
                0 => Add(RegexNode.Empty()),
                1 => _stack[--_sp],
                _ => Add(RegexNode.Concat(LinkChildren(start))),
            };
        }

        private int ParsePiece(int level)
        {
            var atom = ParseAtom(level);
            if (atom == RegexNode.NoNode || _pos >= _p.Length)
            {
                return atom;
            }

            int min;
            int max;
            switch (_p[_pos])
            {
                case '?':
                    (min, max) = (0, 1);
                    _pos++;
                    break;
                case '*':
                    (min, max) = (0, RegexNode.Unbounded);
                    _pos++;
                    break;
                case '+':
                    (min, max) = (1, RegexNode.Unbounded);
                    _pos++;
                    break;
                case '{':
                    if (!ParseCount(out min, out max))
                    {
                        return RegexNode.NoNode;
                    }

                    break;
                default:
                    return atom;
            }

            if (_pos >= _p.Length || _p[_pos] != '?')
            {
                return Add(RegexNode.Repeat(atom, min, max, false));
            }

            // A reluctant quantifier is an XPath construct.
            if (!IsXPath)
            {
                return Fail(RegexError.ConstructNotAllowed, _pos);
            }

            _pos++;
            return Add(RegexNode.Repeat(atom, min, max, true));
        }

        private bool ParseCount(out int min, out int max)
        {
            var start = _pos++;
            max = 0;
            if (!ReadInt(out min, start))
            {
                return false;
            }

            if (_pos >= _p.Length)
            {
                SetEnd();
                return false;
            }

            if (_p[_pos] == '}')
            {
                max = min;
            }
            else if (_p[_pos] == ',')
            {
                _pos++;
                if (_pos < _p.Length && _p[_pos] == '}')
                {
                    max = RegexNode.Unbounded;
                }
                else if (!ReadInt(out max, start))
                {
                    return false;
                }

                if (_pos >= _p.Length)
                {
                    SetEnd();
                    return false;
                }

                if (_p[_pos] != '}')
                {
                    SetError(RegexError.InvalidQuantifier, start);
                    return false;
                }
            }
            else
            {
                SetError(RegexError.InvalidQuantifier, start);
                return false;
            }

            _pos++;
            if (max != RegexNode.Unbounded && max < min)
            {
                SetError(RegexError.InvalidQuantifier, start);
                return false;
            }

            return true;
        }

        private bool ReadInt(out int value, int quantifierStart)
        {
            long result = 0;
            var digits = 0;
            while (_pos < _p.Length && _p[_pos] is >= '0' and <= '9')
            {
                result = (result * 10) + (_p[_pos] - '0');
                if (result > int.MaxValue)
                {
                    value = 0;
                    SetError(RegexError.InvalidQuantifier, quantifierStart);
                    return false;
                }

                digits++;
                _pos++;
            }

            value = (int)result;
            if (digits == 0)
            {
                if (_pos >= _p.Length)
                {
                    SetEnd();
                }
                else
                {
                    SetError(RegexError.InvalidQuantifier, quantifierStart);
                }

                return false;
            }

            return true;
        }

        private int ParseAtom(int level)
        {
            var c = _p[_pos];
            switch (c)
            {
                case '(':
                    return ParseGroup(level);
                case '[':
                    var set = ParseClass(level + 1);
                    return set is null ? RegexNode.NoNode : AddSet(set);
                case ']':
                    return Fail(RegexError.Unbalanced, _pos);
                case '{' or '}' or '?' or '*' or '+':
                    return Fail(RegexError.InvalidQuantifier, _pos);
                case '^' or '$':
                    return IsXPath
                        ? Add(RegexNode.Anchor(_p[_pos++] == '^' ? RegexAnchor.Start : RegexAnchor.End))
                        : Fail(RegexError.ConstructNotAllowed, _pos);
                case '.':
                    _pos++;
                    return Add(RegexNode.AnyChar());
                case '\\':
                    var kind = ParseEscape(false, out var codePoint, out var escapeSet);
                    return kind switch
                    {
                        EscapeKind.Single => AddCharacter(codePoint),
                        EscapeKind.Set => AddSet(escapeSet!),
                        EscapeKind.BackReference => Add(RegexNode.BackReference(codePoint)),
                        _ => RegexNode.NoNode,
                    };
                default:
                    return ReadCodePoint(out codePoint) ? AddCharacter(codePoint) : RegexNode.NoNode;
            }
        }

        // With the flag i a character stands for the set of its case variants.
        private int AddCharacter(int codePoint)
        {
            if (
                (_options & RegexOptions.IgnoreCase) == RegexOptions.None
                || !RegexCaseVariants.TryGetVariants(codePoint, out var variants)
            )
            {
                return Add(RegexNode.Character(codePoint));
            }

            var builder = new RegexSetMath.Builder();
            for (var i = 0; i < variants.Length; i++)
            {
                builder.Add(variants[i], variants[i]);
            }

            return AddSet(builder.Build());
        }

        private int AddSet(int[] ranges)
        {
            _sets.Add(new RegexCharacterSet(ranges));
            return Add(RegexNode.Set(_sets.Count - 1));
        }

        private int ParseGroup(int level)
        {
            if (level + 1 > _maxNesting)
            {
                return Fail(RegexError.LimitExceeded, _pos);
            }

            _pos++;
            var number = RegexNode.NoGroup;
            if (IsXPath)
            {
                if (_pos + 1 < _p.Length && _p[_pos] == '?' && _p[_pos + 1] == ':')
                {
                    _pos += 2;
                }
                else
                {
                    number = ++_groupCount;
                    if (number >= _closed.Length)
                    {
                        Array.Resize(ref _closed, _closed.Length * 2);
                    }
                }
            }

            var inner = ParseRegExp(level + 1);
            if (inner == RegexNode.NoNode)
            {
                return inner;
            }

            if (_pos >= _p.Length)
            {
                return FailEnd();
            }

            _pos++;
            if (number != RegexNode.NoGroup)
            {
                _closed[number] = true;
            }

            return Add(RegexNode.Group(inner, number));
        }

        private bool ReadCodePoint(out int codePoint)
        {
            var c = _p[_pos];
            if (char.IsHighSurrogate(c) && _pos + 1 < _p.Length && char.IsLowSurrogate(_p[_pos + 1]))
            {
                codePoint = char.ConvertToUtf32(c, _p[_pos + 1]);
                _pos += 2;
                return true;
            }

            if (char.IsSurrogate(c))
            {
                codePoint = 0;
                SetError(RegexError.InvalidUtf16, _pos);
                return false;
            }

            codePoint = c;
            _pos++;
            return true;
        }

        // Character class expression; _pos is at the '['.
        private int[]? ParseClass(int level)
        {
            if (level > _maxNesting)
            {
                SetError(RegexError.LimitExceeded, _pos);
                return null;
            }

            _pos++;
            var negate = _pos < _p.Length && _p[_pos] == '^';
            if (negate)
            {
                _pos++;
            }

            var builder = new RegexSetMath.Builder();
            var outer = _literals;
            _literals = (_options & RegexOptions.IgnoreCase) == RegexOptions.None ? null : new RegexSetMath.Builder();
            if (!ParseClassBody(level, builder, out var subtrahend))
            {
                return null;
            }

            if (_literals is not null)
            {
                builder.AddAll(RegexCaseVariants.Close(_literals.Build()));
            }

            _literals = outer;
            var ranges = builder.Build();
            if (negate)
            {
                ranges = RegexSetMath.Negate(ranges);
            }

            return subtrahend is null ? ranges : RegexSetMath.Subtract(ranges, subtrahend);
        }

        // Members up to the closing ']'; a '-[' ends the members and starts the subtrahend.
        private bool ParseClassBody(int level, RegexSetMath.Builder builder, out int[]? subtrahend)
        {
            subtrahend = null;
            while (true)
            {
                if (_pos >= _p.Length)
                {
                    SetEnd();
                    return false;
                }

                var c = _p[_pos];
                if (c == ']')
                {
                    if (builder.IsEmpty)
                    {
                        SetError(RegexError.InvalidCharacterClass, _pos);
                        return false;
                    }

                    _pos++;
                    return true;
                }

                if (c == '-')
                {
                    var next = _pos + 1 < _p.Length ? _p[_pos + 1] : '\0';
                    if (next == '[' && !builder.IsEmpty)
                    {
                        return ParseSubtraction(level, out subtrahend);
                    }

                    if (!builder.IsEmpty && next != ']')
                    {
                        SetError(RegexError.InvalidCharacterClass, _pos);
                        return false;
                    }

                    // A hyphen is a literal only first or last in the class.
                    builder.Add('-', '-');
                    _pos++;
                }
                else if (c == '[' || !ParseClassMember(builder))
                {
                    if (_error == RegexError.None)
                    {
                        SetError(RegexError.InvalidCharacterClass, _pos);
                    }

                    return false;
                }
            }
        }

        // _pos is at the '-' of '-['; the subtrahend must be the last thing in the class.
        private bool ParseSubtraction(int level, out int[]? subtrahend)
        {
            _pos++;
            subtrahend = ParseClass(level + 1);
            if (subtrahend is null)
            {
                return false;
            }

            if (_pos >= _p.Length)
            {
                SetEnd();
                return false;
            }

            if (_p[_pos] != ']')
            {
                SetError(RegexError.InvalidCharacterClass, _pos);
                return false;
            }

            _pos++;
            return true;
        }

        private bool ParseClassMember(RegexSetMath.Builder builder)
        {
            var itemStart = _pos;
            if (!ReadClassItem(out var first, out var set))
            {
                return false;
            }

            if (set is not null)
            {
                builder.AddAll(set);
                return true;
            }

            if (_pos + 1 < _p.Length && _p[_pos] == '-' && _p[_pos + 1] is not (']' or '['))
            {
                return ParseRange(builder, first, itemStart);
            }

            AddLiteral(builder, first, first);
            return true;
        }

        // Single code points and ranges are the members the flag i gives their case variants; escapes are not.
        private readonly void AddLiteral(RegexSetMath.Builder builder, int first, int last)
        {
            builder.Add(first, last);
            _literals?.Add(first, last);
        }

        // _pos is at the '-' of a range.
        private bool ParseRange(RegexSetMath.Builder builder, int first, int itemStart)
        {
            _pos++;
            var endStart = _pos;
            if (_p[_pos] == '-')
            {
                SetError(RegexError.InvalidCharacterClass, endStart);
                return false;
            }

            if (!ReadClassItem(out var last, out var endSet))
            {
                return false;
            }

            if (endSet is not null || last < first)
            {
                SetError(RegexError.InvalidCharacterClass, endSet is not null ? endStart : itemStart);
                return false;
            }

            AddLiteral(builder, first, last);
            return true;
        }

        // One class member: a single code point (set stays null) or a multi-character escape (set is set).
        private bool ReadClassItem(out int codePoint, out int[]? set)
        {
            set = null;
            if (_p[_pos] != '\\')
            {
                return ReadCodePoint(out codePoint);
            }

            var kind = ParseEscape(true, out codePoint, out set);
            return kind != EscapeKind.Failed;
        }

        // _pos is at the backslash.
        private EscapeKind ParseEscape(bool inClass, out int codePoint, out int[]? set)
        {
            var start = _pos++;
            codePoint = 0;
            set = null;
            if (_pos >= _p.Length)
            {
                SetEnd();
                return EscapeKind.Failed;
            }

            var c = _p[_pos++];
            switch (c)
            {
                case 'n':
                    codePoint = '\n';
                    return EscapeKind.Single;
                case 'r':
                    codePoint = '\r';
                    return EscapeKind.Single;
                case 't':
                    codePoint = '\t';
                    return EscapeKind.Single;
                case '\\' or '|' or '.' or '-' or '^' or '?' or '*' or '+' or '{' or '}' or '(' or ')' or '[' or ']':
                    codePoint = c;
                    return EscapeKind.Single;
                case 's' or 'S' or 'i' or 'I' or 'c' or 'C' or 'd' or 'D' or 'w' or 'W':
                    set = MultiCharSet(c);
                    return EscapeKind.Set;
                case 'p' or 'P':
                    return ParseProperty(c == 'P', start, out set);
                case '$' when IsXPath:
                    codePoint = c;
                    return EscapeKind.Single;
                case >= '1' and <= '9' when !inClass && IsXPath:
                    return ParseBackReference(c - '0', start, out codePoint);
                case >= '1' and <= '9' when !inClass:
                    SetError(RegexError.ConstructNotAllowed, start);
                    return EscapeKind.Failed;
                default:
                    SetError(RegexError.InvalidEscape, start);
                    return EscapeKind.Failed;
            }
        }

        // _pos is after the first digit of a back reference. Further digits belong to it only while the number does not exceed
        // the capturing groups opened so far; the group must be closed before the reference.
        private EscapeKind ParseBackReference(int number, int start, out int groupNumber)
        {
            while (_pos < _p.Length && _p[_pos] is >= '0' and <= '9' && (number * 10) + (_p[_pos] - '0') <= _groupCount)
            {
                number = (number * 10) + (_p[_pos++] - '0');
            }

            groupNumber = number;
            if (number > _groupCount || !_closed[number])
            {
                SetError(RegexError.InvalidBackReference, start);
                return EscapeKind.Failed;
            }

            return EscapeKind.BackReference;
        }

        // _pos is after \p or \P.
        private EscapeKind ParseProperty(bool complement, int start, out int[]? set)
        {
            set = null;
            if (_pos >= _p.Length)
            {
                SetEnd();
                return EscapeKind.Failed;
            }

            if (_p[_pos] != '{')
            {
                SetError(RegexError.InvalidEscape, start);
                return EscapeKind.Failed;
            }

            var close = _p.Slice(_pos).IndexOf('}');
            if (close < 0)
            {
                SetEnd();
                return EscapeKind.Failed;
            }

            var name = _p.Slice(_pos + 1, close - 1);
            _pos += close + 1;
            set =
                name.Length > 2 && name[0] == 'I' && name[1] == 's' ? BlockRanges(name.Slice(2)) : CategoryRanges(name);
            if (set is null)
            {
                SetError(RegexError.UnknownCategoryOrBlock, start);
                return EscapeKind.Failed;
            }

            if (complement)
            {
                set = RegexSetMath.Negate(set);
            }

            return EscapeKind.Set;
        }

        private static int[]? BlockRanges(ReadOnlySpan<char> name)
        {
            var handle = UnicodeTables.FindBlock(name);
            if (handle == UnicodeTables.NoBlock)
            {
                return null;
            }

            var builder = new RegexSetMath.Builder();
            for (var row = handle; UnicodeTables.IsBlockRow(row); )
            {
                row = UnicodeTables.GetBlockRange(row, out var first, out var last);
                builder.Add(first, last);
            }

            return builder.Build();
        }

        private static int[]? CategoryRanges(ReadOnlySpan<char> name)
        {
            var mask = CategoryMask(name);
            return mask == 0 ? null : MaskRanges(mask);
        }

        // XSD has no surrogate category, so Cs is never selected, not even through the group C.
        private static int CategoryMask(ReadOnlySpan<char> name)
        {
            const int surrogate = (int)UnicodeGeneralCategory.Cs;
            var mask = 0;
            for (var i = 0; i < CategoryNames.Length; i++)
            {
                var candidate = CategoryNames[i];
                if (
                    i != surrogate
                    && name.Length > 0
                    && name.Length <= 2
                    && name[0] == candidate[0]
                    && (name.Length == 1 || name[1] == candidate[1])
                )
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static int[] MaskRanges(int mask)
        {
            var builder = new RegexSetMath.Builder();
            for (var i = 0; i < UnicodeTables.CategoryRunCount; i++)
            {
                var category = UnicodeTables.GetCategoryRun(i, out var first, out var last);
                if (((mask >> (int)category) & 1) != 0)
                {
                    builder.Add(first, last);
                }
            }

            return builder.Build();
        }

        private int[] MultiCharSet(char escape)
        {
            var set = char.ToLowerInvariant(escape) switch
            {
                's' => SpaceRanges,
                'i' => NameRanges(false),
                'c' => NameRanges(true),
                'd' => MaskRanges(CategoryMask("Nd")),
                _ => RegexSetMath.Negate(MaskRanges(CategoryMask("P") | CategoryMask("Z") | CategoryMask("C"))),
            };

            return char.IsUpper(escape) ? RegexSetMath.Negate(set) : set;
        }

        // The name start or name character set of the edition, scanned once from the shared classes.
        private int[] NameRanges(bool nameChar)
        {
            var slot = (_names == NameEdition.Xml10FifthEdition ? 2 : 0) + (nameChar ? 1 : 0);
            var cached = Volatile.Read(ref NameCache[slot]);
            if (cached is not null)
            {
                return cached;
            }

            var builder = new RegexSetMath.Builder();
            var runStart = -1;
            for (var cp = 0; cp <= RegexSetMath.MaxCodePoint + 1; cp++)
            {
                var inClass =
                    cp <= RegexSetMath.MaxCodePoint
                    && (nameChar ? XmlNameClass.IsNameChar(_names, cp) : XmlNameClass.IsNameStartChar(_names, cp));
                if (inClass && runStart < 0)
                {
                    runStart = cp;
                }
                else if (!inClass && runStart >= 0)
                {
                    builder.Add(runStart, cp - 1);
                    runStart = -1;
                }
            }

            var ranges = builder.Build();
            return Interlocked.CompareExchange(ref NameCache[slot], ranges, null) ?? ranges;
        }
    }
}
