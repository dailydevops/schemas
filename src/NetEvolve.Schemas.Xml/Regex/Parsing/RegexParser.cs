namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
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
/// Limitations: in the XSD dialect the anchors <c>^</c> and <c>$</c> outside a character class, back references
/// (<c>\1</c> to <c>\9</c>), reluctant quantifiers, and flags are rejected with <see cref="RegexError.ConstructNotAllowed"/> as
/// the approved design stipulates; XSD Part 2 itself would read a bare <c>^</c> or <c>$</c> as a literal character. Groups do
/// not capture in the XSD dialect (<see cref="RegexNode.NoGroup"/>). A hyphen in a character class is a literal only at its
/// start or end, and a counted repeat is limited to <see cref="int.MaxValue"/>. The XPath dialect adds the anchors, reluctant
/// quantifiers, back references to a closed group, non-capturing groups <c>(?:</c>, and the flags <c>s m i x q</c>; the other
/// <c>(?</c> constructs are <see cref="RegexError.ConstructNotAllowed"/>. With the <c>x</c> flag, white space inside a counted
/// quantifier <c>{n,m}</c> is not removed. With the <c>i</c> flag a class is closed under the simple case folding before its
/// negation, but a negated escape such as <c>\P{Lu}</c> is closed after its negation, so it also matches an uppercase letter.
/// </remarks>
public static class RegexParser
{
    /// <summary>Parses a pattern with the default limits.</summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="syntax">The dialect and the name edition.</param>
    /// <returns>The tree or the error.</returns>
    public static RegexParseResult Parse(ReadOnlySpan<char> pattern, RegexSyntax syntax) =>
        Parse(pattern, syntax, RegexLimits.Default);

    /// <summary>Parses a pattern.</summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="syntax">The dialect and the name edition.</param>
    /// <param name="limits">The parser limits; must not be <see langword="null"/>.</param>
    /// <returns>The tree or the error.</returns>
    public static RegexParseResult Parse(ReadOnlySpan<char> pattern, RegexSyntax syntax, RegexLimits limits)
    {
        Argument.ThrowIfNull(limits);

        if (
            syntax.Dialect is not (RegexDialect.Xsd or RegexDialect.XPath)
            || (syntax.Dialect == RegexDialect.Xsd && syntax.Flags != RegexFlagSet.None)
        )
        {
            return new(RegexError.ConstructNotAllowed, 0);
        }

        if (pattern.Length > limits.MaxPatternLength)
        {
            return new(RegexError.LimitExceeded, limits.MaxPatternLength);
        }

        return new Parser(pattern, syntax, limits.MaxNesting).Run();
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
        private readonly int _maxNesting;
        private RegexNode[] _nodes;
        private int _count;
        private int[] _stack;
        private int _sp;
        private readonly List<RegexCharacterSet> _sets;
        private int _pos;
        private RegexError _error;
        private int _errorPos;
        private readonly bool _xpath;
        private readonly RegexFlagSet _flags;
        private readonly List<bool> _closedGroups;

        internal Parser(ReadOnlySpan<char> pattern, RegexSyntax syntax, int maxNesting)
        {
            _p = pattern;
            _names = syntax.Names;
            _xpath = syntax.Dialect == RegexDialect.XPath;
            _flags = syntax.Flags;
            _closedGroups = [];
            _maxNesting = maxNesting;
            _nodes = new RegexNode[Math.Max(8, pattern.Length + 2)];
            _count = 0;
            _stack = new int[8];
            _sp = 0;
            _sets = [];
            _pos = 0;
            _error = RegexError.None;
            _errorPos = 0;
        }

        private bool Fold => (_flags & RegexFlagSet.IgnoreCase) != RegexFlagSet.None;

        internal RegexParseResult Run()
        {
            var root = (_flags & RegexFlagSet.Literal) != RegexFlagSet.None ? ParseLiteral() : ParseRegExp(0);
            if (root == RegexNode.NoNode)
            {
                return new(_error, _errorPos);
            }

            // Only an unmatched ')' stops the top level before the end.
            return _pos < _p.Length
                ? new(RegexError.Unbalanced, _pos)
                : new(new RegexTree(_nodes, _count, root, _sets.ToArray()));
        }

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

        // The q flag: every code point of the pattern is a character.
        private int ParseLiteral()
        {
            var start = _sp;
            while (_pos < _p.Length)
            {
                if (!ReadCodePoint(out var codePoint))
                {
                    return RegexNode.NoNode;
                }

                Push(AddChar(codePoint));
            }

            return (_sp - start) switch
            {
                0 => Add(RegexNode.Empty()),
                1 => _stack[--_sp],
                _ => Add(RegexNode.Concat(LinkChildren(start))),
            };
        }

        // The x flag removes white space between the tokens of the pattern.
        private void SkipSpace()
        {
            while (
                (_flags & RegexFlagSet.Extended) != RegexFlagSet.None
                && _pos < _p.Length
                && _p[_pos] is ' ' or '\t' or '\n' or '\r'
            )
            {
                _pos++;
            }
        }

        private int AddChar(int codePoint) =>
            Fold && RegexCaseFolding.TryGetClass(codePoint, out var equivalents)
                ? AddSet(equivalents)
                : Add(RegexNode.Character(codePoint));

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
            SkipSpace();
            while (_pos < _p.Length && _p[_pos] != '|' && _p[_pos] != ')')
            {
                var piece = ParsePiece(level);
                if (piece == RegexNode.NoNode)
                {
                    return piece;
                }

                Push(piece);
                SkipSpace();
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
            if (atom == RegexNode.NoNode)
            {
                return atom;
            }

            SkipSpace();
            if (_pos >= _p.Length)
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

            // A reluctant quantifier is an XPath construct.
            SkipSpace();
            if (_pos < _p.Length && _p[_pos] == '?')
            {
                if (!_xpath)
                {
                    return Fail(RegexError.ConstructNotAllowed, _pos);
                }

                _pos++;
                return Add(RegexNode.Repeat(atom, min, max, true));
            }

            return Add(RegexNode.Repeat(atom, min, max, false));
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
                    if (!_xpath)
                    {
                        return Fail(RegexError.ConstructNotAllowed, _pos);
                    }

                    _pos++;
                    return Add(RegexNode.Anchor(c == '^' ? RegexAnchor.Start : RegexAnchor.End));
                case '.':
                    _pos++;
                    return (_flags & RegexFlagSet.DotAll) != RegexFlagSet.None
                        ? AddSet([0, RegexSetMath.MaxCodePoint])
                        : Add(RegexNode.AnyChar());
                case '\\':
                    var kind = ParseEscape(false, out var codePoint, out var escapeSet);
                    return kind switch
                    {
                        EscapeKind.Single => AddChar(codePoint),
                        EscapeKind.Set => AddSet(Fold ? RegexCaseFolding.Close(escapeSet!) : escapeSet!),
                        EscapeKind.BackReference => Add(RegexNode.BackReference(codePoint)),
                        _ => RegexNode.NoNode,
                    };
                default:
                    return ReadCodePoint(out codePoint) ? AddChar(codePoint) : RegexNode.NoNode;
            }
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

            var groupNumber = RegexNode.NoGroup;
            _pos++;
            if (_xpath && _pos < _p.Length && _p[_pos] == '?')
            {
                // Only the non-capturing group of the (?: form exists.
                if (_pos + 1 >= _p.Length || _p[_pos + 1] != ':')
                {
                    return Fail(RegexError.ConstructNotAllowed, _pos);
                }

                _pos += 2;
            }
            else if (_xpath)
            {
                _closedGroups.Add(false);
                groupNumber = _closedGroups.Count;
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
            if (groupNumber != RegexNode.NoGroup)
            {
                _closedGroups[groupNumber - 1] = true;
            }

            return Add(RegexNode.Group(inner, groupNumber));
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
            if (!ParseClassBody(level, builder, out var subtrahend))
            {
                return null;
            }

            var ranges = builder.Build();
            if (Fold)
            {
                ranges = RegexCaseFolding.Close(ranges);
            }

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

            builder.Add(first, first);
            return true;
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

            builder.Add(first, last);
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
                case '$' when _xpath:
                    codePoint = c;
                    return EscapeKind.Single;
                case >= '1' and <= '9' when !inClass && _xpath:
                    return ParseBackReference(c, start, out codePoint);
                case >= '1' and <= '9' when !inClass:
                    SetError(RegexError.ConstructNotAllowed, start);
                    return EscapeKind.Failed;
                default:
                    SetError(RegexError.InvalidEscape, start);
                    return EscapeKind.Failed;
            }
        }

        // _pos is after the first digit. More digits belong to the reference while they name a closed group.
        private EscapeKind ParseBackReference(char first, int start, out int groupNumber)
        {
            groupNumber = first - '0';
            if (!IsClosed(groupNumber))
            {
                SetError(RegexError.InvalidBackReference, start);
                return EscapeKind.Failed;
            }

            while (_pos < _p.Length && _p[_pos] is >= '0' and <= '9' && IsClosed((groupNumber * 10) + (_p[_pos] - '0')))
            {
                groupNumber = (groupNumber * 10) + (_p[_pos++] - '0');
            }

            return EscapeKind.BackReference;
        }

        private readonly bool IsClosed(int groupNumber) =>
            groupNumber <= _closedGroups.Count && _closedGroups[groupNumber - 1];

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
