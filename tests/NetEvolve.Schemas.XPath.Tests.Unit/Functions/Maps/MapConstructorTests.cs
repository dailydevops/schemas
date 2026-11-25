namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Maps;

using System.Threading;
using System.Threading.Tasks;
using static MapFixture;

/// <summary>The map constructor and the map as a function item of arity 1 (XPath 3.1, 3.11.1 and 3.11.1.1).</summary>
public class MapConstructorTests
{
    [Test]
    [Arguments("map{}", "kind:Function")]
    [Arguments("map { 1 : 'a' }", "kind:Function")]
    [Arguments("map:size(map{})", "integer:0")]
    [Arguments("map:size(map { 1 : 'a', 2 : 'b', 'x' : 'c' })", "integer:3")]
    [Arguments("map { 1 : 'a', 2 : 'b' }(1)", "string:a")]
    [Arguments("map { 1 : 'a', 2 : 'b' }(2)", "string:b")]
    [Arguments("map { 1 : 'a' }(3)", "()")]
    [Arguments("map { 1 : (1, 2, 3) }(1)", "integer:1, integer:2, integer:3")]
    [Arguments("map { 1 : () }(1)", "()")]
    [Arguments("map { 1 + 1 : 'two' }(2)", "string:two")]
    [Arguments("map { 'a' : map { 'b' : 3 } }('a')('b')", "integer:3")]
    [Arguments("let $m := map { 'k' : 'v' } return $m('k')", "string:v")]
    [Arguments("function-arity(map { 1 : 2 })", "integer:1")]
    [Arguments("empty(function-name(map { 1 : 2 }))", "boolean:True")]
    public async Task Constructing_ReturnsTheMap(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("map { 1 : 'a', 1 : 'b' }", "XQDY0137")]
    [Arguments("map { 1 : 'a', 1.0 : 'b' }", "XQDY0137")]
    [Arguments("map { 1 : 'a', 1e0 : 'b' }", "XQDY0137")]
    [Arguments("map { xs:double('NaN') : 'a', xs:float('NaN') : 'b' }", "XQDY0137")]
    [Arguments("map { 'a' : 1, xs:untypedAtomic('a') : 2 }", "XQDY0137")]
    [Arguments("map { (1, 2) : 3 }", "XPTY0004")]
    [Arguments("map { () : 3 }", "XPTY0004")]
    [Arguments("map { map {} : 3 }", "FOTY0013")]
    [Arguments("map { 1 : 'a' }((1, 2))", "XPTY0004")]
    [Arguments("map { 1 : 'a' }(())", "XPTY0004")]
    [Arguments("map { 1 : 'a' }(1, 2)", "XPTY0004")]
    [Arguments("map { 1 : 'a' }(map {})", "FOTY0013")]
    [Arguments("map { 1 : 'a' }()", "XPTY0004")]
    public async Task Constructing_WithAnInvalidKeyOrCall_RaisesTheError(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo(code);
    }

    [Test]
    [Arguments("map { 1 : 'a' } instance of map(*)", "boolean:True")]
    [Arguments("map {} instance of map(*)", "boolean:True")]
    [Arguments("map {} instance of function(*)", "boolean:True")]
    [Arguments("map { 1 : 'a' } instance of function(xs:anyAtomicType) as item()*", "boolean:True")]
    [Arguments("map { 1 : 'a' } instance of item()", "boolean:True")]
    [Arguments("(map {}, map {}) instance of map(*)+", "boolean:True")]
    [Arguments("() instance of map(*)?", "boolean:True")]
    [Arguments("(map {}, 1) instance of map(*)*", "boolean:False")]
    [Arguments("function($a) { $a } instance of map(*)", "boolean:False")]
    [Arguments("1 instance of map(*)", "boolean:False")]
    [Arguments("map { 1 : 'a' } instance of map(xs:integer, xs:string)", "boolean:True")]
    [Arguments("map { 1 : 'a' } instance of map(xs:decimal, xs:string)", "boolean:True")]
    [Arguments("map { 1 : 'a' } instance of map(xs:anyAtomicType, item()*)", "boolean:True")]
    [Arguments("map { 1 : 'a' } instance of map(xs:string, xs:string)", "boolean:False")]
    [Arguments("map { 1 : 'a' } instance of map(xs:integer, xs:integer)", "boolean:False")]
    [Arguments("map { 1 : 'a', 2 : 3 } instance of map(xs:integer, xs:string)", "boolean:False")]
    [Arguments("map { 1 : 'a', 'x' : 'b' } instance of map(xs:integer, xs:string)", "boolean:False")]
    [Arguments("map {} instance of map(xs:string, xs:string)", "boolean:True")]
    [Arguments("map { 1 : (1, 2) } instance of map(xs:integer, xs:integer*)", "boolean:True")]
    [Arguments("map { 1 : (1, 2) } instance of map(xs:integer, xs:integer)", "boolean:False")]
    [Arguments("map { 1 : () } instance of map(xs:integer, xs:integer?)", "boolean:True")]
    [Arguments("map { 1 : () } instance of map(xs:integer, xs:integer+)", "boolean:False")]
    [Arguments("map { 1 : map { 'a' : 2 } } instance of map(xs:integer, map(xs:string, xs:integer))", "boolean:True")]
    [Arguments(
        "map { 1 : map { 'a' : 'b' } } instance of map(xs:integer, map(xs:string, xs:integer))",
        "boolean:False"
    )]
    [Arguments("map { 1 : 2 } instance of map(xs:integer, function(*))", "boolean:False")]
    [Arguments("map { 1 : map {} } instance of map(xs:integer, function(*))", "boolean:True")]
    [Arguments("map { 1 : 2 } treat as map(*) instance of map(*)", "boolean:True")]
    public async Task InstanceOf_MatchesTheMapTests(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    public async Task TreatAs_AMapAgainstAnotherType_RaisesXPDY0050(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code("map { 1 : 'a' } treat as map(xs:string, item()*)")).IsEqualTo("XPDY0050");
    }

    [Test]
    public async Task AMapParameter_OfAnInlineFunction_AcceptsMapsOnly(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("(function($m as map(*)) { map:size($m) })(map { 1 : 2 })")).IsEqualTo("integer:1");
        await Assert.That(Code("(function($m as map(*)) { map:size($m) })(1)")).IsEqualTo("XPTY0004");
        await Assert
            .That(Run("(function($m as map(xs:integer, xs:string)) { $m(1) })(map { 1 : 'x' })"))
            .IsEqualTo("string:x");
        await Assert
            .That(Code("(function($m as map(xs:integer, xs:string)) { $m(1) })(map { 1 : 2 })"))
            .IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task TheMapFunctions_ExistFromXPath31Only(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code("map:size(1)", NetEvolve.Schemas.XPath.Api.XPathVersion.V30)).IsEqualTo("XPST0017");
        await Assert.That(Code("map:size(1)")).IsEqualTo("XPTY0004");
    }
}
