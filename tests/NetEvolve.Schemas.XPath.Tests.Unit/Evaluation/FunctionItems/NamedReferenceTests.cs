namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.FunctionItems;

using System.Threading;
using System.Threading.Tasks;
using static FunctionItemFixture;

/// <summary>Named function references (XPath 3.0, 3.1.6), <c>fn:function-name</c>, <c>fn:function-arity</c>, and <c>fn:function-lookup</c>.</summary>
public class NamedReferenceTests
{
    [Test]
    [Arguments("abs#1(-3)", "integer:3")]
    [Arguments("(abs#1)(-3)", "integer:3")]
    [Arguments("fn:abs#1(-3)", "integer:3")]
    [Arguments("concat#3('a', 'b', 'c')", "string:abc")]
    [Arguments("true#0()", "boolean:True")]
    [Arguments("xs:integer#1('5')", "integer:5")]
    [Arguments("count((abs#1, abs#1))", "integer:2")]
    public async Task Calling_InvokesTheFunction(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("nosuch#1")]
    [Arguments("abs#5")]
    [Arguments("abs#0")]
    [Arguments("fn:nosuch#2")]
    public async Task AnUnknownFunction_IsXPST0017(string expression, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task AnUnboundPrefix_IsXPST0081(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code("q:abs#1")).IsEqualTo("XPST0081");
    }

    [Test]
    [Arguments("abs#1('x')")]
    [Arguments("abs#1(1, 2)")]
    [Arguments("(abs#1)()")]
    public async Task AWrongArityOrArgumentType_IsXPTY0004(string expression, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task AContextDependentFunction_KeepsTheFocusOfTheReference(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("(10, 20, 30)[position#0() = 2]")).IsEqualTo("integer:20");
        await Assert.That(Run("(10, 20, 30)[last#0() = 3][1]")).IsEqualTo("integer:10");
        await Assert.That(Run("string#0()", withContextItem: true)).IsEqualTo("string:5");

        // The reference is evaluated with the focus of the predicate, and a call made through a variable keeps that focus.
        await Assert.That(Run("(10, 20, 30)[(for $f in position#0 return $f()) = 3]")).IsEqualTo("integer:30");
        await Assert.That(Code("position#0()")).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task FunctionName_IsTheNameOfANamedFunction_AndEmptyForAnInlineFunction(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("local-name-from-QName(function-name(abs#1))")).IsEqualTo("string:abs");
        await Assert.That(Run("namespace-uri-from-QName(function-name(abs#1))")).IsEqualTo("string:" + FnNamespace);
        await Assert.That(Run("function-name(function() { 1 })")).IsEqualTo("()");
    }

    [Test]
    [Arguments("function-arity(abs#1)", "integer:1")]
    [Arguments("function-arity(concat#4)", "integer:4")]
    [Arguments("function-arity(true#0)", "integer:0")]
    [Arguments("function-arity(function($a, $b) { 1 })", "integer:2")]
    public async Task FunctionArity_IsTheNumberOfParameters(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    public async Task FunctionLookup_FindsAFunction_OrReturnsTheEmptySequence(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var name = "QName('" + FnNamespace + "', 'abs')";
        await Assert.That(Run($"function-lookup({name}, 1)(-2)")).IsEqualTo("integer:2");
        await Assert.That(Run($"function-lookup({name}, 2)")).IsEqualTo("()");
        await Assert.That(Run("function-lookup(QName('urn:none', 'abs'), 1)")).IsEqualTo("()");
        await Assert.That(Run($"function-arity(function-lookup({name}, 1))")).IsEqualTo("integer:1");
        await Assert.That(Run("function-lookup#2 instance of function(*)")).IsEqualTo("boolean:True");
    }

    [Test]
    public async Task FunctionLookup_KeepsTheFocusOfTheCall(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var name = "QName('" + FnNamespace + "', 'string')";
        await Assert.That(Run($"function-lookup({name}, 0)()", withContextItem: true)).IsEqualTo("string:5");
    }
}
