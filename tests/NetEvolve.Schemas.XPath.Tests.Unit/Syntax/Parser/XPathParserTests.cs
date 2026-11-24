namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Parser;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using static N;

public class XPathParserTests
{
    public static IEnumerable<Func<string>> ProductionTexts() => Keys(ParserCases.Productions);

    public static IEnumerable<Func<string>> PrecedenceTexts() => Keys(ParserCases.Precedence);

    [Test]
    [MethodDataSource(nameof(ProductionTexts))]
    public async Task Parse_Production_BuildsTheExpectedTree(string text)
    {
        var actual = ParseOrFail(text);

        await Assert.That(TreeComparer.Diff(ParserCases.Productions[text], actual)).IsNull();
    }

    [Test]
    [MethodDataSource(nameof(PrecedenceTexts))]
    public async Task Parse_PrecedenceAndAssociativity_BuildsTheExpectedTree(string text)
    {
        var actual = ParseOrFail(text);

        await Assert.That(TreeComparer.Diff(ParserCases.Precedence[text], actual)).IsNull();
    }

    [Test]
    [Arguments("/", "Path")]
    [Arguments("/*", "Path")]
    [Arguments("(/)", "Paren")]
    [Arguments("/union", "Path")]
    [Arguments("string(1)", "Function")]
    [Arguments("my:element(1)", "Function")]
    [Arguments("fn:item()", "Function")]
    [Arguments("1 instance of xs:integer+", "InstanceOf")]
    [Arguments("1 instance of item()* , 2", "Seq")]
    public async Task Parse_ExtraGrammaticalConstraint_AcceptsTheValidForm(string text, string kind)
    {
        var actual = ParseOrFail(text);

        await Assert.That(actual.GetType().Name).StartsWith(kind);
    }

    [Test]
    [Arguments("/ * 5", 4, 1)]
    [Arguments("typeswitch(1)", 0, 10)]
    [Arguments("item()", 0, 4)]
    [Arguments("empty-sequence()", 0, 14)]
    [Arguments("element(1)", 8, 1)]
    [Arguments("node(a)", 5, 1)]
    [Arguments("1 instance of xs:integer + 1", 27, 1)]
    [Arguments("1 instance of xs:integer * 2", 27, 1)]
    [Arguments("if(1)", 5, 0)]
    public async Task Parse_ExtraGrammaticalConstraint_RejectsTheInvalidForm(string text, int offset, int length) =>
        await AssertRejected(text, offset, length);

    [Test]
    [Arguments("", 0, 0)]
    [Arguments("1 +", 3, 0)]
    [Arguments("(", 1, 0)]
    [Arguments(")", 0, 1)]
    [Arguments("1 2", 2, 1)]
    [Arguments("a b", 2, 1)]
    [Arguments("a = b = c", 6, 1)]
    [Arguments("a eq b ne c", 7, 2)]
    [Arguments("1 to 2 to 3", 7, 2)]
    [Arguments("for $x in 1", 11, 0)]
    [Arguments("for $x return 1", 7, 6)]
    [Arguments("for $x in 1 return", 18, 0)]
    [Arguments("some $x in 1", 12, 0)]
    [Arguments("if (1) then 2", 13, 0)]
    [Arguments("if (1) 2", 7, 1)]
    [Arguments("$", 1, 0)]
    [Arguments("$1", 1, 1)]
    [Arguments("a[", 2, 0)]
    [Arguments("a[]", 2, 1)]
    [Arguments("a[1", 3, 0)]
    [Arguments("[1]", 0, 1)]
    [Arguments("@", 1, 0)]
    [Arguments("child::", 7, 0)]
    [Arguments("bogus::a", 0, 5)]
    [Arguments("a::b", 0, 1)]
    [Arguments("a/", 2, 0)]
    [Arguments("a//", 3, 0)]
    [Arguments("...", 2, 1)]
    [Arguments("1 instance of", 13, 0)]
    [Arguments("1 cast as", 9, 0)]
    [Arguments("1 cast as xs:integer cast as xs:string", 21, 4)]
    [Arguments("a instance of b instance of c", 16, 8)]
    [Arguments("f(1,)", 4, 1)]
    [Arguments("f(,1)", 2, 1)]
    [Arguments("f(1", 3, 0)]
    [Arguments("xs:integer(", 11, 0)]
    [Arguments("(1,)", 3, 1)]
    [Arguments("document-node(a)", 14, 1)]
    [Arguments("document-node(element(a)", 24, 0)]
    [Arguments("schema-element(*)", 15, 1)]
    [Arguments("schema-attribute(a", 18, 0)]
    [Arguments("element(a,)", 10, 1)]
    [Arguments("element(a, t?", 13, 0)]
    [Arguments("attribute(a, t?)", 14, 1)]
    [Arguments("processing-instruction(1)", 23, 1)]
    [Arguments("processing-instruction(a", 24, 0)]
    [Arguments("a instance of 1", 14, 1)]
    [Arguments("a instance of item(1)", 19, 1)]
    [Arguments("a cast as 1", 10, 1)]
    [Arguments("'abc", 0, 4)]
    [Arguments("(: x", 0, 4)]
    [Arguments("1 ! 2", 2, 1)]
    [Arguments("1x", 0, 1)]
    [Arguments("#", 0, 1)]
    public async Task Parse_SyntaxError_ReportsXpst0003WithOffsetAndLength(string text, int offset, int length) =>
        await AssertRejected(text, offset, length);

    [Test]
    public async Task Parse_SliceOfALargerInput_ReportsOffsetsRelativeToTheSlice()
    {
        var ok = XPathParser.TryParse("xx1 + 2yy".AsSpan(2, 5), out var expression, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(expression!.Location.Offset).IsEqualTo(0);
        await Assert.That(expression.Location.Length).IsEqualTo(5);
    }

    [Test]
    [Arguments("parens", 10_000)]
    [Arguments("signs", 10_000)]
    [Arguments("predicates", 10_000)]
    [Arguments("arguments", 10_000)]
    [Arguments("conditionals", 10_000)]
    [Arguments("parens", 101)]
    public async Task Parse_HostileNesting_ReturnsADiagnosticInsteadOfOverflowingTheStack(string shape, int depth)
    {
        var text = Nested(shape, depth);

        var ok = XPathParser.TryParse(text.AsSpan(), out var expression, out var diagnostic);

        await Assert.That(ok).IsFalse();
        await Assert.That(expression).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Static);
        await Assert.That(diagnostic.Offset).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    [Arguments("parens")]
    [Arguments("signs")]
    [Arguments("predicates")]
    [Arguments("arguments")]
    [Arguments("conditionals")]
    public async Task Parse_NestingBelowTheBound_Succeeds(string shape)
    {
        var text = Nested(shape, XPathParser.MaxNestingDepth - 1);

        await Assert.That(XPathParser.TryParse(text.AsSpan(), out var expression, out _)).IsTrue();
        await Assert.That(expression).IsNotNull();
    }

    [Test]
    [Arguments("$x instance of ", "(", "xs:integer", ")", 10_000)]
    [Arguments("$x instance of ", "function(", "item()", ") as item()", 10_000)]
    [Arguments("$x instance of ", "function(", "item()", ") as item()", 101)]
    public async Task Parse_HostileTypeNesting_InVersion30_ReturnsADiagnosticInsteadOfOverflowingTheStack(
        string prefix,
        string open,
        string core,
        string close,
        int depth
    )
    {
        var text = prefix + string.Concat(Repeat(open, depth)) + core + string.Concat(Repeat(close, depth));

        var ok = XPathParser.TryParse(text.AsSpan(), XPathVersion.V30, out var expression, out var diagnostic);

        await Assert.That(ok).IsFalse();
        await Assert.That(expression).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
    }

    [Test]
    public async Task Parse_Locations_CoverTheirConstruct()
    {
        var sum = (ArithmeticExpression)ParseOrFail("a + 12");
        var path = (PathExpression)ParseOrFail("a//b");
        var call = (FunctionCall)ParseOrFail(" f(1) ");
        var binding = ((ForExpression)ParseOrFail("for $x in 1 return $x")).Bindings[0];
        var unary = (UnaryExpression)ParseOrFail("- 1");
        var conditional = (IfExpression)ParseOrFail("if (a) then b else c");
        var quantified = (QuantifiedExpression)ParseOrFail("some $x in 1 satisfies $x");

        await Assert.That(sum.Location).IsEqualTo(new SourceSpan(0, 6));
        await Assert.That(sum.Left.Location).IsEqualTo(new SourceSpan(0, 1));
        await Assert.That(sum.Right.Location).IsEqualTo(new SourceSpan(4, 2));
        await Assert.That(path.Location).IsEqualTo(new SourceSpan(0, 4));
        await Assert.That(path.Steps[1].Location).IsEqualTo(new SourceSpan(1, 2));
        await Assert.That(call.Location).IsEqualTo(new SourceSpan(1, 4));
        await Assert.That(call.Name.Location).IsEqualTo(new SourceSpan(1, 1));
        await Assert.That(binding.Location).IsEqualTo(new SourceSpan(4, 7));
        await Assert.That(unary.Location).IsEqualTo(new SourceSpan(0, 3));
        await Assert.That(conditional.Location).IsEqualTo(new SourceSpan(0, 20));
        await Assert.That(quantified.Location).IsEqualTo(new SourceSpan(0, 25));
    }

    [Test]
    public async Task Parse_ValidExpression_ReturnsNoDiagnostic()
    {
        var ok = XPathParser.TryParse("1".AsSpan(), out var expression, out var diagnostic);

        await Assert.That(ok).IsTrue();
        await Assert.That(expression).IsNotNull();
        await Assert.That(diagnostic).IsEqualTo(default(XPathDiagnostic));
    }

    private static async Task AssertRejected(string text, int offset, int length)
    {
        var ok = XPathParser.TryParse(text.AsSpan(), out var expression, out var diagnostic);

        await Assert.That(ok).IsFalse();
        await Assert.That(expression).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0003");
        await Assert.That(diagnostic.Offset).IsEqualTo(offset);
        await Assert.That(diagnostic.Length).IsEqualTo(length);
    }

    private static Expression ParseOrFail(string text) =>
        XPathParser.TryParse(text.AsSpan(), out var expression, out var diagnostic)
            ? expression!
            : throw new InvalidOperationException(
                $"'{text}' was rejected: {diagnostic.Message} at {diagnostic.Offset}"
            );

    private static string Nested(string shape, int depth) =>
        shape switch
        {
            "parens" => new string('(', depth) + "1" + new string(')', depth),
            "signs" => new string('-', depth) + "1",
            "predicates" => string.Concat(Repeat("a[", depth)) + "1" + new string(']', depth),
            "arguments" => string.Concat(Repeat("f(", depth)) + "1" + new string(')', depth),
            _ => string.Concat(Repeat("if (1) then ", depth)) + "1" + string.Concat(Repeat(" else 2", depth)),
        };

    private static string[] Repeat(string value, int count)
    {
        var parts = new string[count];
        Array.Fill(parts, value);
        return parts;
    }

    private static IEnumerable<Func<string>> Keys(IReadOnlyDictionary<string, Expression> table)
    {
        foreach (var key in table.Keys)
        {
            yield return () => key;
        }
    }
}
