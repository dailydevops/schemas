namespace NetEvolve.Schemas.XPath.Syntax.Parser;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Lexer;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Recursive descent over the lexer tokens for the expression productions of XPath 2.0 (Second Edition) A.1, [2] to
/// [19], plus the XPath 3.0 additions. The binary levels share one precedence-climbing loop that follows the precedence order of A.4; the paths,
/// steps, and primaries are in <c>ExpressionParser.Steps.cs</c>, the types and kind tests in
/// <c>ExpressionParser.Types.cs</c>.
/// </summary>
/// <remarks>
/// The first syntax error ends the parse with XPST0003 and the range of the offending token. The parser holds one
/// token of lookahead and copies the lexer for a second one; the text is never copied except for names and the values
/// stored in the tree.
/// </remarks>
internal ref partial struct ExpressionParser
{
    private const int OrLevel = 1;
    private const int AndLevel = 2;
    private const int ComparisonLevel = 3;
    private const int ConcatLevel = 4;
    private const int RangeLevel = 5;
    private const int AdditiveLevel = 6;
    private const int MultiplicativeLevel = 7;
    private const int UnionLevel = 8;
    private const int IntersectLevel = 9;

    private readonly ReadOnlySpan<char> _text;
    private readonly bool _v30;
    private XPathLexer _lexer;
    private XPathToken _current;
    private int _lastEnd;
    private int _depth;

    /// <summary>Initializes a new instance of the <see cref="ExpressionParser"/> struct.</summary>
    /// <param name="text">The expression; it must outlive the parser.</param>
    /// <param name="version">The language version; the productions of XPath 3.0 apply from <see cref="XPathVersion.V30"/> on.</param>
    public ExpressionParser(ReadOnlySpan<char> text, XPathVersion version = XPathVersion.V20)
    {
        _text = text;
        _v30 = version >= XPathVersion.V30;
        _lexer = new XPathLexer(text, version);
        _current = _lexer.Next();
        _lastEnd = 0;
        _depth = 0;
    }

    /// <summary>Parses the whole input as one expression.</summary>
    /// <returns>The tree.</returns>
    /// <exception cref="XPathException">The input is not a valid expression (XPST0003).</exception>
    public Expression ParseExpression()
    {
        var expression = ParseExpr();
        return _current.Kind == XPathTokenKind.EndOfInput ? expression : throw Unexpected();
    }

    // [2] Expr
    private Expression ParseExpr()
    {
        var start = _current.Span.Offset;
        var first = ParseExprSingle();
        if (_current.Kind != XPathTokenKind.Comma)
        {
            return first;
        }

        List<Expression> items = [first];
        while (_current.Kind == XPathTokenKind.Comma)
        {
            _ = Advance();
            items.Add(ParseExprSingle());
        }

        return new SequenceExpression(Span(start), items);
    }

    // [3] ExprSingle
    private Expression ParseExprSingle()
    {
        Enter();
        var expression = _current.Kind switch
        {
            XPathTokenKind.For => ParseFor(),
            XPathTokenKind.Let => ParseLet(),
            XPathTokenKind.Some or XPathTokenKind.Every => ParseQuantified(),
            XPathTokenKind.If => ParseIf(),
            _ => ParseBinary(OrLevel),
        };
        _depth--;
        return expression;
    }

    // [4] ForExpr
    private ForExpression ParseFor()
    {
        var start = Advance().Span.Offset;
        var bindings = ParseBindings(XPathTokenKind.In);
        Expect(XPathTokenKind.Return);
        var result = ParseExprSingle();
        return new ForExpression(Span(start), bindings, result);
    }

    // XPath 3.0 [LetExpr]: the bindings use ":=" instead of "in".
    private LetExpression ParseLet()
    {
        var start = Advance().Span.Offset;
        var bindings = ParseBindings(XPathTokenKind.ColonEqual);
        Expect(XPathTokenKind.Return);
        var result = ParseExprSingle();
        return new LetExpression(Span(start), bindings, result);
    }

    // [6] QuantifiedExpr
    private QuantifiedExpression ParseQuantified()
    {
        var keyword = Advance();
        var bindings = ParseBindings(XPathTokenKind.In);
        Expect(XPathTokenKind.Satisfies);
        var satisfies = ParseExprSingle();
        return new QuantifiedExpression(
            Span(keyword.Span.Offset),
            keyword.Kind == XPathTokenKind.Some ? Quantifier.Some : Quantifier.Every,
            bindings,
            satisfies
        );
    }

    // [5] SimpleForClause and the binding list of [6]
    private List<VariableBinding> ParseBindings(XPathTokenKind separator)
    {
        List<VariableBinding> bindings = [];
        do
        {
            var start = _current.Span.Offset;
            Expect(XPathTokenKind.Dollar);
            var name = ParseVariableName();
            Expect(separator);
            var source = ParseExprSingle();
            bindings.Add(new VariableBinding(Span(start), name, source));
        } while (Accept(XPathTokenKind.Comma));

        return bindings;
    }

    // [7] IfExpr
    private IfExpression ParseIf()
    {
        var start = Advance().Span.Offset;
        Expect(XPathTokenKind.LeftParen);
        var condition = ParseExpr();
        Expect(XPathTokenKind.RightParen);
        Expect(XPathTokenKind.Then);
        var then = ParseExprSingle();
        Expect(XPathTokenKind.Else);
        var otherwise = ParseExprSingle();
        return new IfExpression(Span(start), condition, then, otherwise);
    }

    // [8] to [15]: or, and, comparison, range, additive, multiplicative, union, intersect and except. The levels are
    // left associative except comparison and range, which allow one operator.
    private Expression ParseBinary(int minimum)
    {
        var start = _current.Span.Offset;
        var left = ParseTypeOperators();
        while (true)
        {
            var level = LevelOf(_current.Kind);
            if (level == 0 || level < minimum)
            {
                return left;
            }

            var token = Advance();
            var right = ParseBinary(level + 1);
            left = CreateBinary(token.Kind, Span(start), left, right);
            if (level is ComparisonLevel or RangeLevel && LevelOf(_current.Kind) == level)
            {
                throw Unexpected();
            }
        }
    }

    // [16] to [19]: instance of, treat as, castable as, cast as; each at most once, in this order.
    private Expression ParseTypeOperators()
    {
        var start = _current.Span.Offset;
        var expression = ParseUnary();
        if (_current.Kind == XPathTokenKind.Cast)
        {
            _ = Advance();
            Expect(XPathTokenKind.As);
            expression = new CastExpression(Span(start), expression, ParseSingleType());
        }

        if (_current.Kind == XPathTokenKind.Castable)
        {
            _ = Advance();
            Expect(XPathTokenKind.As);
            expression = new CastableExpression(Span(start), expression, ParseSingleType());
        }

        if (_current.Kind == XPathTokenKind.Treat)
        {
            _ = Advance();
            Expect(XPathTokenKind.As);
            expression = new TreatExpression(Span(start), expression, ParseSequenceType());
        }

        if (_current.Kind == XPathTokenKind.Instance)
        {
            _ = Advance();
            Expect(XPathTokenKind.Of);
            expression = new InstanceOfExpression(Span(start), expression, ParseSequenceType());
        }

        return expression;
    }

    // [20] UnaryExpr
    private Expression ParseUnary()
    {
        if (_current.Kind is not (XPathTokenKind.Plus or XPathTokenKind.Minus))
        {
            return ParseSimpleMap();
        }

        var sign = Advance();
        Enter();
        var operand = ParseUnary();
        _depth--;
        return new UnaryExpression(
            Span(sign.Span.Offset),
            sign.Kind == XPathTokenKind.Plus ? UnaryOperator.Plus : UnaryOperator.Minus,
            operand
        );
    }

    // XPath 3.0 [SimpleMapExpr]: path expressions joined by "!", left associative; it binds tighter than the unary
    // signs and looser than "/" (the lexer reads "!" only in 3.0 mode).
    private Expression ParseSimpleMap()
    {
        var start = _current.Span.Offset;
        var left = ParsePath();
        while (_current.Kind == XPathTokenKind.Bang)
        {
            _ = Advance();
            left = new SimpleMapExpression(Span(start), left, ParsePath());
        }

        return left;
    }

    private static int LevelOf(XPathTokenKind kind) =>
        kind switch
        {
            XPathTokenKind.Or => OrLevel,
            XPathTokenKind.Concat => ConcatLevel,
            XPathTokenKind.And => AndLevel,
            XPathTokenKind.Equal
            or XPathTokenKind.NotEqual
            or XPathTokenKind.Less
            or XPathTokenKind.LessOrEqual
            or XPathTokenKind.Greater
            or XPathTokenKind.GreaterOrEqual
            or XPathTokenKind.Eq
            or XPathTokenKind.Ne
            or XPathTokenKind.Lt
            or XPathTokenKind.Le
            or XPathTokenKind.Gt
            or XPathTokenKind.Ge
            or XPathTokenKind.Is
            or XPathTokenKind.Precedes
            or XPathTokenKind.Follows => ComparisonLevel,
            XPathTokenKind.To => RangeLevel,
            XPathTokenKind.Plus or XPathTokenKind.Minus => AdditiveLevel,
            XPathTokenKind.Star or XPathTokenKind.Div or XPathTokenKind.IDiv or XPathTokenKind.Mod =>
                MultiplicativeLevel,
            XPathTokenKind.Union or XPathTokenKind.Pipe => UnionLevel,
            XPathTokenKind.Intersect or XPathTokenKind.Except => IntersectLevel,
            _ => 0,
        };

    private static Expression CreateBinary(XPathTokenKind kind, SourceSpan span, Expression left, Expression right) =>
        kind switch
        {
            XPathTokenKind.Or => new LogicalExpression(span, LogicalOperator.Or, left, right),
            XPathTokenKind.And => new LogicalExpression(span, LogicalOperator.And, left, right),
            XPathTokenKind.Concat => new StringConcatExpression(span, left, right),
            XPathTokenKind.To => new RangeExpression(span, left, right),
            XPathTokenKind.Plus => new ArithmeticExpression(span, ArithmeticOperator.Add, left, right),
            XPathTokenKind.Minus => new ArithmeticExpression(span, ArithmeticOperator.Subtract, left, right),
            XPathTokenKind.Star => new ArithmeticExpression(span, ArithmeticOperator.Multiply, left, right),
            XPathTokenKind.Div => new ArithmeticExpression(span, ArithmeticOperator.Divide, left, right),
            XPathTokenKind.IDiv => new ArithmeticExpression(span, ArithmeticOperator.IntegerDivide, left, right),
            XPathTokenKind.Mod => new ArithmeticExpression(span, ArithmeticOperator.Modulo, left, right),
            XPathTokenKind.Union or XPathTokenKind.Pipe => new SetExpression(span, SetOperator.Union, left, right),
            XPathTokenKind.Intersect => new SetExpression(span, SetOperator.Intersect, left, right),
            XPathTokenKind.Except => new SetExpression(span, SetOperator.Except, left, right),
            _ => new ComparisonExpression(span, ComparisonOf(kind), left, right),
        };

    private static ComparisonOperator ComparisonOf(XPathTokenKind kind) =>
        kind switch
        {
            XPathTokenKind.Equal => ComparisonOperator.GeneralEqual,
            XPathTokenKind.NotEqual => ComparisonOperator.GeneralNotEqual,
            XPathTokenKind.Less => ComparisonOperator.GeneralLessThan,
            XPathTokenKind.LessOrEqual => ComparisonOperator.GeneralLessThanOrEqual,
            XPathTokenKind.Greater => ComparisonOperator.GeneralGreaterThan,
            XPathTokenKind.GreaterOrEqual => ComparisonOperator.GeneralGreaterThanOrEqual,
            XPathTokenKind.Eq => ComparisonOperator.ValueEqual,
            XPathTokenKind.Ne => ComparisonOperator.ValueNotEqual,
            XPathTokenKind.Lt => ComparisonOperator.ValueLessThan,
            XPathTokenKind.Le => ComparisonOperator.ValueLessThanOrEqual,
            XPathTokenKind.Gt => ComparisonOperator.ValueGreaterThan,
            XPathTokenKind.Ge => ComparisonOperator.ValueGreaterThanOrEqual,
            XPathTokenKind.Is => ComparisonOperator.NodeIs,
            XPathTokenKind.Precedes => ComparisonOperator.NodePrecedes,
            _ => ComparisonOperator.NodeFollows,
        };

    // Consumes the current token and reads the next one.
    private XPathToken Advance()
    {
        var token = _current;
        _lastEnd = token.Span.End;
        _current = _lexer.Next();
        return token;
    }

    private bool Accept(XPathTokenKind kind)
    {
        if (_current.Kind != kind)
        {
            return false;
        }

        _ = Advance();
        return true;
    }

    private void Expect(XPathTokenKind kind)
    {
        if (!Accept(kind))
        {
            throw Unexpected();
        }
    }

    // The kind of the token after the current one, without consuming anything.
    private readonly XPathTokenKind PeekKind()
    {
        var lookahead = _lexer;
        return lookahead.Next().Kind;
    }

    private readonly SourceSpan Span(int start) => new(start, _lastEnd - start);

    private readonly ReadOnlySpan<char> TextOf(XPathToken token) => _text.Slice(token.Span.Offset, token.Span.Length);

    // Each nesting construct passes through here; the bound turns hostile input into a diagnostic.
    private void Enter()
    {
        if (++_depth > XPathParser.MaxNestingDepth)
        {
            throw Unexpected();
        }
    }

    private readonly XPathException Unexpected() => Fail(_current);

    private static XPathException Fail(XPathToken token) =>
        XPathErrors.Exception(XPathErrorCodes.XPST0003, token.Span.Offset, token.Span.Length);
}
