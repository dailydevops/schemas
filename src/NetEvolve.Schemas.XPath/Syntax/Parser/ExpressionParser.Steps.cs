namespace NetEvolve.Schemas.XPath.Syntax.Parser;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Lexer;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Paths, steps, predicates, and primary expressions: productions [21] to [41] and [42] to [48].</summary>
internal ref partial struct ExpressionParser
{
    // [21] PathExpr: a leading "/" followed by a token that cannot start a step is the root alone (the leading lone
    // slash constraint); the lexer already reads "*" and the operator keywords after "/" as a name test.
    private Expression ParsePath()
    {
        var start = _current.Span.Offset;
        switch (_current.Kind)
        {
            case XPathTokenKind.Slash:
                _ = Advance();
                List<Expression> rooted = [];
                if (StartsStep(_current.Kind))
                {
                    ParseRelativeSteps(rooted);
                }

                return new PathExpression(Span(start), PathRoot.Root, rooted);
            case XPathTokenKind.SlashSlash:
                _ = Advance();
                List<Expression> descendants = [];
                ParseRelativeSteps(descendants);
                return new PathExpression(Span(start), PathRoot.RootDescendants, descendants);
            default:
                var first = ParseStep();
                if (_current.Kind is not (XPathTokenKind.Slash or XPathTokenKind.SlashSlash))
                {
                    return first;
                }

                List<Expression> steps = [first];
                ParseStepsAfterFirst(steps);
                return new PathExpression(Span(start), PathRoot.Relative, steps);
        }
    }

    // [22] RelativePathExpr
    private void ParseRelativeSteps(List<Expression> steps)
    {
        steps.Add(ParseStep());
        ParseStepsAfterFirst(steps);
    }

    // A "//" between steps is the step descendant-or-self::node() followed by the next step.
    private void ParseStepsAfterFirst(List<Expression> steps)
    {
        while (_current.Kind is XPathTokenKind.Slash or XPathTokenKind.SlashSlash)
        {
            var separator = Advance();
            if (separator.Kind == XPathTokenKind.SlashSlash)
            {
                steps.Add(new AxisStep(separator.Span, Axis.DescendantOrSelf, new AnyKindTest(separator.Span), []));
            }

            steps.Add(ParseStep());
        }
    }

    private static bool StartsStep(XPathTokenKind kind) =>
        kind
            is XPathTokenKind.IntegerLiteral
                or XPathTokenKind.DecimalLiteral
                or XPathTokenKind.DoubleLiteral
                or XPathTokenKind.StringLiteral
                or XPathTokenKind.Name
                or XPathTokenKind.QName
                or XPathTokenKind.BracedName
                or XPathTokenKind.Function
                or XPathTokenKind.Wildcard
                or XPathTokenKind.PrefixWildcard
                or XPathTokenKind.LocalWildcard
                or XPathTokenKind.Dollar
                or XPathTokenKind.LeftParen
                or XPathTokenKind.Dot
                or XPathTokenKind.DotDot
                or XPathTokenKind.At;

    // [23] StepExpr, [24] AxisStep: an abbreviated or explicit axis step, or a filter expression.
    private Expression ParseStep()
    {
        var start = _current.Span.Offset;
        switch (_current.Kind)
        {
            case XPathTokenKind.DotDot:
                var dots = Advance();
                var dotPredicates = ParsePredicates();
                return new AxisStep(Span(start), Axis.Parent, new AnyKindTest(dots.Span), dotPredicates);
            case XPathTokenKind.At:
                _ = Advance();
                return CreateAxisStep(start, Axis.Attribute, ParseNodeTest());
            case XPathTokenKind.Name or XPathTokenKind.QName or XPathTokenKind.BracedName:
                var next = PeekKind();
                if (next == XPathTokenKind.Hash)
                {
                    return ParseFilter(start, ParseNamedFunctionReference());
                }

                if (_current.Kind == XPathTokenKind.Name && next == XPathTokenKind.ColonColon)
                {
                    var axis = ParseAxisName();
                    return CreateAxisStep(start, axis, ParseNodeTest());
                }

                if (next == XPathTokenKind.LeftParen && !IsKindTestName(_current))
                {
                    return ParseFilter(start, ParseFunctionCall());
                }

                var test = ParseNodeTest();
                return CreateAxisStep(start, DefaultAxis(test), test);
            case XPathTokenKind.Wildcard or XPathTokenKind.PrefixWildcard or XPathTokenKind.LocalWildcard:
                return CreateAxisStep(start, Axis.Child, ParseNodeTest());
            default:
                return ParseFilter(start, ParsePrimary());
        }
    }

    private AxisStep CreateAxisStep(int start, Axis axis, NodeTest test)
    {
        var predicates = ParsePredicates();
        return new AxisStep(Span(start), axis, test, predicates);
    }

    // The default axis is child, except for an attribute test, which selects attributes.
    private static Axis DefaultAxis(NodeTest test) =>
        test is AttributeTest or SchemaAttributeTest ? Axis.Attribute : Axis.Child;

    // [25] ForwardAxis and [29] ReverseAxis: the axis name and "::".
    private Axis ParseAxisName()
    {
        var axis = TextOf(_current) switch
        {
            "child" => Axis.Child,
            "descendant" => Axis.Descendant,
            "attribute" => Axis.Attribute,
            "self" => Axis.Self,
            "descendant-or-self" => Axis.DescendantOrSelf,
            "following-sibling" => Axis.FollowingSibling,
            "following" => Axis.Following,
            "namespace" => Axis.Namespace,
            "parent" => Axis.Parent,
            "ancestor" => Axis.Ancestor,
            "preceding-sibling" => Axis.PrecedingSibling,
            "preceding" => Axis.Preceding,
            "ancestor-or-self" => Axis.AncestorOrSelf,
            _ => throw Unexpected(),
        };
        _ = Advance();
        Expect(XPathTokenKind.ColonColon);
        return axis;
    }

    // [31] NodeTest, [32] NameTest, [33] WildcardNode
    private NodeTest ParseNodeTest()
    {
        var token = _current;
        switch (token.Kind)
        {
            case XPathTokenKind.Wildcard:
                _ = Advance();
                return new NameTest(token.Span, NameTestKind.AnyName, null, null);
            case XPathTokenKind.PrefixWildcard:
                _ = Advance();
                return new NameTest(
                    token.Span,
                    NameTestKind.AnyLocalName,
                    _text.Slice(token.Span.Offset, token.Span.Length - 2).ToString(),
                    null
                );
            case XPathTokenKind.LocalWildcard:
                _ = Advance();
                return new NameTest(
                    token.Span,
                    NameTestKind.AnyNamespace,
                    null,
                    _text.Slice(token.Span.Offset + 2, token.Span.Length - 2).ToString()
                );
            case XPathTokenKind.Name or XPathTokenKind.QName or XPathTokenKind.BracedName:
                if (PeekKind() == XPathTokenKind.LeftParen && IsKindTestName(token))
                {
                    return ParseKindTest();
                }

                _ = Advance();
                var name = CreateName(token);
                return new NameTest(
                    token.Span,
                    NameTestKind.QualifiedName,
                    name.Prefix,
                    name.LocalName,
                    name.NamespaceUri
                );
            default:
                throw Unexpected();
        }
    }

    // [38] PredicateList, [39] Predicate
    private IReadOnlyList<Expression> ParsePredicates()
    {
        if (_current.Kind != XPathTokenKind.LeftBracket)
        {
            return Array.Empty<Expression>();
        }

        List<Expression> predicates = [];
        while (Accept(XPathTokenKind.LeftBracket))
        {
            predicates.Add(ParseExpr());
            Expect(XPathTokenKind.RightBracket);
        }

        return predicates;
    }

    // [36] FilterExpr; in 3.0 [PostfixExpr]: predicates and argument lists (dynamic function calls) in any order.
    private Expression ParseFilter(int start, Expression primary)
    {
        var expression = primary;
        while (true)
        {
            if (_current.Kind == XPathTokenKind.LeftBracket)
            {
                expression = new FilterExpression(Span(start), expression, ParsePredicates());
            }
            else if (_v30 && _current.Kind == XPathTokenKind.LeftParen)
            {
                expression = new DynamicFunctionCall(Span(start), expression, ParseArguments());
            }
            else
            {
                return expression;
            }
        }
    }

    // XPath 3.0 [NamedFunctionRef]: an EQName, "#", and the arity; a reserved function name is not allowed.
    private NamedFunctionReference ParseNamedFunctionReference()
    {
        var nameToken = _current;
        if (nameToken.Kind == XPathTokenKind.Name && IsReservedFunctionName(TextOf(nameToken)))
        {
            throw Fail(nameToken);
        }

        _ = Advance();
        var name = CreateName(nameToken);
        Expect(XPathTokenKind.Hash);
        var arity = _current;
        if (
            arity.Kind != XPathTokenKind.IntegerLiteral
            || !int.TryParse(
                TextOf(arity).ToString(),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value
            )
        )
        {
            throw Unexpected();
        }

        _ = Advance();
        return new NamedFunctionReference(Span(nameToken.Span.Offset), name, value);
    }

    // XPath 3.0 [InlineFunctionExpr]: function($a as type, ...) as type { expr }
    private InlineFunctionExpression ParseInlineFunction()
    {
        var start = Advance().Span.Offset;
        Expect(XPathTokenKind.LeftParen);
        List<FunctionParameter> parameters = [];
        if (_current.Kind != XPathTokenKind.RightParen)
        {
            do
            {
                var parameterStart = _current.Span.Offset;
                Expect(XPathTokenKind.Dollar);
                var name = ParseVariableName();
                var type = Accept(XPathTokenKind.As) ? ParseSequenceType() : null;
                parameters.Add(new FunctionParameter(Span(parameterStart), name, type));
            } while (Accept(XPathTokenKind.Comma));
        }

        Expect(XPathTokenKind.RightParen);
        var returnType = Accept(XPathTokenKind.As) ? ParseSequenceType() : null;
        Expect(XPathTokenKind.LeftBrace);
        var body = ParseExpr();
        Expect(XPathTokenKind.RightBrace);
        return new InlineFunctionExpression(Span(start), parameters, returnType, body);
    }

    // [48] ArgumentList; in 3.0 an argument may be the placeholder "?".
    private List<Expression> ParseArguments()
    {
        Expect(XPathTokenKind.LeftParen);
        List<Expression> arguments = [];
        if (_current.Kind != XPathTokenKind.RightParen)
        {
            do
            {
                arguments.Add(ParseArgument());
            } while (Accept(XPathTokenKind.Comma));
        }

        Expect(XPathTokenKind.RightParen);
        return arguments;
    }

    private Expression ParseArgument()
    {
        if (
            _v30
            && _current.Kind == XPathTokenKind.Question
            && PeekKind() is XPathTokenKind.Comma or XPathTokenKind.RightParen
        )
        {
            return new ArgumentPlaceholder(Advance().Span);
        }

        return ParseExprSingle();
    }

    // [40] PrimaryExpr
    private Expression ParsePrimary()
    {
        var token = _current;
        switch (token.Kind)
        {
            case XPathTokenKind.IntegerLiteral:
                _ = Advance();
                return new IntegerLiteral(token.Span, TextOf(token).ToString());
            case XPathTokenKind.DecimalLiteral:
                _ = Advance();
                return new DecimalLiteral(token.Span, TextOf(token).ToString());
            case XPathTokenKind.DoubleLiteral:
                _ = Advance();
                return new DoubleLiteral(token.Span, TextOf(token).ToString());
            case XPathTokenKind.StringLiteral:
                _ = Advance();
                return new StringLiteral(token.Span, UnquoteString(token));
            case XPathTokenKind.Dollar:
                _ = Advance();
                var variable = ParseVariableName();
                return new VariableReference(Span(token.Span.Offset), variable);
            case XPathTokenKind.Function:
                return ParseInlineFunction();
            case XPathTokenKind.Dot:
                _ = Advance();
                return new ContextItemExpression(token.Span);
            case XPathTokenKind.LeftParen:
                _ = Advance();
                if (Accept(XPathTokenKind.RightParen))
                {
                    return new ParenthesizedExpression(Span(token.Span.Offset), null);
                }

                var inner = ParseExpr();
                Expect(XPathTokenKind.RightParen);
                return new ParenthesizedExpression(Span(token.Span.Offset), inner);
            default:
                throw Unexpected();
        }
    }

    // [47] FunctionCall: a reserved name, written without a prefix, is not a function name (the reserved function
    // names constraint).
    private FunctionCall ParseFunctionCall()
    {
        var nameToken = _current;
        if (nameToken.Kind == XPathTokenKind.Name && IsReservedFunctionName(TextOf(nameToken)))
        {
            throw Fail(nameToken);
        }

        _ = Advance();
        var name = CreateName(nameToken);
        var arguments = ParseArguments();
        return new FunctionCall(Span(nameToken.Span.Offset), name, arguments);
    }

    // XPath 3.0 adds function, namespace-node, switch, and if to the reserved function names.
    private readonly bool IsReservedFunctionName(ReadOnlySpan<char> name) =>
        name is "typeswitch" or "item" or "empty-sequence"
        || (_v30 && name is "function" or "namespace-node" or "switch" or "if");

    // [44] VarName: the name after "$".
    private QualifiedName ParseVariableName()
    {
        var token = _current;
        if (token.Kind is not (XPathTokenKind.Name or XPathTokenKind.QName or XPathTokenKind.BracedName))
        {
            throw Unexpected();
        }

        _ = Advance();
        return CreateName(token);
    }

    private readonly QualifiedName CreateName(XPathToken token)
    {
        var text = TextOf(token);
        if (token.Kind == XPathTokenKind.BracedName)
        {
            // Q{uri}local: the lexer guarantees both braces and a local name.
            var close = text.IndexOf('}');
            return new QualifiedName(
                token.Span,
                null,
                text.Slice(close + 1).ToString(),
                text.Slice(2, close - 2).ToString()
            );
        }

        var colon = text.IndexOf(':');
        return colon < 0
            ? new QualifiedName(token.Span, null, text.ToString())
            : new QualifiedName(token.Span, text.Slice(0, colon).ToString(), text.Slice(colon + 1).ToString());
    }

    // The value of a string literal: the quotes are dropped and a doubled quote stands for one.
    private readonly string UnquoteString(XPathToken token)
    {
        var quote = _text[token.Span.Offset];
        var raw = _text.Slice(token.Span.Offset + 1, token.Span.Length - 2);
        if (raw.IndexOf(quote) < 0)
        {
            return raw.ToString();
        }

        var buffer = new char[raw.Length];
        var length = 0;
        for (var i = 0; i < raw.Length; i++)
        {
            buffer[length++] = raw[i];
            if (raw[i] == quote)
            {
                i++;
            }
        }

        return new string(buffer, 0, length);
    }
}
