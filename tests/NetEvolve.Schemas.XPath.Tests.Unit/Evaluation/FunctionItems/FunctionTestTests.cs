namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.FunctionItems;

using System.Threading;
using System.Threading.Tasks;
using static FunctionItemFixture;

/// <summary>Function tests in sequence types (XPath 3.0, 2.5.5): <c>function(*)</c> and typed function tests with subtyping on parameter and result types.</summary>
public class FunctionTestTests
{
    private const string IntegerIdentity = "(function($a as xs:integer) as xs:integer { $a })";

    [Test]
    [Arguments("abs#1 instance of function(*)", "boolean:True")]
    [Arguments("function() { 1 } instance of function(*)", "boolean:True")]
    [Arguments("1 instance of function(*)", "boolean:False")]
    [Arguments("'a' instance of function(*)", "boolean:False")]
    [Arguments("abs#1 instance of item()", "boolean:True")]
    [Arguments("abs#1 instance of xs:anyAtomicType", "boolean:False")]
    [Arguments("abs#1 instance of node()", "boolean:False")]
    [Arguments("() instance of function(*)?", "boolean:True")]
    [Arguments("() instance of function(*)", "boolean:False")]
    [Arguments("(abs#1, abs#1) instance of function(*)+", "boolean:True")]
    [Arguments("(abs#1, 1) instance of function(*)*", "boolean:False")]
    [Arguments("(abs#1, abs#1) instance of function(*)", "boolean:False")]
    public async Task AnyFunctionTest_MatchesEveryFunctionItem(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(IntegerIdentity + " instance of function(xs:integer) as xs:integer", "boolean:True")]
    [Arguments(IntegerIdentity + " instance of function(xs:integer) as xs:decimal", "boolean:True")]
    [Arguments(IntegerIdentity + " instance of function(xs:integer) as item()*", "boolean:True")]
    [Arguments(IntegerIdentity + " instance of function(xs:integer) as xs:string", "boolean:False")]
    [Arguments(IntegerIdentity + " instance of function(xs:integer) as xs:integer?", "boolean:True")]
    [Arguments(IntegerIdentity + " instance of function(xs:integer, xs:integer) as xs:integer", "boolean:False")]
    [Arguments(IntegerIdentity + " instance of function() as xs:integer", "boolean:False")]
    public async Task TheResultType_IsCovariant_AndTheArityMustAgree(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(
        "(function($a as xs:decimal) as item()* { $a }) instance of function(xs:integer) as item()*",
        "boolean:True"
    )]
    [Arguments(
        "(function($a as xs:integer) as item()* { $a }) instance of function(xs:decimal) as item()*",
        "boolean:False"
    )]
    [Arguments(
        "(function($a as item()*) as item()* { $a }) instance of function(xs:string) as item()*",
        "boolean:True"
    )]
    [Arguments(
        "(function($a as xs:string) as item()* { $a }) instance of function(xs:string?) as item()*",
        "boolean:False"
    )]
    [Arguments(
        "(function($a as xs:string?) as item()* { $a }) instance of function(xs:string) as item()*",
        "boolean:True"
    )]
    public async Task TheParameterTypes_AreContravariant(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("function($a) { $a } instance of function(item()*) as item()*", "boolean:True")]
    [Arguments("function($a) { $a } instance of function(xs:integer) as xs:integer", "boolean:False")]
    [Arguments("function() as xs:integer { 1 } instance of function() as function(*)", "boolean:False")]
    [Arguments("function() as function(*) { abs#1 } instance of function() as function(*)", "boolean:True")]
    [Arguments(
        "function($f as function(xs:integer) as xs:integer) as item()* { $f } instance of function(function(xs:integer) as xs:integer) as item()*",
        "boolean:True"
    )]
    [Arguments(
        "function($f as function(*)) as item()* { $f } instance of function(function(xs:integer) as xs:integer) as item()*",
        "boolean:True"
    )]
    public async Task TheTypesOfAnInlineFunction_DefaultToItemStar_AndNestTheirTests(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("abs#1 instance of function(item()*) as item()*", "boolean:False")]
    [Arguments("abs#1 instance of function(xs:integer) as item()*", "boolean:True")]
    [Arguments("abs#1 instance of function(xs:string) as item()*", "boolean:False")]
    [Arguments("abs#1 instance of function(xs:integer, xs:integer) as item()*", "boolean:False")]
    public async Task ANamedFunction_MatchesByItsDeclaredParameterTypes(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    public async Task AFunctionTypedParameter_ConvertsByTheFunctionTest(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("(function($f as function(*)) { $f(-1) })(abs#1)")).IsEqualTo("integer:1");
        await Assert.That(Code("(function($f as function(*)) { $f(-1) })(1)")).IsEqualTo("XPTY0004");
        await Assert
            .That(Code("(function($f as function(xs:string) as xs:string) { $f('a') })(abs#1)"))
            .IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task TreatAs_UsesTheFunctionTest(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("(abs#1 treat as function(*))(-2)")).IsEqualTo("integer:2");
        await Assert.That(Code("1 treat as function(*)")).IsEqualTo("XPDY0050");
    }

    [Test]
    [Arguments("abs#1 instance of function(q:t) as item()*")]
    public async Task AnUnknownPrefix_InAFunctionTest_IsXPST0081(string expression, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("XPST0081");
    }
}
