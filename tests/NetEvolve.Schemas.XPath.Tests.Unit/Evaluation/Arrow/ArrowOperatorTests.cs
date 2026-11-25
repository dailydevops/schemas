namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arrow;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using static ArrowFixture;

/// <summary>The arrow operator (XPath 3.1, 3.16): <c>E =&gt; f(args)</c> is <c>f(E, args)</c>.</summary>
public class ArrowOperatorTests
{
    [Test]
    [Arguments("'abc' => upper-case()", "upper-case('abc')")]
    [Arguments("(1, 2, 3) => count()", "count((1, 2, 3))")]
    [Arguments("'abc' => substring(2)", "substring('abc', 2)")]
    [Arguments("'abc' => fn:upper-case()", "fn:upper-case('abc')")]
    public async Task AStaticTarget_LowersToTheSameCallAsTheFunctionForm(
        string arrow,
        string call,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var left = (CallNode)Compile(arrow).Root;
        var right = (CallNode)Compile(call).Root;

        await Assert.That(left.Function).IsSameReferenceAs(right.Function);
        await Assert.That(left.Arguments.Count).IsEqualTo(right.Arguments.Count);
        for (var i = 0; i < left.Arguments.Count; i++)
        {
            await Assert.That(left.Arguments[i].GetType()).IsEqualTo(right.Arguments[i].GetType());
        }

        await Assert.That(Run(arrow)).IsEqualTo(Run(call));
    }

    [Test]
    [Arguments("'abc' => upper-case() => substring(2) => string-length()", "integer:2")]
    [Arguments("'abc' => upper-case() => substring(2)", "string:BC")]
    [Arguments("(1, 2, 3) => reverse() => string-join(',')", "string:3,2,1")]
    public async Task AChainOfArrows_EvaluatesLeftToRight(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("let $f := upper-case#1 return 'a' => $f()", "string:A")]
    [Arguments("let $f := function($a, $b) { $a * $b } return 6 => $f(7)", "integer:42")]
    [Arguments("'a' => (upper-case#1)()", "string:A")]
    [Arguments("2 => (function($a, $b) { $a + $b })(3)", "integer:5")]
    [Arguments("let $f := upper-case#1 return 'a' => $f() => $f()", "string:A")]
    public async Task AVariableOrParenthesizedTarget_IsADynamicCall(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("(1, 2, 3) => count()", "integer:3")]
    [Arguments("(1, 2, 3) => sum()", "integer:6")]
    [Arguments("() => count()", "integer:0")]
    [Arguments("(1, 2, 3) => (function($s) { count($s) })()", "integer:3")]
    public async Task TheLeftOperand_IsPassedAsAWholeSequence(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("('a' => concat(?, 'c'))('b')", "string:abc")]
    [Arguments("function-arity('a' => concat(?, 'c'))", "integer:1")]
    [Arguments("function-arity('a' => concat(?, ?))", "integer:1")]
    [Arguments("(1 => (function($a, $b, $c) { $a + $b + $c })(?, 3))(2)", "integer:6")]
    public async Task APlaceholder_ProducesAPartialApplication(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("1 => no-such-function()", "XPST0017")]
    [Arguments("1 => abs(2)", "XPST0017")]
    [Arguments("'a' => concat()", "XPST0017")]
    [Arguments("1 => zz:abs()", "XPST0081")]
    public async Task AnUnknownFunctionOrArity_IsAStaticError(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Diagnose(expression).QName.LocalName).IsEqualTo(code);
    }

    [Test]
    public async Task AStaticError_KeepsTheSourcePositionOfTheArrowExpression(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostic = Diagnose("1 + (2 => abs(3))");

        await Assert.That(diagnostic.QName.LocalName).IsEqualTo("XPST0017");
        await Assert.That(diagnostic.Offset).IsEqualTo(5);
        await Assert.That(diagnostic.Length).IsEqualTo(10);
    }

    [Test]
    [Arguments("1 => (2)()")]
    [Arguments("1 => (upper-case#1)(2)")]
    public async Task ADynamicTargetThatIsNotAMatchingFunction_IsXPTY0004(
        string expression,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Diagnose(expression).QName.LocalName).IsEqualTo("XPTY0004");
    }

    [Test]
    [Arguments(XPathVersion.V20)]
    [Arguments(XPathVersion.V30)]
    public async Task BeforeXPath31_TheArrowIsASyntaxError(XPathVersion version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Diagnose("1 => abs()", version).QName.LocalName).IsEqualTo("XPST0003");
    }
}
