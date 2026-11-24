namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.V31;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Lexer;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Syntax.V30;
using static NetEvolve.Schemas.XPath.Tests.Unit.Syntax.V30.XPath30Dump;

public class XPath31ParserTests
{
    private static readonly string[] Version30Methods =
    [
        nameof(XPath30ParserTests.Parse_Version30Construct_BuildsTheExpectedTree),
        nameof(XPath30ParserTests.Parse_Version30Operators_FollowTheGrammarPrecedence),
    ];

    private const string Ex = "SequenceType<Occurrence=ExactlyOne>";

    public static IEnumerable<Func<string>> Version20Texts() =>
        ParserCases.Productions.Keys.Concat(ParserCases.Precedence.Keys).Select(key => (Func<string>)(() => key));

    // The texts of the XPath 3.0 parser tests: the arguments of the construct and the operator cases.
    public static IEnumerable<Func<string>> Version30Texts() =>
        Version30Methods
            .SelectMany(name =>
                typeof(XPath30ParserTests)
                    .GetMethod(name)!
                    .GetCustomAttributes<ArgumentsAttribute>()
                    .Select(attribute => (string)attribute.Values![0]!)
            )
            .Select(text => (Func<string>)(() => text));

    private static readonly string[] Version31Only =
    [
        "map { }",
        "map { 'a' : 1 }",
        "map { 'a' : 1, 'b' : 2 }",
        "[ ]",
        "[ 1, 2 ]",
        "array { }",
        "array { 1, 2 }",
        "$m?a",
        "$m?1",
        "$m?(1)",
        "$m?*",
        "?a",
        "?*",
        "$x => f()",
        "$x => $f()",
        "$x => (f#1)()",
        "$x instance of map(*)",
        "$x instance of map(xs:string, item()*)",
        "$x instance of array(*)",
        "$x instance of array(xs:integer)",
    ];

    // One text per XPath 3.1 production, each of which is a syntax error before XPath 3.1.
    public static IEnumerable<Func<string>> Version31OnlyTexts() =>
        Version31Only.Select(text => (Func<string>)(() => text));

    [Test]
    [Arguments("map { }", "MapConstructor")]
    [Arguments("map{}", "MapConstructor")]
    [Arguments("map { 'a' : 1 }", "MapConstructor(MapEntry(StringLiteral<Value=a> IntegerLiteral<Lexeme=1>))")]
    [Arguments(
        "map { 'a' : 1, 'b' : $v }",
        "MapConstructor(MapEntry(StringLiteral<Value=a> IntegerLiteral<Lexeme=1>) MapEntry(StringLiteral<Value=b> VariableReference(v)))"
    )]
    [Arguments(
        "map { 'a' : map { 'b' : 2 } }",
        "MapConstructor(MapEntry(StringLiteral<Value=a> MapConstructor(MapEntry(StringLiteral<Value=b> IntegerLiteral<Lexeme=2>))))"
    )]
    [Arguments("map { $k : 1 }", "MapConstructor(MapEntry(VariableReference(k) IntegerLiteral<Lexeme=1>))")]
    [Arguments(
        "map { 1 + 2 : 3 }",
        "MapConstructor(MapEntry(ArithmeticExpression<Operator=Add>(IntegerLiteral<Lexeme=1> IntegerLiteral<Lexeme=2>) IntegerLiteral<Lexeme=3>))"
    )]
    [Arguments("[ ]", "SquareArrayConstructor")]
    [Arguments("[]", "SquareArrayConstructor")]
    [Arguments("[ 1, 2 ]", "SquareArrayConstructor(IntegerLiteral<Lexeme=1> IntegerLiteral<Lexeme=2>)")]
    [Arguments(
        "[ (1, 2), 3 ]",
        "SquareArrayConstructor(ParenthesizedExpression(SequenceExpression(IntegerLiteral<Lexeme=1> IntegerLiteral<Lexeme=2>)) IntegerLiteral<Lexeme=3>)"
    )]
    [Arguments(
        "[ [ 1 ], [ ] ]",
        "SquareArrayConstructor(SquareArrayConstructor(IntegerLiteral<Lexeme=1>) SquareArrayConstructor)"
    )]
    [Arguments("array { }", "CurlyArrayConstructor")]
    [Arguments("array{}", "CurlyArrayConstructor")]
    [Arguments(
        "array { 1, 2 }",
        "CurlyArrayConstructor(SequenceExpression(IntegerLiteral<Lexeme=1> IntegerLiteral<Lexeme=2>))"
    )]
    [Arguments("array { [ 1 ] }", "CurlyArrayConstructor(SquareArrayConstructor(IntegerLiteral<Lexeme=1>))")]
    [Arguments(
        "map { 'a' : [ 1, map { } ] }",
        "MapConstructor(MapEntry(StringLiteral<Value=a> SquareArrayConstructor(IntegerLiteral<Lexeme=1> MapConstructor)))"
    )]
    public async Task Parse_Constructor_BuildsTheExpectedTree(string text, string expected) =>
        await Assert.That(Tree(text, XPathVersion.V31)).IsEqualTo(expected);

    [Test]
    [Arguments("$m?a", "LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m))")]
    [Arguments("$m ? a", "LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m))")]
    [Arguments("$m?div", "LookupExpression<KeyKind=Name,KeyName=div>(VariableReference(m))")]
    [Arguments("$m?for", "LookupExpression<KeyKind=Name,KeyName=for>(VariableReference(m))")]
    [Arguments("$m?1", "LookupExpression<KeyKind=Integer>(VariableReference(m) IntegerLiteral<Lexeme=1>)")]
    [Arguments("$m?*", "LookupExpression<KeyKind=Wildcard>(VariableReference(m))")]
    [Arguments(
        "$m?(1, 2)",
        "LookupExpression<KeyKind=Expression>(VariableReference(m) ParenthesizedExpression(SequenceExpression(IntegerLiteral<Lexeme=1> IntegerLiteral<Lexeme=2>)))"
    )]
    [Arguments(
        "$m?a?b",
        "LookupExpression<KeyKind=Name,KeyName=b>(LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m)))"
    )]
    [Arguments(
        "$m?a?1?*",
        "LookupExpression<KeyKind=Wildcard>(LookupExpression<KeyKind=Integer>(LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m)) IntegerLiteral<Lexeme=1>))"
    )]
    [Arguments(
        "$m?a[1]",
        "FilterExpression(LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m)) IntegerLiteral<Lexeme=1>)"
    )]
    [Arguments(
        "$m?a(2)",
        "DynamicFunctionCall(LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m)) IntegerLiteral<Lexeme=2>)"
    )]
    [Arguments("f(1)?a", "LookupExpression<KeyKind=Name,KeyName=a>(FunctionCall(f IntegerLiteral<Lexeme=1>))")]
    [Arguments(
        "map { 'a' : 1 }?a",
        "LookupExpression<KeyKind=Name,KeyName=a>(MapConstructor(MapEntry(StringLiteral<Value=a> IntegerLiteral<Lexeme=1>)))"
    )]
    [Arguments(
        "[ 1, 2 ]?1",
        "LookupExpression<KeyKind=Integer>(SquareArrayConstructor(IntegerLiteral<Lexeme=1> IntegerLiteral<Lexeme=2>) IntegerLiteral<Lexeme=1>)"
    )]
    [Arguments("?a", "LookupExpression<KeyKind=Name,KeyName=a>")]
    [Arguments("?1", "LookupExpression<KeyKind=Integer>(IntegerLiteral<Lexeme=1>)")]
    [Arguments("?*", "LookupExpression<KeyKind=Wildcard>")]
    [Arguments("?(1)", "LookupExpression<KeyKind=Expression>(ParenthesizedExpression(IntegerLiteral<Lexeme=1>))")]
    [Arguments("$s ! ?a", "SimpleMapExpression(VariableReference(s) LookupExpression<KeyKind=Name,KeyName=a>)")]
    [Arguments(
        "$s ! ?a?b",
        "SimpleMapExpression(VariableReference(s) LookupExpression<KeyKind=Name,KeyName=b>(LookupExpression<KeyKind=Name,KeyName=a>))"
    )]
    [Arguments(
        "$m?a + 1",
        "ArithmeticExpression<Operator=Add>(LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m)) IntegerLiteral<Lexeme=1>)"
    )]
    [Arguments(
        "$m?a * 2",
        "ArithmeticExpression<Operator=Multiply>(LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m)) IntegerLiteral<Lexeme=2>)"
    )]
    public async Task Parse_Lookup_BuildsTheExpectedTree(string text, string expected) =>
        await Assert.That(Tree(text, XPathVersion.V31)).IsEqualTo(expected);

    [Test]
    [Arguments("$x => f()", "ArrowExpression(VariableReference(x) f)")]
    [Arguments(
        "$x => fn:f(1, 2)",
        "ArrowExpression(VariableReference(x) fn:f IntegerLiteral<Lexeme=1> IntegerLiteral<Lexeme=2>)"
    )]
    [Arguments("$x => Q{urn:x}f()", "ArrowExpression(VariableReference(x) Q{urn:x}f)")]
    [Arguments("$x => $f(1)", "ArrowExpression(VariableReference(x) VariableReference(f) IntegerLiteral<Lexeme=1>)")]
    [Arguments(
        "$x => (f#1)()",
        "ArrowExpression(VariableReference(x) ParenthesizedExpression(NamedFunctionReference<Arity=1>(f)))"
    )]
    [Arguments("$x => f(?, 1)", "ArrowExpression(VariableReference(x) f ArgumentPlaceholder IntegerLiteral<Lexeme=1>)")]
    [Arguments(
        "$x => f() => g(1)",
        "ArrowExpression(ArrowExpression(VariableReference(x) f) g IntegerLiteral<Lexeme=1>)"
    )]
    [Arguments("$x=>f()", "ArrowExpression(VariableReference(x) f)")]
    [Arguments("$m?a => f()", "ArrowExpression(LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m)) f)")]
    public async Task Parse_Arrow_BuildsTheExpectedTree(string text, string expected) =>
        await Assert.That(Tree(text, XPathVersion.V31)).IsEqualTo(expected);

    [Test]
    [Arguments("-$x => f()", "ArrowExpression(UnaryExpression<Operator=Minus>(VariableReference(x)) f)")]
    [Arguments(
        "$x => f() cast as xs:integer",
        "CastExpression(ArrowExpression(VariableReference(x) f) SingleType(xs:integer))"
    )]
    [Arguments(
        "- $x => f() instance of xs:integer",
        "InstanceOfExpression(ArrowExpression(UnaryExpression<Operator=Minus>(VariableReference(x)) f) "
            + Ex
            + "(AtomicType(xs:integer)))"
    )]
    [Arguments(
        "$a + $b => f()",
        "ArithmeticExpression<Operator=Add>(VariableReference(a) ArrowExpression(VariableReference(b) f))"
    )]
    [Arguments(
        "$a || $b => f()",
        "StringConcatExpression(VariableReference(a) ArrowExpression(VariableReference(b) f))"
    )]
    [Arguments("$a ! $b => f()", "ArrowExpression(SimpleMapExpression(VariableReference(a) VariableReference(b)) f)")]
    [Arguments(
        "$a => f() = $b",
        "ComparisonExpression<Operator=GeneralEqual>(ArrowExpression(VariableReference(a) f) VariableReference(b))"
    )]
    public async Task Parse_Arrow_FollowsTheGrammarPrecedence(string text, string expected) =>
        await Assert.That(Tree(text, XPathVersion.V31)).IsEqualTo(expected);

    [Test]
    [Arguments("$x instance of map(*)", "InstanceOfExpression(VariableReference(x) " + Ex + "(AnyMapTest))")]
    [Arguments(
        "$x instance of map(*)?",
        "InstanceOfExpression(VariableReference(x) SequenceType<Occurrence=ZeroOrOne>(AnyMapTest))"
    )]
    [Arguments(
        "$x instance of map(xs:string, item()*)",
        "InstanceOfExpression(VariableReference(x) "
            + Ex
            + "(TypedMapTest(AtomicType(xs:string) SequenceType<Occurrence=ZeroOrMore>(AnyItemType))))"
    )]
    [Arguments(
        "$x instance of map(xs:string, xs:integer?)+",
        "InstanceOfExpression(VariableReference(x) SequenceType<Occurrence=OneOrMore>(TypedMapTest(AtomicType(xs:string) SequenceType<Occurrence=ZeroOrOne>(AtomicType(xs:integer)))))"
    )]
    [Arguments(
        "$x instance of map(xs:string, map(*))",
        "InstanceOfExpression(VariableReference(x) "
            + Ex
            + "(TypedMapTest(AtomicType(xs:string) "
            + Ex
            + "(AnyMapTest))))"
    )]
    [Arguments("$x instance of array(*)", "InstanceOfExpression(VariableReference(x) " + Ex + "(AnyArrayTest))")]
    [Arguments(
        "$x instance of array(xs:integer)",
        "InstanceOfExpression(VariableReference(x) " + Ex + "(TypedArrayTest(" + Ex + "(AtomicType(xs:integer)))))"
    )]
    [Arguments(
        "$x instance of array(xs:integer*)*",
        "InstanceOfExpression(VariableReference(x) SequenceType<Occurrence=ZeroOrMore>(TypedArrayTest(SequenceType<Occurrence=ZeroOrMore>(AtomicType(xs:integer)))))"
    )]
    [Arguments(
        "$x instance of array(map(*))",
        "InstanceOfExpression(VariableReference(x) " + Ex + "(TypedArrayTest(" + Ex + "(AnyMapTest))))"
    )]
    [Arguments(
        "$x instance of function(map(*)) as array(*)",
        "InstanceOfExpression(VariableReference(x) "
            + Ex
            + "(TypedFunctionTest("
            + Ex
            + "(AnyMapTest) "
            + Ex
            + "(AnyArrayTest))))"
    )]
    [Arguments("$x treat as map(*)", "TreatExpression(VariableReference(x) " + Ex + "(AnyMapTest))")]
    [Arguments(
        "$x instance of map(*) and $y",
        "LogicalExpression<Operator=And>(InstanceOfExpression(VariableReference(x) "
            + Ex
            + "(AnyMapTest)) VariableReference(y))"
    )]
    public async Task Parse_MapAndArrayTypes_BuildTheExpectedTree(string text, string expected) =>
        await Assert.That(Tree(text, XPathVersion.V31)).IsEqualTo(expected);

    [Test]
    [Arguments(
        "$x instance of xs:integer? and $m?a",
        "LogicalExpression<Operator=And>(InstanceOfExpression(VariableReference(x) SequenceType<Occurrence=ZeroOrOne>(AtomicType(xs:integer))) LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m)))"
    )]
    [Arguments(
        "$x cast as xs:integer? and $m?a",
        "LogicalExpression<Operator=And>(CastExpression(VariableReference(x) SingleType<AllowsEmpty=True>(xs:integer)) LookupExpression<KeyKind=Name,KeyName=a>(VariableReference(m)))"
    )]
    [Arguments("f(?, ?a)", "FunctionCall(f ArgumentPlaceholder LookupExpression<KeyKind=Name,KeyName=a>)")]
    [Arguments("$f(?)", "DynamicFunctionCall(VariableReference(f) ArgumentPlaceholder)")]
    [Arguments("f(?)", "FunctionCall(f ArgumentPlaceholder)")]
    [Arguments("f(?1)", "FunctionCall(f LookupExpression<KeyKind=Integer>(IntegerLiteral<Lexeme=1>))")]
    [Arguments("$s[?a]", "FilterExpression(VariableReference(s) LookupExpression<KeyKind=Name,KeyName=a>)")]
    [Arguments("$s[1]", "FilterExpression(VariableReference(s) IntegerLiteral<Lexeme=1>)")]
    [Arguments("$s[[1]]", "FilterExpression(VariableReference(s) SquareArrayConstructor(IntegerLiteral<Lexeme=1>))")]
    [Arguments("[ $s[1] ]", "SquareArrayConstructor(FilterExpression(VariableReference(s) IntegerLiteral<Lexeme=1>))")]
    [Arguments(
        "$a ! [ 1 ]",
        "SimpleMapExpression(VariableReference(a) SquareArrayConstructor(IntegerLiteral<Lexeme=1>))"
    )]
    [Arguments(
        "(1, 2)[1]",
        "FilterExpression(ParenthesizedExpression(SequenceExpression(IntegerLiteral<Lexeme=1> IntegerLiteral<Lexeme=2>)) IntegerLiteral<Lexeme=1>)"
    )]
    [Arguments(
        "[ 1 ][1]",
        "FilterExpression(SquareArrayConstructor(IntegerLiteral<Lexeme=1>) IntegerLiteral<Lexeme=1>)"
    )]
    public async Task Parse_AmbiguousTokens_AreDisambiguatedByPosition(string text, string expected) =>
        await Assert.That(Tree(text, XPathVersion.V31)).IsEqualTo(expected);

    [Test]
    [Arguments("map")]
    [Arguments("array")]
    [Arguments("map/array")]
    [Arguments("$x/map")]
    [Arguments("a/map/b")]
    public async Task Parse_MapAndArrayAsNames_AreNameTestsInVersion31(string text) =>
        await Assert.That(Tree(text, XPathVersion.V31)).IsEqualTo(Tree(text, XPathVersion.V30));

    [Test]
    [MethodDataSource(nameof(Version31OnlyTexts))]
    public async Task Parse_Version31Construct_InVersion20_FailsWithXPST0003(string text)
    {
        var diagnostic = Diagnose(text, XPathVersion.V20);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
    }

    [Test]
    [MethodDataSource(nameof(Version31OnlyTexts))]
    public async Task Parse_Version31Construct_InVersion30_FailsWithXPST0003(string text)
    {
        var diagnostic = Diagnose(text, XPathVersion.V30);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
    }

    [Test]
    [MethodDataSource(nameof(Version31OnlyTexts))]
    public async Task Parse_Version31Construct_InVersion31_Succeeds(string text) =>
        await Assert.That(Tree(text, XPathVersion.V31)).DoesNotStartWith("ERROR");

    [Test]
    [Arguments("map { 'a' : 1", 13)]
    [Arguments("map { 'a' 1 }", 10)]
    [Arguments("map { 'a' : }", 12)]
    [Arguments("map { 'a' : 1, }", 15)]
    [Arguments("map { 'a' : 1 : 2 }", 14)]
    [Arguments("map { , }", 6)]
    [Arguments("map 1", 4)]
    [Arguments("[ 1", 3)]
    [Arguments("[ 1, ]", 5)]
    [Arguments("[ , 1 ]", 2)]
    [Arguments("array 1", 6)]
    [Arguments("array { 1", 9)]
    [Arguments("$m?", 3)]
    [Arguments("$m?()", 4)]
    [Arguments("$m?1.5", 3)]
    [Arguments("$m?-1", 3)]
    [Arguments("$m?$k", 3)]
    [Arguments("$m?(1", 5)]
    [Arguments("$m?a:b", 4)]
    [Arguments("a?b", 1)]
    [Arguments("$x =>", 5)]
    [Arguments("$x => f", 7)]
    [Arguments("$x => 1()", 6)]
    [Arguments("$x => f(", 8)]
    [Arguments("$x => item()", 6)]
    [Arguments("$x => map()", 6)]
    [Arguments("=> f()", 0)]
    [Arguments("$x instance of map(xs:string)", 29)]
    [Arguments("$x instance of map(item(), item())", 23)]
    [Arguments("$x instance of map(xs:string, item()", 37)]
    [Arguments("$x instance of map(*, item())", 24)]
    [Arguments("$x instance of array()", 21)]
    [Arguments("$x instance of array(xs:integer, xs:integer)", 31)]
    [Arguments("$x instance of array(*", 22)]
    [Arguments("map(1)", 0)]
    [Arguments("array(1)", 0)]
    public async Task Parse_InvalidVersion31Syntax_FailsWithXPST0003AtTheOffendingToken(string text, int offset)
    {
        var diagnostic = Diagnose(text, XPathVersion.V31);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
        await Assert.That(diagnostic.Offset).IsEqualTo(offset);
    }

    [Test]
    [MethodDataSource(nameof(Version20Texts))]
    public async Task Parse_Version20Expression_ProducesTheSameTreeInVersion31(string text) =>
        await Assert.That(Tree(text, XPathVersion.V31)).IsEqualTo(Tree(text, XPathVersion.V20));

    [Test]
    [MethodDataSource(nameof(Version30Texts))]
    public async Task Parse_Version30Expression_ProducesTheSameTreeInVersion31(string text) =>
        await Assert.That(Tree(text, XPathVersion.V31)).IsEqualTo(Tree(text, XPathVersion.V30));

    [Test]
    [Arguments("", "[", "1", "]", 10_000)]
    [Arguments("", "[", "1", "]", 101)]
    [Arguments("", "array { ", "1", " }", 10_000)]
    [Arguments("", "array { ", "1", " }", 101)]
    [Arguments("", "map { 'a' : ", "1", " }", 10_000)]
    [Arguments("", "map { 'a' : ", "1", " }", 101)]
    [Arguments("$m", "?(", "1", ")", 10_000)]
    [Arguments("$m", "?(", "1", ")", 101)]
    [Arguments("$x", " => f(", "1", ")", 10_000)]
    [Arguments("$x => (", "(", "f#1", ")", 10_000)]
    [Arguments("$x instance of ", "map(xs:string, ", "item()", ")", 10_000)]
    [Arguments("$x instance of ", "map(xs:string, ", "item()", ")", 101)]
    [Arguments("$x instance of ", "array(", "item()", ")", 10_000)]
    [Arguments("$x instance of ", "array(", "item()", ")", 101)]
    public async Task Parse_HostileNesting_InVersion31_ReturnsADiagnosticInsteadOfOverflowingTheStack(
        string prefix,
        string open,
        string core,
        string close,
        int depth
    )
    {
        var text =
            prefix
            + string.Concat(Enumerable.Repeat(open, depth))
            + core
            + string.Concat(Enumerable.Repeat(close, depth));

        var ok = XPathParser.TryParse(text.AsSpan(), XPathVersion.V31, out var expression, out var diagnostic);

        await Assert.That(ok).IsFalse();
        await Assert.That(expression).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
    }

    [Test]
    [Arguments("[", "1", "]")]
    [Arguments("array { ", "1", " }")]
    [Arguments("map { 'a' : ", "1", " }")]
    public async Task Parse_NestingBelowTheBound_InVersion31_Succeeds(string open, string core, string close)
    {
        var depth = XPathParser.MaxNestingDepth - 2;
        var text =
            string.Concat(Enumerable.Repeat(open, depth)) + core + string.Concat(Enumerable.Repeat(close, depth));

        await Assert.That(XPathParser.TryParse(text.AsSpan(), XPathVersion.V31, out var expression, out _)).IsTrue();
        await Assert.That(expression).IsNotNull();
    }

    [Test]
    public async Task Next_Version31Expression_AllocatesZeroBytes()
    {
        const string text =
            "map { 'a' : [ 1, 2 ], 'b' : array { 3 } }?a?(1)?* => f(?) instance of map(xs:string, array(item()*))? and ?x";
        var count = 0;

        AllocationAssert.NoAllocation(() =>
        {
            count = 0;
            var lexer = new XPathLexer(text.AsSpan(), XPathVersion.V31);
            while (lexer.Next().Kind is not (XPathTokenKind.EndOfInput or XPathTokenKind.Error))
            {
                count++;
            }
        });

        await Assert.That(count).IsGreaterThan(50);
    }
}
