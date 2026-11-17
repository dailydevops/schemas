namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Lexer;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Syntax.Lexer;

public class XPathLexerTests
{
    [Test]
    [Arguments("div", "Name")]
    [Arguments("div div div", "Name Div Name")]
    [Arguments("a div b", "Name Div Name")]
    [Arguments("a idiv b mod c", "Name IDiv Name Mod Name")]
    [Arguments("and and and", "Name And Name")]
    [Arguments("or or or", "Name Or Name")]
    [Arguments("a/div/or", "Name Slash Name Slash Name")]
    [Arguments("@div", "At Name")]
    [Arguments("child::and", "Name ColonColon Name")]
    [Arguments("$for + $div", "Dollar Name Plus Dollar Name")]
    [Arguments("for", "Name")]
    [Arguments("for $x in a return $x", "For Dollar Name In Name Return Dollar Name")]
    [Arguments("for $in in in return in", "For Dollar Name In Name Return Name")]
    [Arguments("some $x in a satisfies $x", "Some Dollar Name In Name Satisfies Dollar Name")]
    [Arguments("every $x in a satisfies $x", "Every Dollar Name In Name Satisfies Dollar Name")]
    [Arguments("if", "Name")]
    [Arguments("if (a) then b else c", "If LeftParen Name RightParen Then Name Else Name")]
    [Arguments("then else", "Name Else")]
    [Arguments("a union b | c intersect d except e", "Name Union Name Pipe Name Intersect Name Except Name")]
    [Arguments("1 to 3", "IntegerLiteral To IntegerLiteral")]
    [Arguments("to to to", "Name To Name")]
    [Arguments("a eq b ne c lt d le e gt f ge g is h", "Name Eq Name Ne Name Lt Name Le Name Gt Name Ge Name Is Name")]
    [Arguments("eq", "Name")]
    [Arguments(
        "a = b != c < d <= e > f >= g << h >> i",
        "Name Equal Name NotEqual Name Less Name LessOrEqual Name Greater Name GreaterOrEqual Name Precedes Name Follows Name"
    )]
    [Arguments("a instance of b", "Name Instance Of Name")]
    [Arguments("a treat as b", "Name Treat As Name")]
    [Arguments("a castable as b", "Name Castable As Name")]
    [Arguments("a cast as b", "Name Cast As Name")]
    [Arguments("instance of as", "Name Name Name")]
    [Arguments("a instance of item()* and b", "Name Instance Of Name LeftParen RightParen Star And Name")]
    [Arguments("a instance of xs:integer+ or b", "Name Instance Of QName Plus Or Name")]
    [Arguments("a cast as xs:integer? and b", "Name Cast As QName Question And Name")]
    [Arguments(
        "a instance of element(x, y)? and b",
        "Name Instance Of Name LeftParen Name Comma Name RightParen Question And Name"
    )]
    [Arguments("(a instance of xs:integer*) and b", "LeftParen Name Instance Of QName Star RightParen And Name")]
    [Arguments("a instance of xs:integer and b * c", "Name Instance Of QName And Name Star Name")]
    [Arguments(
        "a[. instance of xs:integer] * if (1) then 2 else 3",
        "Name LeftBracket Dot Instance Of QName RightBracket Star If LeftParen IntegerLiteral RightParen Then IntegerLiteral Else IntegerLiteral"
    )]
    [Arguments(
        "a[. instance of xs:integer] * for $x in b return $x",
        "Name LeftBracket Dot Instance Of QName RightBracket Star For Dollar Name In Name Return Dollar Name"
    )]
    [Arguments(
        "a, b . . .. [ ] @ - + $ ::",
        "Name Comma Name Dot Dot DotDot LeftBracket RightBracket At Minus Plus Dollar ColonColon"
    )]
    public async Task Kinds_SameWordInDifferentPositions_AreNamesOrOperators(string expression, string expected) =>
        await Assert.That(LexerRunner.Kinds(expression)).IsEqualTo(expected);

    [Test]
    public async Task Names_AreNCNamesAndQNames_WithLongestMatch()
    {
        await Assert.That(LexerRunner.Kinds("a-b a.b a-1 a")).IsEqualTo("Name Name Name Name");
        await Assert.That(LexerRunner.Texts("a-b a.b a - b")).IsEqualTo("a-b|a.b|a|-|b");
        await Assert.That(LexerRunner.Kinds("p:l")).IsEqualTo("QName");
        await Assert.That(LexerRunner.Texts("p:l/x")).IsEqualTo("p:l|/|x");
        await Assert.That(LexerRunner.Kinds("p::l")).IsEqualTo("Name ColonColon Name");
        await Assert.That(LexerRunner.Texts("äö:\U00010000x")).IsEqualTo("äö:\U00010000x");
    }

    [Test]
    public async Task Comments_NestedAndBetweenAnyTwoTokens_AreSkipped()
    {
        const string text = "(: a (: b :) c :) 1(:x:)+(: (:y:) :)2 (::)";

        var tokens = LexerRunner.Lex(text);

        await Assert.That(LexerRunner.Kinds(text)).IsEqualTo("IntegerLiteral Plus IntegerLiteral");
        await Assert.That(tokens[0].Span.Offset).IsEqualTo(18);
        await Assert.That(tokens[0].Span.Length).IsEqualTo(1);
        await Assert.That(tokens[1].Span.Offset).IsEqualTo(24);
        await Assert.That(tokens[2].Span.Offset).IsEqualTo(36);
    }

    [Test]
    public async Task Comments_SplitTokens_BecauseTheyAreWhitespace()
    {
        await Assert.That(LexerRunner.Kinds("a(:c:)b")).IsEqualTo("Name Name");
        await Assert.That(LexerRunner.Kinds("a (: c :) div (: d :) b")).IsEqualTo("Name Div Name");
        await Assert.That(LexerRunner.Kinds("if(: c :)(a)")).IsEqualTo("If LeftParen Name RightParen");
        await Assert.That(LexerRunner.Kinds("for (: c :) $x")).IsEqualTo("For Dollar Name");
    }

    [Test]
    public async Task Comment_Unterminated_ReportsErrorAtTheOpeningPosition()
    {
        var tokens = LexerRunner.Lex("1 (: a (: b :)");

        await Assert.That(tokens.Count).IsEqualTo(2);
        await Assert.That(tokens[1].Kind).IsEqualTo(XPathTokenKind.Error);
        await Assert.That(tokens[1].Error).IsEqualTo(XPathLexicalError.UnterminatedComment);
        await Assert.That(tokens[1].Span.Offset).IsEqualTo(2);
        await Assert.That(tokens[1].Span.Length).IsEqualTo(12);
    }

    [Test]
    public async Task Comment_OpenedByParenColonParen_IsNotClosed() =>
        await Assert.That(LexerRunner.Lex("(:)")[0].Error).IsEqualTo(XPathLexicalError.UnterminatedComment);

    [Test]
    [Arguments("12", "IntegerLiteral", 0, 2)]
    [Arguments("  007 ", "IntegerLiteral", 2, 3)]
    [Arguments("1.5", "DecimalLiteral", 0, 3)]
    [Arguments(".5", "DecimalLiteral", 0, 2)]
    [Arguments("1.", "DecimalLiteral", 0, 2)]
    [Arguments("1e3", "DoubleLiteral", 0, 3)]
    [Arguments("1E+3", "DoubleLiteral", 0, 4)]
    [Arguments("1.5e-2", "DoubleLiteral", 0, 6)]
    [Arguments(".5E2", "DoubleLiteral", 0, 4)]
    [Arguments("1.e3", "DoubleLiteral", 0, 4)]
    [Arguments("\"abc\"", "StringLiteral", 0, 5)]
    [Arguments("'abc'", "StringLiteral", 0, 5)]
    [Arguments("\"\"", "StringLiteral", 0, 2)]
    [Arguments("\"a\"\"b\"", "StringLiteral", 0, 6)]
    [Arguments("'it''s'", "StringLiteral", 0, 7)]
    [Arguments("\"it's\"", "StringLiteral", 0, 6)]
    [Arguments("'a\"\"b'", "StringLiteral", 0, 6)]
    [Arguments("\"\"\"\"", "StringLiteral", 0, 4)]
    public async Task Literals_ReportKindOffsetAndLength(string text, string kind, int offset, int length)
    {
        var tokens = LexerRunner.Lex(text);

        await Assert.That(tokens.Count).IsEqualTo(1);
        await Assert.That(tokens[0].Kind.ToString()).IsEqualTo(kind);
        await Assert.That(tokens[0].Span.Offset).IsEqualTo(offset);
        await Assert.That(tokens[0].Span.Length).IsEqualTo(length);
    }

    [Test]
    public async Task Literals_NextToEachOther_AreSeparated()
    {
        await Assert.That(LexerRunner.Texts("1.5.3")).IsEqualTo("1.5|.3");
        await Assert.That(LexerRunner.Kinds("1..2")).IsEqualTo("DecimalLiteral DecimalLiteral");
        await Assert.That(LexerRunner.Kinds("1 .. 2")).IsEqualTo("IntegerLiteral DotDot IntegerLiteral");
        await Assert.That(LexerRunner.Kinds("a[1]")).IsEqualTo("Name LeftBracket IntegerLiteral RightBracket");
        await Assert.That(LexerRunner.Kinds("\"a\" \"b\"")).IsEqualTo("StringLiteral StringLiteral");
    }

    [Test]
    [Arguments("\"abc", 0, 4)]
    [Arguments("a 'it''s", 2, 6)]
    [Arguments("\"a\"\"", 0, 4)]
    public async Task StringLiteral_Unterminated_ReportsErrorAtTheOpeningQuote(string text, int offset, int length)
    {
        var tokens = LexerRunner.Lex(text);
        var token = tokens[tokens.Count - 1];

        await Assert.That(token.Kind).IsEqualTo(XPathTokenKind.Error);
        await Assert.That(token.Error).IsEqualTo(XPathLexicalError.UnterminatedString);
        await Assert.That(token.Span.Offset).IsEqualTo(offset);
        await Assert.That(token.Span.Length).IsEqualTo(length);
    }

    [Test]
    [Arguments("1div 2")]
    [Arguments("1e")]
    [Arguments("1.5x")]
    public async Task Number_FollowedByNameCharacter_IsAnError(string text)
    {
        var tokens = LexerRunner.Lex(text);

        await Assert.That(tokens[0].Kind).IsEqualTo(XPathTokenKind.Error);
        await Assert.That(tokens[0].Error).IsEqualTo(XPathLexicalError.InvalidNumber);
        await Assert.That(tokens[0].Span.Offset).IsEqualTo(0);
    }

    [Test]
    [Arguments("a # b", 2, 1)]
    [Arguments("a ! b", 2, 1)]
    [Arguments("a : b", 2, 1)]
    [Arguments("a:", 1, 1)]
    [Arguments("a }", 2, 1)]
    [Arguments("{", 0, 1)]
    [Arguments("a \U000F0000", 2, 2)]
    [Arguments("a \uD83D", 2, 1)]
    public async Task IllegalCharacter_ReportsErrorWithPosition(string text, int offset, int length)
    {
        var tokens = LexerRunner.Lex(text);
        var token = tokens[tokens.Count - 1];

        await Assert.That(token.Kind).IsEqualTo(XPathTokenKind.Error);
        await Assert.That(token.Error).IsEqualTo(XPathLexicalError.IllegalCharacter);
        await Assert.That(token.Span.Offset).IsEqualTo(offset);
        await Assert.That(token.Span.Length).IsEqualTo(length);
    }

    [Test]
    public async Task Error_IsFollowedByEndOfInput()
    {
        var lexer = new XPathLexer("a # b".AsSpan());
        var kinds = new[] { lexer.Next().Kind, lexer.Next().Kind, lexer.Next().Kind, lexer.Next().Kind };

        await Assert.That(kinds[0]).IsEqualTo(XPathTokenKind.Name);
        await Assert.That(kinds[1]).IsEqualTo(XPathTokenKind.Error);
        await Assert.That(kinds[2]).IsEqualTo(XPathTokenKind.EndOfInput);
        await Assert.That(kinds[3]).IsEqualTo(XPathTokenKind.EndOfInput);
    }

    [Test]
    public async Task EndOfInput_ReportsAnEmptySpanAtTheEnd()
    {
        var lexer = new XPathLexer("a (: c :) ".AsSpan());
        _ = lexer.Next();
        var end = lexer.Next();

        await Assert.That(end.Kind).IsEqualTo(XPathTokenKind.EndOfInput);
        await Assert.That(end.Span.Offset).IsEqualTo(10);
        await Assert.That(end.Span.Length).IsEqualTo(0);
    }

    [Test]
    [Arguments("*", "Wildcard", "*")]
    [Arguments("p:*", "PrefixWildcard", "p:*")]
    [Arguments("*:l", "LocalWildcard", "*:l")]
    [Arguments("@*", "At Wildcard", "@|*")]
    [Arguments("a/*", "Name Slash Wildcard", "a|/|*")]
    [Arguments("a/p:*/*:l", "Name Slash PrefixWildcard Slash LocalWildcard", "a|/|p:*|/|*:l")]
    [Arguments("a/@*:l", "Name Slash At LocalWildcard", "a|/|@|*:l")]
    [Arguments("child::*", "Name ColonColon Wildcard", "child|::|*")]
    [Arguments("2 * 3", "IntegerLiteral Star IntegerLiteral", "2|*|3")]
    [Arguments("* * *", "Wildcard Star Wildcard", "*|*|*")]
    [Arguments("a * b", "Name Star Name", "a|*|b")]
    [Arguments("(a) * *", "LeftParen Name RightParen Star Wildcard", "(|a|)|*|*")]
    [Arguments("* div *", "Wildcard Div Wildcard", "*|div|*")]
    [Arguments("*:l * p:*", "LocalWildcard Star PrefixWildcard", "*:l|*|p:*")]
    public async Task Wildcards_AreDistinguishedFromMultiplication(string text, string kinds, string texts)
    {
        await Assert.That(LexerRunner.Kinds(text)).IsEqualTo(kinds);
        await Assert.That(LexerRunner.Texts(text)).IsEqualTo(texts);
    }

    [Test]
    [Arguments("/", "Slash")]
    [Arguments("//a", "SlashSlash Name")]
    [Arguments("/*", "Slash Wildcard")]
    [Arguments("/ * 5", "Slash Wildcard IntegerLiteral")]
    [Arguments("(/) * 5", "LeftParen Slash RightParen Star IntegerLiteral")]
    [Arguments("/div", "Slash Name")]
    [Arguments("/ div 5", "Slash Name IntegerLiteral")]
    [Arguments("/union/a", "Slash Name Slash Name")]
    [Arguments("/ and /", "Slash Name Slash")]
    [Arguments("//*", "SlashSlash Wildcard")]
    [Arguments("a/b//c/..", "Name Slash Name SlashSlash Name Slash DotDot")]
    [Arguments("/ (: c :) *", "Slash Wildcard")]
    public async Task LeadingSlash_NextTokenIsAStepStart_NotAnOperator(string text, string kinds) =>
        await Assert.That(LexerRunner.Kinds(text)).IsEqualTo(kinds);

    [Test]
    public async Task Tokenize_FixedExpression_DoesNotAllocate()
    {
        // Names, literals, comments, wildcards, keywords, and operators; 200 characters.
        const string text =
            "for $item in /library/book[@price > 10.5e0 and starts-with(title, 'it''s')] return "
            + "(: pick :) if ($item/p:* div 2 instance of xs:integer?) then *:local * 3 else \"x\"\"y\" union a/b//c (: abcdefghijklm :)";

        AllocationAssert.NoAllocation(() =>
        {
            var lexer = new XPathLexer(text.AsSpan());
            var count = 0;
            while (lexer.Next().Kind != XPathTokenKind.EndOfInput)
            {
                count++;
            }

            if (count == 0)
            {
                throw new System.InvalidOperationException();
            }
        });

        await Assert.That(text.Length).IsEqualTo(200);
        await Assert.That(LexerRunner.Kinds(text)).DoesNotContain("Error");
    }
}
