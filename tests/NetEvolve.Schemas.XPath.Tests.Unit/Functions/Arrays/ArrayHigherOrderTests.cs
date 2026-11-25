namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Arrays;

using System.Threading;
using System.Threading.Tasks;
using static ArrayFixture;

/// <summary>The higher-order functions of the <c>array</c> namespace: for-each, filter, fold-left, fold-right, for-each-pair, and sort.</summary>
public class ArrayHigherOrderTests
{
    [Test]
    [Arguments("array:flatten(array:for-each([1, 2, 3], function($x) { $x * 2 }))", "integer:2, integer:4, integer:6")]
    [Arguments("array:flatten(array:for-each([1, -2], abs#1))", "integer:1, integer:2")]
    [Arguments("array:size(array:for-each([1, 2], function($x) { ($x, $x) }))", "integer:2")]
    [Arguments("array:size(array:for-each([1, 2], function($x) { () }))", "integer:2")]
    [Arguments("array:size(array:for-each([], function($x) { 1 }))", "integer:0")]
    [Arguments("array:flatten(array:filter([1, 2, 3, 4], function($x) { $x mod 2 = 0 }))", "integer:2, integer:4")]
    [Arguments("array:size(array:filter([1, 2], function($x) { false() }))", "integer:0")]
    [Arguments("array:size(array:filter([(1, 2), 3], function($x) { count($x) = 2 }))", "integer:1")]
    [Arguments("array:fold-left([1, 2, 3], 0, function($a, $b) { $a + $b })", "integer:6")]
    [Arguments("array:fold-left([], 'z', function($a, $b) { 'never' })", "string:z")]
    [Arguments("array:fold-left([1, 2, 3], '', function($a, $b) { concat('(', $a, $b, ')') })", "string:(((1)2)3)")]
    [Arguments("array:fold-right([1, 2, 3], '', function($a, $b) { concat('(', $a, $b, ')') })", "string:(1(2(3)))")]
    [Arguments("array:fold-right([], 'z', function($a, $b) { 'never' })", "string:z")]
    [Arguments(
        "array:flatten(array:for-each-pair([1, 2, 3], ['a', 'b'], function($x, $y) { concat($x, $y) }))",
        "string:1a, string:2b"
    )]
    [Arguments("array:size(array:for-each-pair([], [1], function($x, $y) { 1 }))", "integer:0")]
    public async Task TheCallbackFunctions_ApplyTheFunctionInOrder(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("array:flatten(array:sort([3, 1, 2]))", "integer:1, integer:2, integer:3")]
    [Arguments("array:flatten(array:sort(['b', 'a', 'c']))", "string:a, string:b, string:c")]
    [Arguments("array:flatten(array:sort([1, -3, 2], (), abs#1))", "integer:1, integer:2, integer:-3")]
    [Arguments("array:flatten(array:sort([1, 3, 2], (), function($x) { -$x }))", "integer:3, integer:2, integer:1")]
    [Arguments("array:flatten(array:sort(['bb', 'a', 'cc'], (), string-length#1))", "string:a, string:bb, string:cc")]
    [Arguments("array:flatten(array:sort([(2, 1), (1, 5)]))", "integer:1, integer:5, integer:2, integer:1")]
    [Arguments("array:flatten(array:sort([(1, 2), 1]))", "integer:1, integer:1, integer:2")]
    [Arguments("array:flatten(array:sort([1, 2.5, 2e0]))", "integer:1, double:2, decimal:2.5")]
    [Arguments("array:size(array:sort([]))", "integer:0")]
    [Arguments(
        "array:flatten(array:sort(['b', 'a'], 'http://www.w3.org/2005/xpath-functions/collation/codepoint'))",
        "string:a, string:b"
    )]
    [Arguments("let $a := [3, 1] let $b := array:sort($a) return array:flatten($a)", "integer:3, integer:1")]
    public async Task Sort_IsStableAndOrdersByTheKey(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("array:filter([1], function($x) { 1 })", "XPTY0004")]
    [Arguments("array:filter([1], function($x) { (true(), false()) })", "XPTY0004")]
    [Arguments("array:filter([1], function($x) { () })", "XPTY0004")]
    [Arguments("array:for-each([1], 1)", "XPTY0004")]
    [Arguments("array:for-each([1], function($x, $y) { 1 })", "XPTY0004")]
    [Arguments("array:fold-left([1], 0, function($a) { 1 })", "XPTY0004")]
    [Arguments("array:for-each-pair([1], [1], function($x) { 1 })", "XPTY0004")]
    [Arguments("array:sort([1, 'a'])", "XPTY0004")]
    [Arguments("array:sort([1], 'urn:unknown-collation')", "FOCH0002")]
    public async Task ABadCallbackOrKey_IsAnError(string expression, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo(code);
    }
}
