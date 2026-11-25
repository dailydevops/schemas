namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.FunctionItems;

using System.Threading;
using System.Threading.Tasks;
using static FunctionItemFixture;

/// <summary>Dynamic function calls (XPath 3.0, 3.2.2) and partial function application with the <c>?</c> placeholder (3.1.8).</summary>
public class DynamicCallAndPartialTests
{
    [Test]
    [Arguments("for $f in abs#1 return $f(-4)", "integer:4")]
    [Arguments("for $f in (function($a) { $a * 2 }) return $f(21)", "integer:42")]
    [Arguments("(function($f) { $f(2) })(function($a) { $a + 1 })", "integer:3")]
    public async Task ADynamicCall_InvokesTheFunctionItem(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("(1)(2)")]
    [Arguments("()(1)")]
    [Arguments("'f'(1)")]
    [Arguments("(abs#1, abs#1)(1)")]
    [Arguments("for $f in abs#1 return $f()")]
    [Arguments("for $f in abs#1 return $f(1, 2)")]
    [Arguments("for $f in abs#1 return $f('x')")]
    public async Task ANonFunctionOrWrongArityOrType_IsXPTY0004(string expression, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("XPTY0004");
    }

    [Test]
    [Arguments("concat('a', ?, 'c')('b')", "string:abc")]
    [Arguments("function-arity(concat('a', ?, 'c'))", "integer:1")]
    [Arguments("function-arity(concat(?, ?, ?))", "integer:3")]
    [Arguments("function-arity(substring(?, 2))", "integer:1")]
    [Arguments("substring(?, 2)('abcd')", "string:bcd")]
    [Arguments("concat(?, 'z')('y')", "string:yz")]
    [Arguments("function-arity(function($a, $b) { $a - $b }(?, 1))", "integer:1")]
    [Arguments("(function($a, $b) { $a - $b })(?, 1)(10)", "integer:9")]
    [Arguments("(function($a, $b) { $a - $b })(10, ?)(1)", "integer:9")]
    [Arguments("for $f in (function($a, $b, $c) { $a + $b + $c }) return $f(1, ?, ?)(2, 3)", "integer:6")]
    public async Task ThePartialApplication_HasTheReducedArity(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    public async Task TheFixedArguments_AreEvaluatedOnce(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var probe = new Probe();
        var result = Run("for $f in concat(tick(), ?) return ($f('a'), $f('b'), $f('c'))", probe);

        await Assert.That(result).IsEqualTo("string:1a, string:1b, string:1c");
        await Assert.That(probe.Ticks).IsEqualTo(1);

        var dynamic = new Probe();
        var viaItem = Run("(function($a, $b) { $a + $b })(tick(), ?)(10)", dynamic);

        await Assert.That(viaItem).IsEqualTo("integer:11");
        await Assert.That(dynamic.Ticks).IsEqualTo(1);
    }

    [Test]
    public async Task ThePartialApplication_IsAFunctionItemWithoutName(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(
                Run(
                    "(function($a as xs:string, $b as xs:string) as xs:string { $a })('x', ?) instance of function(xs:string) as xs:string"
                )
            )
            .IsEqualTo("boolean:True");
        await Assert.That(Run("function-name(concat('a', ?, 'c'))")).IsEqualTo("()");
    }

    [Test]
    [Arguments("abs(?, ?)")]
    [Arguments("nosuch(?)")]
    public async Task APartialApplication_OfAnUnknownFunction_IsXPST0017(
        string expression,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("XPST0017");
    }

    [Test]
    [Arguments("(abs#1)(?, ?)")]
    [Arguments("(function($a) { $a })(?, ?)")]
    [Arguments("(1)(?)")]
    public async Task APartialApplication_OfAWrongArityOrNonFunction_IsXPTY0004(
        string expression,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task ThePartialApplication_ConvertsTheArgumentsAtTheCall(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code("for $f in abs#1(?) return $f('x')")).IsEqualTo("XPTY0004");
        await Assert
            .That(Run("for $f in (function($a as xs:double, $b) { $a }(?, 1)) return $f(2)"))
            .IsEqualTo("double:2");
    }
}
