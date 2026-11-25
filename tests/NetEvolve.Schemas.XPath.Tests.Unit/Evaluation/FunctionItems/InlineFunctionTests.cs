namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.FunctionItems;

using System.Threading;
using System.Threading.Tasks;
using static FunctionItemFixture;

/// <summary>Inline function expressions (XPath 3.0, 3.1.7): evaluation, closures that capture variables by value, nesting, and function conversion.</summary>
public class InlineFunctionTests
{
    [Test]
    [Arguments("(function($a) { $a + 1 })(2)", "integer:3")]
    [Arguments("(function($a, $b) { $a - $b })(10, 4)", "integer:6")]
    [Arguments("(function() { 'x' })()", "string:x")]
    [Arguments("function() { 1 }()", "integer:1")]
    [Arguments("(function($a) { $a })((1, 2, 3))", "integer:1, integer:2, integer:3")]
    [Arguments("(function($a) { $a })(())", "()")]
    public async Task Calling_ReturnsTheValueOfTheBody(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    public async Task TheValue_IsOneFunctionItem(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("function($a) { $a }")).IsEqualTo("kind:Function");
        await Assert.That(Run("count((function() { 1 }, function() { 2 }))")).IsEqualTo("integer:2");
    }

    [Test]
    public async Task AVariable_IsCapturedByValueAtCreationTime(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The functions outlive the iterations that created them: each one still sees its own value of $x.
        await Assert
            .That(Run("for $f in (for $x in (1, 2, 3) return function() { $x }) return $f()"))
            .IsEqualTo("integer:1, integer:2, integer:3");
    }

    [Test]
    public async Task AVariableOfTheCaller_DoesNotLeakIntoTheBody(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(Run("for $f in (for $x in 1 return function() { $x }) return (for $x in 9 return $f())"))
            .IsEqualTo("integer:1");
    }

    [Test]
    public async Task NestedClosures_CaptureEveryEnclosingScope(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(Run("for $x in (1, 2) return (function($a) { function($b) { $a + $b + $x } })(10)(100)"))
            .IsEqualTo("integer:111, integer:112");
        await Assert.That(Run("(function($a) { (function($b) { $a * $b })(3) })(4)")).IsEqualTo("integer:12");
    }

    [Test]
    public async Task AParameter_ShadowsAnOuterVariable_AndTheOuterValueIsRestored(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("for $x in 1 return ((function($x) { $x })(5), $x)")).IsEqualTo("integer:5, integer:1");
    }

    [Test]
    public async Task TheBody_HasNoFocus(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code("function() { . }()", withContextItem: true)).IsEqualTo("XPDY0002");
        await Assert.That(Run(". + (function() { 1 })()", withContextItem: true)).IsEqualTo("integer:6");
    }

    [Test]
    [Arguments("function($a as xs:integer) as xs:integer { $a }(3)", "integer:3")]
    [Arguments("function($a as xs:double) { $a }(1)", "double:1")]
    [Arguments("function($a as xs:string?) { $a }(())", "()")]
    [Arguments("function($a as xs:integer*) { count($a) }((1, 2, 3))", "integer:3")]
    [Arguments("function($a) as xs:double { 2 }(0)", "double:2")]
    public async Task DeclaredTypes_ConvertArgumentsAndResult(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("function($a as xs:integer) { $a }('x')")]
    [Arguments("function($a as xs:integer) { $a }((1, 2))")]
    [Arguments("function($a as xs:integer) { $a }(())")]
    [Arguments("function() as xs:integer { 'a' }()")]
    [Arguments("function() as xs:integer { () }()")]
    [Arguments("function($a, $b) { $a }(1)")]
    [Arguments("function($a) { $a }(1, 2)")]
    [Arguments("function() { 1 }(2)")]
    public async Task AWrongArityOrType_IsXPTY0004(string expression, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task AnUnknownVariable_InTheBody_IsXPST0008(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code("function() { $nope }")).IsEqualTo("XPST0008");
    }

    [Test]
    public async Task AnUnknownPrefix_InAParameterType_IsXPST0081(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code("function($a as q:t) { $a }")).IsEqualTo("XPST0081");
    }
}
