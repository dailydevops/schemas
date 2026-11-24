namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.V30;

using System;
using System.Collections.Generic;
using System.Linq;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Lexer;
using NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Parser;
using static XPath30Dump;

public class XPath30ParserTests
{
    private const string Ex = "SequenceType<Occurrence=ExactlyOne>";

    public static IEnumerable<Func<string>> Version20Texts() =>
        ParserCases.Productions.Keys.Concat(ParserCases.Precedence.Keys).Select(key => (Func<string>)(() => key));

    [Test]
    [Arguments(
        "let $x := 1 return $x",
        "LetExpression(VariableBinding(x IntegerLiteral<Lexeme=1>) VariableReference(x))"
    )]
    [Arguments(
        "let $x := 1, $y := 2 return $y",
        "LetExpression(VariableBinding(x IntegerLiteral<Lexeme=1>) VariableBinding(y IntegerLiteral<Lexeme=2>) VariableReference(y))"
    )]
    [Arguments("$a ! $b", "SimpleMapExpression(VariableReference(a) VariableReference(b))")]
    [Arguments("$a || $b", "StringConcatExpression(VariableReference(a) VariableReference(b))")]
    [Arguments(
        "Q{urn:x}local",
        "AxisStep<Axis=Child>(NameTest<Kind=QualifiedName,LocalName=local,NamespaceUri=urn:x>)"
    )]
    [Arguments("$Q{urn:x}v", "VariableReference(Q{urn:x}v)")]
    [Arguments(
        "function($a as xs:integer, $b) as xs:integer { $a }",
        "InlineFunctionExpression(FunctionParameter(a "
            + Ex
            + "(AtomicType(xs:integer))) FunctionParameter(b) "
            + Ex
            + "(AtomicType(xs:integer)) VariableReference(a))"
    )]
    [Arguments("function() { 1 }", "InlineFunctionExpression(IntegerLiteral<Lexeme=1>)")]
    [Arguments("fn:count#1", "NamedFunctionReference<Arity=1>(fn:count)")]
    [Arguments("Q{urn:x}f#2", "NamedFunctionReference<Arity=2>(Q{urn:x}f)")]
    [Arguments(
        "$f(1)(2)",
        "DynamicFunctionCall(DynamicFunctionCall(VariableReference(f) IntegerLiteral<Lexeme=1>) IntegerLiteral<Lexeme=2>)"
    )]
    [Arguments("f#1(2)", "DynamicFunctionCall(NamedFunctionReference<Arity=1>(f) IntegerLiteral<Lexeme=2>)")]
    [Arguments(
        "$f[1](2)",
        "DynamicFunctionCall(FilterExpression(VariableReference(f) IntegerLiteral<Lexeme=1>) IntegerLiteral<Lexeme=2>)"
    )]
    [Arguments("f(?, 1)", "FunctionCall(f ArgumentPlaceholder IntegerLiteral<Lexeme=1>)")]
    [Arguments("$f(?)", "DynamicFunctionCall(VariableReference(f) ArgumentPlaceholder)")]
    [Arguments("$x instance of function(*)", $"InstanceOfExpression(VariableReference(x) {Ex}(AnyFunctionTest))")]
    [Arguments(
        "$x instance of function(xs:integer, item()*) as xs:string?",
        $"InstanceOfExpression(VariableReference(x) {Ex}(TypedFunctionTest({Ex}(AtomicType(xs:integer)) SequenceType<Occurrence=ZeroOrMore>(AnyItemType) SequenceType<Occurrence=ZeroOrOne>(AtomicType(xs:string)))))"
    )]
    [Arguments(
        "$x instance of (xs:integer)*",
        "InstanceOfExpression(VariableReference(x) SequenceType<Occurrence=ZeroOrMore>(ParenthesizedItemType(AtomicType(xs:integer))))"
    )]
    [Arguments(
        "$x instance of namespace-node()",
        $"InstanceOfExpression(VariableReference(x) {Ex}(KindItemType(NamespaceNodeTest)))"
    )]
    [Arguments("namespace-node()", "AxisStep<Axis=Child>(NamespaceNodeTest)")]
    public async Task Parse_Version30Construct_BuildsTheExpectedTree(string text, string expected) =>
        await Assert.That(Tree(text)).IsEqualTo(expected);

    [Test]
    [Arguments(
        "$a || $b = $c",
        "ComparisonExpression<Operator=GeneralEqual>(StringConcatExpression(VariableReference(a) VariableReference(b)) VariableReference(c))"
    )]
    [Arguments(
        "-$a ! $b",
        "UnaryExpression<Operator=Minus>(SimpleMapExpression(VariableReference(a) VariableReference(b)))"
    )]
    [Arguments(
        "$a ! $b ! $c",
        "SimpleMapExpression(SimpleMapExpression(VariableReference(a) VariableReference(b)) VariableReference(c))"
    )]
    [Arguments(
        "$a || $b || $c",
        "StringConcatExpression(StringConcatExpression(VariableReference(a) VariableReference(b)) VariableReference(c))"
    )]
    [Arguments(
        "$a + $b || $c",
        "StringConcatExpression(ArithmeticExpression<Operator=Add>(VariableReference(a) VariableReference(b)) VariableReference(c))"
    )]
    [Arguments(
        "$a to $b || $c",
        "StringConcatExpression(RangeExpression(VariableReference(a) VariableReference(b)) VariableReference(c))"
    )]
    [Arguments(
        "$a ! $b/$c",
        "SimpleMapExpression(VariableReference(a) PathExpression<Root=Relative>(VariableReference(b) VariableReference(c)))"
    )]
    [Arguments(
        "$a ! $b + 1",
        "ArithmeticExpression<Operator=Add>(SimpleMapExpression(VariableReference(a) VariableReference(b)) IntegerLiteral<Lexeme=1>)"
    )]
    [Arguments(
        "$a || $b and $c",
        "LogicalExpression<Operator=And>(StringConcatExpression(VariableReference(a) VariableReference(b)) VariableReference(c))"
    )]
    [Arguments(
        "$a ! $b || $c",
        "StringConcatExpression(SimpleMapExpression(VariableReference(a) VariableReference(b)) VariableReference(c))"
    )]
    public async Task Parse_Version30Operators_FollowTheGrammarPrecedence(string text, string expected) =>
        await Assert.That(Tree(text)).IsEqualTo(expected);

    [Test]
    [Arguments("let $x := 1 return $x", 4)]
    [Arguments("$a ! $b", 3)]
    [Arguments("$a || $b", 4)]
    [Arguments("Q{urn:x}local", 1)]
    [Arguments("function($a) { $a }", 13)]
    [Arguments("fn:count#1", 8)]
    [Arguments("f(?)", 2)]
    [Arguments("$f(1)", 2)]
    [Arguments("$x instance of function(*)", 23)]
    [Arguments("$x instance of namespace-node()", 29)]
    [Arguments("$x instance of (xs:integer)", 15)]
    public async Task Parse_Version30ConstructInVersion20_FailsWithXPST0003AtTheOffendingToken(string text, int offset)
    {
        var diagnostic = Diagnose(text, XPathVersion.V20);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
        await Assert.That(diagnostic.Offset).IsEqualTo(offset);
    }

    [Test]
    [Arguments("$a ! -$b")]
    [Arguments("Q{urn:x")]
    [Arguments("Q{urn:x}")]
    [Arguments("Q{a{b}c")]
    [Arguments("f#x")]
    [Arguments("f#99999999999")]
    [Arguments("if#1")]
    [Arguments("item#1")]
    [Arguments("function#1")]
    [Arguments("let $x := 1")]
    [Arguments("let $x in 1 return $x")]
    [Arguments("function($a { 1 }")]
    [Arguments("function() { }")]
    [Arguments("$x instance of function(xs:integer)")]
    [Arguments("$x instance of (xs:integer")]
    public async Task Parse_InvalidVersion30Syntax_FailsWithXPST0003(string text)
    {
        var diagnostic = Diagnose(text, XPathVersion.V30);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
    }

    [Test]
    [MethodDataSource(nameof(Version20Texts))]
    public async Task Parse_Version20Expression_ProducesTheSameTreeInVersion30(string text) =>
        await Assert.That(Tree(text, XPathVersion.V30)).IsEqualTo(Tree(text, XPathVersion.V20));

    [Test]
    public async Task Next_Version30Expression_AllocatesZeroBytes()
    {
        const string text =
            "let $x := 1 return $x ! (f#1(?) || Q{urn:x}y) instance of function(xs:integer*) as item()+ and function($a as xs:int) as xs:int { $a }(1)";
        var count = 0;

        AllocationAssert.NoAllocation(() =>
        {
            count = 0;
            var lexer = new XPathLexer(text.AsSpan(), XPathVersion.V30);
            while (lexer.Next().Kind is not (XPathTokenKind.EndOfInput or XPathTokenKind.Error))
            {
                count++;
            }
        });

        await Assert.That(count).IsGreaterThan(40);
    }
}
