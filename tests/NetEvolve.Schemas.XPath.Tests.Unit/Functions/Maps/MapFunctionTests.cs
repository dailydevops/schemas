namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Maps;

using System.Threading;
using System.Threading.Tasks;
using static MapFixture;

/// <summary>The functions of the map namespace (Functions and Operators 3.1, 17.1), except <c>map:merge</c>.</summary>
public class MapFunctionTests
{
    [Test]
    [Arguments("map:size(map { 1 : 'a', 2 : 'b' })", "integer:2")]
    [Arguments("sum(map:keys(map { 1 : 'a', 2 : 'b', 3 : 'c' }))", "integer:6")]
    [Arguments("map:keys(map {})", "()")]
    [Arguments("count(map:keys(map { 'a' : 1, 'b' : 2 }))", "integer:2")]
    [Arguments("map:contains(map { 1 : 'a' }, 1)", "boolean:True")]
    [Arguments("map:contains(map { 1 : 'a' }, 2)", "boolean:False")]
    [Arguments("map:contains(map { 1 : () }, 1)", "boolean:True")]
    [Arguments("map:contains(map {}, 'x')", "boolean:False")]
    [Arguments("map:get(map { 'a' : 1 }, 'a')", "integer:1")]
    [Arguments("map:get(map { 'a' : 1 }, 'b')", "()")]
    [Arguments("map:get(map { 'a' : (1, 2) }, 'a')", "integer:1, integer:2")]
    [Arguments("map:get(map { 'a' : () }, 'a')", "()")]
    [Arguments("map:size(map:entry('k', (1, 2, 3)))", "integer:1")]
    [Arguments("map:get(map:entry('k', (1, 2, 3)), 'k')", "integer:1, integer:2, integer:3")]
    [Arguments("map:get(map:entry('k', ()), 'k')", "()")]
    [Arguments("map:get(map:put(map { 1 : 'a' }, 2, 'b'), 2)", "string:b")]
    [Arguments("map:size(map:put(map { 1 : 'a' }, 2, 'b'))", "integer:2")]
    [Arguments("map:get(map:put(map { 1 : 'a' }, 1, 'z'), 1)", "string:z")]
    [Arguments("map:size(map:put(map { 1 : 'a' }, 1.0, 'z'))", "integer:1")]
    [Arguments("map:size(map:put(map {}, 'k', ()))", "integer:1")]
    [Arguments("map:size(map:remove(map { 1 : 'a', 2 : 'b' }, 1))", "integer:1")]
    [Arguments("map:contains(map:remove(map { 1 : 'a', 2 : 'b' }, 1), 1)", "boolean:False")]
    [Arguments("map:contains(map:remove(map { 1 : 'a', 2 : 'b' }, 1), 2)", "boolean:True")]
    [Arguments("map:size(map:remove(map { 1 : 'a', 2 : 'b' }, 3))", "integer:2")]
    [Arguments("map:size(map:remove(map { 1 : 'a', 2 : 'b', 3 : 'c' }, (1, 3, 5)))", "integer:1")]
    [Arguments("map:size(map:remove(map { 1 : 'a', 2 : 'b' }, ()))", "integer:2")]
    [Arguments("map:size(map:remove(map { 1 : 'a' }, 1.0))", "integer:0")]
    [Arguments("map:size(map:remove(map {}, 1))", "integer:0")]
    [Arguments("map:size(map:remove(map:remove(map { 1 : 'a' }, 1), 1))", "integer:0")]
    [Arguments("sum(map:for-each(map { 1 : 10, 2 : 20 }, function($k, $v) { $k + $v }))", "integer:33")]
    [Arguments("map:for-each(map {}, function($k, $v) { 1 })", "()")]
    [Arguments("count(map:for-each(map { 1 : (1, 2), 2 : () }, function($k, $v) { $v }))", "integer:2")]
    [Arguments(
        "sum(map:for-each(map { 1 : 10 }, function($k as xs:integer, $v as xs:integer) { $k + $v }))",
        "integer:11"
    )]
    public async Task Calling_ReturnsTheResult(string expression, string expected, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("map:size(1)", "XPTY0004")]
    [Arguments("map:size(())", "XPTY0004")]
    [Arguments("map:size((map {}, map {}))", "XPTY0004")]
    [Arguments("map:size(function($a) { $a })", "XPTY0004")]
    [Arguments("map:keys('x')", "XPTY0004")]
    [Arguments("map:contains(1, 1)", "XPTY0004")]
    [Arguments("map:contains(map {}, ())", "XPTY0004")]
    [Arguments("map:contains(map {}, (1, 2))", "XPTY0004")]
    [Arguments("map:contains(map {}, map {})", "FOTY0013")]
    [Arguments("map:get('x', 1)", "XPTY0004")]
    [Arguments("map:get(map {}, ())", "XPTY0004")]
    [Arguments("map:entry((), 1)", "XPTY0004")]
    [Arguments("map:entry((1, 2), 1)", "XPTY0004")]
    [Arguments("map:put(1, 1, 1)", "XPTY0004")]
    [Arguments("map:put(map {}, (), 1)", "XPTY0004")]
    [Arguments("map:remove(1, 1)", "XPTY0004")]
    [Arguments("map:remove(map {}, map {})", "FOTY0013")]
    [Arguments("map:for-each(1, function($k, $v) { 1 })", "XPTY0004")]
    [Arguments("map:for-each(map { 1 : 2 }, 3)", "XPTY0004")]
    [Arguments("map:for-each(map { 1 : 2 }, function($k) { 1 })", "XPTY0004")]
    [Arguments("map:for-each(map { 1 : 2 }, function($k, $v) { error() })", "FOER0000")]
    public async Task Calling_WithABadArgument_RaisesTheError(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo(code);
    }

    [Test]
    public async Task Put_AndRemove_LeaveTheSourceMapUnchanged(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string Source = "let $m := map { 1 : 'a', 2 : 'b' }, ";
        await Assert
            .That(Run(Source + "$p := map:put($m, 3, 'c') return (map:size($m), map:size($p))"))
            .IsEqualTo("integer:2, integer:3");
        await Assert
            .That(Run(Source + "$p := map:put($m, 1, 'z') return ($m(1), $p(1))"))
            .IsEqualTo("string:a, string:z");
        await Assert
            .That(
                Run(
                    Source + "$r := map:remove($m, 1) return (map:size($m), map:size($r), exists($m(1)), exists($r(1)))"
                )
            )
            .IsEqualTo("integer:2, integer:1, boolean:True, boolean:False");
    }

    [Test]
    public async Task ALargeMap_AnswersEveryKey(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string Big = "map:merge(for $i in 1 to 3000 return map:entry($i, $i * 2))";
        await Assert.That(Run($"map:size({Big})")).IsEqualTo("integer:3000");
        await Assert
            .That(Run($"let $m := {Big} return sum(for $i in 1 to 3000 return $m($i))"))
            .IsEqualTo("integer:9003000");
        await Assert.That(Run($"let $m := {Big} return sum(map:keys($m))")).IsEqualTo("integer:4501500");
        await Assert
            .That(Run($"let $m := {Big} return map:size(map:remove($m, (1 to 3000)[. mod 2 = 0]))"))
            .IsEqualTo("integer:1500");
        await Assert.That(Run($"let $m := {Big} return map:size(map:remove($m, 1 to 3000))")).IsEqualTo("integer:0");
        await Assert.That(Run($"let $m := {Big} return exists($m(0)) or exists($m(3001))")).IsEqualTo("boolean:False");
        await Assert.That(Run($"let $m := {Big} return map:size(map:put($m, 'x', 1))")).IsEqualTo("integer:3001");
    }
}
