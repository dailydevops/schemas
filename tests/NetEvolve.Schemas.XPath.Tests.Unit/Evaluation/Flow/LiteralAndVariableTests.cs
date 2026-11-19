namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static FlowFixture;

/// <summary>Literals and variable references (XPath 2.0, 3.1.1 and 3.1.2), and the parenthesized expression.</summary>
public class LiteralAndVariableTests
{
    [Test]
    [Arguments("42", "integer:42")]
    [Arguments("0", "integer:0")]
    [Arguments("9223372036854775807", "integer:9223372036854775807")]
    [Arguments("123456789012345678901234567890", "integer:123456789012345678901234567890")]
    [Arguments("1.5", "decimal:1.5")]
    [Arguments(".5", "decimal:0.5")]
    [Arguments("1e2", "double:100")]
    [Arguments("2.5E-1", "double:0.25")]
    [Arguments("'ab'", "string:ab")]
    [Arguments("\"ab\"", "string:ab")]
    [Arguments("'it''s'", "string:it's")]
    [Arguments("''", "string:")]
    [Arguments("(42)", "integer:42")]
    [Arguments("((42))", "integer:42")]
    [Arguments("()", "()")]
    public async Task Literals_HaveTheirType(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    public async Task AnIntegerLiteral_IsAnInteger_NotADecimal() =>
        await Assert.That(Evaluate("42")[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);

    [Test]
    public async Task AnIntegerLiteralBeyondTheDigitLimit_IsAStaticError()
    {
        var compiled = TryCompile(new string('9', 5000), out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo("FOAR0002");
        await Assert.That(diagnostic.Offset).IsEqualTo(0);
        await Assert.That(diagnostic.Length).IsEqualTo(5000);
    }

    [Test]
    [Arguments("$host", "integer:100")]
    [Arguments("($host)", "integer:100")]
    [Arguments("$numbers", "integer:1, integer:2, integer:3")]
    [Arguments("$empty", "()")]
    [Arguments("$word", "string:a")]
    public async Task AVariableReference_GivesTheBoundValue(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    public async Task ADeclaredVariableThatIsNotBound_RaisesXpdy0002() =>
        await Assert.That(Code("$unbound")).IsEqualTo("XPDY0002");

    [Test]
    [Arguments("$undeclared", "XPST0008")]
    [Arguments("$p:host", "XPST0081")]
    public async Task AVariableThatIsNotDeclared_IsAStaticError(string expression, string code)
    {
        var compiled = TryCompile(expression, out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo(code);
    }

    [Test]
    public async Task ACompiledExpression_SeesTheValueBoundForEachEvaluation()
    {
        var compiled = Compile("$host");
        using var first = NewContext();
        using var second = NewContext();
        second.Variables.Set(Host, XdmItem.FromInteger(7));

        await Assert.That(ArithmeticFixture.Describe(compiled.Evaluate(first))).IsEqualTo("integer:100");
        await Assert.That(ArithmeticFixture.Describe(compiled.Evaluate(second))).IsEqualTo("integer:7");
    }
}
