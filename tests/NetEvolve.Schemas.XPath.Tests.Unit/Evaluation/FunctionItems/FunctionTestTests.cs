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

    [Test]
    [Arguments(
        "(function($n as element(a)) as element(a) { $n }) instance of function(element(a)) as node()",
        "boolean:True"
    )]
    [Arguments(
        "(function($n as element(a)) as element(a) { $n }) instance of function(element(a)) as element(*)",
        "boolean:True"
    )]
    [Arguments(
        "(function($n as element(a)) as element(a) { $n }) instance of function(element(a)) as element(b)",
        "boolean:False"
    )]
    [Arguments(
        "(function($n as element(a)) as element(a) { $n }) instance of function(node()) as node()",
        "boolean:False"
    )]
    [Arguments(
        "(function($n as element(*)) as node() { $n }) instance of function(element(a)) as node()",
        "boolean:True"
    )]
    [Arguments(
        "(function($n as element(a)) as node() { $n }) instance of function(attribute(a)) as node()",
        "boolean:False"
    )]
    [Arguments("(function($n as node()) as item()* { $n }) instance of function(text()) as item()*", "boolean:True")]
    [Arguments("(function($n as text()) as item()* { $n }) instance of function(text()) as item()*", "boolean:True")]
    [Arguments(
        "(function($n as text()) as item()* { $n }) instance of function(comment()) as item()*",
        "boolean:False"
    )]
    [Arguments(
        "(function($n as document-node(element(a))) as item()* { $n }) instance of function(document-node(element(a))) as item()*",
        "boolean:True"
    )]
    [Arguments(
        "(function($n as processing-instruction(p)) as item()* { $n }) instance of function(processing-instruction(q)) as item()*",
        "boolean:False"
    )]
    [Arguments(
        "(function($n as xs:integer) as item()* { $n }) instance of function(element(a)) as item()*",
        "boolean:False"
    )]
    [Arguments(
        "(function($n as element(a)) as item()* { $n }) instance of function(xs:integer) as item()*",
        "boolean:False"
    )]
    public async Task KindTestsInAFunctionTest_AreComparedByKindAndName(
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
        "(function($a as xs:integer+) as item()* { $a }) instance of function(xs:integer) as item()*",
        "boolean:True"
    )]
    [Arguments(
        "(function($a as xs:integer+) as item()* { $a }) instance of function(xs:integer*) as item()*",
        "boolean:False"
    )]
    [Arguments(
        "(function($a as xs:integer*) as item()* { $a }) instance of function(xs:integer+) as item()*",
        "boolean:True"
    )]
    [Arguments(
        "(function($a as empty-sequence()) as empty-sequence() { () }) instance of function(empty-sequence()) as empty-sequence()",
        "boolean:True"
    )]
    [Arguments(
        "(function($a as empty-sequence()) as empty-sequence() { () }) instance of function(empty-sequence()) as xs:integer?",
        "boolean:True"
    )]
    [Arguments(
        "(function($a as empty-sequence()) as empty-sequence() { () }) instance of function(empty-sequence()) as xs:integer",
        "boolean:False"
    )]
    [Arguments(
        "(function($a as xs:integer) as item()* { $a }) instance of function(empty-sequence()) as item()*",
        "boolean:False"
    )]
    [Arguments(
        "(function($a as item()*) as xs:integer { 1 }) instance of function(xs:integer) as xs:anyAtomicType",
        "boolean:True"
    )]
    public async Task TheOccurrence_AndTheEmptySequenceType_TakePartInSubtyping(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("concat#2 instance of function(xs:string, xs:string) as item()*", "boolean:True")]
    [Arguments("count#1 instance of function(item()*) as xs:integer", "boolean:False")]
    [Arguments("count#1 instance of function(item()*) as item()*", "boolean:True")]
    [Arguments("name#1 instance of function(node()?) as item()*", "boolean:True")]
    [Arguments("abs#1 instance of function(xs:anyAtomicType) as item()*", "boolean:False")]
    [Arguments("abs#1 instance of function(xs:decimal?) as item()*", "boolean:True")]
    [Arguments("abs#1 instance of function(function(*)) as item()*", "boolean:False")]
    public async Task TheParametersOfABuiltInFunction_AreItsDeclaredTypes(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }
}
