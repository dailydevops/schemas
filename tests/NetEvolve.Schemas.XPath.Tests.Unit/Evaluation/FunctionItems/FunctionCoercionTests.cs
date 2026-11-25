namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.FunctionItems;

using System.Threading;
using System.Threading.Tasks;
using static FunctionItemFixture;

/// <summary>
/// Function coercion (XPath 3.0, 3.1.5.2) of a function item that is passed to a parameter with a function test, the recursion bound of nested
/// calls, duplicate parameter names, parenthesized item types, and the lookup of functions.
/// </summary>
public class FunctionCoercionTests
{
    [Test]
    public async Task AFunctionItem_ThatIsNoInstanceOfTheTest_IsCoercedWhenTheArityAgrees(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // abs#1 is no instance of function(xs:string) as item()*, but it is coerced to it, so the function is accepted.
        await Assert.That(Run("(function($f as function(xs:string) as item()*) { 1 })(abs#1)")).IsEqualTo("integer:1");

        // The result of the wrapped function is converted to the result type of the test: the integer is promoted to a double.
        await Assert
            .That(Run("(function($f as function(xs:integer) as xs:double) { $f(2) })(function($a) { $a })"))
            .IsEqualTo("double:2");
    }

    [Test]
    public async Task TheCoercedFunction_ConvertsTheArgumentsAndTheResult(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(Code("(function($f as function(xs:integer) as xs:string) { $f(2) })(function($a) { $a })"))
            .IsEqualTo("XPTY0004");
        await Assert
            .That(
                Code("(function($f as function(xs:string) as item()*) { $f('x') })(function($a as xs:integer) { $a })")
            )
            .IsEqualTo("XPTY0004");
    }

    [Test]
    [Arguments("(function($f as function(xs:string) as item()*) { 1 })(function() { 1 })")]
    [Arguments("(function($f as function(xs:string) as item()*) { 1 })((abs#1, abs#1))")]
    [Arguments("(function($f as function(xs:string) as item()*) { 1 })(())")]
    [Arguments("(function($f as function(xs:string) as item()*) { 1 })(1)")]
    public async Task AFunctionItem_OfTheWrongArityOrCount_IsNotCoerced(
        string expression,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task AFunctionTypedParameter_AcceptsASequenceOfFunctionItems(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(Run("(function($f as (function(xs:string) as item()*)+) { count($f) })((abs#1, abs#1))"))
            .IsEqualTo("integer:2");
        await Assert
            .That(Run("(function($f as (function(xs:string) as item()*)?) { count($f) })(())"))
            .IsEqualTo("integer:0");
    }

    [Test]
    public async Task NestedCalls_AreBoundedByTheRecursionDepthLimit(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string Recursive = "for $g in function($f, $n) { if ($n = 0) then 0 else $f($f, $n - 1) } return $g($g, ";
        await Assert.That(Run(Recursive + "10)")).IsEqualTo("integer:0");
        await Assert.That(Code(Recursive + "1000000)")).IsEqualTo("XP0003");
    }

    [Test]
    public async Task ADuplicateParameterName_IsXQST0039(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code("function($a, $a) { 1 }")).IsEqualTo("XQST0039");
        await Assert.That(Run("function-arity(function($a, $b) { 1 })")).IsEqualTo("integer:2");
    }

    [Test]
    [Arguments("abs#1 instance of (function(*))", "boolean:True")]
    [Arguments("1 instance of (xs:integer)", "boolean:True")]
    [Arguments("1 instance of (function(*))", "boolean:False")]
    [Arguments("(function($a as (xs:integer)) { $a })(1)", "integer:1")]
    public async Task AParenthesizedItemType_IsTheItemTypeInside(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    public async Task FunctionLookup_ReachesHostFunctions_AndRejectsBadArguments(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var probe = new Probe();
        var name = "QName('" + FnNamespace + "', 'tick')";
        await Assert.That(Run($"function-lookup({name}, 0)()", probe)).IsEqualTo("integer:1");
        await Assert.That(Run($"function-lookup({name}, -1)")).IsEqualTo("()");
        await Assert
            .That(Run($"function-arity(function-lookup(QName('{FnNamespace}', 'function-lookup'), 2))"))
            .IsEqualTo("integer:2");
        await Assert.That(Code("function-lookup('abs', 1)")).IsEqualTo("XPTY0004");
        await Assert.That(Code("function-name(1)")).IsEqualTo("XPTY0004");
        await Assert.That(Code("function-arity('a')")).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task TheFocus_IsRestoredAfterACall(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("(10, 20, 30)[position() = (function() { 1 })() + 1]")).IsEqualTo("integer:20");
        await Assert.That(Run("(10, 20, 30)[position#0() = last#0() - 1]")).IsEqualTo("integer:20");
    }

    [Test]
    [Arguments("data((1, abs#1))")]
    [Arguments("data((abs#1, 1))")]
    public async Task Atomization_OfASequence_ChecksEveryItem(string expression, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("FOTY0013");
    }
}
